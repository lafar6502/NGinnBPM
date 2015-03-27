using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NGinnBPM.Runtime.TaskExecutionEvents;
using NGinnBPM.Runtime.Tasks;
using NGinnBPM.Runtime.Services;
using NGinnBPM.MessageBus;

namespace NGinnBPM.Runtime.ExecutionEngine
{
    /// <summary>
    /// Process session roughly equals to a transaction
    /// 
    /// </summary>
    public class ProcessSession : ITaskExecutionContext, IDisposable
    {
        [ThreadStatic]
        private static ProcessSession _ses;

        public static ProcessSession Current
        {
            get { return _ses; }
            set { _ses = value; }
        }

        public ProcessSession(TaskPersisterSession taskPersister, IMessageBus bus, IServiceResolver serviceResolver)
        {
            MessageBus = bus;
            TaskPersister = taskPersister;
            ServiceResolver = ServiceResolver;
            SyncQueue = new Queue<ProcessMessage>();
            AsyncQueue = new Queue<ProcessMessage>();
        }

        public IMessageBus MessageBus { get; set; }
        public TaskPersisterSession TaskPersister { get; set; }
        public IServiceResolver ServiceResolver { get; set; }

        private void ValidateMessage(ProcessMessage pm)
        {
            if (string.IsNullOrEmpty(pm.FromProcessInstanceId) ||
                string.IsNullOrEmpty(pm.FromTaskInstanceId)) throw new Exception("Missing source task/process id");
        }

        private void ValidateMessage(TaskExecEvent ev)
        {
            ValidateMessage((ProcessMessage)ev);
            if (string.IsNullOrEmpty(ev.ParentTaskInstanceId) && ev.FromTaskInstanceId != ev.FromProcessInstanceId) throw new Exception("Missing parent task id");
        }

        public void ScheduleTaskEvent(TaskExecutionEvents.TaskExecEvent ev, DateTime deliveryDate)
        {
            ValidateMessage(ev);
            MessageBus.NotifyAt(deliveryDate, ev);
        }

        public void NotifyTaskEvent(TaskExecutionEvents.TaskExecEvent ev)
        {
            ValidateMessage(ev);
            SyncQueue.Enqueue(ev);
        }

        protected void SendTaskControlMessage(TaskExecutionEvents.TaskControlCommand msg)
        {
            ValidateMessage(msg);
            SyncQueue.Enqueue(msg);
        }

        public bool IsPersistent
        {
            get
            {
                return true;
            }
        }

        public Queue<ProcessMessage> SyncQueue { get; set; }
        
        public Queue<ProcessMessage> AsyncQueue { get; set; }


        
        


        
        public void Dispose()
        {
        }

        
        
        private Dictionary<string, object> _sessionData = new Dictionary<string, object>();

        /// <summary>
        /// Store some data in a session
        /// </summary>
        /// <param name="key"></param>
        /// <param name="v"></param>
        public void SetSessionData(string key, object v)
        {
            lock (_sessionData)
            {
                _sessionData.Remove(key);
                _sessionData.Add(key, v);
            }
        }

        public void Set<T>(T defaultService)
        {
            SetSessionData("~" + typeof(T).FullName, defaultService);
        }

        public T Get<T>()
        {
            return GetSessionData<T>("~" + typeof(T).FullName);
        }

        /// <summary>
        /// retrieve data stored in a session
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public T GetSessionData<T>(string key)
        {
            lock (_sessionData)
            {
                object v;
                return _sessionData.TryGetValue(key, out v) ? (T)v : default(T);
            }
        }

        public T GetOrAddSessionData<T>(string key, Func<T> valueProvider)
        {
            lock (_sessionData)
            {
                object v;
                if (!_sessionData.TryGetValue(key, out v))
                {
                    v = valueProvider();
                    _sessionData[key] = v;
                }
                return (T)v;
            }
        }



        public T GetService<T>()
        {
            return ServiceResolver.GetInstance<T>();
        }

        public T GetService<T>(string name)
        {
            return ServiceResolver.GetInstance<T>(name);
        }

        public void EnableChildTask(EnableChildTask msg)
        {
            SendTaskControlMessage(msg);
        }

        public void CancelChildTask(CancelTask msg)
        {
            SendTaskControlMessage(msg);
        }
    }
}
