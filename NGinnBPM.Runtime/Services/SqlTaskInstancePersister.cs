using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using NGinnBPM.Runtime.Tasks;

namespace NGinnBPM.Runtime.Services
{
    internal class SqlTaskPersisterSession : TaskPersisterSession
    {
        private SqlSession _ses;

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
            throw new NotImplementedException();
            using (var cmd = _ses.Connection.CreateCommand())
            {
                int pcnt = 0;
                foreach (var th in records)
                {
                    if (th.State == RecordState.New)
                    {
                    }
                    else if (th.State == RecordState.Modified)
                    {
                    }
                }
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
