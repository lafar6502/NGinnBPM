using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NGinnBPM.Runtime.Tasks;
using NGinnBPM.Runtime.ExecutionEngine;

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
            public string DbVersion { get; set; }
        }

        private Dictionary<string, TaskHolder> _cache = new Dictionary<string, TaskHolder>();

        public TaskPersisterSession(ITaskInstanceSerializer ser)
        {
            _ser = ser;
            PersistenceMode = TaskPersistenceMode.PersistAliveTasksOnly;
        }

        public TaskPersistenceMode PersistenceMode { get; set; }
        

        public virtual TaskInstance GetForRead(string instanceId)
        {
            TaskHolder th;
            if (!_cache.TryGetValue(instanceId, out th))
            {
                th = LoadTaskRecord(instanceId, false);
                th.State = RecordState.Unmodified;
                _cache[instanceId] = th;
            }
            return th.Deserialized;
        }

        public virtual TaskInstance GetForUpdate(string instanceId)
        {
            TaskHolder th;
            if (!_cache.TryGetValue(instanceId, out th))
            {
                th = LoadTaskRecord(instanceId, true);
                th.State = RecordState.Unmodified;
                _cache[instanceId] = th;
            }
            return th.Deserialized;
        }

        /// <summary>
        /// Returns a task instance only if it's been already cached by the session.
        /// </summary>
        /// <param name="instanceId"></param>
        /// <returns></returns>
        public virtual TaskInstance GetSessionLocalInstance(string instanceId)
        {
            TaskHolder th;
            return _cache.TryGetValue(instanceId, out th) ? th.Deserialized : null;
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

        
        
        
        
        public virtual void SaveChanges()
        {
            if (PersistenceMode == TaskPersistenceMode.DontPersistAnything) return;
            var l = _cache.Where(kv => kv.Value.State == RecordState.Modified || kv.Value.State == RecordState.New);
            if (PersistenceMode == TaskPersistenceMode.PersistAliveTasksOnly)
            {
                l = l.Where(kv => kv.Value.State == RecordState.Modified || (kv.Value.State == RecordState.New && kv.Value.Deserialized.IsAlive));
            }
            if (l.Count() == 0) return;
            WriteRecords(l.Select(kv => kv.Value));
        }

        protected abstract TaskHolder LoadTaskRecord(string instanceId, bool forUpdate);
        protected abstract void WriteRecords(IEnumerable<TaskHolder> records);

        public void Dispose()
        {
        }

        
    }
}
