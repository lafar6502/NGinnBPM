using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BL = Boo.Lang;
using Rhino.DSL;
using NLog;
using System.Reflection;

namespace NGinnBPM.Runtime.ProcessDSL
{
    internal class PackageDSLEngine : DslEngine
    {
        private static Logger log = LogManager.GetCurrentClassLogger();
        private string _baseDir;

        public PackageDSLEngine(string baseDir)
        {
            _baseDir = baseDir;
        }

        private string[] _namespaces = new string[] { 
            "NLog", 
            "System",
            "System.Data.SqlTypes",
            "System.IO",
            "System.Text",
            "NGinnBPM.ProcessModel",
            "NGinnBPM.ProcessModel.Data",
            "NGinnBPM.Runtime.Tasks",
            "NGinnBPM.Runtime.TaskExecutionEvents",
            "NGinnBPM.Runtime.Services",
            "NGinnBPM.MessageBus",
            "NLog"
        };

        public string[] Namespaces
        {
            get { return _namespaces; }
            set { _namespaces = value; }
        }

        protected override void CustomizeCompiler(Boo.Lang.Compiler.BooCompiler compiler, Boo.Lang.Compiler.CompilerPipeline pipeline, string[] urls)
        {
            Logger log = LogManager.GetCurrentClassLogger();
            compiler.Parameters.Ducky = true;
            compiler.Parameters.Debug = true;

            

            pipeline.Insert(1, new ImplicitBaseClassCompilerStep(typeof(ProcessDefDSLBase), "Prepare", _namespaces));
        }

        public override string CanonizeUrl(string url)
        {
            return System.IO.Path.GetFullPath(System.IO.Path.Combine(_baseDir, url));
        }

    }
}
