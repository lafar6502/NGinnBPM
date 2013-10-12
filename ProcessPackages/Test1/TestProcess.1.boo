process_data_types:
    define_enum "SOLUTION_CODE", ("ALA", "MA", "KOTA")
    define_struct "DUPA_JASIA":
        member "Name"
        
process_body:
    task "T1":
        variables:
            input_variable "A1", "string", {"required": true, default: {|InputData.V3}},
            local_variable "A3", "string", {},
            bidir_variable "A4", "string", {"required" : true}
            define_variable "A1", {type: string, required: true, array: true, direction: input}
        input_bindings:
        output_bindings:
        