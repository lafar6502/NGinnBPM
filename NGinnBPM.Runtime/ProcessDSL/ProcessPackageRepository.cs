using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using NLog;

namespace NGinnBPM.Runtime.ProcessDSL
{
    public class ProcessPackageRepository : IProcessPackageRepo
    {
        public string BaseDirectory { get; set; }
        private static Logger log = LogManager.GetCurrentClassLogger();

        private Dictionary<string, BooDSLProcessPackage> _packageCache = new Dictionary<string, BooDSLProcessPackage>();

        public IEnumerable<string> PackageNames
        {
            get
            {
                var di = new DirectoryInfo(BaseDirectory);
                return di.GetDirectories().Select(x => x.Name);
            }
        }




        public IProcessPackage GetProcessPackage(string name)
        {
            BooDSLProcessPackage pkg;
            if (!_packageCache.TryGetValue(name.ToLower(), out pkg))
            {
                if (!PackageExists(name)) return null;
                pkg = LoadPackage(name, true);
                lock (_packageCache)
                {
                    if (!_packageCache.ContainsKey(name.ToLower()))
                    {
                        _packageCache.Add(name.ToLower(), pkg);
                    }
                }
            }
            return pkg;
            
        }

        /// <summary>
        /// Validate and reload process package.
        /// </summary>
        /// <param name="name"></param>
        public void ReloadPackage(string name)
        {
            var p = LoadPackage(name, true);
            lock (_packageCache)
            {
                _packageCache.Remove(name.ToLower());
                _packageCache.Add(name.ToLower(), p);
            }
        }

        protected bool PackageExists(string name)
        {
            return Directory.Exists(Path.Combine(BaseDirectory, name));
        }

        protected BooDSLProcessPackage LoadPackage(string name, bool validate)
        {
            string pth = Path.Combine(BaseDirectory, name);
            log.Info("Loading package {0} from {1}", name, pth); 
            if (!Directory.Exists(pth)) throw new DirectoryNotFoundException(pth);
            var p = new BooDSLProcessPackage
            {
                BaseDirectory = pth
            };
            var pn = p.ProcessNames.FirstOrDefault();
            if (validate && !string.IsNullOrEmpty(pn))
            {
                p.GetProcessDefinition(pn);
            }
            return p;
        }

        public void ReloadAll()
        {
            _packageCache = new Dictionary<string, BooDSLProcessPackage>();
        }
    }
}
