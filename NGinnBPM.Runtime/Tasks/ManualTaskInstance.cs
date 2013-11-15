using System;
using System.Collections.Generic;
using NGinnBPM.MessageBus;
using System.Text;
using System.Runtime.Serialization;
using NGinnBPM.Runtime.TaskExecutionEvents;
using NGinnBPM.ProcessModel;
using NGinnBPM.ProcessModel.Data;
using NGinnBPM.Runtime;
using NGinnBPM.MessageBus;

namespace NGinnBPM.Runtime.Tasks
{
    [DataContract]
    public class ManualTaskInstance : AtomicTaskInstance
    {
        [DataMember]
        public string Endpoint { get; set; }
        [DataMember]
        public string Assignee { get; set; }
        [DataMember]
        public string AssigneeGroup { get; set; }
        [DataMember]
        public string Summary { get; set; }
        [DataMember]
        public string Description { get; set; }
        [DataMember]
        public string Profile { get; set; }

        protected override void OnTaskEnabled()
        {
            var msg = new NGinnBPM.Lib.CreateManualTask
            {
                Assignee = this.Assignee,
                AssigneeGroup = this.AssigneeGroup,
                CorrelationId = this.InstanceId,
                InstanceId = this.InstanceId,
                TaskData = this.TaskData,
                Description = this.Description,
                TaskId = this.TaskId,
                ProcessDefinition = this.ProcessDefinitionId
            };
            if (Endpoint.StartsWith("http://") || Endpoint.StartsWith("https://"))
            {
                IServiceClient sc = ServiceClient.Create(Endpoint);
                var rt = sc.CallService<object>(msg);
            }
            else if (Endpoint.StartsWith("sql://"))
            {
                this.Context.GetService<IMessageBus>().Send(Endpoint, msg);
            }
            else throw new NotImplementedException();
        }
    }
}
