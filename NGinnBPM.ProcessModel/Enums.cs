using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NGinnBPM.ProcessModel
{

    public enum TaskSplitType
    {
        XOR,
        AND,
        OR
    }

    /// <summary>
    /// Task output port types
    /// TODO implement missing types
    /// </summary>
    public enum TaskOutPortType
    {
        /// <summary>Standard output port</summary>
        Out,
        /// <summary>Failure output port</summary>
        Error,
        /// <summary>Signal output port (unused yet)</summary>
        Signal,
        /// <summary>Cancel output port. Token generated here after task is cancelled.</summary>
        Cancel,
        /// <summary>Compensation output port. Token generated here if a task is compensated.</summary>
        Compensate
    }

    /// <summary>
    /// Task input port type. 
    /// TODO - some sensible implementation is needed.
    /// What input ports can we have
    /// - normal
    /// - cancel (this should rather lead to a place, not a task)
    /// - compensate
    /// TODO - implement
    /// </summary>
    public enum TaskInPortType
    {
        In
    }


    public enum NGinnTaskType
    {
        Empty,
        Manual,
        Script,
        Custom,
        RaiseError,
        ReceiveMessage,
        SendMessage,
        Subprocess,
        Timer,
        Notification,
        XmlHttp,
        Composite,
        Debug
    }
}
