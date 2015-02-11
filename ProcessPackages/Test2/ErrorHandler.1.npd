# testing error handlers and async execution
# when these are used
#

variable "V0", "string":
    default_value Environment.MachineName
    options {dir: input, required:true}
        
start_place "start"
end_place "end"


task "T1", "timer":
    init_task:
        Task.ExpirationDate = DateTime.Now.AddSeconds(10)

task "T2", "debug":
    init_task:
        Task.DoFail = true

task "T3", "debug":
    init_task:
        Task.DoFail = false
        
flow "start", "T1"
flow "T1", "T2"
flow "T2", "T3", {"sourcePort" : TaskOutPortType.Error}
flow "T2", "end"
flow "T3", "end"
