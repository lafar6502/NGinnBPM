using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestHost.cs
{
    public class Class1
    {
        public static void Main(string[] args)
        {
            NGinnConfigurator.Begin()
                .FinishConfiguration();
        }
    }
}
