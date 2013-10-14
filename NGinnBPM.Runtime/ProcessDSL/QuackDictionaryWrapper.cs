using System;
using System.Collections.Generic;
using System.Text;
using Boo.Lang;
using SC = System.Collections;

namespace NGinnBPM.Runtime.ProcessDSL
{
    public class QuackTaskDataWrapper : IQuackFu
    {
        private Func<string, object> _getter;
        private Action<string, object> _setter;
        

        public QuackTaskDataWrapper(Dictionary<string, object> data)
        {
            _getter = k => data[k];
            _setter = delegate(string k, object v)
            {
                data[k] = v;
            };
        }

        public QuackTaskDataWrapper(SC.IDictionary dic)
        {
            _getter = k => dic[k];
            _setter = delegate(string k, object v)
            {
                dic[k] = v;
            };
        }

        #region IQuackFu Members

        public object QuackGet(string name, object[] parameters)
        {
            object v = _getter(name);
            if (parameters != null && parameters.Length > 0) throw new Exception("Indexers not supported");
            if (v is Dictionary<string, object>)
            {
                return new QuackTaskDataWrapper(v as Dictionary<string, object>);
            }
            else if (v is SC.IDictionary)
            {
                return new QuackTaskDataWrapper(v as SC.IDictionary);
            }
            return v;
        }

        public object QuackInvoke(string name, params object[] args)
        {
            throw new NotImplementedException();
        }

        public object QuackSet(string name, object[] parameters, object value)
        {
            if (parameters != null && parameters.Length > 0) throw new Exception("Indexers not supported");
            _setter(name, value);
            return value;
        }

        #endregion
    }

}
