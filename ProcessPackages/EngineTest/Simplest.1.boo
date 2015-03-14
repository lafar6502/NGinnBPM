#simplest test case

start_place "start"
end_place "end"

task "T1", "empty":
    pass

flow "start", "T1"
flow "T1", "end"