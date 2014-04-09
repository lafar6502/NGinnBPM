using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NGinnBPM.Runtime;
using NGinnBPM.Runtime.Services;
using BL = Boo.Lang;

namespace NGinnBPM.Runtime.ProcessDSL2
{
    /// <summary>
    /// Process script runtime API
    /// </summary>
    public partial class ProcessRuntimeDSLBase 
    {
        internal Dictionary<string, Func<bool>> _conds = new Dictionary<string, Func<bool>>();
        internal Dictionary<string, Action> _stmts = new Dictionary<string, Action>();
        internal Dictionary<string, Func<object>> _exprs = new Dictionary<string, Func<object>>();

        protected void add_cond(string id, Func<bool> condition)
        {
            _conds[id] = condition;
        }

        protected void add_expr(string id, Func<object> expr)
        {
            _exprs[id] = expr;
        }

        protected void add_stmt(string id, Action stmt)
        {
            _stmts[id] = stmt;
        }
    }
}
