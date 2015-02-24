using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NGinnBPM.Runtime.Tasks;

namespace NGinnBPM.Runtime.ExecutionEngine
{
    /// <summary>
    /// session context interface accessible to task instances
    /// during execution
    /// </summary>
    public interface ITaskSessionContext
    {
        /// <summary>
        /// sends a process notification or control message
        /// </summary>
        /// <param name="msg"></param>
        void PublishProcessMessage(TaskExecutionEvents.ProcessMessage msg);
        /// <summary>
        /// Schedule a task event with future delivery date
        /// </summary>
        /// <param name="ev"></param>
        /// <param name="deliveryDate"></param>
        void ScheduleTaskEvent(TaskExecutionEvents.TaskExecEvent ev, DateTime deliveryDate);

        

        /// <summary>
        /// container services access
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T GetService<T>();
        /// <summary>
        /// container services access
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        T GetService<T>(string name);
        
    }
}
