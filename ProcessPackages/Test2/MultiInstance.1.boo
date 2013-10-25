# testing error handlers and async execution
# when these are used
#

variable "V0", "string":
    default_value(["ALA", "MA", "KOTKA"])
    options {dir: input, required:true, isArray: true}
variable "Result", "string", {dir:local, isArray: true}

start_place "start"
end_place "end"

task "T1", "timer":
    variable "VN", "string", {dir: input, required: true}
    variable "Vlen", "int", {dir: input, required: true}
    multi_instance_split([{"VN" : i, "Vlen" : i.Length} for i in TaskData.V0])
    init_task:
        Task.ExpirationDate = DateTime.Now.AddSeconds(10 * TaskData.Vlen)
    
task "T2", "debug":
    variable "VD", "string", {dir: input, isArray: true}
    task_input_data {"VD": ""}
    
flow "start", "T1"
flow "T1", "T2"
flow "T2", "end"
