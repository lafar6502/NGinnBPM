using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NGinnBPM.ProcessModel.Data;
using NGinnBPM.MessageBus;
using NGinnBPM.ProcessModel.Exceptions;
using NGinnBPM.ProcessModel.Util;
using System.Collections;
using System.Diagnostics;
using System.Runtime.Serialization;
using NGinnBPM.Runtime.TaskExecutionEvents;
using Newtonsoft.Json;
using NGinnBPM.ProcessModel;
using NLog;

namespace NGinnBPM.Runtime.Tasks
{
    /// <summary>
    /// Status of transition in composite task
    /// </summary>
    public enum TransitionStatus
    {
        /// <summary>Transition enabled</summary>
        Enabled,
        /// <summary>Transition started</summary>
        Started,
        /// <summary>Task has completed, but completion has not been 'consumed'</summary>
        Completed,
        /// <summary>Transition cancelled</summary>
        Cancelled,
        /// <summary>Task completed and completion handled (data transferred and tokens produced)</summary>
        Closed,
        /// <summary>Transition failed but still active. This status is the same as 'Started', but
        /// the difference is that it denotes a failed task. Like in 'Started' state, the transition
        /// has allocated tokens and is not completed. From this moment we can:
        /// - retry it (just like cancel returning tokens to input places)
        /// - fake completion or cancellation
        /// - fail it, which leads to composite task failure.
        FailedActive,
        /// <summary>Transition failed and failure handled. Nothing more to do here.</summary>
        Failed,
        /// <summary>
        /// For future, when enabling and cancelling will be message-based
        /// </summary>
        Enabling = 100,
        Cancelling = 101
    }
    /// <summary>
    /// Information about a transition in composite task (subnet)
    /// TODO: this class should be made internal
    /// </summary>
    [DataContract]
    public class TransitionInfo
    {
        
        [DataMember]
        public string InstanceId { get;set;}
        [DataMember]
        public string TaskId { get;set;}
        [DataMember]
        public TransitionStatus Status { get;set;}
        /// <summary>
        /// Output data. used by multi-instance tasks.
        /// </summary>
        [DataMember(IsRequired=false)]
        public Dictionary<string, object> OutputData { get;set;}
        
        private List<string> _allocPlaces = new List<string>();

        [DataMember(IsRequired=false)]
        public IList<string> AllocatedPlaces
        {
            get { return _allocPlaces; }
            set { _allocPlaces = new List<string>(value); }
        }
        
        public void SetAllocatedPlaces(ICollection col)
        {
            AllocatedPlaces = new List<string>();
            foreach (string s in col)
                AllocatedPlaces.Add(s);
        }

        /// <summary>
        /// Check if transition is active (Enabled, Started or FailedActive)
        /// </summary>
        [IgnoreDataMember]
        public bool IsTransitionActive
        {
            get
            {
                return Status == TransitionStatus.Enabled
                    || Status == TransitionStatus.Started
                    || Status == TransitionStatus.FailedActive
                    || Status == TransitionStatus.Enabling
                    || Status == TransitionStatus.Cancelling;
            }
        }

        public override string ToString()
        {
            return string.Format("Transition ID: {0}, Task: {1}, Status: {2}", InstanceId, TaskId, Status);
        }
    }


    /// <summary>
    /// Composite task instance. Controls execution
    /// of an inline subprocess.
    /// 
    /// 1. Composite task completion:
    /// If there are no more tokens and no active tasks:
    /// complete the composite task. If completion impossible because of invalid output data
    /// structure, fail the task.
    /// 2. Force-completion:
    /// cancel all outstanding tasks. Try to complete (like in 1.)
    /// 3. Cancellation:
    /// cancel all outstanding tasks. Report task cancelled.
    /// 4. Force-fail
    /// cancel all outstanding tasks. Report task failed.
    /// 5. Exception raised  - like in (4.)
    /// 
    /// 
    /// Error handling.
    /// 1. Task reports an error (fails)
    /// 2. Parent composite handles this information. Marks the transition as failed
    /// and looks for error handler with the task.
    /// 2a. If error handler is found, execution continues with the error handler
    /// 2b. If error handler is not found, composite task fails and process continues with parent composite
    /// (if there is a parent composite at all)
    /// 3. For some tasks it should be possible to retry the failed task without bubbling the error up.
    /// Let's assume that if composite has no error handler, it will not bubble the error up by default.
    /// 
    /// 
    /// Status updates.
    /// How composite's status can be updated
    /// 1. by DoOneStep
    /// 2. child task completed, cancelled, started, failed
    /// </summary>
    /// new TODO
    /// EnableTaskTimeout - wyrzucić, niech tym się zajmuje infrastruktura a nie taski
    /// CancelTaskTimeout - to samo!
    [DataContract]
    public class CompositeTaskInstance : TaskInstance
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        public CompositeTaskInstance()
        {
            Marking = new Dictionary<string, int>();
            AllTasks = new List<TransitionInfo>();
        }
        /// <summary>
        /// Return list of currently active tasks
        /// </summary>
        [IgnoreDataMember]
        public IEnumerable<TransitionInfo> ActiveTasks
        {
            get 
            {
                return AllTasks.Where(ti => ti.IsTransitionActive);
            }
        }

        /// <summary>
        /// Get number of tokens in given place
        /// </summary>
        /// <param name="placeId"></param>
        /// <returns></returns>
        public int GetMarkingOfPlace(string placeId)
        {
            return GetTotalTokens(placeId);
        }

        /// <summary>
        /// Get list of active transitions with given input place
        /// </summary>
        /// <param name="placeId"></param>
        /// <returns></returns>
        public IList<TransitionInfo> GetActiveTransitionsForPlace(string placeId)
        {
            RequireActivation(true);
            List<TransitionInfo> lst = new List<TransitionInfo>();
            PlaceDef pl = ProcessDefinition.GetRequiredPlace(placeId);
            foreach (TaskDef tsk in pl.NodesOut)
            {
                TransitionInfo ti = GetActiveInstanceOfTask(tsk.Id);
                if (ti != null)
                    lst.Add(ti);
            }
            return lst;
        }

        public TransitionInfo GetChildTransitionInfo(string instanceId)
        {
            var ti = this.AllTasks.Find(x => x.InstanceId == instanceId);
            return ti;
        }

        /// <summary>
        /// Get active instance of specified task
        /// </summary>
        /// <param name="taskId"></param>
        /// <returns></returns>
        public TransitionInfo GetActiveInstanceOfTask(string taskId)
        {
            return AllTasks.FirstOrDefault(x => x.TaskId == taskId && x.IsTransitionActive);
        }

        /// <summary>
        /// Return list of all task instances created during execution
        /// of composite task
        /// </summary>
        [DataMember(IsRequired=false)]
        public List<TransitionInfo> AllTasks { get;set;}
        
        [DataMember(IsRequired=true)]
        [JsonConverter(typeof(NoTypeJsonConverter))]
        public Dictionary<string, int> Marking {get;set;}
        

       

        public override void Deactivate()
        {
            base.Deactivate();
            Dictionary<string, int> nm = new Dictionary<string, int>();
            foreach (string k in Marking.Keys)
                if (Marking[k] > 0) nm[k] = Marking[k];
            Marking = nm;
            
        }

        /// <summary>
        /// Continue composite task execution until no more operations are possible in current transaction.
        /// </summary>
        private void ContinueTaskExecution()
        {
            if (Status != TaskStatus.Enabled && Status != TaskStatus.Selected && Status != TaskStatus.Cancelling)
            {
                //nothing to do here.
                return;
            }

            bool cont;
            while ((cont = CheckIfCanContinue(true)))
            {
                foreach (TaskDef tsk in MyTask.Tasks)
                {
                    EnableTaskIfPossible(tsk.Id);
                }
            }

            if (ActiveTasks.FirstOrDefault() != null)
            {
                return; //we have active tasks so let's wait until they execute
            }
            bool hasCompleted = true;
            //no active tasks, let's do deadlock detection
            foreach (PlaceDef pl in MyTask.Places)
            {
                int n = GetMarkingOfPlace(pl.Id);
                if (n > 0 && pl.PlaceType != PlaceTypes.End)
                {
                    log.Warn("Deadlock detected. Token in place {0} cannot be consumed", pl.Id);
                    hasCompleted = false;
                    break;
                }
            }
            if (!hasCompleted)
            {
                return;
            }
            log.Info("Composite task {0} finished!", TaskId);
            if (Status == TaskStatus.Cancelling)
            {
                DefaultHandleTaskCancel(null);
                Debug.Assert(Status == TaskStatus.Cancelled);
                return;
            }
            else if (Status == TaskStatus.Enabled || Status == TaskStatus.Selected)
            {
                DefaultHandleTaskCompletion(null);
                Debug.Assert(Status == TaskStatus.Completed);
                return;
            }
        }

        /*private void OnInternalStatusChanged()
        {
            if (Status != TaskStatus.Enabled && Status != TaskStatus.Selected && Status != TaskStatus.Cancelling)
            {
                //nothing to do here.
                return;
            }
            if (ActiveTasks.Count > 0)
            {
                return;
            }
            _canContinue = CheckIfCanContinue(false);
            if (_canContinue.Value) return;
            foreach (PlaceDef pl in MyTask.Places)
            {
                int n = GetMarkingOfPlace(pl.Id);
                if (n > 0 && pl.PlaceType != PlaceTypes.End)
                {
                    log.Warn("Deadlock detected. Token in place {0} cannot be consumed", pl.Id);
                    return;
                }
            }
            log.Info("Composite task finished!");
            if (Status == TaskStatus.Cancelling)
            {
                DefaultHandleTaskCancel(null);
                Debug.Assert(Status == TaskStatus.Cancelled);
                return;
            }
            else if (Status == TaskStatus.Enabled || Status == TaskStatus.Selected)
            {
                DefaultHandleTaskCompletion(null);
                Debug.Assert(Status == TaskStatus.Completed);
                return;
            }
            else throw new Exception();
        }*/

        /// <summary>
        /// Detect if task has been completed and handle completion.
        /// </summary>
        /*protected void DetectTaskCompletion()
        {
            if (Status != TaskStatus.Enabled && Status != TaskStatus.Selected)
                return;
            if (this.ActiveTasks.Count > 0) 
                return;
            _canContinue = CheckIfCanContinue(false);
            if (_canContinue.Value) return;
            foreach (Place pl in MyTask.GetPlaces())
            {
                int n = GetMarkingOfPlace(pl.Id);
                if (n > 0 && pl.PlaceType != PlaceTypes.End)
                {
                    log.Warn("Deadlock detected. Token in place {0} cannot be consumed", pl.Id);
                    return;
                }
            }
            log.Info("Composite task finished!");
            DefaultHandleTaskFinished(GetOutputData());
        }*/

        /// <summary>
        /// Check if task execution can continue.
        /// Task can continue if at least one further transition can be enabled.
        /// If a deadlock is detected, DeadlockException is thrown.
        /// </summary>
        /// <returns></returns>
        protected bool CheckIfCanContinue(bool throwOnDeadlock)
        {
            bool foundActive = false;
            foreach (TaskDef tsk in MyTask.Tasks)
            {
                TransitionInfo ti = GetActiveInstanceOfTask(tsk.Id);
                if (ti != null)
                {
                    log.Debug("Skipping already active transition {0}", ti.ToString());
                    foundActive = true;
                    continue;
                }
                if (CanEnableTransition(tsk.Id))
                {
                    log.Info("Transition {0} can be enabled", tsk.Id);
                    return true;
                }   
            }
            if (!foundActive)
            {
                foreach (PlaceDef pl in MyTask.Places)
                {
                    if (pl.PlaceType != PlaceTypes.End && GetMarkingOfPlace(pl.Id) > 0)
                    {
                        log.Warn("Deadlock detected in composite task {0} ({1}) of process {2}", InstanceId, TaskId, ProcessDefinitionId);
                        if (throwOnDeadlock)
                            throw new DeadlockException(this.InstanceId, this.TaskId, this.ProcessDefinitionId); 
                    }
                }
            }
            return false;
        }

        protected string EnableTaskIfPossible(string taskId)
        {
            TransitionInfo ti = GetActiveInstanceOfTask(taskId);
            if (ti != null)
            {
                log.Debug("EnableTaskIfPossible ({1}): Skipping already active transition {0}", ti.ToString(), taskId);
                return null;
            }
            if (!CanEnableTransition(taskId))
            {
                log.Debug("EnableTaskIfPossible: Transition {0} cannot be enabled, skipping", taskId);
                return null;
            }
            log.Info("Enabling transition {0}", taskId);
            string s = EnableTransition(taskId);
            return s;
        }
        
        /// <summary>
        /// Do a single step of task execution
        /// Does not automatically persist task state
        /// </summary>
        /*public void DoOneStep()
        {
            bool enabled = false;
            log.Debug("[DoOneStep start]");
            foreach (TaskDef tsk in MyTask.Tasks)
            {
                lock (this)
                {
                    if (EnableTaskIfPossible(tsk.Id) != null)
                    {
                        _canContinue = null;
                        enabled = true;
                    }
                }
            }
            
            if (!enabled)
            {
                ContinueTaskExecution();
            }
            log.Debug("[KickTokens end. Returning {0}]", enabled);
        }*/

        /// <summary>
        /// Enable specified transition
        /// </summary>
        /// <param name="taskId"></param>
        private string EnableTransition(string taskId)
        {
            lock (this)
            {
                List<string> enablingPlaces;
                bool b = CanEnableTransition(taskId, out enablingPlaces);
                if (!b) throw new Exception("Task cannot be enabled: " + taskId);
                TaskDef tsk = MyTask.GetTask(taskId);
                TransitionInfo ti = new TransitionInfo();
                ti.Status = TransitionStatus.Enabling;
                ti.TaskId = taskId;
                ti.InstanceId = AllocateNewTaskInstanceId(ti.TaskId);
                //TODO: think about automatic handling of enable task timeout 
                //this should be handled by the framework so we don't have to do it here.
                EnableChildTask msg;
                if (tsk.IsMultiInstance)
                {
                    var data = new List<Dictionary<string, object>>(ScriptRuntime.PrepareMultiInstanceTaskInputData(this, tsk, Context));
                    msg = new EnableMultiChildTask {
                        MultiInputData = data
                    };
                }
                else
                {
                    msg = new EnableChildTask {
                        InputData = ScriptRuntime.PrepareChildTaskInputData(this, tsk, Context)
                    };
                }

                //msg.Mode = tsk.AllowSynchronousExec ? MessageHandlingMode.SameTransaction : MessageHandlingMode.AnotherTransaction;
                msg.CorrelationId = ti.InstanceId;
                msg.FromTaskInstanceId = this.InstanceId;
                msg.FromProcessInstanceId = this.ProcessInstanceId;
                msg.ToTaskInstanceId = ti.InstanceId;
                msg.ProcessDefinitionId = ProcessDefinition.DefinitionId;
                msg.TaskId = tsk.Id;
            
                AllTasks.Add(ti);
                Context.EnableChildTask(msg);
                log.Info("Child task {0} created: {1}", taskId, ti.InstanceId);
                return ti.InstanceId;
            }
        }




        
        protected TransitionInfo GetTransitionInfo(string corrId)
        {
            return AllTasks.FirstOrDefault(x => x.InstanceId == corrId);
        }

        
        /// <summary>
        /// Handle notification about child task been cancelled.
        /// What should happen then?
        /// Transition info should be updated to reflect the fact that child task
        /// has been cancelled. However, no tokens should be returned to task's input places,
        /// because it would re-enable the task, and that's not what we want.
        /// If all tasks are cancelled and there will be no more tokens, composite task
        /// will complete. If it cannot complete due to missing output data, it will fail.
        /// </summary>
        /// <param name="tce"></param>
        private void HandleChildTaskCancelled(TaskCancelled tce)
        {
            if (tce.ParentTaskInstanceId != this.InstanceId)
                throw new Exception("Parent task correlation id is incorrect");
            TransitionInfo ti = GetTransitionInfo(tce.FromTaskInstanceId);
            if (ti == null)
                throw new Exception("Child task not found");
            log.Debug("Child task {0} cancelled. Current transition status: {1}", tce.FromTaskInstanceId, ti.Status);

            if (ti.Status == TransitionStatus.Enabling)
            {
                ti.Status = TransitionStatus.Enabled; 
            }
                
            if (ti.Status == TransitionStatus.Cancelled)
            {
                return; //nothing tbd
            }
            else if (ti.Status == TransitionStatus.Started || ti.Status == TransitionStatus.FailedActive)
            {
                ti.Status = TransitionStatus.Cancelled; //do nothing more, the task has consumed the tokens already
            }
            else if (ti.Status == TransitionStatus.Enabled)
            {
                ConsumeTaskInputTokens(ti.InstanceId);
                Debug.Assert(ti.Status == TransitionStatus.Started);
            }
            else if (ti.Status == TransitionStatus.Cancelling)
            {
                //everyth. ok.
            }
            else
            {
                log.Warn("Child task {0} ({1}) cancelled, but current transition status is {2}. Ignoring the notification - status inconsistent", tce.FromTaskInstanceId, ti.TaskId, ti.Status);
                return;
            }
            ti.Status = TransitionStatus.Cancelled;
            if (Status == TaskStatus.Enabled || Status == TaskStatus.Selected)
            {
                ProduceTaskOutputTokens(ti.InstanceId);
            }
            ContinueTaskExecution();
            //OnInternalStatusChanged();
            //DetectTaskCompletion();
        }
        /// <summary>
        /// Handle child task started event
        /// </summary>
        /// <param name="tse"></param>
        private void HandleChildTaskStarted(TaskSelected tse)
        {
            if (tse.ParentTaskInstanceId != this.InstanceId)
                throw new TaskRuntimeException("Parent task correlation id is incorrect").SetInstanceId(InstanceId);
            TransitionInfo ti = GetTransitionInfo(tse.FromTaskInstanceId);
            if (ti == null)
                throw new TaskRuntimeException("Child task not found: " + tse.FromTaskInstanceId).SetInstanceId(InstanceId);
            if (ti.Status == TransitionStatus.Started)
                return;
            if (ti.Status == TransitionStatus.Enabling)
                ti.Status = TransitionStatus.Enabled;
            if (ti.Status == TransitionStatus.Enabled)
            {
                log.Debug("Child task {0} ({1}) started", ti.InstanceId, ti.TaskId);
                OnChildTaskStarted(tse.FromTaskInstanceId);
            }
            else
            {
                log.Warn("Child task {0} ({1}) started, but current transition status is {2}. Ignoring the notification - status inconsistent", tse.FromTaskInstanceId, ti.TaskId, ti.Status);
                return;
            }
            //else throw new Exception("Invalid transition status");
        }

        /// <summary>
        /// Handle child task completed eveent
        /// </summary>
        /// <param name="tce"></param>
        private void HandleChildTaskCompleted(TaskCompleted tce)
        {
            lock (this)
            {
                if (tce.ParentTaskInstanceId != this.InstanceId)
                    throw new TaskRuntimeException("Parent task correlation id is incorrect").SetInstanceId(InstanceId);
                TransitionInfo ti = GetTransitionInfo(tce.FromTaskInstanceId);
                if (ti == null)
                    throw new TaskRuntimeException("Child task not found: " + tce.FromTaskInstanceId).SetInstanceId(InstanceId);
                if (ti.Status == TransitionStatus.Completed)
                    return;
                if (ti.IsTransitionActive)
                {
                    OnChildTaskCompleted(tce);
                }
                else
                {
                    log.Warn("Child task {0} ({1}) completed, but current transition status is {2}. Ignoring the notification - status inconsistent.", tce.FromTaskInstanceId, ti.TaskId, ti.Status);
                    return;
                }
                //else throw new Exception("Invalid transition status");
            }
        }

        /// <summary>
        /// Handle child task failed event.
        /// Warning: TODO: when we have a FailedActive task,
        /// next TaskFailedEvent will set its status to Failed and handle the failure.
        /// TODO: handle a case when the transition is 'cancelling' but the task fails or completes
        /// before nginn cancels it
        /// </summary>
        /// <param name="tfe"></param>
        private void HandleChildTaskFailed(TaskFailed tfe)
        {
            lock (this)
            {
                if (tfe.ParentTaskInstanceId != this.InstanceId)
                    throw new TaskRuntimeException("Parent task correlation id is incorrect").SetInstanceId(InstanceId);
                TransitionInfo ti = GetTransitionInfo(tfe.FromTaskInstanceId);
                if (ti == null)
                    throw new TaskInstanceNotFoundException(tfe.FromTaskInstanceId);
                log.Info("Child task {0} failed: {1}", tfe.FromTaskInstanceId, tfe.ErrorInfo);
                if (ti.Status == TransitionStatus.Failed)
                {
                    return;
                }
                else if (ti.IsTransitionActive) //also if is FailedActive
                {
                    if (ti.Status == TransitionStatus.Enabling)
                        ti.Status = TransitionStatus.Enabled;
                    ConsumeTaskInputTokens(tfe.FromTaskInstanceId);
                    List<FlowDef> handlers = new List<FlowDef>(MyTask.GetTask(ti.TaskId).GetFlowsForPortOut(TaskOutPortType.Error));
                    if (handlers.Count > 0)
                    {
                        ti.Status = TransitionStatus.Failed;
                        log.Info("Continuing with {0} error handlers", ti.TaskId);
                        ProduceTaskOutputTokens(ti.InstanceId);
                    }
                    else
                    {
                        bool isErrorTask = tfe.IsExpected;
                        TaskDef tsk = MyTask.GetTask(ti.TaskId);
                        //if the task is already failed or it has error handlers set up
                        if (isErrorTask || ti.Status == TransitionStatus.FailedActive ||
                            MyTask.GetFlowsForPortOut(TaskOutPortType.Error).FirstOrDefault() != null)
                        {
                            ti.Status = TransitionStatus.Failed;
                            log.Info("Failing current task: {0}!", ti.TaskId, InstanceId);
                            ForceFail(tfe.ErrorInfo); //TODO?? maybe FailInternal()...
                            return;
                        }
                        else
                        {
                            log.Info("No error handlers for task {0} and not bubbling up.", ti.TaskId);
                            ti.Status = TransitionStatus.FailedActive;
                            //RG: v2 TODO 
                            //Context.MessageBus.Notify(new ChildTaskFailedNotification(ti.InstanceId, InstanceId, string.Format("Unexpected task error ({0}/{1}): {2}", ProcessDefinition.DefinitionId, ti.TaskId, tfe.ErrorInfo)));
                        }
                    }
                }
                else
                {
                    log.Warn("Child task {0} ({1}) failed, but current transition status is {2}. Ignoring the notification - status inconsistent", tfe.FromTaskInstanceId, ti.TaskId, ti.Status);
                    return;
                }
                    //throw new Exception("Invalid task status!");
            }
        }

        

        
        

        /// <summary>
        /// Add a token to specified place
        /// </summary>
        /// <param name="placeId"></param>
        public void AddToken(string placeId)
        {
            lock (this)
            {
                int cnt = 0;
                Marking.TryGetValue(placeId, out cnt);
                Marking[placeId] = cnt + 1;
                OnTokenAdded(placeId);
            }
        }

        /// <summary>
        /// Remove token from a place.
        /// Will cancel all transitions that no longer can be enabled after token removal.
        /// </summary>
        /// <param name="placeId"></param>
        protected void RemoveToken(string placeId)
        {
            lock (this)
            {
                int n = GetNumFreeTokens(placeId);
                if (n <= 0)
                    throw new TaskRuntimeException("No more tokens to remove in place: " + placeId).SetInstanceId(InstanceId);
                Marking.Remove(placeId);
                Marking[placeId] = n - 1;
                OnTokenRemoved(placeId);
            }
        }

        protected virtual void OnTokenAdded(string placeId)
        {
            
        }

        protected virtual void OnTokenRemoved(string placeId)
        {
            
        }

        [IgnoreDataMember]
        protected CompositeTaskDef MyTask
        {
            get
            {
                if (ProcessDefinition == null) throw new TaskRuntimeException("Parent process missing. Activate!");
                return (CompositeTaskDef)ProcessDefinition.GetRequiredTask(TaskId);
            }
        }

        protected override void OnTaskEnabling()
        {
            base.OnTaskEnabling();
            AddToken(MyTask.StartPlace.Id);
            ContinueTaskExecution();
        }
        
        /// <summary>
        /// Return number of free (unallocated) tokens in given place
        /// </summary>
        /// <param name="placeId"></param>
        /// <returns></returns>
        protected int GetNumFreeTokens(string placeId)
        {
            int n = 0;
            return Marking.TryGetValue(placeId, out n) ? n : 0;
        }

        /// <summary>
        /// Get total number of tokens (free + allocated for 'STARTED' tasks) in
        /// specified place.
        /// </summary>
        /// <param name="placeId"></param>
        /// <returns></returns>
        protected int GetTotalTokens(string placeId)
        {
            lock (this)
            {
                int n = GetNumFreeTokens(placeId);
                foreach (TaskDef tsk in MyTask.GetPlace(placeId).NodesOut)
                {
                    TransitionInfo ti = GetActiveInstanceOfTask(tsk.Id);
                    if (ti != null && (ti.Status == TransitionStatus.Started || ti.Status == TransitionStatus.FailedActive))
                    {
                        if (ti.AllocatedPlaces.Contains(placeId))
                            n++;
                    }
                }
                return n;
            }
        }

        /// <summary>
        /// Check if there's a barrier set at a given place.
        /// </summary>
        /// <param name="placeId"></param>
        /// <returns></returns>
        protected bool IsBarrierSetAt(string placeId)
        {
#warning TODO
            return false;
            //return Context.Environment.GetBarrier(this.ProcessDefinitionId, placeId);
        }

        /// <summary>
        /// Check if transition has enough input tokens to be enabled.
        /// Warning: doesn't check if the transition has already been enabled.
        /// </summary>
        /// <param name="taskId"></param>
        /// <param name="enablingPlaces"></param>
        /// <returns></returns>
        protected bool CanEnableTransition(string taskId, out List<string> enablingPlaces)
        {
            enablingPlaces = new List<string>();
            TaskDef tsk = ProcessDefinition.GetRequiredTask(taskId);
            if (tsk.JoinType == TaskSplitType.AND)
            {
                foreach (PlaceDef pl in tsk.NodesIn)
                {
                    if (!IsBarrierSetAt(pl.Id) && GetNumFreeTokens(pl.Id) > 0)
                    {
                        enablingPlaces.Add(pl.Id);
                    }
                    else return false;
                }
                return true;
            }
            else if (tsk.JoinType == TaskSplitType.XOR)
            {
                foreach (PlaceDef pl in tsk.NodesIn)
                {
                    if (!IsBarrierSetAt(pl.Id) && GetNumFreeTokens(pl.Id) > 0)
                    {
                        enablingPlaces.Add(pl.Id);
                        return true;
                    }
                }
                return false;
            }
            else if (tsk.JoinType == TaskSplitType.OR)
            {
                foreach (PlaceDef pl in tsk.NodesIn)
                {
                    if (!IsBarrierSetAt(pl.Id) && GetNumFreeTokens(pl.Id) > 0)
                    {
                        enablingPlaces.Add(pl.Id);
                    }
                }
                if (enablingPlaces.Count == 0)
                {
                    return false; //no input tokens for OR join
                }
                ///now check the OrJoinCheckList. If there are tokens in places from the list,
                ///don't enable the transition - we have to wait until all the tokens disappear from 
                ///these places.
                foreach (string plid in tsk.OrJoinCheckList)
                {
                    PlaceDef pl = MyTask.GetPlace(plid);
                    if (tsk.NodesIn.Contains(pl)) continue;
                    if (GetTotalTokens(plid) > 0)
                    {
                        log.Info("OR join not enabled: token in {0}", plid);
                        return false;
                    }
                }
                return true;
            }
            else throw new Exception();
        }

        protected bool CanEnableTransition(string taskId)
        {
            List<string> enablingPlaces;
            return CanEnableTransition(taskId, out enablingPlaces);
        }


        
        /// <summary>
        /// will execute first step - consume task input tokens
        /// and put the transition in 'Started' state
        /// second step is producing proper output tokens.
        /// Warning: TODO: FailedActive transition is treated just like Started
        /// </summary>
        /// <param name="instanceId"></param>
        private void ConsumeTaskInputTokens(string instanceId)
        {
            TransitionInfo ti = GetTransitionInfo(instanceId);
            Debug.Assert(ti != null);
            Debug.Assert(ti.IsTransitionActive);
            if (ti.Status == TransitionStatus.Enabling) ti.Status = TransitionStatus.Enabled;
            if (ti.Status == TransitionStatus.Enabled) //start the task first...
            {
                OnChildTaskStarted(ti.InstanceId);
            }
            if (!(ti.Status == TransitionStatus.Started || ti.Status == TransitionStatus.FailedActive))
            {
                throw new Exception("Invalid transition status " + instanceId + ", status is " + ti.Status.ToString());
            }
        }


        private void ExecuteFlow(string instanceId, FlowDef fl)
        {
            TransitionInfo ti = GetTransitionInfo(instanceId);
            if (ti.TaskId != fl.From) throw new Exception();
            if (fl.IsCancelling)
            {
                RemoveAllTokensInPlace(fl.To);
            }
            else
            {
                AddToken(fl.To);
            }
        }

        /// <summary>
        /// Starts compensation run by producing tokens on the 'compensate' output of 
        /// all completed tasks.
        /// </summary>
        private void StartCompensationOfCompletedTasks()
        {
            if (this.Status != TaskStatus.Cancelling) throw new Exception();
            
            var completedTasks = AllTasks.Where(ti => ti.Status == TransitionStatus.Completed).Select(ti => ti.TaskId).Distinct();
            foreach (string id in completedTasks)
            {
                TaskDef td = MyTask.GetTask(id);
                foreach (FlowDef fd in td.GetFlowsForPortOut(TaskOutPortType.Compensate))
                {
                    if (fd.IsCancelling)
                    {
                        log.Info("Compensation removing all tokens at {0}", fd.To);
                        RemoveAllTokensInPlace(fd.To); //this one is tricky - what tokens do we remove when we have removed all tokens prior to compensation?
                        //answer: removing tokens that were genereated as a part of compensation
                    }
                    else
                    {
                        log.Info("Compensating token placed at {0}", fd.To);
                        AddToken(fd.To); //generate compensating token
                    }
                }
            }
        }

        /// <summary>
        /// Handle task completion - no matter if its a normal completion,
        /// failure or cancellation. Execute appropriate output flows.
        /// </summary>
        /// <param name="instanceId"></param>
        private void ProduceTaskOutputTokens(string instanceId)
        {
            TransitionInfo ti = GetTransitionInfo(instanceId);
            Debug.Assert(ti != null);
            Debug.Assert(ti.Status == TransitionStatus.Completed || ti.Status == TransitionStatus.Cancelled || ti.Status == TransitionStatus.Failed);
            TaskDef tsk =  MyTask.GetTask(ti.TaskId);
            if (ti.Status == TransitionStatus.Completed)
            {
                List<FlowDef> normalFlows = new List<FlowDef>();
                List<FlowDef> cancellingFlows = new List<FlowDef>();
                foreach(FlowDef fl in tsk.GetFlowsForPortOut(TaskOutPortType.Out))
                {
                    if (fl.IsCancelling)
                        cancellingFlows.Add(fl);
                    else 
                        normalFlows.Add(fl);
                }
                if (normalFlows.Count == 0)
                {
                    throw new ProcessDefinitionException(MyTask.ParentProcess.DefinitionId, ti.TaskId, "No out flow");
                }
                ///always eval all cancellations 
                foreach(FlowDef fl in cancellingFlows)
                {
                    ExecuteFlow(instanceId, fl);
                }

                if (tsk.SplitType == TaskSplitType.AND)
                {
                    foreach (FlowDef fl in normalFlows)
                    {
                        Debug.Assert(fl.InputCondition == null || fl.InputCondition.Length == 0);
                        ExecuteFlow(instanceId, fl);
                    }
                }
                else if (tsk.SplitType == TaskSplitType.OR)
                {
                    int cnt = 0;
                    foreach (FlowDef fl in normalFlows)
                    {
                        if (EvaluateFlowInputCondition(fl))
                        {
                            cnt++;
                            ExecuteFlow(instanceId, fl);
                        }
                        if (cnt == 0)
                        {
                            ExecuteFlow(instanceId, normalFlows[normalFlows.Count - 1]);
                        }
                    }
                }
                else if (tsk.SplitType == TaskSplitType.XOR)
                {
                    bool b = false;
                    for (int i = 0; i < normalFlows.Count - 1; i++)
                    {
                        FlowDef fl = normalFlows[i];
                        if (fl.InputCondition != null)
                        {
                            if (EvaluateFlowInputCondition(fl))
                            {
                                ExecuteFlow(instanceId, fl);
                                b = true;
                                break;
                            }
                        }
                        else
                            log.Warn("NULL flow input condition in XOR split. Flow: {0} in {1}", fl.ToString(), ProcessDefinition.DefinitionId);
                    }
                    if (!b)
                    {
                        ExecuteFlow(instanceId, normalFlows[normalFlows.Count - 1]);
                    }
                }
                else throw new Exception();
            }
            else if (ti.Status == TransitionStatus.Failed)
            {
                var fls = MyTask.GetTask(ti.TaskId).GetFlowsForPortOut(TaskOutPortType.Error);
                foreach (FlowDef fl in fls)
                {
                    Debug.Assert(fl.InputCondition == null || fl.InputCondition.Length == 0);
                    ExecuteFlow(instanceId, fl);
                }
            }
            else if (ti.Status == TransitionStatus.Cancelled)
            {
                var fls = MyTask.GetTask(ti.TaskId).GetFlowsForPortOut(TaskOutPortType.Cancel);
                foreach (FlowDef fl in fls)
                {
                    Debug.Assert(string.IsNullOrEmpty(fl.InputCondition));
                    ExecuteFlow(instanceId, fl);
                }
            }
            else throw new Exception();
        }

        /// <summary>
        /// Invoked when a child task completes. Executes task output data bindings
        /// and moves the tokens.
        /// TODO: handle a case when the transition is 'cancelling' but the task fails or completes
        /// before nginn cancels it
        /// </summary>
        /// <param name="tce"></param>
        private void OnChildTaskCompleted(TaskCompleted tce)
        {
            log.Debug("Child task completed: {0}", tce.FromTaskInstanceId);
            TransitionInfo ti = GetTransitionInfo(tce.FromTaskInstanceId);
            TaskDef tsk = MyTask.GetTask(ti.TaskId);
            if (ti.Status == TransitionStatus.Enabling)
                ti.Status = TransitionStatus.Enabled;

            if (ti.Status == TransitionStatus.Enabled || ti.Status == TransitionStatus.Started)
            {
                ConsumeTaskInputTokens(tce.FromTaskInstanceId);
                if (tce is MultiTaskCompleted)
                {
                    MultiTaskCompleted mce = (MultiTaskCompleted)tce;
                    if (!string.IsNullOrEmpty(tsk.MultiInstanceResultsBinding) && mce.MultiOutputData != null)
                    {
                        TaskData[tsk.MultiInstanceResultsBinding] = mce.MultiOutputData;
                    }
                }
                else
                {
                    if (tce.OutputData != null)
                    {
                        ScriptRuntime.ExecuteChildTaskOutputDataBinding(this, tsk, tce.OutputData, Context);
                    }
                }
                ti.Status = TransitionStatus.Completed;
                ProduceTaskOutputTokens(ti.InstanceId);
            }
            else
            {
                ti.Status = TransitionStatus.Completed;
                log.Warn("Task {0} has completed, but the transition status was {1} and it did not produce output tokens or execute data bindings", ti.InstanceId, ti.Status);
            }
        }

        /// <summary>
        /// Evaluate input condition for a flow
        /// </summary>
        /// <param name="fl"></param>
        /// <returns></returns>
        protected bool EvaluateFlowInputCondition(FlowDef fl)
        {
            return this.ScriptRuntime.EvalFlowCondition(this, fl, Context);
        }

        /// <summary>
        /// Invoked when a child task starts
        /// </summary>
        /// <param name="ev"></param>
        protected virtual void OnChildTaskStarted(string childInstanceId)
        {
            TransitionInfo ti = GetTransitionInfo(childInstanceId);
            if (ti.Status != TransitionStatus.Enabled)
                throw new Exception();
            TaskDef tsk = MyTask.GetTask(ti.TaskId);
            List<string> enablingPlaces;
            bool b = CanEnableTransition(ti.TaskId, out enablingPlaces);
            if (!b) throw new TaskRuntimeException("Error: child task started, invalid parent task state. Probably an error in process definition").SetInstanceId(InstanceId).SetTaskAndProcessDef(ProcessDefinitionId, TaskId);
            foreach (string plid in enablingPlaces)
            {
                ConsumeToken(plid, childInstanceId);
            }
            ti.SetAllocatedPlaces(enablingPlaces);
            ti.Status = TransitionStatus.Started;
        }

        /// <summary>
        /// Consume a token from specified place.
        /// Cancels all other transitions if they no longer can be enabled.
        /// </summary>
        /// <param name="placeId">Id of place with the token</param>
        /// <param name="childInstanceId">Id of task instance consuming the token</param>
        private void ConsumeToken(string placeId, string childInstanceId)
        {
            log.Info("ConsumeToken: removing token from {0}", placeId);
            RemoveToken(placeId);
            PlaceDef pl = MyTask.FindPlace(placeId);
            foreach (TaskDef tsk in pl.NodesOut)
            {
                TransitionInfo ti = GetActiveInstanceOfTask(tsk.Id);
                if (ti == null)
                    continue;
                if (ti.InstanceId == childInstanceId)
                    continue;
                if (ti.Status == TransitionStatus.Started || ti.Status == TransitionStatus.FailedActive || ti.Status == TransitionStatus.Cancelling)
                    continue;
                Debug.Assert(ti.Status == TransitionStatus.Enabled || ti.Status == TransitionStatus.Enabling);
                bool b = CanEnableTransition(tsk.Id);
                if (!b)
                {
                    log.Info("Transition {0} no longer can be enabled. Cancelling.", ti.ToString());
                    CancelTransition1(ti.InstanceId);
                    //CancelTransition(ti.InstanceId, true);
                }
                else
                    log.Info("Transition {0} still can be enabled", ti.ToString());
            }
        }

        /// <summary>
        /// Cancellation - remove all tokens in specified place.
        /// For enabled tasks - simple. Check if they can still be enabled after removing the token.
        /// For started tasks: Check if they can still be enabled after removing the token. If not,
        /// it means they have to have the place in allocated tokens. Need to be cancelled.
        /// TODO: do something with FailedActive tasks
        /// 
        /// </summary>
        /// <param name="placeId"></param>
        private void RemoveAllTokensInPlace(string placeId)
        {
            lock (this)
            {
                PlaceDef pl = MyTask.GetPlace(placeId);
                log.Info("Removing all tokens from {0}", placeId);
                foreach (TaskDef tsk in pl.NodesOut)
                {
                    while (GetNumFreeTokens(pl.Id) > 0)
                    {
                        RemoveToken(pl.Id);
                    }
                    TransitionInfo ti = GetActiveInstanceOfTask(tsk.Id);
                    if (ti == null) continue;
                    if (ti.Status == TransitionStatus.Cancelling)
                    {
                        //has already given its tokens back
                        continue;
                    }
                    bool cancel = false;
                    if (ti.Status == TransitionStatus.Started || ti.Status == TransitionStatus.FailedActive)
                    {
                        //started transition needs to be cancelled if it has allocated a token from current place
                        if (ti.AllocatedPlaces.Contains(pl.Id))
                            cancel = true;
                    }
                    //and enabled transition will be cancelled if it cannot stay enabled
                    if (!cancel && !CanEnableTransition(tsk.Id))
                        cancel = true;
                    if (cancel)
                    {
                        log.Info("Cancelling transition {0} ({1})", ti.InstanceId, tsk.Id);
                        CancelTransition1(ti.InstanceId);
                        //CancelTransition(ti.InstanceId, true);
                    }
                }
            }
        }

        /// <summary>
        /// Restarts a transition. How? by cancelling current instance, returning tokens to input
        /// places and re-enabling the transition. This works only for active (enabled or started) transitions
        /// and for FailedActive ones
        /// </summary>
        /// <param name="tid">child transition instance ID</param>
        public void RestartChildTransition(string tid)
        {
            log.Info("Restarting child transition {0}", tid);
            lock (this)
            {
                TransitionInfo ti = GetTransitionInfo(tid);
                if (ti == null) throw new TaskInstanceNotFoundException(tid);
                CancelTransition(tid, false);
                Debug.Assert(ti.Status == TransitionStatus.Cancelled);
                string newTi = EnableTaskIfPossible(ti.TaskId);
                log.Info("Restarted transition {0} ({1}). New instance ID: {2}", ti.InstanceId, ti.TaskId, newTi);
            }
        }

        /// <summary>
        /// TODO: use that!
        /// New version of cancellation routine. This one consists of two parts: first part initiates the cancellation by
        /// sending CancelTaskMessage. TransitionInfo is put into 'Cancelling' state. 
        /// The second part is run when TaskCancelled message arrives from the child task that has been cancelled.
        /// </summary>
        /// <param name="instanceId"></param>
        private void CancelTransition1(string instanceId)
        {
            TransitionInfo ti = GetTransitionInfo(instanceId);
            if (ti == null) throw new TaskRuntimeException("Invalid instance Id").SetInstanceId(InstanceId).SetTaskId(TaskId).SetProcessDef(ProcessDefinitionId);
            log.Info("Cancelling transition {0}", ti.ToString());
            if (!ti.IsTransitionActive) throw new TaskRuntimeException("Invalid transition status").SetInstanceId(InstanceId);
            if (ti.Status == TransitionStatus.Cancelling)
            {
                return;
            }
            ti.Status = TransitionStatus.Cancelling;
            Context.CancelChildTask(new CancelTask
            {
                FromProcessInstanceId = this.ProcessInstanceId,
                FromTaskInstanceId = this.InstanceId,
                ToTaskInstanceId = ti.InstanceId,
                Reason = ""
            });
            //Context.MessageBus.Notify(new object[] {ctm, new ScheduledMessage(ctt, DateTime.Now.AddHours(24))});
        }

        

        /// <summary>
        /// Cancel active transition. 
        /// Returns tokens to input places if the transition has been STARTED.
        /// Handles generation of output tokens for 'Cancel' outgoing flows
        /// </summary>
        /// <param name="instanceId"></param>
        private void CancelTransition(string instanceId, bool produceCancelOutTokens)
        {
            TransitionInfo ti = GetTransitionInfo(instanceId);
            if (ti == null) throw new TaskRuntimeException("Invalid instance Id").SetInstanceId(InstanceId).SetTaskId(TaskId).SetProcessDef(ProcessDefinitionId);
            log.Info("Cancelling transition {0}", ti.ToString());
            if (!ti.IsTransitionActive) throw new TaskRuntimeException("Invalid transition status").SetInstanceId(InstanceId);
            if (ti.Status == TransitionStatus.Started || ti.Status == TransitionStatus.FailedActive)
            {
                //return tokens to input places
                foreach (string plid in ti.AllocatedPlaces)
                {
                    AddToken(plid);
                }
            }
            bool doCancel = ti.Status == TransitionStatus.Started || ti.Status == TransitionStatus.Enabled || ti.Status == TransitionStatus.Enabling;
            ti.Status = TransitionStatus.Cancelled;
            if (doCancel)
            {
#warning: don't we have to handle TransitionStatus.Cancelling here (hm, why? maybe we don't care?)
                Context.CancelChildTask(new CancelTask
                {
                    FromProcessInstanceId = this.ProcessInstanceId,
                    FromTaskInstanceId = this.InstanceId,
                    ToTaskInstanceId = ti.InstanceId,
                    Reason = ""
                });
            }
            ///we're producing cancel out tokens even if task hasn't really been cancelled (it was failed before cancelling)
            if (produceCancelOutTokens)
            {
                ProduceTaskOutputTokens(ti.InstanceId);
            }
        }

        /// <summary>
        /// Cancel a composite task.
        /// Cancels every active task instance in the composite task.
        /// Completed tasks are not touched.
        /// </summary>
        public override void Cancel(string reason)
        {
            lock (this)
            {
                if (Status == TaskStatus.Cancelling || Status == TaskStatus.Cancelled)
                {
                    return; //do nothing.
                }
                if (Status != TaskStatus.Enabled && Status != TaskStatus.Selected)
                    throw new InvalidTaskStatusException("Invalid task status").SetInstanceId(InstanceId);
                Status = TaskStatus.Cancelling;
                foreach (TransitionInfo ti in this.ActiveTasks)
                {
                    if (ti.Status != TransitionStatus.Cancelling)
                        CancelTransition1(ti.InstanceId); //todo: no need to put timeout for each cancellation here
                    //CancelTransition(ti.InstanceId, false);
                }
                //now remove all tokens and start compensating flow
                Marking = new Dictionary<string, int>();
                StartCompensationOfCompletedTasks();
                ContinueTaskExecution();
            }
        }

        
        
        /// <summary>
        /// Force-complete the composite task.
        /// All currently active tasks in it will be cancelled, then data will be updated.
        /// TODO: implement
        /// </summary>
        /// <param name="finishedBy"></param>
        /// <param name="updatedData"></param>
        public override void ForceComplete(Dictionary<string, object> updatedData)
        {
            if (Status != TaskStatus.Enabled &&
            Status != TaskStatus.Selected)
                throw new InvalidTaskStatusException(InstanceId, "Invalid task status");

            foreach (TransitionInfo ti in this.ActiveTasks)
            {
                if (ti.Status != TransitionStatus.Cancelling)
                    CancelTransition1(ti.InstanceId);
                //CancelTransition(ti.InstanceId, false);
            }
            Marking = new Dictionary<string, int>();
            ContinueTaskExecution();    
        }

        /// <summary>
        /// Force-fail composite task
        /// Warning: this will cancel all currently active subtasks without producing tokens on their 
        /// cancellation handlers. Compensation logic won't be run too. We just kill the task ignoring process logic.
        /// </summary>
        /// <param name="errorInformation"></param>
        public override void ForceFail(string errorInformation)
        {
            lock (this)
            {
                if (Status != TaskStatus.Enabled &&
                Status != TaskStatus.Selected)
                    throw new InvalidTaskStatusException(InstanceId, "Invalid task status");

                foreach (TransitionInfo ti in this.ActiveTasks)
                {
                    if (ti.Status != TransitionStatus.Cancelling) CancelTransition1(ti.InstanceId);
                }
                Marking = new Dictionary<string, int>();
                DefaultHandleTaskFailure(errorInformation, true);
            }
        }

        private Dictionary<string, object> ExecuteInputDataBindings(TaskDef tsk)
        {
            return this.ScriptRuntime.PrepareChildTaskInputData(this, tsk, this.Context);
        }


        /// <summary>
        /// Execute data bindings for multi-instance task
        /// Returns array of data records - results of input data binding for each task 
        /// instance
        /// </summary>
        /// <param name="taskId"></param>
        /// <returns></returns>
        private IEnumerable<Dictionary<string, object>> GetDataForMultiInstanceTask(string taskId)
        {
            return ScriptRuntime.PrepareMultiInstanceTaskInputData(this, MyTask.GetTask(taskId), Context);
        }

        /// <summary>
        /// Prepare input data for single-instance child task by executing its input data bindings.
        /// </summary>
        /// <param name="taskId"></param>
        /// <returns></returns>
        protected Dictionary<string, object> PrepareDataForChildTask(string taskId)
        {
            return ScriptRuntime.PrepareChildTaskInputData(this, MyTask.GetTask(taskId), Context);
        }

        #region IMessageConsumer<TaskStartedEvent> Members

        private void Handle(TaskSelected message)
        {
            HandleChildTaskStarted(message);
        }

        #endregion

        #region IMessageConsumer<TaskFailedEvent> Members

        private void Handle(TaskFailed message)
        {
            HandleChildTaskFailed(message);
        }

        #endregion

        #region IMessageConsumer<TaskCompletedEvent> Members

        private void Handle(TaskCompleted message)
        {
            HandleChildTaskCompleted(message);
        }

        #endregion

        #region IMessageConsumer<TaskCancelled> Members

        private void Handle(TaskCancelled message)
        {
            HandleChildTaskCancelled(message);
        }

        #endregion


        /// <summary>
        /// creates a new task instance id for a child task.
        /// The id generated should be unique.
        /// This way composite tasks can control what ids are assigned to child tasks.
        /// </summary>
        /// <param name="taskId"></param>
        /// <returns></returns>
        protected string AllocateNewTaskInstanceId(string taskId)
        {
            int n;
            lock (this)
            {
                n = ++ChildCounter;
            }
            string s = string.Format("{0}.{1}", InstanceId, n);
            if (this.GetTransitionInfo(s) != null) throw new Exception("Child task instance ID is duplicate. Oh my!");
            return s;
        }

        [DataMember]
        public int ChildCounter { get; set; }

        

        #region IMessageConsumer<EnableTaskTimeout> Members

        /* RG: not here!
         * public void Handle(EnableTaskTimeout message)
        {
            RequireActivation(true);
            lock (this)
            {
                string corrid = message.CorrelationId;
                if (string.IsNullOrEmpty(corrid))
                {
                    log.Info("No correlation id. Ignoring the message");
                    return;
                }
                TransitionInfo ti = GetTransitionInfo(corrid);
                if (ti == null) throw new TaskRuntimeException("Child transition not found").SetInstanceId(InstanceId);
                if (ti.Status == TransitionStatus.Enabling)
                {
                    //TODO: some cleanup, ye
                    Fail(string.Format("Child task enable timed out: {0}", ti.TaskId));
                }
                else
                {
                    log.Debug("Ignoring message {0}", message);
                }
            }
        }
        */
        #endregion

        #region IMessageConsumer<TaskEnabled> Members

        /// <summary>
        /// Handle child task enabled message
        /// </summary>
        /// <param name="message"></param>
        private void Handle(TaskEnabled message)
        {
            RequireActivation(true);
            if (message.ParentTaskInstanceId != this.InstanceId)
                throw new Exception("Parent task correlation id is incorrect");
            lock (this)
            {
                string childId = string.IsNullOrEmpty(message.CorrelationId) ? message.FromTaskInstanceId : message.CorrelationId;
                TransitionInfo ti = GetTransitionInfo(childId);
                if (ti == null) throw new TaskRuntimeException(string.Format("Child transition not found: {0}", childId)).SetInstanceId(InstanceId);
                if (ti.Status == TransitionStatus.Enabling)
                {
                    ti.Status = TransitionStatus.Enabled;
                    if (ti.InstanceId != message.FromTaskInstanceId)
                    {
                        //TODO: what if we don't get TaskEnabled, but TaskCompleted as the first event?
                        //we need to handle InstanceId change too!!
                        log.Info("New task instance ID changed {0}->{1}", ti.InstanceId, message.FromTaskInstanceId);
                        ti.InstanceId = message.FromTaskInstanceId;
                    }
                }
                else
                {
                    log.Debug("Ignoring message {0}", message);
                }
            }
        }

        #endregion

        public override void HandleTaskExecEvent(TaskExecEvent ev)
        {
            base.HandleTaskExecEvent(ev);
            HandleChildTaskEvent(ev);
            ContinueTaskExecution();
        }

        protected void HandleChildTaskEvent(TaskExecEvent ev)
        {
            if (ev.ParentTaskInstanceId != this.InstanceId) throw new Exception("invalid instance id");
            var ti = GetTransitionInfo(ev.FromTaskInstanceId);
            if (ti == null) throw new Exception("Child transition not found: " + ev.FromTaskInstanceId);
            if (ev is TaskEnabled)
            {
                Handle((TaskEnabled)ev);
                return;
            }
            else if (ev is TaskCancelled)
            {
                Handle((TaskCancelled)ev);
                return;
            }
            else if (ev is TaskCompleted)
            {
                Handle((TaskCompleted)ev);
                return;
            }
            else if (ev is TaskFailed)
            {
                Handle((TaskFailed)ev);
                return;
            }
            else if (ev is TaskSelected)
            {
                Handle((TaskSelected)ev);
                return;
            }
            else throw new Exception();
        }
        
        /// <summary>
        /// Child task cancellation timed out
        /// TODO implement
        /// </summary>
        /// <param name="message"></param>
        /*
        public void Handle(CancelTaskTimeout message)
        {
            if (this.Status == TaskStatus.Completed ||
                this.Status == TaskStatus.Failed ||
                this.Status == TaskStatus.Cancelled)
            {
                return;
            }
            if (string.IsNullOrEmpty(message.ChildInstanceId)) return;
            var ti = this.GetTransitionInfo(message.ChildInstanceId);
            if (ti.Status == TransitionStatus.Cancelling)
            {
                log.Info("Child task {1} ({0}) cancellation timed out. ", ti.InstanceId, ti.TaskId);
#warning TODO
                HandleChildTaskCancelled(new TaskCancelled { });
            }
            else
            {
                log.Debug("Child task {1} ({0}) cancellation timeout - ignoring because the transition is not in cancelling. ", ti.InstanceId, ti.TaskId);
            }
        }*/

        public override string ToString()
        {
            var state = new Dictionary<string, object>
            { { "Marking", this.Marking },
                { "ActiveTasks", this.ActiveTasks },
                {"Status", this.Status},
                {"InstanceId", this.InstanceId},
                {"TaskId", this.TaskId},
                {"Data", this.TaskData},
                {"ProcessDefinition", this.ProcessDefinitionId},
                {"ProcessInstanceId", this.ProcessInstanceId}
            };
            return Jsonizer.ToJsonString(state);
        }
    }
}
