using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino.DSL;
using System.IO;

namespace NGinnBPM.Runtime.ProcessDSL
{
    /// <summary>
    /// Repository with a single process package
    /// </summary>
    public class BooDSLProcessPackage : IProcessPackage
    {
        public string BaseDirectory { get; set; }
        private DslFactory _dsl;

        protected DslFactory GetDSL()
        {
            if (_dsl == null)
            {
                _dsl = new DslFactory();
                _dsl.BaseDirectory = BaseDirectory;
                _dsl.Register<ProcessDefDSLBase>(new PackageDSLEngine(BaseDirectory));
            }
            return _dsl;
        }
        public string Name
        {
            get { return new DirectoryInfo(BaseDirectory).Name; }
        }

        public IEnumerable<string> ProcessNames
        {
            get 
            {
                return Directory.GetFiles(BaseDirectory, "*.boo")
                    .Select(x => Path.GetFileNameWithoutExtension(x))
                    .Where(x => !string.Equals(x, "_package", StringComparison.InvariantCultureIgnoreCase));
            }
        }

        public IProcessScriptRuntime GetScriptRuntime(string processDefinition)
        {
            var dsl = GetProcessDsl(processDefinition);
            return new BooDslProcessRuntime(dsl);
        }


        public ProcessModel.ProcessDef GetProcessDefinition(string definitionId)
        {
            var dsl = GetProcessDsl(definitionId);
            
            return dsl.GetProcessDef();
        }

        protected ProcessDefDSLBase GetProcessDsl(string definitionId)
        {
            string fn = definitionId.EndsWith(".boo") ? definitionId : definitionId + ".boo";
            var pd = GetDSL().Create<ProcessDefDSLBase>(fn);
            pd.Package = this;
            return pd;
        }


        public ProcessModel.PackageDef GetPackageDef()
        {
            throw new NotImplementedException();
        }
    }
}
