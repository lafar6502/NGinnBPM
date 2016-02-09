using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NGinnBPM.ProcessModel;
using NGinnBPM.ProcessModel.Data;
using NGinnBPM.Runtime.Tasks;
using BL = Boo.Lang;
using SC = System.Collections;
using AST = Boo.Lang.Compiler.Ast;

namespace NGinnBPM.Runtime.ProcessDSL
{
    /// <summary>
    /// Base class for process definition DSL
    /// </summary>
    public abstract class ProcessDefDSLBase
    {
        protected abstract void Prepare();

        public ProcessDefDSLBase(string scriptUrl)
        {
            log.Info("url: {0}", scriptUrl);
        }

        public ProcessDef GetProcessDef()
        {
            _curProcessDef = new ProcessDef();
            string name = this.GetType().FullName;
            int idx = name.LastIndexOf('.');
            if (idx < 0) throw new Exception("Invalid type name");
            _curProcessDef.PackageName = Package == null ? null : Package.Name;
            _curProcessDef.ProcessName = name.Substring(0, idx);
            _curProcessDef.Version = Int32.Parse(name.Substring(idx + 1));
            _curProcessDef.Version = 1;
            _curProcessDef.DataTypes = new TypeSet();
            _currentCompositeTask = new CompositeTaskDef
            {
                Id = _curProcessDef.ProcessName
            };
            _curProcessDef.Body = _currentCompositeTask;
            Prepare();
            _curProcessDef.FinishModelBuild();
            return _curProcessDef;
        }

        protected static readonly string required = "required";
        protected static readonly string isArray = "array";
        protected static readonly string dir = "dir";
        protected static VariableDef.Dir input = VariableDef.Dir.In;
        protected static VariableDef.Dir output = VariableDef.Dir.Out;
        protected static VariableDef.Dir local = VariableDef.Dir.Local;
        protected static VariableDef.Dir in_out = VariableDef.Dir.InOut;
        protected static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
        /// <summary>
        /// Task input data, set when enabling the task.
        /// </summary>
        protected BL.IQuackFu InputData { get; set; }
        /// <summary>
        /// current task's data
        /// </summary>
        protected BL.IQuackFu TaskData { get; set; }
        /// <summary>
        /// Output data contains task output data when binding to parent task variables
        /// (it is set on task completion only)
        /// </summary>
        protected BL.IQuackFu OutputData { get; set; }
        /// <summary>
        /// Item contains a data record when iterating multi-instance input data
        /// </summary>
        protected BL.IQuackFu Item { get; set; }
        /// <summary>
        /// ParentData is an alias for TaskData
        /// </summary>
        protected BL.IQuackFu ParentData
        {
            get { return TaskData; }
        }
        [BL.DuckTyped]
        protected TaskInstance Task { get; set; }
        /// <summary>
        /// Task's document (actual type depends on document repository provided)
        /// </summary>
        [BL.DuckTyped]
        protected object Document { get; set; }

        protected ITaskExecutionContext Context { get; set; }
        internal Dictionary<string, Func<bool>> _flowConditions = new Dictionary<string, Func<bool>>();
        internal Dictionary<string, Func<object>> _variableBinds = new Dictionary<string, Func<object>>();
        internal Dictionary<string, Action> _taskScripts = new Dictionary<string, Action>();
        public IProcessPackage Package { get; set; }

        private ProcessDef _curProcessDef = null;

        #region runtime data initialization
        public void SetTaskInstanceInfo(TaskInstance ti, ITaskExecutionContext ctx)
        {
            this.Task = ti;
            this.TaskData = new QuackTaskDataWrapper(ti.TaskData);
            this.Context = ctx;
        }

        public void SetInputData(Dictionary<string, object> data)
        {
            InputData = new QuackTaskDataWrapper(data);
        }

        public void SetOutputData(Dictionary<string, object> data)
        {
            OutputData = new QuackTaskDataWrapper(data);
        }

        public void SetItem(object v)
        {
            if (v is SC.IDictionary)
            {
                Item = new QuackTaskDataWrapper((SC.IDictionary)v);
            }
            else if (v is Dictionary<string, object>)
            {
                Item = new QuackTaskDataWrapper((Dictionary<string, object>)v);
            }
            else 
            {
                Item = new QuackTaskDataWrapper(new Dictionary<string, object> {
                    {"Value", v}
                });
            }
        }


        #endregion
        #region process_data_types

        protected void process_data_types(Action act)
        {
            
        }

        protected void define_enum(string name, SC.IEnumerable values)
        {
            _curProcessDef.DataTypes.AddType(new EnumDef { Name = name, EnumValues = new List<string>(values.Cast<string>()) });
        }

        private StructDef _curStructDef;
        protected void define_struct(string name, Action act)
        {
            _curStructDef = new StructDef { Name = name };
            act();
            _curProcessDef.DataTypes.AddType(_curStructDef);
            _curStructDef = null;
        }

        protected void member(string name, string type)
        {
            member(name, type, new string[] { });
        }

        protected void member(string name, string type, params string[] options)
        {
            if (_curStructDef == null) throw new Exception("member allowed only in define_struct");
            MemberDef md = new MemberDef { Name = name, TypeName = type };
            _curStructDef.Members.Add(md);
        }
        #endregion //process_data_types

        #region tasks

        protected static T GetOption<T>(SC.IDictionary options, string name, T defVal)
        {
            if (options == null || !options.Contains(name)) return defVal;
            object v = options[name];
            if (typeof(T).IsAssignableFrom(v.GetType())) return (T) v;
            if (typeof(T).IsEnum)
            {
                return (T) Enum.Parse(typeof(T), Convert.ToString(v), true);
            }
            else return (T) Convert.ChangeType(v, typeof(T));
        }

        protected void variable(string name, string type, SC.IDictionary options)
        {
            variable(name, type, delegate()
            {
                this.options(options);
            });
        }


        private VariableDef _curVar;
        protected void variable(string name, string type, Action act)
        {
            _curVar = new VariableDef { Name = name, TypeName = type };
            act();
            if (_curTask != null)
            {
                if (_curTask.Variables == null) _curTask.Variables = new List<VariableDef>();
                _curTask.Variables.Add(_curVar);
            }
            else
            {
                if (_currentCompositeTask.Variables == null) _currentCompositeTask.Variables = new List<VariableDef>();
                _currentCompositeTask.Variables.Add(_curVar);
            }
            _curVar = null;
        }

        protected void variable_default(Func<object> f, string codeString)
        {
            string k = DslUtil.TaskVariableDefaultKey(_curTask == null ? _currentCompositeTask.Id : _curTask.Id, _curVar.Name);
            _curVar.DefaultValueExpr = codeString;
            _variableBinds[k] = f;
        }

        [BL.Meta]
        public static AST.Expression default_value(AST.Expression expr)
        {
            AST.BlockExpression condition = new AST.BlockExpression();
            condition.Body.Add(new AST.ReturnStatement(expr));
            return new AST.MethodInvocationExpression(new AST.ReferenceExpression("variable_default"), condition, new AST.StringLiteralExpression(expr.ToCodeString()));
        }

        protected void input_bindings_code(Func<SC.IDictionary> fun, string codeStr)
        {
            TaskDef tsk = _curTask != null ? _curTask : (TaskDef)_currentCompositeTask;
            string k = DslUtil.TaskInputDataBindingKey(tsk.Id);
            _variableBinds[k] = fun;
        }

        protected void output_bindings_code(Func<SC.IDictionary> fun, string codeStr)
        {
            TaskDef tsk = _curTask != null ? _curTask : (TaskDef)_currentCompositeTask;
            string k = DslUtil.TaskOutputDataBindingKey(tsk.Id);
            _variableBinds[k] = fun;
        }
        
        /// <summary>
        /// Complete binding of task input data
        /// </summary>
        /// <param name="expr"></param>
        /// <returns></returns>
        [BL.Meta]
        protected static  AST.Expression task_input_data(AST.Expression expr)
        {
            AST.BlockExpression condition = new AST.BlockExpression();
            condition.Body.Add(new AST.ReturnStatement(expr));
            return new AST.MethodInvocationExpression(new AST.ReferenceExpression("input_bindings_code"), condition, new AST.StringLiteralExpression(expr.ToCodeString()));
        }

        /// <summary>
        /// Complete binding of task output data
        /// </summary>
        /// <param name="expr"></param>
        /// <returns></returns>
        [BL.Meta]
        protected static AST.Expression task_output_data(AST.Expression expr)
        {
            AST.BlockExpression condition = new AST.BlockExpression();
            condition.Body.Add(new AST.ReturnStatement(expr));
            return new AST.MethodInvocationExpression(new AST.ReferenceExpression("output_bindings_code"), condition, new AST.StringLiteralExpression(expr.ToCodeString()));
        }

        

        protected void variable_input_binding(Func<object> f, string codeString)
        {
            if (_curVar == null) throw new Exception("input_binding only in variable def");
            string k = DslUtil.TaskVarInBindingKey(_curTask != null ? _curTask.Id : _currentCompositeTask.Id, _curVar.Name);
            var b = new DataBindingDef
                {
                    BindType = DataBindingType.Expr,
                    Source = codeString,
                    Target = _curVar.Name
                };
            _variableBinds[k] = f;
            if (_curTask != null)
            {
                _curTask.AddInputDataBinding(b);
            }
            else if (_currentCompositeTask != null)
            {
                _currentCompositeTask.AddInputDataBinding(b);
            }
            else throw new Exception();
        }

        [BL.Meta]
        protected static AST.Expression input_binding(AST.Expression expr)
        {
            AST.BlockExpression condition = new AST.BlockExpression();
            condition.Body.Add(new AST.ReturnStatement(expr));
            return new AST.MethodInvocationExpression(new AST.ReferenceExpression("variable_input_binding"), condition, new AST.StringLiteralExpression(expr.ToCodeString()));
        }

        protected void add_output_binding(string destinationVariable, Func<object> expression, string codeString)
        {
            var b = new DataBindingDef
            {
                BindType = DataBindingType.Expr,
                Source = codeString,
                Target = destinationVariable
            };
            var k = DslUtil.TaskVarOutBindingKey(_curTask != null ? _curTask.Id : _currentCompositeTask.Id, destinationVariable);
            _variableBinds[k] = expression;
            if (_curTask != null)
            {
                _curTask.AddOutputDataBinding(b);
            }
            else if (_currentCompositeTask != null)
            {
                _currentCompositeTask.AddOutputDataBinding(b);
            }
        }

        [BL.Meta]
        protected static AST.Expression output_binding(AST.Expression vname, AST.Expression expr)
        {
            AST.BlockExpression condition = new AST.BlockExpression();
            condition.Body.Add(new AST.ReturnStatement(expr));
            return new AST.MethodInvocationExpression(new AST.ReferenceExpression("add_output_binding"), vname, condition, new AST.StringLiteralExpression(expr.ToCodeString()));
        }

        

        [BL.Meta]
        protected void init_parameter(AST.Expression pref, AST.Expression expr)
        {
        }


        private CompositeTaskDef _currentCompositeTask;

        protected AtomicTaskDef _curTask = null;
        protected void task(string id, string taskType, Action act)
        {
            if (_curTask != null) throw new Exception("Nesting atomic tasks not allowed");
            if (_currentCompositeTask == null) throw new Exception("Tasks must be nested in a process or composite task");
            _curTask = new AtomicTaskDef { Id = id, AutoBindVariables = true };
            _curTask.TaskType = (NGinnTaskType)Enum.Parse(typeof(NGinnTaskType), taskType, true);
            act();
            if (_curTask.JoinType == TaskSplitType.OR &&
                (_curTask.OrJoinCheckList == null || _curTask.OrJoinCheckList.Count == 0))
            {
                throw new Exception("Define or_join_checklist for task " + _curTask.Id);
            }
            _currentCompositeTask.AddTask(_curTask);
            _curTask = null;
        }

        protected void multi_instance_split_expression(Func<object> dataSplitQuery, string code)
        {
            TaskDef tsk = _curTask != null ? _curTask : (TaskDef) _currentCompositeTask;
            string k = DslUtil.TaskMultiInstanceSplitKey(tsk.Id);
            tsk.MultiInstanceSplitExpression = string.IsNullOrEmpty(code) ? "# compiled code block" : code;
            _variableBinds[k] = dataSplitQuery;
        }

        /// <summary>
        /// Multi-instance split expression
        /// This function is used for defining multi-instance tasks
        /// </summary>
        /// <param name="expr"></param>
        /// <returns></returns>
        [BL.Meta]
        protected static AST.Expression multi_instance_split(AST.Expression expr)
        {
            AST.BlockExpression condition = new AST.BlockExpression();
            condition.Body.Add(new AST.ReturnStatement(expr));
            return new AST.MethodInvocationExpression(new AST.ReferenceExpression("multi_instance_split_expression"), condition, new AST.StringLiteralExpression(expr.ToCodeString()));
        }


        protected void custom_task(string id, Type taskType, Action act)
        {
            task(id, "custom", () =>
            {
                implementation_class(taskType);
                act();
            });
        }

        /// <summary>
        /// Task instance class
        /// </summary>
        /// <param name="t"></param>
        protected void implementation_class(Type t)
        {
            _curTask.ImplementationClass = t.FullName;
        }

        /// <summary>
        /// Task instance class
        /// </summary>
        /// <param name="t"></param>
        protected void implementation_class(string t)
        {
            _curTask.ImplementationClass = t;
        }

        /// <summary>
        /// Composite task definition
        /// </summary>
        /// <param name="id"></param>
        /// <param name="act"></param>
        protected void composite_task(string id, Action act)
        {
            var p = _currentCompositeTask;
            _currentCompositeTask = new CompositeTaskDef();
            _currentCompositeTask.Id = id;
            act();
            if (_currentCompositeTask.JoinType == TaskSplitType.OR &&
                (_currentCompositeTask.OrJoinCheckList == null || _currentCompositeTask.OrJoinCheckList.Count == 0))
            {
                throw new Exception("Define or_join_checklist for task " + _currentCompositeTask.Id);
            }
            p.AddTask(_currentCompositeTask);
            _currentCompositeTask = p;
        }

        protected void split_type(TaskSplitType ts)
        {
            if (_curTask != null)
                _curTask.SplitType = ts;
            else if (_currentCompositeTask != null)
                _currentCompositeTask.SplitType = ts;
            else throw new Exception();
        }

        protected void join_type(TaskSplitType ts)
        {
            if (_curTask != null)
                _curTask.JoinType = ts;
            else if (_currentCompositeTask != null)
                _currentCompositeTask.JoinType = ts;
            else throw new Exception();
        }

        protected void or_join_checklist(params string[] names)
        {
            var tsk = _curTask == null ? (TaskDef) _currentCompositeTask : _curTask;
            if (tsk.JoinType != TaskSplitType.OR) throw new Exception("or_join_checklist only allowed for OR join tasks");
            if (tsk.OrJoinCheckList == null) tsk.OrJoinCheckList = new List<string>();
            tsk.OrJoinCheckList.AddRange(names);
        }

        protected void init_task(Action act)
        {
            var tsk = _curTask == null ? (TaskDef)_currentCompositeTask : _curTask;
            string k = DslUtil.TaskScriptKey(tsk.Id, "_paramInit");
            _taskScripts[k] = act;
        }

        protected void prepare_output_data(Action act)
        {
            var tsk = _curTask == null ? (TaskDef)_currentCompositeTask : _curTask;
            string k = DslUtil.TaskScriptKey(tsk.Id, "_variableUpdateOnComplete");
            _taskScripts[k] = act;
        }

        #endregion tasks

        #region flows
        protected void flow(string from, string to)
        {
            flow(from, to, (SC.IDictionary) null);
        }

        protected void flow(string from, string to, SC.IDictionary options)
        {
            var fd = new FlowDef
            {
                From = from,
                To = to,
                IsCancelling = GetOption(options, "cancelling", false),
                Label = GetOption(options, "label", (string)null),
                SourcePortType = GetOption(options, "sourcePort", TaskOutPortType.Out),
                TargetPortType = GetOption(options, "targetPort", TaskInPortType.In),
                EvalOrder = GetOption(options, "evalOrder", 0)
            };
            _currentCompositeTask.AddFlow(fd);
        }

        private FlowDef _curFlow = null;
        protected void flow(string from, string to, Action act)
        {
            _curFlow = new FlowDef { From = from, To = to };
            act();
            _currentCompositeTask.AddFlow(_curFlow);
            _curFlow = null;
        }

        protected void flow_condition(Func<bool> cond, string condString)
        {
            if (_curFlow != null)
            {
                _curFlow.InputCondition = condString;
                _flowConditions[DslUtil.FlowConditionKey(_currentCompositeTask.Id, _curFlow.From, _curFlow.To)] = cond;
            }
            else throw new Exception();
        }

        [BL.Meta]
        public static AST.Expression when(AST.Expression expr)
        {
            AST.BlockExpression condition = new AST.BlockExpression();
            condition.Body.Add(new AST.ReturnStatement(expr));
            return new AST.MethodInvocationExpression(new AST.ReferenceExpression("flow_condition"), condition, new AST.StringLiteralExpression(expr.ToCodeString()));
        }

        protected void options(SC.IDictionary options)
        {
            if (_curFlow != null)
            {
                _curFlow.IsCancelling = GetOption(options, "cancelling", _curFlow.IsCancelling);
                _curFlow.Label = GetOption(options, "label", _curFlow.Label);
                _curFlow.SourcePortType = GetOption(options, "sourcePort", _curFlow.SourcePortType);
                _curFlow.TargetPortType = GetOption(options, "targetPort", _curFlow.TargetPortType);
                _curFlow.EvalOrder = GetOption(options, "evalOrder", _curFlow.EvalOrder);
            }
            else if (_curVar != null)
            {
                _curVar.IsRequired = GetOption(options, required, _curVar.IsRequired);
                _curVar.IsArray = GetOption(options, isArray, _curVar.IsArray);
                _curVar.VariableDir = GetOption(options, dir, _curVar.VariableDir);
                _curVar.DefaultValueExpr = GetOption(options, "defaultValue", _curVar.DefaultValueExpr);
            }
            else throw new Exception();
        }

        #endregion //flows

        #region places
        protected void start_place(string id)
        {
            if (_currentCompositeTask.Places.Any(x => x.Id == id)) throw new Exception("Place already defined: " + id);
            _currentCompositeTask.AddPlace(new PlaceDef { Id = id, PlaceType = PlaceTypes.Start });
        }

        protected void end_place(string id)
        {
            if (_currentCompositeTask.Places.Any(x => x.Id == id)) throw new Exception("Place already defined: " + id);
            _currentCompositeTask.AddPlace(new PlaceDef { Id = id, PlaceType = PlaceTypes.End });
        }

        protected void place(string id)
        {
            place(id, null);
        }

        protected void place(string id, SC.IDictionary options)
        {
            if (_currentCompositeTask.Places.Any(x => x.Id == id)) throw new Exception("Place already defined: " + id);
            var pl = new PlaceDef { Id = id, PlaceType = PlaceTypes.Intermediate };
            if (options != null)
            {
                pl.Label = GetOption(options, "label", "");
                pl.PlaceType = GetOption(options, "type", pl.PlaceType);
                pl.Description = GetOption(options, "description", (string)null);
            }
            _currentCompositeTask.AddPlace(pl);
        }
        #endregion places


        /// <summary>
        /// Setting extension properties
        /// TODO finish
        /// </summary>
        /// <param name="options"></param>
        protected void metadata(SC.IDictionary options)
        {
            if (_curFlow != null)
            {
            }
            else if (_curVar != null)
            {
            }
            else if (_curStructDef != null)
            {
            }
            else if (_curTask != null)
            {
            }
            else if (_currentCompositeTask != null)
            {
            }
            else if (_curProcessDef != null)
            {
                throw new NotImplementedException();
            }
            throw new NotImplementedException();
        }
    }
}
