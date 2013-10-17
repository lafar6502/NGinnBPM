using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NGinnBPM.MessageBus;
using NGinnBPM.Runtime.TaskExecutionEvents;
using NGinnBPM.Runtime;
using NLog;

namespace NGinnBPM.Runtime.Services
{
    /// <summary>
    /// Handles async process control messages coming from the message bus
    /// </summary>
    public class AsyncProcessControlMessageHandler : 
        IMessageConsumer<TaskExecEvent>,
        IMessageConsumer<TaskControlMessage>
    {
        private ProcessRunner _pr;
        private static Logger log = LogManager.GetCurrentClassLogger();

        public AsyncProcessControlMessageHandler(ProcessRunner pr)
        {
            _pr = pr;
        }

        public void Handle(TaskExecEvent message)
        {
            _pr.DeliverTaskExecEvent(message);
        }

        public void Handle(TaskControlMessage message)
        {
            _pr.DeliverTaskControlMessage(message);
        }
    }
}
