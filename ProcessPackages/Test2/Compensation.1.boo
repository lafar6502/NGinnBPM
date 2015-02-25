# testing error handlers and async execution
# when these are used
#
import NGinnBPM.ProcessModel

variable "V0", "string":
    default_value Environment.MachineName
    options {dir: input, required:true}
        
start_place "start"
end_place "end"

task "T0", "debug":
    init_task:
        Task.DoFail = false
        
task "T1", "timer":
    init_task:
        Task.ExpirationDate = DateTime.Now.AddSeconds(20)

task "T2", "debug":
    init_task:
        Task.Delay = true
        Task.DoFail = false
        log.Warn("T2 - OK")

task "T3", "debug":
    init_task:
        Task.Delay = true
        Task.DoFail = false
        log.Warn("T3 - OK COMPENSATING")
        
flow "start", "T0"
flow "T0", "T1"
flow "T0", "T3", {"sourcePort" : TaskOutPortType.Compensate}
flow "T1", "T2"
flow "T2", "end"
flow "T3", "end"
