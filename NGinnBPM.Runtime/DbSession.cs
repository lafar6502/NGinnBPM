using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NGinnBPM.Runtime
{
    /// <summary>
    /// Dbsession is a simple wrapper around a database connection used by NGinn BPM
    /// Components participating in transaction can use DBSession for accessing the connection
    /// and sharing it instead of opening a new one.
    /// </summary>
    public class DbSession : IDisposable
    {
        [ThreadStatic]
        private static DbSession _cur;

        public static DbSession Current
        {
            get { return _cur; }
            set { _cur = value; }
        }

        public virtual object Connection 
        {
            get { return null; }
        }
        
        public virtual void Dispose()
        {
            
        }
    }
}
