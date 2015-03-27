using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NGinnBPM.Runtime.Tasks;

namespace NGinnBPM.Runtime
{
    public interface ITaskExecutionContext
    {
        void EnableChildTask(TaskExecutionEvents.EnableChildTask msg);
        void CancelChildTask(TaskExecutionEvents.CancelTask msg);

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
