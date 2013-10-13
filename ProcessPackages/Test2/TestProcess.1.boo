process_data_types:
    define_enum "SOLUTION_CODE", ("ALA", "MA", "KOTA")
    define_struct "JakiesDane":
        member "Name", "string"
        member "SomeNum", "int"
        
start_place "start"
end_place "end"

task "T1", "manual":
    variable "V1", "string", {dir: input, required: true, array: false}
    #init_parameter AssigneeGroup, InputData.Value1 + 17
    init_task:
        Task.AssigneeGroup = TaskData.AssigneeId
        
flow "start", "T1"

flow "T1", "end":
    when TaskData.AssigneeGroup > 30
    options {}
