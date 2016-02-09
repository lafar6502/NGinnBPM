using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NGinnBPM.Runtime;
using NGinnBPM.Runtime.Services;
using NGinnBPM.ProcessModel;
using NGinnBPM.Runtime.Tasks;
using BL = Boo.Lang;
using SC = System.Collections;
using NGinnBPM.Runtime.ProcessDSL;

namespace NGinnBPM.Runtime.ProcessDSL2
{
    public  abstract partial class ProcessRuntimeDSLBase 
    {
        public ProcessDef ProcessDefinition { get;set;}
        protected BooProcessPackage Package { get; set; }
        [BL.DuckTyped]
        protected TaskInstance Task { get; set; }
        protected ITaskExecutionContext Context { get; set; }
        protected BL.IQuackFu InputData { get; set; }
        protected BL.IQuackFu OutputData { get; set; }
        protected BL.IQuackFu TaskData { get; set; }
        [BL.DuckTyped]
        protected object Item { get; set; }
        /// <summary>
        /// Documents retrieved for each 'docref' input/local variable
        /// </summary>
        protected BL.IQuackFu Documents { get; set; }

        
        public void Initialize(ProcessDef pd, BooProcessPackage pp)
        {
            ProcessDefinition = pd;
            Package = pp;
            Prepare();
        }

        public void SetTaskInstanceInfo(TaskInstance ti, ITaskExecutionContext ctx)
        {
            this.Task = ti;
            this.TaskData = new QuackTaskDataWrapper(ti.TaskData);
            this.Context = ctx;
        }

        public void SetInputData(Dictionary<string, object> data)
        {
            InputData = data == null ? null : new QuackTaskDataWrapper(data);
        }

        public void SetOutputData(Dictionary<string, object> data)
        {
            OutputData = data == null ? null : new QuackTaskDataWrapper(data);
        }

        public void SetItem(object v)
        {
            if (v is SC.IDictionary)
            {
                Item = new QuackTaskDataWrapper((SC.IDictionary)v);
            }
            else if (v is Dictionary<string, object>)
            {
                Item = new QuackTaskDataWrapper((Dictionary<string, object>)v);
            }
            else
            {
                Item = new QuackTaskDataWrapper(new Dictionary<string, object> {
                    {"Value", v}
                });
            }
        }

        protected abstract void Prepare();
    }
}
