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


        static void validateCompleted(CompositeTaskInstanceInfo ti, Dictionary<string, object> data)
        {
            if (ti.Status != NGinnBPM.Runtime.TaskStatus.Completed) throw new Exception("Not completed");
            if (ti.ActiveTasks != null && ti.ActiveTasks.Count > 0) throw new Exception("Active tasks");
        }

        public static void RunTests()
        {
            var c = ConfigureNGinnBPM();
            var repo = c.GetInstance<IProcessPackageRepo>();

            var pkg = repo.GetProcessPackage("EngineTest");
            TestProcess("EngineTest.Simplest.1", c, validateCompleted);
            TestProcess("EngineTest.CancellingFlow.1", c, null);
            TestProcess("EngineTest.DeferredChoice.1", c, validateCompleted);
            TestProcess("EngineTest.DeferredChoice.2", c, validateCompleted);
            TestProcess("EngineTest.Parallel.1", c, validateCompleted);
            TestProcess("EngineTest.SimpleFailure.1", c, null);
            TestProcess("EngineTest.XORLoop.1", c, validateCompleted);
            TestProcess("EngineTest.Composite.1", c, validateCompleted);
            TestProcess("EngineTest.Composite.2", c, null);
            TestProcess("EngineTest.SimpleErrorHandling.1", c, null);
            TestProcess("EngineTest.OrJoin.1", c, validateCompleted);

            Console.WriteLine("enter..");
            Console.ReadLine();
        }

        public static void TestProcess(string definitionId, IServiceResolver container, Action<CompositeTaskInstanceInfo, Dictionary<string, object>> validate)
        {
            using (var ts = new TransactionScope())
            {
                var pr = container.GetInstance<ProcessEngine>();
                var proc = pr.StartProcess(definitionId, new Dictionary<string, object> { });
                log.Info("Started process {0}: {1}", definitionId, proc);
                var ti = pr.GetTaskInstanceInfo(proc);
                var data = pr.GetTaskData(proc);
                if (validate != null)
                {
                    validate(ti, data);
                }
                /*
                if (ti.Status != NGinnBPM.Runtime.TaskStatus.Completed)
                {
                    throw new Exception("Process did not complete");
                }*/
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
