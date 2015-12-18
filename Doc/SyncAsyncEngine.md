
Task transitions from the perspective of parent task


Enabling -> Enabled
Enabling -> Started
Enabling -> Completed

Enabling -> Failed

Enabling -> Cancelled (cannot occur when enabling a new task)
Enabling -> Cancelling

Enabled -> Started
Enabled -> Completed
Enabled -> Failed
Started -> Completed
Started -> Failed
Started -> Cancelling
Started -> Cancelled
Cancelling -> Cancelled
Cancelling -> Failed

----
what operations does composite task initiate

- enable child task
- cancel child task

all other operations are initiated by the process engine
(as a result of message bus message or an api call)

***   SO... ***

tasks don't have to send any messages to their parent task
this information can be sent by the engine on detecting task status change.
***



=== Process execution events
What: We're starting a process and we want to get notified when the process is started, cancelled, completed or failed
with async execution we can use message bus
with sync execution we get the response as a return value
what about Tasks/async/await? Is it applicable here?


