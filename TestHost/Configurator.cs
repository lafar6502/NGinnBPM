using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NGinnBPM.MessageBus;
using NGinnBPM.MessageBus.Windsor;
using Castle.Windsor;
using Castle.MicroKernel.Registration;
using NGinnBPM.Runtime;
using NGinnBPM.Runtime.Services;
using NGinnBPM.Runtime.Tasks;

namespace TestHost.cs
{
    public class NGinnConfigurator
    {
        private IWindsorContainer _wc;

        public static NGinnConfigurator Begin(IWindsorContainer wc)
        {
            return new NGinnConfigurator
            {
                _wc = wc
            };
        }

        public static NGinnConfigurator Begin()
        {
            return Begin(new WindsorContainer());
        }

        public NGinnConfigurator FinishConfiguration()
        {
            MessageBusConfigurator.Begin(_wc)
                .AddMessageHandlersFromAssembly(typeof(TaskInstance).Assembly)
                .AddMessageHandlersFromAssembly(typeof(NGinnConfigurator).Assembly)
                .ConfigureFromAppConfig()
                .AutoStartMessageBus(true)
                .FinishConfiguration();

            return this;
        }
    }
}
