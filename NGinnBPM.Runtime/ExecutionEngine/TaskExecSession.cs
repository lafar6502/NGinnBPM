using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGinnBPM.Runtime.ExecutionEngine
{
    class TaskExecSession : ITaskSessionContext, IDisposable
    {
        public void PublishProcessMessage(TaskExecutionEvents.ProcessMessage msg)
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


        
    }
}
