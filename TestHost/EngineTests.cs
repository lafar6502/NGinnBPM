using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NGinnBPM.Runtime.ProcessDSL;
using System.IO;
using NGinnBPM.MessageBus;
using NGinnBPM.Runtime;
using D2 = NGinnBPM.Runtime.ProcessDSL2;
using NGinnBPM.Runtime.ExecutionEngine;
using System.Transactions;
using NLog;
using System.Threading.Tasks;

namespace TestHost.cs
{
    
    public class EngineTests
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        public static void RunTests()
        {
            var c = ConfigureNGinnBPM();
            var repo = c.GetInstance<IProcessPackageRepo>();

            var pkg = repo.GetProcessPackage("EngineTest");
            foreach (var pn in pkg.ProcessNames)
            {
                var pd = pkg.GetProcessDefinition(pn);

                string processJson = ProcessDefJsonSerializer.Serialize(pd);
                //File.WriteAllText(pn + ".json", processJson);

                TestProcess(pkg.Name + "." + pn, c);
            }
        }

        public static void TestProcess(string definitionId, IServiceResolver container)
        {
            using (var ts = new TransactionScope())
            {
                var pr = container.GetInstance<ProcessEngine>();
                var proc = pr.StartProcess(definitionId, new Dictionary<string, object> { });
                log.Info("Started process {0}: {1}", definitionId, proc);
                var ti = pr.GetTaskInstanceInfo(proc);
                
                if (ti.Status != NGinnBPM.Runtime.TaskStatus.Completed)
                {
                    throw new Exception("Process did not complete");
                }
                ts.Complete();
            }
        }

        public static IServiceResolver ConfigureNGinnBPM()
        {
            var cfg = NGinnConfigurator.Begin()
                //.ConfigureProcessRepository("..\\..\\..\\ProcessPackages")
                .ConfigureJsonProcessRepository("..\\..\\..\\PackageRepo2")
                .ConfigureSqlStorage("nginn")
                .FinishConfiguration();
            return cfg.GetContainer();
        }
            
    }
}
