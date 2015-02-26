using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using Newtonsoft.Json;
using NGinnBPM.Runtime.Tasks;
using NLog;

namespace NGinnBPM.Runtime.Services
{
    internal class SqlProcessPersisterSession : TaskPersisterSession
    {
        private SqlSession _ses;
        private static Logger log = LogManager.GetCurrentClassLogger();

        protected class InstanceHolder : TaskHolder
        {
            
        }

        public SqlProcessPersisterSession(ITaskInstanceSerializer ser, SqlSession ses)
            : base(ser)
        {
            _ses = ses;
        }

        protected static string GetProcessInstanceId(string taskInstanceId)
        {
            return InstanceId.GetProcessInstanceId(taskInstanceId);
        }

        protected class ProcessHolder
        {
            public string ProcessInstance { get; set; }
            public string DbVersion { get; set; }
            public RecordState State { get; set; }
            public string SerializedData { get; set; }
            public bool IsForUpdate { get; set; }
            public List<TaskInstance> TaskInstances { get; set; }
        }

        private Dictionary<string, ProcessHolder> _cache = new Dictionary<string, ProcessHolder>();

        protected class PH
        {
            public string TypeId { get; set; }
            public string Data { get; set; }
        }

        protected string SerializeTaskList(IEnumerable<TaskInstance> list)
        {
            List<PH> l = new List<PH>();
            foreach (TaskInstance ti in list)
            {
                if (ti.Status != TaskStatus.Cancelling &&
                    ti.Status != TaskStatus.Enabled &&
                    ti.Status != TaskStatus.Enabling &&
                    ti.Status != TaskStatus.Selected &&
                    !(ti is CompositeTaskInstance))
                {
                    continue; //not serialized
                }
                string ttype;
                string s = _ser.Serialize(ti, out ttype);
                l.Add(new PH
                {
                    TypeId = ttype,
                    Data = s
                });
            }
            return JsonConvert.SerializeObject(l);
        }

        protected List<TaskInstance> DeserializeTaskList(string s)
        {
            var l = JsonConvert.DeserializeObject<List<PH>>(s);
            return new List<TaskInstance>(l.Select(x => _ser.Deserialize(x.Data, x.TypeId)));
        }

        protected ProcessHolder GetProcessRecord(string instanceId, bool forUpdate)
        {
            ProcessHolder ph;
            if (_cache.TryGetValue(instanceId, out ph))
            {
                if (forUpdate && !ph.IsForUpdate)
                {
                    ph = null;
                }
            }
            if (ph == null)
            {
                ph = LoadProcessRecord(instanceId, forUpdate);
                _cache[instanceId] = ph;
            }
            return ph;
        }

        public override TaskInstance GetForRead(string instanceId)
        {
            string iid = GetProcessInstanceId(instanceId);
            var ph = GetProcessRecord(iid, false);
            if (ph == null) return null;
            var ti = ph.TaskInstances.Find(x => x.InstanceId == instanceId);
            return ti;
        }

        public override TaskInstance GetForUpdate(string instanceId)
        {
            string iid = GetProcessInstanceId(instanceId);
            var ph = GetProcessRecord(iid, true);
            if (ph == null) return null;
            var ti = ph.TaskInstances.Find(x => x.InstanceId == instanceId);
            return ti;

        }

        

        

        public override TaskInstance GetSessionLocalInstance(string instanceId)
        {
            return base.GetSessionLocalInstance(instanceId);
        }

        public override void SaveChanges()
        {
            var toSave = _cache.Values.Where(x => x.State == RecordState.Modified || x.State == RecordState.New);
            StoreProcessRecords(toSave);
            _cache.Clear();
        }

        public override void SaveNew(TaskInstance ti)
        {
            var ph = GetProcessRecord(ti.ProcessInstanceId, true);
            if (ph == null)
            {
                //new instance
                ph = new ProcessHolder
                {
                    ProcessInstance = ti.ProcessInstanceId,
                    State = RecordState.New,
                    IsForUpdate = true,
                    DbVersion = null,
                    TaskInstances = new List<TaskInstance>()
                };
                ph.TaskInstances.Add(ti);
                _cache[ph.ProcessInstance] = ph;
            }
            else
            {
                var i2 = ph.TaskInstances.FindIndex(x => x.InstanceId == ti.InstanceId);
                if (i2 >= 0) throw new Exception("Task already exists: " + ti.InstanceId);
                ph.TaskInstances.Add(ti);
                if (ph.State == RecordState.Unmodified) ph.State = RecordState.Modified;
            }
        }

        public override void Update(TaskInstance ti)
        {
            var ph = GetProcessRecord(ti.ProcessInstanceId, true);
            if (ph == null) throw new Exception("Process instance not found: " + ti.ProcessInstanceId);
            var i2 = ph.TaskInstances.FindIndex(x => x.InstanceId == ti.InstanceId);
            if (i2 < 0) throw new Exception("Task not found: " + ti.InstanceId);
            ph.TaskInstances[i2] = ti;

        }

        protected ProcessHolder LoadProcessRecord(string instanceId, bool forUpdate)
        {
            var ph = new ProcessHolder { State = RecordState.Unmodified, IsForUpdate = forUpdate };
            using (var cmd = _ses.Connection.CreateCommand())
            {
                cmd.CommandText = string.Format("select instance_id, definition_id, version, task_instance_data from ProcessInstance {1} where instance_id='{0}'", instanceId, forUpdate ? "with(updlock,rowlock)" : "with(rowlock)");
                using (var dr = cmd.ExecuteReader())
                {
                    if (!dr.Read()) return null;
                    ph.SerializedData = Convert.ToString(dr["task_instance_data"]);
                    ph.ProcessInstance = Convert.ToString(dr["instance_id"]);
                    ph.DbVersion = Convert.ToString(dr["version"]);
                }
            }
            ph.TaskInstances = DeserializeTaskList(ph.SerializedData);
            return ph;
        }

        protected void StoreProcessRecords(IEnumerable<ProcessHolder> records)
        {
            if (records.Count() == 0) return;
            var sb = new StringBuilder();
            using (var cmd = _ses.Connection.CreateCommand())
            {
                int pcnt = 0;
                foreach (var ph in records)
                {
                    foreach (var ti in ph.TaskInstances.Where(x => x is CompositeTaskInstance))
                    {
                        var s = ti.ToString();
                        log.Info("Saving: {0}", s);
                    }
                    ph.SerializedData = SerializeTaskList(ph.TaskInstances);
                    if (string.IsNullOrEmpty(ph.SerializedData)) throw new Exception("Task not serialized!");
                    int ov = string.IsNullOrEmpty(ph.DbVersion) ? 1 : Int32.Parse(ph.DbVersion);
                    if (ph.State== RecordState.New)
                    {
                        sb.AppendFormat("insert into ProcessInstance (instance_id, definition_id, task_instance_data, version) values(@instid{0}, @defid{0}, @taskdata{0}, @version{0});\n", pcnt);
                    }
                    else if (ph.State == RecordState.Modified)
                    {
                        sb.AppendFormat("update TaskInstance set task_instance_data=@taskdata{0}, version=@version{0} where instance_id=@instid{0} and version=@oldversion{0};\n", pcnt);
                    }
                    SqlUtil.AddParameter(cmd, "@instid" + pcnt, ph.ProcessInstance);
                    SqlUtil.AddParameter(cmd, "@taskdata" + pcnt, ph.SerializedData);
                    SqlUtil.AddParameter(cmd, "@version" + pcnt, (ov + 1).ToString());
                    SqlUtil.AddParameter(cmd, "@defid" + pcnt, ph.TaskInstances[0].ProcessDefinitionId);
                    if (ph.State == RecordState.Modified)
                    {
                        SqlUtil.AddParameter(cmd, "@oldversion" + pcnt, ph.DbVersion);
                    }
                    pcnt++;
                }
                cmd.CommandText = sb.ToString();
                int rowz = cmd.ExecuteNonQuery();
                log.Info("Updated rows: {0}. Input rows: {1}. Query: {2}", rowz, records.Count(), sb.ToString());
            }
        }

        protected override TaskPersisterSession.TaskHolder LoadTaskRecord(string instanceId, bool forUpdate)
        {
            throw new NotImplementedException();
            var th = new TaskHolder { State = RecordState.Unmodified };
            using (var cmd = _ses.Connection.CreateCommand())
            {
                cmd.CommandText = string.Format("select instance_id, task_data, task_type, version from TaskInstance {1} where instance_id='{0}'", instanceId, forUpdate ? "with(updlock,rowlock)" : "with(rowlock)");
                using (var dr = cmd.ExecuteReader())
                {
                    if (!dr.Read()) return null;
                    th.TaskData = Convert.ToString(dr["task_data"]);
                    th.TaskTypeId = Convert.ToString(dr["task_type"]);
                    th.DbVersion = Convert.ToString(dr["version"]);
                    th.Deserialized = _ser.Deserialize(th.TaskData, th.TaskTypeId);
                }
            }
            return th;
        }

        protected override void WriteRecords(IEnumerable<TaskPersisterSession.TaskHolder> records)
        {
            
            var sb = new StringBuilder();
            using (var cmd = _ses.Connection.CreateCommand())
            {
                int pcnt = 0;
                foreach (var th in records)
                {
                    if (string.IsNullOrEmpty(th.TaskData)) throw new Exception("Task not serialized!");
                    int ov = string.IsNullOrEmpty(th.DbVersion) ? 1 : Int32.Parse(th.DbVersion);
                    if (th.State == RecordState.New)
                    {
                        sb.AppendFormat("insert into TaskInstance (instance_id, task_data, task_type, version) values(@instid{0}, @taskdata{0}, @tasktype{0}, @version{0});\n", pcnt);
                    }
                    else if (th.State == RecordState.Modified)
                    {
                        sb.AppendFormat("update TaskInstance set task_data=@taskdata{0}, version=@version{0},task_type=@tasktype{0} where instance_id=@instid{0} and version=@oldversion{0};\n", pcnt);
                    }
                    SqlUtil.AddParameter(cmd, "@instid" + pcnt, th.Deserialized.InstanceId);
                    SqlUtil.AddParameter(cmd, "@taskdata" + pcnt, th.TaskData);
                    SqlUtil.AddParameter(cmd, "@version" + pcnt, (ov + 1).ToString());
                    SqlUtil.AddParameter(cmd, "@tasktype" + pcnt, th.TaskTypeId);
                    if (th.State == RecordState.Modified)
                    {
                        SqlUtil.AddParameter(cmd, "@oldversion" + pcnt, th.DbVersion);
                    }
                    pcnt++;
                }
                log.Info("Query: {0}", sb.ToString());
                cmd.CommandText = sb.ToString();
                cmd.ExecuteNonQuery();
            }
        }
    }

    public class SqlProcessPersister : ITaskInstancePersister
    {
        public ITaskInstanceSerializer TaskSerializer { get; set; }

        public TaskPersisterSession OpenSession()
        {
            if (DbSession.Current == null) throw new Exception("DBSession.Current");
            return OpenSession(DbSession.Current);
        }

        public TaskPersisterSession OpenSession(DbSession ses)
        {
            SqlSession ss = ses as SqlSession;
            if (ss == null) throw new Exception("SqlSession required");
            return new SqlProcessPersisterSession(TaskSerializer, ss);
        }
    }
}
