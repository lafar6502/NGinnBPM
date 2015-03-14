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
using NGinnBPM.Runtime.ExecutionEngine;
using System.IO;

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

        public NGinnConfigurator ConfigureProcessRepository(string baseDir)
        {
            string bd = AppDomain.CurrentDomain.BaseDirectory;
            bd = Path.IsPathRooted(baseDir) ? baseDir : Path.Combine(bd, baseDir);
            _wc.Register(Component.For<IProcessPackageRepo>().ImplementedBy<NGinnBPM.Runtime.ProcessDSL.ProcessPackageRepository>()
                .DependsOn(new {
                    BaseDirectory = bd
                }).LifeStyle.Singleton);
            return this;
        }

        public NGinnConfigurator ConfigureJsonProcessRepository(string baseDir)
        {
            string bd = AppDomain.CurrentDomain.BaseDirectory;
            bd = Path.IsPathRooted(baseDir) ? baseDir : Path.Combine(bd, baseDir);
            _wc.Register(Component.For<IProcessPackageRepo>().ImplementedBy<NGinnBPM.Runtime.ProcessDSL2.ProcessPackageRepository>()
                .DependsOn(new
                {
                    BaseDirectory = bd
                }).LifeStyle.Singleton);
            return this;
        }

        public NGinnConfigurator ConfigureSqlStorage(string connString)
        {
            _wc.Register(Component.For<IDbSessionFactory>()
                .ImplementedBy<SqlDbSessionFactory>()
                .DependsOn(new
                {
                    ConnectionString = connString
                }).LifeStyle.Singleton);
            return this;
        }

        public NGinnConfigurator FinishConfiguration()
        {
            _wc.Register(Component.For<ProcessEngine>()
                .ImplementedBy<ProcessEngine>()
                .LifeStyle.Singleton);
            _wc.Register(Component.For<ITaskInstancePersister>()
                .ImplementedBy<SqlProcessPersister>()
                .LifeStyle.Singleton);
            _wc.Register(Component.For<ITaskInstanceSerializer>()
                .ImplementedBy<JsonTaskInstanceSerializer>());

            MessageBusConfigurator.Begin(_wc)
                .AddMessageHandlersFromAssembly(typeof(TaskInstance).Assembly)
                .AddMessageHandlersFromAssembly(typeof(NGinnConfigurator).Assembly)
                .ConfigureFromAppConfig()
                .AutoStartMessageBus(true)
                .FinishConfiguration();
            return this;
        }

        public IServiceResolver GetContainer()
        {
            return _wc.Resolve<IServiceResolver>();
        }
    }
}
