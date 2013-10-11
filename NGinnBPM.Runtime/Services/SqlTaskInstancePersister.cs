using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;

namespace NGinnBPM.Runtime.Services
{
    internal class SqlTaskPersisterSession : TaskPersisterSession
    {

    }

    public class SqlTaskInstancePersister : ITaskInstancePersister
    {
        public TaskPersisterSession OpenSession()
        {
            throw new NotImplementedException();
        }
    }
}
