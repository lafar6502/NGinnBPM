


## Error handling

An error occuring during task execution will cause the task to fail. In such situation the task instance's status will change to 'Failed'.
What happens next depends on whether the task has an error handler or not.
An error handler for a task is represented by a flow  attached to 'error' port of that task. If a task with such flow fails it generates a token in the flow's target place and that's it. The error is caught and taken care of so the parent task does not get notified about it.
In case the task has no error handler the error will be propagated to the parent composite task. The propagation is done by publishing 'TaskFailed' event. In such case no output tokens are created on task failure, the task just changes its status to failed and the failure event is delivered to the parent task.
After receiving such notification the composite task must handle the error somehow. The default procedure is to cancel whole composite task and fail it too. This means that all child tasks that were active when the error occurred will be cancelled and then the composite task will report failure. And failure handling rules described here also apply to parent task so the behavior will be the same: if the composite task has no error handler then the error will be propagated upwards. If it reaches process instance level the whole process instance will fail.

## Compensation
## compensation
Compensation is a procedure of reverting the effects of already completed tasks. Such tasks have already executed and cannot be cancelled, so the only option for reverting their effects is to apply a compensating operation.
How to initiate compensation procedure?

Rules for compensation
- compensation is performed as a part of composite task cancellation
- in such case all active tasks will be cancelled and all tokens removed from the net
- each completed child task with a compensation flow (flow from the 'compensate' port) will generate a token on that flow
- and execution will continue in the 'Cancelling' state. All compensation logic must execute and then the task will change status to Cancelled or Failed
- Failed status will be set when there's an error thrown during compensation phase


## Sync and async execution

NGinn.BPM process engine supports both synchronous and asynchronous process execution. Business processes can consist of a mix of synchronous and asynchronous tasks so it is necessary to support both execution models and switch between them easily.
A process fragment is synchronous when it contains synchronous tasks only. A synchronous task completes immediately after being enabled, so when the 'Enable' call ends the task is already completed. In contrast, an asynchronous task will stay in 'Enabled' or 'Started' state and will complete some time later, in another transaction. The simple rule is that if a task completes in same transaction where it was initiated then it's synchronous. If a composite task (or a process) contains only synchronous tasks then it's also synchronous. Otherwise it's asynchronous.
Warning: it's impossible to execute async process synchronously. It's also impossible to execute sync process asynchronously. TODO: make sure we can stick to such restrictions - maybe we should relax the #2 rule.
Implications
1. Execution engine API has to handle both async and sync execution, that means returning output data from the 'StartProcess' for synchronous processes
2. There should be an option to disable sending messages to the message bus in case of sync execution
3. 

## Transactions

Transaction is an 'unit of work' executed synchronously, and according to transaction processing rules (ACID). NGinnBPM process model does not describe transactions explicitly - it's the execution engine's responsibility to execute a process and properly divide the execution into transactions. It's important to know that process state does not change outside of a transaction - all modifications and state transitions are a part of some transaction. 

## Process/task state persistence

TODO: do we support in-memory-only execution?

## Task instance lifecycle



