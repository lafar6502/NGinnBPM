using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NGinnBPM.Runtime.ProcessDSL;
using System.IO;

namespace TestHost.cs
{
    public class Class1
    {
        public static void Main(string[] args)
        {
            TestProcessDsl();
        }

        public static void TestProcessDsl()
        {
            string bd = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\ProcessPackages");
            BooDslPackageRepository pkg = new BooDslPackageRepository
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
            }

        }
    }
}
