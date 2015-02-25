using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NGinnBPM.MessageBus;
using NGinnBPM.Runtime.TaskExecutionEvents;
using NGinnBPM.Runtime;
using NGinnBPM.Runtime.ExecutionEngine;
using NLog;

namespace NGinnBPM.Runtime.Services
{
    /// <summary>
    /// Handles async process control messages coming from the message bus
    /// </summary>
    public class AsyncProcessControlMessageHandler : 
        IMessageConsumer<TaskExecEvent>,
        IMessageConsumer<TaskControlCommand>
    {
        private ProcessEngine _pr;
        private static Logger log = LogManager.GetCurrentClassLogger();

        public IDbSessionFactory DbSessionFactory { get; set; }

        public AsyncProcessControlMessageHandler(ProcessEngine pr)
        {
            _pr = pr;
        }

        public void Handle(TaskExecEvent message)
        {
            MessageBusUtil.ShareDbConnection(DbSessionFactory, () => _pr.DeliverTaskExecEvent(message));
        }

        public void Handle(TaskControlCommand message)
        {
            MessageBusUtil.ShareDbConnection(DbSessionFactory, () => _pr.DeliverTaskControlMessage(message));
        }
    }
}
