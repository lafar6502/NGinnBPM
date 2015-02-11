using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NGinnBPM.Runtime
{
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
