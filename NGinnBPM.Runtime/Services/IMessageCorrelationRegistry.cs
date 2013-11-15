using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NGinnBPM.Runtime.Services
{
    public interface IMessageCorrelationRegistry
    {
        void Subscribe(string mailbox, string taskInstanceId);
        void Unsubscribe(string mailbox, string taskInstanceId);
        IEnumerable<string> GetMailboxSubscribers(string mailboxId, bool andRemoveThemAll);
    }
}
