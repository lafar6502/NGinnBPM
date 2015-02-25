using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NLog;
using Castle.Windsor;
using Castle.MicroKernel;
using System.IO;
using Castle.MicroKernel.Registration;
using NGinnBPM.Runtime.Services;
using NGinnBPM.Runtime.ExecutionEngine;
using System.Configuration;

namespace NGinnBPM.Runtime.Configuration
{
    public class WindsorConfigurator
    {
        private IWindsorContainer _wc;
        
        public WindsorConfigurator(IWindsorContainer wc)
        {
            _wc = wc;
        }

        public static WindsorConfigurator Begin(IWindsorContainer wc)
        {
            return new WindsorConfigurator(wc);
        }

        public WindsorConfigurator UseBooProcessRepository(string baseDir)
        {
            string bd = AppDomain.CurrentDomain.BaseDirectory;
            bd = Path.IsPathRooted(baseDir) ? baseDir : Path.Combine(bd, baseDir);
            _wc.Register(Component.For<IProcessPackageRepo>().ImplementedBy<NGinnBPM.Runtime.ProcessDSL.ProcessPackageRepository>()
                .DependsOn(new
                {
                    BaseDirectory = bd
                }).LifeStyle.Singleton);
            return this;
        }

        public WindsorConfigurator UseJsonProcessRepository(string baseDir)
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

        public WindsorConfigurator UseSqlStorage(string connString)
        {
            var cs = ConfigurationManager.ConnectionStrings[connString];

            _wc.Register(Component.For<IDbSessionFactory>()
                .ImplementedBy<SqlDbSessionFactory>()
                .DependsOn(new
                {
                    ConnectionString = cs == null ? connString : cs.ConnectionString
                }).LifeStyle.Singleton);
            return this;
        }

        public WindsorConfigurator FinishConfiguration()
        {
            _wc.Register(Component.For<ProcessEngine>()
                .ImplementedBy<ProcessEngine>()
                .LifeStyle.Singleton);
            _wc.Register(Component.For<ITaskInstancePersister>()
                .ImplementedBy<SqlTaskInstancePersister>()
                .LifeStyle.Singleton);
            _wc.Register(Component.For<ITaskInstanceSerializer>()
                .ImplementedBy<JsonTaskInstanceSerializer>());
            NGinnBPM.MessageBus.Windsor.MessageBusConfigurator.RegisterMessageHandlersFromAssembly(typeof(Tasks.TaskInstance).Assembly, _wc);
            return this;
        }

        public IWindsorContainer GetContainer()
        {
            return _wc;
        }
    }
}
