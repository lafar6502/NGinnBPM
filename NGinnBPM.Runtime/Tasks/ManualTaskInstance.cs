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

        protected override void OnTaskEnabling()
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
            SendMessage(msg, Endpoint);
        }

        public override void Cancel(string reason)
        {
            if (Status == TaskStatus.Enabled ||
                Status == TaskStatus.Selected)
            {
                var msg = new NGinnBPM.Lib.CancelManualTask
                {
                    InstanceId = this.InstanceId,
                    Reason = reason
                };
                SendMessage(msg, Endpoint);
            }
            DefaultHandleTaskCancel(reason);
        }

        protected void SendMessage(object msg, string endpoint)
        {
            if (endpoint.StartsWith("http://") || Endpoint.StartsWith("https://"))
            {
                IServiceClient sc = ServiceClient.Create(endpoint);
                var rt = sc.CallService<object>(msg);
            }
            else if (endpoint.StartsWith("sql://"))
            {
                this.Context.GetService<IMessageBus>().Send(endpoint, msg);
            }
        }
    }
}
