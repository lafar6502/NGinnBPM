using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NGinnBPM.Runtime.Services
{
    public class InMemoryTaskPersister : ITaskInstancePersister
    {
        public TaskPersisterSession OpenSession()
        {
            throw new NotImplementedException();
        }

        public TaskPersisterSession OpenSession(DbSession ses)
        {
            throw new NotImplementedException();
        }

        internal class InMemoryTaskPersisterSession : TaskPersisterSession
        {
            public InMemoryTaskPersisterSession(ITaskInstanceSerializer ser) : base(ser)
            {
            }

            protected override void WriteRecords(IEnumerable<TaskPersisterSession.TaskHolder> records)
            {
                throw new NotImplementedException();
            }

            protected override TaskPersisterSession.TaskHolder LoadTaskRecord(string instanceId, bool forUpdate)
            {
                throw new NotImplementedException();
            }
        }
    }
}
