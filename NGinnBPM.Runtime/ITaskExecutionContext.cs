using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NGinnBPM.Runtime.Tasks;

namespace NGinnBPM.Runtime
{
    public interface ITaskExecutionContext
    {
        void NotifyTaskEvent(TaskExecutionEvents.TaskExecEvent ev);
        /// <summary>
        /// Send a task control request.
        /// Warning: this will not execute synchronously. It will be executed later, when processing
        /// messages in a transaction. So you cannot assume that after send returns then the message has already
        /// been processed.
        /// What is worse, if something fails you will not get that information from Send. You might even not get anything at all.
        /// But if an exception occurs during message processing an exception will be thrown and whole transaciton
        /// will be rolled back, so you don't really have to worry about messages failing.
        /// </summary>
        /// <param name="msg"></param>
        void SendTaskControlMessage(TaskExecutionEvents.TaskControlMessage msg);
        void ScheduleTaskEvent(TaskExecutionEvents.TaskExecEvent ev, DateTime deliveryDate);
        

        /// <summary>
        /// Retrieve task output data by executing output variable/parameter bindings
        /// </summary>
        /// <param name="ti"></param>
        /// <returns></returns>
        Dictionary<string, object> GetTaskOutputDataHelper(TaskInstance ti);

        
    }
}
