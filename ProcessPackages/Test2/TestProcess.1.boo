process_data_types:
    define_enum "SOLUTION_CODE", ("ALA", "MA", "KOTA")
    define_struct "DUPA_JASIA":
        member "Name"
        
process_body:
    task "T1", "manual":
        variables:
            input_variable "A1", "string", {"required": true, default: {|InputData.V3}},
            local_variable "A3", "string", {},
            bidir_variable "A4", "string", {"required" : true}
            define_variable "A1", {type: string, required: true, array: true, direction: input}
        input_bindings:
        output_bindings:
        
    composite_task "T2":
        variable "A1", {type: string, required: true, array:false, direction:in}
        
        input_data_binding:
            bind "A1"
        output_data_binding:
            bind "Dupa", Data.A1 + Data.A2
        input_parameter_binding:
            bind_param Title, "To jest zadanie ${TaskData.A1}"
            bind_param
        