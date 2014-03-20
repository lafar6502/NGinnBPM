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
    public class ManualTaskInstance : AtomicTaskInstance
    {
        /// <summary>
        /// Endpoint where create task message is sent
        /// </summary>
        [DataMember]
        public string Endpoint { get; set; }
        /// <summary>
        /// Task assignee
        /// </summary>
        [DataMember]
        public string Assignee { get; set; }
        /// <summary>
        /// Assignee group (id, name, query...)
        /// </summary>
        [DataMember]
        public string AssigneeGroup { get; set; }
        /// <summary>
        /// Task summary
        /// </summary>
        [DataMember]
        public string Summary { get; set; }
        /// <summary>
        /// Task description
        /// </summary>
        [DataMember]
        public string Description { get; set; }
        /// <summary>
        /// Task profile...
        /// </summary>
        [DataMember]
        public string Profile { get; set; }
        /// <summary>
        /// Task deadline
        /// </summary>
        [DataMember]
        public DateTime? Deadline { get; set; }
        /// <summary>
        /// Dont create a task in a hosting application. 
        /// This is used if the app will check in NGinnBPM if
        /// specific task instance is active
        /// </summary>
        [DataMember]
        public bool DontCreateTask { get; set; }


        protected override void OnTaskEnabling()
        {
            if (!DontCreateTask)
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
        }

        public override void Cancel(string reason)
        {
            if (Status == TaskStatus.Enabled ||
                Status == TaskStatus.Selected)
            {
                if (!DontCreateTask)
                {
                    var msg = new NGinnBPM.Lib.CancelManualTask
                    {
                        InstanceId = this.InstanceId,
                        Reason = reason
                    };
                    SendMessage(msg, Endpoint);
                }
            }
            DefaultHandleTaskCancel(reason);
        }

        protected void SendMessage<T>(T msg, string endpoint)
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
            else
            {
                IMessageConsumer<T> h = Context.GetService<IMessageConsumer<T>>(endpoint);
                h.Handle(msg);
            }
        }
    }
}
