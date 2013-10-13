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
            throw new NotImplementedException();
        }

        public Dictionary<string, object> GatherOutputData(TaskInstance ti, ITaskExecutionContext ctx)
        {
            throw new NotImplementedException();
        }

        public bool EvalFlow(TaskInstance ti, ProcessModel.FlowDef fd, ITaskExecutionContext ctx)
        {
            _pd.SetTaskInstanceInfo(ti, ctx);
            return fd.FInputCondition();
        }
    }
}
