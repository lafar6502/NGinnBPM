using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NGinnBPM.Runtime.TaskExecutionEvents;
using System.Transactions;

namespace NGinnBPM.Runtime
{
    /// <summary>
    /// Process runner does run the processes by pushing them forward in a transactional manner.
    /// A single process transaction does as much as it can, without bothering parent tasks (they don't get mixed into child task's transaction).
    /// This way we can execute a whole process in a single transaction if we want.
    /// </summary>
    public class ProcessRunner 
    {
        public Services.ITaskInstancePersister TaskPersister { get; set; }


        public string StartProcess(string definitionId, Dictionary<string, object> inputData)
        {
            throw new NotImplementedException();
        }

        public void UpdateTask(string instanceId, Dictionary<string, object> updatedData)
        {
            throw new NotImplementedException();
        }

        public void CancelTask(string instanceId, string reason)
        {
            throw new NotImplementedException();
        }

        public void SelectTask(string instanceId)
        {
            throw new NotImplementedException();
        }

        public void ForceCompleteTask(string instanceId, Dictionary<string, object> updatedData)
        {
            throw new NotImplementedException();

        }

        public void ForceFailTask(string instanceId, string reason)
        {
            throw new NotImplementedException();
        }


        protected void RunProcessTransaction(Action<ProcessSession> act)
        {
            TransactionOptions to = new TransactionOptions { 
                IsolationLevel = IsolationLevel.ReadCommitted,
                Timeout = TimeSpan.FromSeconds(60)
            };
            TransactionScope ts = null;
            ProcessSession ps = null;
            try
            {
                ts = new TransactionScope(TransactionScopeOption.Required, to);
                ps = ProcessSession.CreateNew(this);
                ProcessSession.Current = ps;
                act(ps);
                
                ts.Dispose();
            }
            finally
            {
                ProcessSession.Current = null;
                if (ps != null)
                {
                    ps.Dispose();
                }
                if (ts != null)
                {
                    ts.Dispose();
                }
            }            
        }

        public void DoSomething()
        {
            try
            {
                ProcessSession.Current = ProcessSession.CreateNew(this);

                var ti = new TaskInstance();
                ti.Activate(ProcessSession.Current);
                ti.Enable(new Dictionary<string, object>());
                
            }
            finally
            {
                var ps = ProcessSession.Current;
                if (ps != null)
                {
                    ps.Dispose();
                    ProcessSession.Current = null;
                }
            }
        }
        
    }
}
