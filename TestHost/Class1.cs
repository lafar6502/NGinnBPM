using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NGinnBPM.Runtime.ProcessDSL;
using System.IO;
using NGinnBPM.MessageBus;
using NGinnBPM.Runtime;

namespace TestHost.cs
{
    public class Class1
    {
        public static void Main(string[] args)
        {
            //TestProcessDsl();
            //TestPackageRepo();
            var c = ConfigureNGinnBPM();

            var pr = c.GetInstance<ProcessRunner>();
            var proc = pr.StartProcess("Test2.TimerTest.1", new Dictionary<string,object> {});

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
