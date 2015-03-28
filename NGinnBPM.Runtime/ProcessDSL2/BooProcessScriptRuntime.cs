using System;
using System.Collections.Generic;
using SC = System.Collections;
using System.Linq;
using System.Text;
using NGinnBPM.Runtime;
using NGinnBPM.Runtime.Tasks;
using NGinnBPM.ProcessModel;
using NGinnBPM.Runtime.ProcessDSL;

namespace NGinnBPM.Runtime.ProcessDSL2
{
    internal class BooProcessScriptRuntime : IProcessScriptRuntime
    {
        private ProcessRuntimeDSLBase _pd;
        private ProcessDef _def;

        internal BooProcessScriptRuntime(ProcessRuntimeDSLBase pd)
        {
            _pd = pd;
            _def = pd.ProcessDefinition;
        }

        public string ProcessDefinitionId
        {
            get { return _def.DefinitionId; }
        }

        public void InitializeNewTask(TaskInstance ti, Dictionary<string, object> inputData, ITaskExecutionContext ctx)
        {
            if (string.IsNullOrEmpty(ti.InstanceId) ||
                string.IsNullOrEmpty(ti.TaskId) ||
                string.IsNullOrEmpty(ti.ProcessDefinitionId) ||
                string.IsNullOrEmpty(ti.ProcessInstanceId))
                throw new Exception("Task not inited properly");
            _pd.SetTaskInstanceInfo(ti, ctx);
            
            TaskDef td = _def.GetRequiredTask(ti.TaskId);
            if (td.Variables != null)
            {
                foreach (var vd in td.Variables)
                {
                    if (vd.VariableDir == ProcessModel.Data.VariableDef.Dir.In ||
                        vd.VariableDir == ProcessModel.Data.VariableDef.Dir.InOut)
                    {
                        if (inputData.ContainsKey(vd.Name))
                            ti.TaskData[vd.Name] = inputData[vd.Name];
                    }
                    if (!ti.TaskData.ContainsKey(vd.Name))
                    {
                        var k = DslUtil.TaskVariableDefaultKey(td.Id, vd.Name);
                        if (_pd._exprs.ContainsKey(k))
                        {
                            ti.TaskData[vd.Name] = _pd._exprs[k]();
                        }
                        else if (!string.IsNullOrEmpty(vd.DefaultValueExpr))
                        {
                            ti.TaskData[vd.Name] = vd.DefaultValueExpr; //TODO: add type conversion
                        }
                        else if (vd.IsRequired)
                            throw new NGinnBPM.ProcessModel.Exceptions.DataValidationException("Required variable missing: " + vd.Name).SetTaskId(ti.TaskId).SetProcessDef(ti.ProcessDefinitionId);
                    }
                }
            }
            //now initialize task parameters
            if (td.InputParameterBindings != null)
            {
                foreach (var bd in td.InputParameterBindings)
                {
                    var pi = ti.GetType().GetProperty(bd.Target);
                    if (pi == null)
                    {
                        throw new NGinnBPM.ProcessModel.Exceptions.TaskParameterInvalidException(bd.Target, "Property not found: " + bd.Target).SetTaskId(ti.TaskId);
                    }
                    string k = DslUtil.TaskParamInBindingKey(td.Id, bd.Target);
                    if (bd.BindType == DataBindingType.Expr)
                    {
                        pi.SetValue(ti, _pd._exprs[k](), null);
                    }
                    else if (bd.BindType == DataBindingType.CopyVar)
                    {
                        pi.SetValue(ti, ti.TaskData.ContainsKey(bd.Source) ? ti.TaskData[bd.Source] : null, null);
                    }
                    else if (bd.BindType == DataBindingType.Literal)
                    {
                        pi.SetValue(ti, Convert.ChangeType(bd.Source, pi.PropertyType), null);
                    }
                    else throw new Exception();
                }
            }
            string ks = DslUtil.TaskScriptKey(ti.TaskId, "BeforeEnable");
            if (_pd._stmts.ContainsKey(ks))
                _pd._stmts[ks]();
        }

        public Dictionary<string, object> GatherOutputData(TaskInstance ti, ITaskExecutionContext ctx)
        {
            _pd.SetTaskInstanceInfo(ti, ctx);
            string ks = DslUtil.TaskScriptKey(ti.TaskId, "AfterComplete");
            if (_pd._stmts.ContainsKey(ks)) _pd._stmts[ks]();
            var td = _def.GetRequiredTask(ti.TaskId);
            
            foreach (var bd in td.OutputParameterBindings)
            {
                string k = DslUtil.TaskParamOutBindingKey(td.Id, bd.Target);
                if (bd.BindType == DataBindingType.Expr)
                {
                    ti.TaskData[bd.Target] = _pd._exprs[k]();
                }
                else if (bd.BindType == DataBindingType.CopyVar)
                {
                    var pi = ti.GetType().GetProperty(bd.Source);
                    if (pi == null) throw new NGinnBPM.ProcessModel.Exceptions.TaskParameterInvalidException(bd.Source, "Property not found: " + bd.Source).SetTaskId(ti.TaskId);
                    ti.TaskData[bd.Target] = pi.GetValue(ti, null);
                }
                else if (bd.BindType == DataBindingType.Literal)
                {
                    ti.TaskData[bd.Target] = bd.Source; //todo: type convert
                }
                else throw new Exception();
            }
            
            string k2 = DslUtil.TaskOutputDataBindingKey(td.Id);
            if (_pd._exprs.ContainsKey(k2))
            {
                SC.IDictionary dic = (SC.IDictionary)_pd._exprs[k2]();
                return ToTaskData(dic);
            }
            Dictionary<string, object> ret = new Dictionary<string, object>();
            foreach (var vd in td.Variables.Where(x => x.VariableDir == ProcessModel.Data.VariableDef.Dir.Out || x.VariableDir == ProcessModel.Data.VariableDef.Dir.InOut))
            {
                ret[vd.Name] = ti.TaskData[vd.Name];
            }
            return ret;
        }

        public bool EvalFlowCondition(TaskInstance ti, ProcessModel.FlowDef fd, ITaskExecutionContext ctx)
        {
            _pd.SetTaskInstanceInfo(ti, ctx);
            string k = DslUtil.FlowConditionKey(fd.Parent.Id, fd.From, fd.To);
            if (!_pd._conds.ContainsKey(k)) throw new Exception("!no flow cond..");
            _pd.SetTaskInstanceInfo(ti, ctx);
            return _pd._conds[k]();
        }


        
        public void SetTaskScriptContext(TaskInstance task, Dictionary<string, object> inputData, Dictionary<string, object> outputData)
        {
            throw new NotImplementedException();
            //_pd.SetTaskInstanceInfo(task, ctx);
            _pd.SetInputData(inputData);
            _pd.SetOutputData(outputData);
        }


        public Dictionary<string, object> PrepareChildTaskInputData(CompositeTaskInstance cti, TaskDef childTask, ITaskExecutionContext ctx)
        {
            _pd.SetTaskInstanceInfo(cti, ctx);
            
            string k1 = DslUtil.TaskInputDataBindingKey(childTask.Id);
            if (_pd._exprs.ContainsKey(k1))
            {
                //get full data record
                SC.IDictionary dic = (SC.IDictionary)_pd._exprs[k1]();
                return ToTaskData(dic);
            }

            Dictionary<string, object> ret = new Dictionary<string, object>();
            if (childTask.Variables != null)
            {
                if (childTask.AutoBindVariables)
                {
                    foreach (var vd in childTask.Variables)
                    {
                        if (vd.VariableDir == ProcessModel.Data.VariableDef.Dir.In ||
                            vd.VariableDir == ProcessModel.Data.VariableDef.Dir.InOut)
                        {
                            //TODO add type conversion/control
                            if (cti.TaskData.ContainsKey(vd.Name)) ret[vd.Name] = cti.TaskData[vd.Name];
                        }
                    }
                }
                if (childTask.InputDataBindings != null)
                {
                    foreach (var bd in childTask.InputDataBindings)
                    {
                        if (bd.BindType == DataBindingType.CopyVar)
                        {
                            ret[bd.Target] = cti.TaskData[bd.Source];
                        }
                        else if (bd.BindType == DataBindingType.Literal)
                        {
                            ret[bd.Target] = bd.Source;
                        }
                        else if (bd.BindType == DataBindingType.Expr)
                        {
                            string k = DslUtil.TaskVarInBindingKey(childTask.Id, bd.Target);
                            if (!_pd._exprs.ContainsKey(k)) throw new Exception("Fail: missing delegate: " + k);
                            ret[bd.Target] = _pd._exprs[k]();
                        }
                    }
                }
            }
            return ret;
        }

        private Dictionary<string, object> ToTaskData(SC.IDictionary dic)
        {
            Dictionary<string, object> ret = new Dictionary<string, object>();
            foreach (string k in dic.Keys)
            {
                object v = dic[k];
                if (v is SC.IDictionary) v = ToTaskData((SC.IDictionary)v);
                ret[k] = v;
            }
            return ret;
        }

        public IEnumerable<Dictionary<string, object>> PrepareMultiInstanceTaskInputData(CompositeTaskInstance cti, TaskDef childTask, ITaskExecutionContext ctx)
        {
            if (!childTask.IsMultiInstance) throw new Exception();
            var k = DslUtil.TaskMultiInstanceSplitKey(childTask.Id);
            Func<object> fun = _pd._exprs[k];
            if (fun == null) throw new Exception();
            _pd.SetTaskInstanceInfo(cti, ctx);
            SC.IEnumerable enu;
            var val = fun();
            enu = val is SC.IEnumerable ? (SC.IEnumerable) val : new object[] { val };
            List<Dictionary<string, object>> ret = new List<Dictionary<string, object>>();
            foreach (object item in enu)
            {
                if (item is Dictionary<string, object>) 
                {
                    ret.Add((Dictionary<string, object>) item);
                }
                else if (item is SC.IDictionary)
                {
                    ret.Add(ToTaskData((SC.IDictionary)item));
                }
                else
                {
                    throw new Exception();
                }
            }
            return ret;
            
            /*
            ITaskScript scr = Context.ScriptManager.GetTaskScript(this.ParentProcess, taskId);
            Task tsk = MyTask.RequireTask(taskId);
            scr.TaskContext = Context;
            Dictionary<string, object> srcData = new Dictionary<string, object>(TaskData);
            scr.SourceData = srcData;
            object obj = scr.EvalMultiInstanceSplitQuery();
            IEnumerable enu;
            if (obj is IEnumerable)
                enu = (IEnumerable)obj;
            else
            {
                ArrayList al = new ArrayList();
                al.Add(obj);
                enu = al;
            }
            List<Dictionary<string, object>> lst = new List<Dictionary<string, object>>();

            foreach (object v in enu)
            {
                srcData[tsk.MultiInstanceItemAlias] = v;
                lst.Add(ExecuteInputDataBindings(scr, tsk));
            }
            return lst;
            */
            throw new NotImplementedException();
        }

        public void ExecuteChildTaskOutputDataBinding(CompositeTaskInstance ti, TaskDef childTask, Dictionary<string, object> childOutputData, ITaskExecutionContext ctx)
        {
            if (string.IsNullOrEmpty(ti.InstanceId) ||
                string.IsNullOrEmpty(ti.TaskId) ||
                string.IsNullOrEmpty(ti.ProcessDefinitionId) ||
                string.IsNullOrEmpty(ti.ProcessInstanceId))
                throw new Exception("Task not inited properly");
            _pd.SetTaskInstanceInfo(ti, ctx);
            _pd.SetOutputData(childOutputData);
            _pd.SetInputData(null);
            
            var ctd = childTask.Parent;
            if (childTask.AutoBindVariables && ctd.Variables != null)
            {
                foreach (var vd in ctd.Variables)
                {
                    if (childOutputData.ContainsKey(vd.Name))
                    {
                        ti.TaskData[vd.Name] = childOutputData[vd.Name];
                    }
                }
            }
            if (childTask.OutputDataBindings != null)
            {
                foreach (var bd in childTask.OutputDataBindings)
                {
                    switch (bd.BindType)
                    {
                        case DataBindingType.CopyVar:
                            ti.TaskData[bd.Target] = childOutputData[bd.Source];
                            break;
                        case DataBindingType.Literal:
                            ti.TaskData[bd.Target] = bd.Source;
                            break;
                        case DataBindingType.Expr:
                            string k = DslUtil.TaskVarOutBindingKey(childTask.Id, bd.Target);
                            if (!_pd._exprs.ContainsKey(k)) throw new Exception("!");
                            ti.TaskData[bd.Target] = _pd._exprs[k]();
                            break;
                    }
                }
            }
        }


        public void ExecuteTaskScriptBlock(TaskInstance ti, string blockName, ITaskExecutionContext ctx)
        {
            string k = DslUtil.TaskScriptKey(ti.TaskId, blockName);
            _pd.SetTaskInstanceInfo(ti, ctx);
            try
            {
                Action act;
                if (_pd._stmts.TryGetValue(k, out act) && act != null)
                {
                    act();
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
