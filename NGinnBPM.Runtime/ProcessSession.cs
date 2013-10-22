using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NGinnBPM.Runtime.TaskExecutionEvents;
using NGinnBPM.Runtime.Tasks;
using NGinnBPM.Runtime.Services;

namespace NGinnBPM.Runtime
{
    public class ProcessSession : ITaskExecutionContext, IDisposable
    {
        [ThreadStatic]
        private static ProcessSession _ses;

        public static ProcessSession Current
        {
            get { return _ses; }
            set { _ses = value; }
        }

        protected TaskPersisterSession _persisterSession;
        protected ProcessRunner _runner;

        public void ScheduleTaskEvent(TaskExecutionEvents.TaskExecEvent ev, DateTime deliveryDate)
        {
            _runner.MessageBus.NotifyAt(deliveryDate, ev);
        }

        public void NotifyTaskEvent(TaskExecutionEvents.TaskExecEvent ev)
        {
            SendProcessMessage(ev, false);
        }

        public void SendTaskControlMessage(TaskExecutionEvents.TaskControlMessage msg)
        {
            SendProcessMessage(msg, false);
        }

        public bool IsPersistent
        {
            get
            {
                return true;
            }
        }

        private Queue<ProcessMessage> _asyncQueue = new Queue<ProcessMessage>();
        private Queue<ProcessMessage> _syncQueue = new Queue<ProcessMessage>();

        protected void SendProcessMessage(ProcessMessage pm, bool separateTransaction)
        {
            if (separateTransaction)
            {
                if (IsPersistent)
                {
                    _runner.MessageBus.Notify(pm);
                }
                else
                {
                    _asyncQueue.Enqueue(pm);
                }
            }
            else
            {
                _syncQueue.Enqueue(pm);
            }
        }

        

        public static ProcessSession CreateNew(ProcessRunner r, TaskPersisterSession ps)
        {
            var s = new ProcessSession
            {
                _runner = r,
                _persisterSession = ps
            };
            if (s._persisterSession == null) throw new Exception("Task persister session not present");
            return s;
        }

        public static ProcessSession CreateNew(ProcessRunner r)
        {
            return CreateNew(r, TaskPersisterSession.Current);
        }

        public TaskPersisterSession PersisterSession
        {
            get { return _persisterSession; }
        }

        public void Dispose()
        {
            if (ProcessSession.Current == this)
            {
                ProcessSession.Current = null;
            }
        }

        protected void ModifyTaskInstance(string instanceId, Action<TaskInstance> act)
        {
            var ti = _persisterSession.GetForUpdate(instanceId);
            act(ti);
            _persisterSession.Update(ti);
        }

        

        private Dictionary<string, object> _sessionData = new Dictionary<string, object>();

        /// <summary>
        /// Store some data in a session
        /// </summary>
        /// <param name="key"></param>
        /// <param name="v"></param>
        public void SetSessionData(string key, object v)
        {
            _sessionData.Remove(key);
            _sessionData.Add(key, v);
        }

        /// <summary>
        /// retrieve data stored in a session
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public T GetSessionData<T>(string key)
        {
            object v;
            return _sessionData.TryGetValue(key, out v) ? (T)v : default(T);
        }

        public T GetOrAddSessionData<T>(string key, Func<T> valueProvider)
        {
            object v;
            if (!_sessionData.TryGetValue(key, out v))
            {
                v = valueProvider();
                _sessionData[key] = v;
            }
            return (T)v;
        }
        


        public void SetupTaskHelper(TaskInstance ti, Dictionary<string, object> inputData)
        {
            
        }

        public Dictionary<string, object> GetTaskOutputDataHelper(TaskInstance ti)
        {
            throw new NotImplementedException();
        }
        
        protected void DeliverEvent(TaskExecEvent ev)
        {
            _runner.DeliverTaskExecEvent(ev);
        }

        protected void DeliverControlMessage(TaskControlMessage msg)
        {
            _runner.DeliverTaskControlMessage(msg);
        }

        private Dictionary<string, object> _content = new Dictionary<string, object>();

        public object this[string index]
        {
            get { return _content[index]; }
            set { _content[index] = value; }
        }


        public void PumpMessages()
        {
            while (_syncQueue.Count > 0)
            {
                var m = _syncQueue.Dequeue();
                switch (m.Mode)
                {
                    case MessageHandlingMode.AnotherTransaction:
                        _runner.MessageBus.Notify(m);
                        break;
                    case MessageHandlingMode.SameTransaction:
                        if (m is TaskExecEvent)
                        {
                            DeliverEvent((TaskExecEvent)m);
                        }
                        else if (m is TaskControlMessage)
                        {
                            DeliverControlMessage((TaskControlMessage)m);
                        }
                        break;
                    default:
                        throw new Exception();
                }
            }
        }
    }
}
