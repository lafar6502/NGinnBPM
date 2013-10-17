using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using NGinnBPM.Runtime.Tasks;
using NLog;

namespace NGinnBPM.Runtime.Services
{
    internal class SqlTaskPersisterSession : TaskPersisterSession
    {
        private SqlSession _ses;
        private static Logger log = LogManager.GetCurrentClassLogger();

        public SqlTaskPersisterSession(ITaskInstanceSerializer ser, SqlSession ses) : base(ser)
        {
            _ses = ses;
        }



        protected override TaskPersisterSession.TaskHolder LoadTaskRecord(string instanceId, bool forUpdate)
        {
            var th = new TaskHolder { State = RecordState.Unmodified };
            using (var cmd = _ses.Connection.CreateCommand())
            {
                cmd.CommandText = string.Format("select instance_id, task_data, task_type, version from TaskInstance {1} where instance_id='{0}'", instanceId, forUpdate ? "with(updlock)" : "");
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

    public class SqlTaskInstancePersister : ITaskInstancePersister
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
            return new SqlTaskPersisterSession(TaskSerializer, ss);
        }
    }
}
