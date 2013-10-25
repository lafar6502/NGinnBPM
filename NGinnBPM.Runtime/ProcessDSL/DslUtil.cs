using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NGinnBPM.ProcessModel;

namespace NGinnBPM.Runtime.ProcessDSL
{
    public class DslUtil
    {
        public static string TaskVariableDefaultKey(string taskId, string variable)
        {
            return string.Format("{0}_vdefault_{1}", taskId, variable);
        }

        public static string FlowConditionKey(string compositeTaskId, string from, string to)
        {
            return string.Format("{0}_flow_{1}_{2}", compositeTaskId, from, to);
        }

        public static string TaskScriptKey(string taskId, string scriptId)
        {
            return string.Format("{0}_script_{1}", taskId, scriptId);
        }

        public static string TaskVarInBindingKey(string taskId, string paramName)
        {
            return string.Format("{0}_input_var_bind_{1}", taskId, paramName);
        }

        public static string TaskInputDataBindingKey(string taskId)
        {
            return string.Format("{0}_input_data_bind", taskId);
        }

        public static string TaskOutputDataBindingKey(string taskId)
        {
            return string.Format("{0}_output_data_bind", taskId);
        }

        

        public static string TaskMultiInstanceSplitKey(string taskId)
        {
            return string.Format("{0}_multi_split", taskId);
        }

        public static string TaskVarOutBindingKey(string taskId, string paramName)
        {
            return string.Format("{0}_output_var_bind_{1}", taskId, paramName);
        }

        public static string TaskParamInBindingKey(string taskId, string paramName)
        {
            return string.Format("{0}_initparam_{1}", taskId, paramName);
        }

        public static string TaskParamOutBindingKey(string taskId, string variableName)
        {
            return string.Format("{0}_outparam_{1}", taskId, variableName);
        }


    }

}
