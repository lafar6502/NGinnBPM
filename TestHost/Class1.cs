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
    
    public class Class1
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        public static void Main(string[] args)
        {
            EngineTests.RunTests();
            return;
            //TestProcessDsl();
            //TestPackageRepo();
            //TestBpmn();
            //TestRepo2();
            //return;
            var c = ConfigureNGinnBPM();

            /*using (var ts = new TransactionScope())
            {
                var pr = c.GetInstance<ProcessEngine>();
                var proc = pr.StartProcess("Test2.DeferredChoice.1", new Dictionary<string, object> { });
                var ti = pr.GetTaskInstanceInfo(proc);
                ts.Complete();
            }*/

            using (var ts = new TransactionScope())
            {
                var pr = c.GetInstance<ProcessEngine>();
                var proc = pr.StartProcess("EngineTest.Simplest.1", new Dictionary<string, object> { });
                var ti = pr.GetTaskInstanceInfo(proc);
                ts.Complete();
            }
            //var proc = pr.StartProcess("Test2.ErrorHandlerTest.1", new Dictionary<string, object> { });
            //var proc = pr.StartProcess("Test2.MultiInstance.1", new Dictionary<string, object> { });
            //TestCompensation(pr);
            //TestProcessScriptGenerator(c);
            Console.ReadLine();

        }

        public static void TestBpmn()
        {
            //eclws\\activitiWF1\\MyProcess.bpmn
            using (var sr = new StreamReader("C:\\temp\\diagram.bpmn", Encoding.UTF8))
            {
                var dfs = NGinnBPM.BPMNTools.Parser.BPMNParser.Parse(sr);
                var x = dfs.rootElement;
                Console.ReadLine();
            }
        }

        public static void TestRepo2()
        {
            D2.BooProcessPackage p = new D2.BooProcessPackage("..\\..\\..\\ProcessPackages\\Test2");
            foreach (string pn in p.ProcessNames)
            {
                Console.WriteLine(pn);
                p.GetProcessDefinition(pn);
            }

            var pd = p.GetProcessDefinition("ErrorHandler.1");
            pd.Version = pd.Version + 1;
            List<string> errs;
                List<string> warns;
            p.ValidateAndSaveProcessDefinition(pd, true, out errs, out warns);
        }

        public static void TestProcessScriptGenerator(IServiceResolver sr)
        {
            var pr = sr.GetInstance<IProcessPackageRepo>();
            var pdef = pr.GetProcessDef("Test2.Compensation.1");
            var sw = new StringWriter();
            NGinnBPM.Runtime.ProcessDSL.BooProcessScriptGenerator gen = new NGinnBPM.Runtime.ProcessDSL.BooProcessScriptGenerator(sw);
            gen.GenerateScript(pdef);
            Console.WriteLine(sw.ToString());
        }

        public static void TestCompensation(ProcessEngine pr)
        {
            var proc = pr.StartProcess("Test2.Compensation.1", new Dictionary<string, object> { });
            Console.WriteLine("Enter to cancel the process {0}", proc);
            Console.ReadLine();
            pr.CancelTask(proc, "Testing");
            Console.WriteLine("Cancelling");
            Console.ReadLine();
        }

        public static void TestPackageRepo()
        {
            string bd = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\ProcessPackages");
            ProcessPackageRepository repo = new ProcessPackageRepository
            {
                BaseDirectory = bd
            };
            foreach (var pn in repo.PackageNames)
            {
                Console.WriteLine("Package {0}", pn);
            }
            foreach (var pn in repo.PackageNames)
            {
                var pkg = repo.GetProcessPackage(pn);
                foreach (var pname in pkg.ProcessNames)
                {
                    Console.WriteLine("Process {0}", pname);
                    Console.WriteLine(pkg.GetProcessDefinition(pname).DefinitionId);
                }
            }
        }

        public static void TestProcessDsl()
        {
            string bd = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\ProcessPackages");
            BooDSLProcessPackage pkg = new BooDSLProcessPackage
            {
                BaseDirectory = Path.Combine(bd, "Test2")
            };
            foreach (string s in pkg.ProcessNames)
            {
                Console.WriteLine("Loading Process: {0}", s);

                var pd = pkg.GetProcessDefinition(s);
                List<string> problems = new List<string>();
                pd.Validate(problems);
                Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(pd));
                foreach (var vd in pd.Body.Variables)
                {
                    
                }
            }

        }

        public static IServiceResolver ConfigureNGinnBPM()
        {
            var cfg = NGinnConfigurator.Begin()
                .ConfigureProcessRepository("..\\..\\..\\ProcessPackages")
                .ConfigureSqlStorage("nginn")
                .FinishConfiguration();
            return cfg.GetContainer();
        }
            
    }
}
