using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NGinnBPM.MessageBus;
using System.Diagnostics;

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
                var ses = fact.OpenSession(MessageBusContext.ReceivingConnection);
                try
                {
                    DbSession.Current = ses;
                    act();
                }
                finally
                {
                    var s = DbSession.Current;
                    DbSession.Current = null;
                    Debug.Assert(s == ses);
                    s.Dispose();
                }
            }
        }
    }
}
