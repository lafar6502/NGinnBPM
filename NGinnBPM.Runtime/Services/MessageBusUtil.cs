using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NGinnBPM.MessageBus;

/// <summary>
/// session (process transaction) =
/// document session +
/// message bus tran
/// and process session
/// </summary>
namespace NGinnBPM.Runtime.Services
{
    public class MessageBusUtil
    {
        public static void ShareDbConnection(IDbSessionFactory fact, Action act)
        {
            if (DbSession.Current != null)
            {
                act();
            }
            else
            {
                using (var ses = fact.OpenSession(MessageBusContext.ReceivingConnection))
                {
                    DbSession.Current = ses;
                    act();
                    DbSession.Current = null;
                }
            }
        }
    }
}
