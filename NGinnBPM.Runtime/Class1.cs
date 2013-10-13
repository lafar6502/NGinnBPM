using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NGinnBPM.ProcessModel;
using NGinnBPM.Runtime.Tasks;

namespace NGinnBPM.Runtime
{
    public class Class1
    {
    }

    public interface IProcessPackageRepo
    {
        IEnumerable<string> PackageNames { get; }
        IProcessPackage GetProcessPackage(string name);
    }

    public interface IProcessPackage
    {
        string Name { get; }
        IEnumerable<string> ProcessNames { get; }
        ProcessDef GetProcessDefinition(string definitionId);
        IProcessScriptRuntime GetScriptRuntime(string processDefinition);
    }

    public interface IProcessScriptRuntime
    {
        string ProcessDefinitionId { get; }

        /// <summary>
        /// Execute task input data binding and initialize parameters according to bindings
        /// </summary>
        /// <param name="ti"></param>
        /// <param name="inputData"></param>
        /// <param name="ctx"></param>
        void InitializeNewTask(TaskInstance ti, Dictionary<string, object> inputData, ITaskExecutionContext ctx);

        /// <summary>
        /// Execute task output parameter bindings and collect task output data
        /// </summary>
        /// <param name="ti"></param>
        /// <param name="ctx"></param>
        /// <returns></returns>
        Dictionary<string, object> GatherOutputData(TaskInstance ti, ITaskExecutionContext ctx);

        bool EvalFlow(TaskInstance ti, FlowDef fd, ITaskExecutionContext ctx);

        
    }
}
