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

        public override TaskInstance GetForRead(string instanceId)
        {
            return base.GetForRead(instanceId);
        }

        public override TaskInstance GetForUpdate(string instanceId)
        {
            return base.GetForUpdate(instanceId);
        }

        protected bool ReadTaskData(string instanceId, bool lockUpdate, out string taskData, out string taskTypeId, out string version)
        {
            taskData = null; taskTypeId = null; version = null;
            using (var cmd = _ses.Connection.CreateCommand())
            {
                cmd.CommandText = string.Format("select instance_id, task_data, task_type, version from TaskInstance {1} where instance_id='{0}'", instanceId, lockUpdate ? "with(updlock)" : "");
                using (var dr = cmd.ExecuteReader())
                {
                    if (!dr.Read()) return false;
                    taskData = Convert.ToString(dr["task_data"]);
                    taskTypeId = Convert.ToString(dr["task_type"]);
                    version = Convert.ToString(dr["version"]);
                    return true;
                }
            }
        }

        protected void InsertTaskData(string instanceId, string taskData, string taskTypeId)
        {
            throw new NotImplementedException();
        }

        protected void UpdateTaskData(string instanceId, string version, string taskData, string taskTypeId)
        {
            throw new NotImplementedException();
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
