using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGinnBPM.Runtime.ExecutionEngine
{
    public interface IProcessTransactionParticipant : IDisposable
    {
        void OnCommit();
        void OnRollback();
    }
}
