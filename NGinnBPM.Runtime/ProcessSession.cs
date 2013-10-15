using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NGinnBPM.Runtime.TaskExecutionEvents;
using NGinnBPM.Runtime.Tasks;

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

        protected class TaskInstanceHolder
        {
            public bool IsNew { get; set; }
            public TaskInstance Instance { get; set; }
        }

        private Dictionary<string, TaskInstanceHolder> _touchedTasks = new Dictionary<string, TaskInstanceHolder>();
        private Queue<TaskExecEvent> _eventQ = new Queue<TaskExecEvent>();
        private Queue<TaskControlMessage> _controlQ = new Queue<TaskControlMessage>();


        void ITaskExecutionContext.NotifyTaskEvent(TaskExecutionEvents.TaskExecEvent ev)
        {
            _eventQ.Enqueue(ev);
        }

        void ITaskExecutionContext.SendTaskControlMessage(TaskExecutionEvents.TaskControlMessage msg)
        {
            _controlQ.Enqueue(msg);
        }

        public static ProcessSession CreateNew(ProcessRunner r)
        {
            var s = new ProcessSession();
            return s;
        }

        public void Dispose()
        {

        }

        protected void ModifyTaskInstance(string instanceId, Action<TaskInstance> act)
        {
            TaskInstanceHolder thi;
            if (!_touchedTasks.TryGetValue(instanceId, out thi))
            {
                thi = new TaskInstanceHolder();
                thi.IsNew = false;
            }
            act(thi.Instance);

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
            if (_sessionData.TryGetValue(key, out v))
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
            
        }

        protected void DeliverControlMessage(TaskControlMessage msg)
        {
        }
        private Dictionary<string, object> _content = new Dictionary<string, object>();

        public object this[string index]
        {
            get { return _content[index]; }
            set { _content[index] = value; }
        }


        public void PumpMessages()
        {
            do
            {
                while(_eventQ.Count > 0)
                {
                    DeliverEvent(_eventQ.Dequeue());
                }
                while(_controlQ.Count > 0)
                {
                    DeliverControlMessage(_controlQ.Dequeue());
                }
            }
            while (_controlQ.Count + _eventQ.Count > 0);
        }
    }
}
