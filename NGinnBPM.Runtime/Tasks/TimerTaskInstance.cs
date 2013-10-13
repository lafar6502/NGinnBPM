using System;
using System.Collections.Generic;
using NGinnBPM.MessageBus;
using NGinnBPM.Runtime.Events;
using NGinnBPM.Lib.Data;
using NGinnBPM.Lib.Schema;
using NGinnBPM.Lib.Exceptions;
using System.Text;
using NGinnBPM.Services;
using NGinnBPM.Lib.Interfaces;
using System.Runtime.Serialization;
using NGinnBPM.Runtime.Messages;

namespace NGinnBPM.Runtime.Tasks
{
    [Serializable]
    [DataContract]
    public class TimerTaskInstance : AtomicTaskInstance,
        ITaskMessageHandler<TaskInstanceTimeout>
    {
        private DateTime _scheduledExpiration = DateTime.MinValue;
        private TimeSpan? _delayAmount = null;

        [DataMember]
        [TaskParameter(IsInput=true, Required=false, DynamicAllowed=true)]
        public DateTime ExpirationDate
        {
            get { return _scheduledExpiration; }
            set { _scheduledExpiration = value; }
        }

        [IgnoreDataMember]
        public string Delay
        {
            get { return _delayAmount.HasValue ? _delayAmount.Value.ToString() : null; }
            set { _delayAmount = TimeSpan.Parse(value); }
        }

        [DataMember]
        [TaskParameter(IsInput = true, Required = false, DynamicAllowed = true)]
        public TimeSpan DelayAmount
        {
            get { return _delayAmount.HasValue ? _delayAmount.Value : TimeSpan.Zero; }
            set { _delayAmount = value; }
        }

        public override void Enable(Dictionary<string, object> inputData)
        {
            base.Enable(inputData);
            if (_scheduledExpiration == DateTime.MinValue && !_delayAmount.HasValue)
                throw new TaskParameterInvalidException("DelayAmount", "Either DelayAmount or ExpirationDate is required").SetTaskId(TaskId);
            DateTime expiration = _scheduledExpiration;
            if (_delayAmount.HasValue)
            {
                expiration = DateTime.Now.Add(_delayAmount.Value);
                _scheduledExpiration = expiration;
            }
            log.Debug("Timer task {0} expiration is {1}", InstanceId, expiration);
            ScheduleInternalTimeout(expiration, null);
        }

       


        #region IMessageConsumer<TaskInstanceTimeout> Members

        public void Handle(TaskInstanceTimeout message)
        {
            RequireActivation(true);
            if (Status == TaskStatus.Enabled || Status == TaskStatus.Selected)
            {
                Complete(null, null);
            }
        }

        #endregion
    }
}
