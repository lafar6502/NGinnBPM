using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NGinnBPM.Runtime;
using NGinnBPM.Runtime.Tasks;
using NGinnBPM.ProcessModel;

namespace NGinnBPM.Runtime.ProcessDSL
{
    internal class BooDslProcessRuntime : IProcessScriptRuntime
    {
        private ProcessDefDSLBase _pd;
        private ProcessDef _def;

        internal BooDslProcessRuntime(ProcessDefDSLBase pd)
        {
            _pd = pd;
            _def = pd.GetProcessDef();
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
                    if (_pd._variableBinds.ContainsKey(k))
                    {
                        ti.TaskData[vd.Name] = _pd._variableBinds[k]();
                    }
                    else if (!string.IsNullOrEmpty(vd.DefaultValueExpr))
                    {
                        ti.TaskData[vd.Name] = vd.DefaultValueExpr;
                    }
                    else if (vd.IsRequired)
                        throw new Exception("Required variable missing: " + vd.Name);
                }
            }
            //now initialize task parameters
        }

        public Dictionary<string, object> GatherOutputData(TaskInstance ti, ITaskExecutionContext ctx)
        {
            throw new NotImplementedException();
        }

        public bool EvalFlowCondition(TaskInstance ti, ProcessModel.FlowDef fd, ITaskExecutionContext ctx)
        {
            string k = DslUtil.FlowConditionKey(fd.Parent.Id, fd.From, fd.To);
            if (!_pd._flowConditions.ContainsKey(k)) throw new Exception("!no flow cond..");
            _pd.SetTaskInstanceInfo(ti, ctx);
            return _pd._flowConditions[k]();
        }


        
        public void SetTaskScriptContext(TaskInstance task, Dictionary<string, object> inputData, Dictionary<string, object> outputData)
        {
            throw new NotImplementedException();
        }
    }
}
