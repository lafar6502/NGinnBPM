using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NGinnBPM.Lib
{
    /// <summary>
    /// Manual task assignment
    /// - to a specified person
    /// - to a specified group queue
    /// - auto-assign to a group member (round-robin, to a least busy one, etc)
    /// - skill based (???)
    /// </summary>
    public class CreateManualTask
    {
        public string InstanceId { get; set; }
        public string ProcessInstanceId { get; set; }
        public string ProcessDefinition { get; set; }
        public string TaskId { get; set; }
        public string CorrelationId { get; set; }
        public string Summary { get; set; }
        public string Description { get; set; }
        public string Assignee { get; set; }
        public string AssigneeGroup { get; set; }
        public DateTime? PlannedStart { get; set; }
        public int? PlannedDurationMin { get; set; }
        public DateTime? Deadline { get; set; }
        public string SharedId { get; set; }
        public Dictionary<string, object> TaskData { get; set; }
        public string TaskProfile { get; set; }
        public string ParentDocumentId { get; set; }
    }

    public class CancelManualTask
    {
        public string InstanceId { get; set; }
        public string Reason { get; set; }
    }

    public class CompleteManualTask
    {
        public string InstanceId { get; set; }
    }
}
