using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NGinnBPM.Runtime.Services
{
    public interface ITaskInstancePersister
    {
        TaskPersisterSession OpenSession();
    }


    public abstract class TaskPersisterSession : IDisposable
    {
        public virtual TaskInstance GetForRead(string instanceId)
        {
            throw new NotImplementedException();
        }

        public virtual TaskInstance GetForUpdate(string instanceId)
        {
            throw new NotImplementedException();
        }


        public virtual void SaveNew(TaskInstance ti)
        {
            throw new NotImplementedException();
        }
        
        public virtual void Update(TaskInstance ti)
        {
            throw new NotImplementedException();
        }

        public virtual IEnumerable<TaskInstance> GetNewTasks()
        {
            throw new NotImplementedException();
        }
        
        public virtual IEnumerable<TaskInstance> GetModifiedTasks()
        {
            throw new NotImplementedException();
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
