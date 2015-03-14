#simplest test case
import NGinnBPM.ProcessModel

start_place "start"
end_place "end"

variable "N", "int":
    default_value 0
    options {dir: input, required:true}

task "T1", "empty":
    split_type TaskSplitType.XOR

task "T2", "empty":
    pass

task "T3", "empty":
    pass
    
        
flow "start", "T1"
flow "T1", "T2":
    when TaskData.N == 0
    options {"evalOrder": 0}
    
flow "T1", "T3":
    options {"evalOrder": 1}
    
flow "T2", "T1"
flow "T3", "end"
