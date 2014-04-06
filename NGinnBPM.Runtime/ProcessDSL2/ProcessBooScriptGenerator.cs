using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NGinnBPM.ProcessModel;
using System.IO;
using NGinnBPM.Runtime.ProcessDSL;

namespace NGinnBPM.Runtime.ProcessDSL2
{
    public class ProcessBooScriptGenerator
    {
        private TextWriter _out;
        private bool _wsAgnostic = false;
        private string _curIndent = "";

        public static void GenerateScript(ProcessDef pd, TextWriter output)
        {
            
            var gd = new ProcessBooScriptGenerator(output);
            gd.GenerateScript(pd);
            output.Flush();
        }

        public static string GenerateScriptString(ProcessDef pd)
        {
            var sw = new StringWriter();
            GenerateScript(pd, sw);
            return sw.ToString();
        }

        protected ProcessBooScriptGenerator(TextWriter output)
        {
            _out = output;
        }

        protected void Indent(Action act)
        {
            var s = _curIndent;
            _curIndent = _curIndent + "    ";
            act();
            _curIndent = s;
        }
        
        protected void WriteLine(string code, params object[] prm)
        {
            _out.Write(_curIndent);
            _out.WriteLine(code, prm);
        }

        protected void GenerateScript(ProcessDef pd)
        {
            GenerateScript(pd.Body);
        }

        protected void GenerateScript(CompositeTaskDef ct)
        {
            GenerateBaseTaskScripts(ct);
            foreach (var fl in ct.Flows)
            {
                if (!string.IsNullOrEmpty(fl.InputCondition))
                {
                    GenCondition(DslUtil.FlowConditionKey(ct.Id, fl.From, fl.To), fl.InputCondition);
                }
            }
            foreach (var t in ct.Tasks)
            {
                GenerateScript(t);
            }
        }



        protected void GenerateBaseTaskScripts(TaskDef td)
        {
            foreach(var vd in td.Variables.Where(x => !string.IsNullOrEmpty(x.DefaultValueExpr)))
            {
                GenExpression(DslUtil.TaskVariableDefaultKey(td.Id, vd.Name), vd.DefaultValueExpr);
            }
            foreach (var bnd in td.InputDataBindings.Where(x => x.BindType == DataBindingType.Expr))
            {
                GenExpression(DslUtil.TaskVarInBindingKey(td.Id, bnd.Target), bnd.Source);
            }
            foreach (var bnd in td.OutputDataBindings.Where(x => x.BindType == DataBindingType.Expr))
            {
                GenExpression(DslUtil.TaskVarOutBindingKey(td.Id, bnd.Target), bnd.Source);
            }
            foreach (var bnd in td.InputParameterBindings.Where(x => x.BindType == DataBindingType.Expr))
            {
                GenExpression(DslUtil.TaskParamInBindingKey(td.Id, bnd.Target), bnd.Source);
            }
            foreach (var bnd in td.OutputParameterBindings.Where(x => x.BindType == DataBindingType.Expr))
            {
                GenExpression(DslUtil.TaskParamOutBindingKey(td.Id, bnd.Target), bnd.Source);
            }
            if (td.IsMultiInstance)
            {
                GenExpression(DslUtil.TaskMultiInstanceSplitKey(td.Id), td.MultiInstanceSplitExpression);
            }
            if (!string.IsNullOrEmpty(td.BeforeEnableScript))
            {
                GenStatement(DslUtil.TaskScriptKey(td.Id, "BeforeEnable"), td.BeforeEnableScript);
            }
            if (!string.IsNullOrEmpty(td.BeforeCompleteScript))
            {
                GenStatement(DslUtil.TaskScriptKey(td.Id, "BeforeComplete"), td.BeforeCompleteScript);
            }
            if (!string.IsNullOrEmpty(td.AfterEnableScript))
            {
                GenStatement(DslUtil.TaskScriptKey(td.Id, "AfterEnable"), td.AfterEnableScript);
            }
        }

        protected void GenExpression(string id, string code)
        {
            WriteLine("add_expr \"{0}\":", id);
            WriteLine("return ({0})", code);
            WriteLine("end");
        }

        protected void GenStatement(string id, string code)
        {
            WriteLine("add_stmt \"{0}\":", id);
            WriteLine(code);
            WriteLine("end");
        }

        protected void GenCondition(string id, string code)
        {
            WriteLine("add_cond \"{0}\":", id);
            WriteLine("return ({0})", code);
            WriteLine("end");
        }

        protected void GenerateScript(AtomicTaskDef td)
        {
            GenerateBaseTaskScripts(td);
        }

        protected void GenerateScript(TaskDef td)
        {
            if (td is CompositeTaskDef)
            {
                GenerateScript((CompositeTaskDef) td);
            }
            else
            {
                GenerateScript((AtomicTaskDef)td);
            }
        }

    }
}
