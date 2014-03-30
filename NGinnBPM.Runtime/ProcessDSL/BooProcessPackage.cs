using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NGinnBPM.ProcessModel;
using RD = Rhino.DSL;

namespace NGinnBPM.Runtime.ProcessDSL
{
    /// <summary>
    /// This class stores processes as Boo scripts so it's a code generator combined with a process
    /// description DSL. Process scripts can be written by hand or can be automatically generated from
    /// a process definition. 
    /// </summary>
    public class BooProcessPackage : IProcessPackage
    {
        public string BaseDir { get; set; }

        private RD.SimpleBaseClassDslCompiler<ProcessDefDSLBase> _dsl;


        /// <summary>
        /// Attempts to update process definition. If the attempt fails current definition will not 
        /// be modified.
        /// </summary>
        /// <param name="pd"></param>
        /// <param name="errors"></param>
        /// <returns></returns>
        public bool TryUpdateProcess(ProcessDef pd, out IList<string> errors)
        {
            
            throw new NotImplementedException();
        }



        public string Name
        {
            get { throw new NotImplementedException(); }
        }

        public IEnumerable<string> ProcessNames
        {
            get { throw new NotImplementedException(); }
        }

        public PackageDef GetPackageDef()
        {
            throw new NotImplementedException();
        }

        public ProcessDef GetProcessDefinition(string definitionId)
        {
            throw new NotImplementedException();
        }

        public IProcessScriptRuntime GetScriptRuntime(string processDefinition)
        {
            throw new NotImplementedException();
        }
    }

    
}
