using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NGinnBPM.Runtime;
using NGinnBPM.Runtime.Services;
using NGinnBPM.ProcessModel;

namespace NGinnBPM.Runtime.ProcessDSL2
{
    public  abstract partial class ProcessRuntimeDSLBase : IProcessScriptRuntime
    {
        public ProcessDef ProcessDefinition { get;set;}
        protected BooProcessPackage Package { get; set; }

        string IProcessScriptRuntime.ProcessDefinitionId
        {
            get { throw new NotImplementedException(); }
        }

        void IProcessScriptRuntime.InitializeNewTask(Tasks.TaskInstance ti, Dictionary<string, object> inputData, ITaskExecutionContext ctx)
        {
            throw new NotImplementedException();
        }

        Dictionary<string, object> IProcessScriptRuntime.GatherOutputData(Tasks.TaskInstance ti, ITaskExecutionContext ctx)
        {
            throw new NotImplementedException();
        }

        bool IProcessScriptRuntime.EvalFlowCondition(Tasks.TaskInstance ti, ProcessModel.FlowDef fd, ITaskExecutionContext ctx)
        {
            string id = ProcessDSL.DslUtil.FlowConditionKey(fd.Parent.Id, fd.From, fd.To);
            Func<bool> cond = null;
            if (!_conds.TryGetValue(id, out cond)) throw new Exception("Condition not found: " + id);
            return cond();
        }

        Dictionary<string, object> IProcessScriptRuntime.PrepareChildTaskInputData(Tasks.CompositeTaskInstance cti, ProcessModel.TaskDef childTask, ITaskExecutionContext ctx)
        {
            throw new NotImplementedException();
        }

        IEnumerable<Dictionary<string, object>> IProcessScriptRuntime.PrepareMultiInstanceTaskInputData(Tasks.CompositeTaskInstance cti, ProcessModel.TaskDef childTask, ITaskExecutionContext ctx)
        {
            throw new NotImplementedException();
        }

        void IProcessScriptRuntime.ExecuteChildTaskOutputDataBinding(Tasks.CompositeTaskInstance ti, ProcessModel.TaskDef childTask, Dictionary<string, object> childOutputData, ITaskExecutionContext ctx)
        {
            throw new NotImplementedException();
        }

        public void Initialize(ProcessDef pd, BooProcessPackage pp)
        {
            ProcessDefinition = pd;
            Package = pp;
            Prepare();
        }

        protected abstract void Prepare();
    }
}
