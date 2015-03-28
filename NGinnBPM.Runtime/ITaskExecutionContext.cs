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
        /// starts a child process 
        /// On completion/failure/cancellation we should get a message 
        /// </summary>
        /// <param name="definitionId"></param>
        /// <param name="inputData"></param>
        /// <param name="parentTaskInstanceId"></param>
        /// <param name="parentProcessInstance"></param>
        /// <returns></returns>
       // string StartProcess(string definitionId, Dictionary<string, object> inputData, string parentTaskInstanceId, string parentProcessInstance);

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
