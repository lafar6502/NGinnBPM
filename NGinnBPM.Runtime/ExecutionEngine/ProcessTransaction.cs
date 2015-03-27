using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGinnBPM.Runtime.ExecutionEngine
{
    public class ProcessTransaction : ITaskExecutionContext, IDisposable
    {

        public void NotifyTaskEvent(TaskExecutionEvents.TaskExecEvent ev)
        {
            throw new NotImplementedException();
        }

        public void SendTaskControlMessage(TaskExecutionEvents.TaskControlCommand msg)
        {
            throw new NotImplementedException();
        }

        public void ScheduleTaskEvent(TaskExecutionEvents.TaskExecEvent ev, DateTime deliveryDate)
        {
            throw new NotImplementedException();
        }

        public T GetService<T>()
        {
            throw new NotImplementedException();
        }

        public T GetService<T>(string name)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void EnableChildTask(TaskExecutionEvents.EnableChildTask msg)
        {
            throw new NotImplementedException();
        }

        public void CancelChildTask(TaskExecutionEvents.CancelTask msg)
        {
            throw new NotImplementedException();
        }
    }
}
