using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NGinnBPM.ProcessModel;
using NGinnBPM.ProcessModel.Data;
using BL = Boo.Lang;
using SC = System.Collections;

namespace NGinnBPM.Runtime.ProcessDSL
{
    /// <summary>
    /// Base class for process definition DSL
    /// </summary>
    public abstract class ProcessDefDSLBase
    {
        protected abstract void Prepare();

        public ProcessDef GetProcessDef()
        {
            _curProcessDef = new ProcessDef();
            _curProcessDef.ProcessName = this.GetType().Name;
            _curProcessDef.Version = 1;
            _curProcessDef.DataTypes = new TypeSet();
            Prepare();
            return _curProcessDef;
        }

        protected static readonly string required = "required";
        protected static readonly string array = "array";
        protected static readonly string dir = "dir";
        protected static VariableDef.Dir input = VariableDef.Dir.In;
        protected static VariableDef.Dir output = VariableDef.Dir.Out;
        protected static VariableDef.Dir local = VariableDef.Dir.Local;
        protected static VariableDef.Dir in_out = VariableDef.Dir.InOut;

        private ProcessDef _curProcessDef = null;

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

        protected void composite_task(string id, Action act)
        {
        }

        protected void task_variables(Action act)
        {
        }

        protected static T GetOption<T>(SC.IDictionary options, string name, T defVal)
        {
            if (!options.Contains(name)) return defVal;
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
            VariableDef vd = new VariableDef
            {
                Name = name,
                TypeName = type,
                IsRequired = GetOption(options, "required", false),
                IsArray = GetOption(options, "array", false),
                VariableDir = GetOption(options, "dir", VariableDef.Dir.Local),
                DefaultValueExpr = GetOption(options, "defaultValue", "")
            };
        }

        private VariableDef _curVar;
        protected void variable(string name, string type, Action act)
        {
        }


        
    }
}
