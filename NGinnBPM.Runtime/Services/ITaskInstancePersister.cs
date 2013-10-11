using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NGinnBPM.Runtime.Services
{
    public interface ITaskInstancePersister
    {
        TaskPersisterSession OpenSession();
        TaskPersisterSession OpenSession(DbSession ses);
    }


    public abstract class TaskPersisterSession : IDisposable
    {
        protected ITaskInstanceSerializer _ser;
        protected enum RecordState
        {
            Unmodified,
            Modified,
            New
        };

        protected class TaskHolder
        {
            public string TaskData { get; set; }
            public string TaskTypeId { get; set; }
            public TaskInstance Deserialized { get; set; }
            public RecordState State { get; set; }  
        }

        private Dictionary<string, TaskHolder> _cache = new Dictionary<string, TaskHolder>();

        public TaskPersisterSession(ITaskInstanceSerializer ser)
        {
            _ser = ser;
        }

        public virtual TaskInstance GetForRead(string instanceId)
        {
            TaskHolder th;
            if (!_cache.TryGetValue(instanceId, out th))
            {

            }
            return th.Deserialized;
        }

        public virtual TaskInstance GetForUpdate(string instanceId)
        {
            TaskHolder th;
            if (!_cache.TryGetValue(instanceId, out th))
            {

            }
            return th.Deserialized;
        }


        public virtual void SaveNew(TaskInstance ti)
        {
            if (string.IsNullOrEmpty(ti.InstanceId)) throw new Exception("Missing instance ID");
            if (_cache.ContainsKey(ti.InstanceId)) throw new Exception("Task already exists");
            string typeId;
            var th = new TaskHolder
            {
                State = RecordState.New,
                Deserialized = ti,
                TaskData = _ser.Serialize(ti, out typeId)
            };
            th.TaskTypeId = typeId;
            _cache[ti.InstanceId] = th;
        }
        
        public virtual void Update(TaskInstance ti)
        {
            TaskHolder th;
            if (!_cache.TryGetValue(ti.InstanceId, out th))
            {
                throw new Exception("Task not cached");
            }
            if (th.State == RecordState.Unmodified)
            {
                th.State = RecordState.Modified;
            }
            string typeId;
            th.TaskData = _ser.Serialize(ti, out typeId);
            th.TaskTypeId = typeId;
        }

        public virtual IEnumerable<TaskInstance> GetNewTasks()
        {
            return _cache.Where(kv => kv.Value.State == RecordState.New).Select(kv => kv.Value.Deserialized);
        }
        
        public virtual IEnumerable<TaskInstance> GetModifiedTasks()
        {
            return _cache.Where(kv => kv.Value.State == RecordState.Modified).Select(kv => kv.Value.Deserialized);
        }
        
        public virtual void SaveChanges()
        {
            throw new NotImplementedException();
        }

        [ThreadStatic]
        private static TaskPersisterSession _ses;

        /// <summary>
        /// Current thread's ambient session (if you have initialized it...)
        /// </summary>
        public static TaskPersisterSession Current
        {
            get { return _ses; }
            set { _ses = value; }
        }

        public void Dispose()
        {
            
        }

        
    }
}
