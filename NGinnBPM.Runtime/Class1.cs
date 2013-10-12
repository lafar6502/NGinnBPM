using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NGinnBPM.ProcessModel;

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
    }
}
