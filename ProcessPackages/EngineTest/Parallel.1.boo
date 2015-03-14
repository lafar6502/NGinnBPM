#simplest test case
import NGinnBPM.ProcessModel

start_place "start"
end_place "end"

task "T1", "empty":
    split_type TaskSplitType.AND

task "T2", "empty":
    pass

task "T3", "empty":
    pass
    
        
flow "start", "T1"
flow "T1", "T2"
flow "T1", "T3"
flow "T2", "end"
flow "T3", "end"
