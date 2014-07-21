using System;
using System.Collections.Generic;
using NGinnBPM.MessageBus;
using System.Text;
using System.Runtime.Serialization;
using NGinnBPM.Runtime.TaskExecutionEvents;
using NGinnBPM.ProcessModel;
using NGinnBPM.ProcessModel.Data;
using NGinnBPM.Runtime;


namespace NGinnBPM.Runtime.Tasks
{
    [DataContract]
    public class SubprocessTaskInstance : AtomicTaskInstance
    {

        [DataMember]
        public string SubprocessDefinitionId { get; set; }

        [DataMember]
        public bool Async { get; set; }

        protected override void OnTaskEnabling()
        {
            
        }

        public override void Cancel(string reason)
        {
         
        }

        
    }
}
