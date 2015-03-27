using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NGinnBPM.Runtime.Tasks;

namespace NGinnBPM.Runtime
{
    public interface ITaskExecutionContext
    {
        //void NotifyTaskEvent(TaskExecutionEvents.TaskExecEvent ev);
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
        void SendTaskControlMessage(TaskExecutionEvents.TaskControlCommand msg);

        
        /// <summary>
        /// Schedule a task event with future delivery date
        /// </summary>
        /// <param name="ev"></param>
        /// <param name="deliveryDate"></param>
        void ScheduleTaskEvent(TaskExecutionEvents.TaskExecEvent ev, DateTime deliveryDate);

        T GetService<T>();
        
        T GetService<T>(string name);
        
    }
}
