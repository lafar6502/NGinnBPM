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
        void SendTaskControlMessage(TaskExecutionEvents.TaskControlMessage msg);

        /// <summary>
        /// Initialize task fields based on inputData and task definition parameter bindings
        /// </summary>
        /// <param name="ti"></param>
        /// <param name="inputData"></param>
        void SetupTaskHelper(TaskInstance ti, Dictionary<string, object> inputData);

        /// <summary>
        /// Retrieve task output data by executing output variable/parameter bindings
        /// </summary>
        /// <param name="ti"></param>
        /// <returns></returns>
        Dictionary<string, object> GetTaskOutputDataHelper(TaskInstance ti);
    }
}
