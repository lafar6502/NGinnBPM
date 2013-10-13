process_data_types:
    define_enum "SOLUTION_CODE", ("ALA", "MA", "KOTA")
    define_struct "JakiesDane":
        member "Name", "string"
        member "SomeNum", "int"
        

variable "V0", "string":
    default_value Environment.MachineName
    options {dir: input, required:true}
        
start_place "start"
end_place "end"


task "T1", "manual":
    variable "V1", "string":
        default_value DateTime.Now.ToString()
        options {dir: input, required: true, array: false}
        input_binding ParentData.V0
        
    output_binding "V0", OutputData.V1 + "- ma kota"
    #init_parameter AssigneeGroup, InputData.Value1 + 17
    init_task:
        Task.AssigneeGroup = TaskData.AssigneeId
    flow_to "end"
        
flow "start", "T1"

flow "T1", "end":
    when TaskData.AssigneeGroup > 30
    options {}
