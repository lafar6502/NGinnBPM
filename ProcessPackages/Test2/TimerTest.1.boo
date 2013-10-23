variable "V0", "string":
    default_value Environment.MachineName
    options {dir: input, required:true}
        
start_place "start"
end_place "end"


task "T1", "timer":
    variable "V1", "string":
        default_value DateTime.Now.ToString()
        options {dir: in_out, required: true, array: false}
        input_binding ParentData.V0
        
    output_binding "V0", OutputData.V1 + " - ala ma kota"
    init_task:
        Task.ExpirationDate = DateTime.Now.AddMinutes(1)

task "T2", "timer":
    variable "V1", "string":
        default_value DateTime.Now.ToString()
        options {dir: in_out, required: true, array: false}
        input_binding ParentData.V0
        
    output_binding "V0", OutputData.V1 + " - ala ma kota"
    init_task:
        Task.ExpirationDate = DateTime.Now.AddMinutes(1)
        
flow "start", "T1"
flow "start", "T2"
flow "T1", "end"
flow "T2", "end"
