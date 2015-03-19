using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NGinnBPM.ProcessModel;
using RD = NGinnBPM.DSLServices;
using System.IO;
using NLog;
using System.Collections.Concurrent;
using Newtonsoft.Json;


namespace NGinnBPM.Runtime.ProcessDSL2
{
    /// <summary>
    /// This class stores processes as JSON documents, with script snippets dynamically compiled in Boo
    /// TODO: initially all process definitions should be loaded at once and boo scripts compiled into one
    /// assembly. Subsequent updates will rely on incremental recompilation. This way startup will be few times faster.
    /// </summary>
    public class BooProcessPackage : IProcessPackage
    {
        /// <summary>
        /// Package base directory.
        /// </summary>
        public string BaseDir { get; private set; }
        

        private static Logger log = LogManager.GetCurrentClassLogger();

        private ProcessDslCompiler _dsl;
        private MemScriptStorage _storage;
        private ConcurrentDictionary<string, PDCacheEntry> _processCache = null;

        private class PDCacheEntry
        {
            public DateTime ReadDate { get; set; }
            public ProcessDef Process { get; set; }
            public string ScriptUrl { get; set; }
        }

        protected class ProcessDslCompiler : RD.SimpleBaseClassDslCompiler<ProcessRuntimeDSLBase>
        {
            public ProcessDslCompiler(RD.ISimpleScriptStorage st)
                : base(st)
            {
                base.WhitespaceAgnostic = true;
                this.CompilationCallback((cc, urls) =>
                {
                    log.Info("Compilation of {0}: {1}", string.Join(",", urls), cc.Errors.ToString(false));
                });
            }

            
        }

        public BooProcessPackage(string baseDir)
        {
            if (!Directory.Exists(baseDir)) throw new Exception("Directory does not exist: " + baseDir);
            this.BaseDir = baseDir;
        }

        protected void EnsureProcessesLoaded()
        {
            var pc = _processCache;
            if (pc != null) return;
            lock (this)
            {
                if (_processCache != null) return;
                if (_dsl == null)
                {
                    var st = new MemScriptStorage(id =>
                    {
                        throw new Exception("Script not found: " + id);
                    });
                    var dsl = new ProcessDslCompiler(st);
                    dsl.Namespaces = new List<string>
                    {
                        "System",
                        "NGinnBPM.ProcessModel",
                        "NGinnBPM.Runtime"

                    };
                    _storage = st;
                    _dsl = dsl;
                }
                var entries = InitialProcessLoad();
                var cache = new ConcurrentDictionary<string, PDCacheEntry>();
                foreach (var ent in entries)
                {
                    cache.TryAdd(ent.Process.ShortDefinitionId.ToLower(), ent);
                }
                _processCache = cache;
            }
            
        }
        /// <summary>
        /// Attempts to update process definition. If the attempt fails current definition will not 
        /// be modified.
        /// </summary>
        /// <param name="pd"></param>
        /// <param name="errors"></param>
        /// <returns></returns>
        public bool TryUpdateProcess(ProcessDef pd)
        {
            EnsureProcessesLoaded();
            var ce = TryReloadProcessDef(pd);
            string fn = Path.Combine(BaseDir, pd.ShortDefinitionId + ".npd");
            log.Info("Saving process file {0}", fn);
            using (var sw = new StreamWriter(fn, false, Encoding.UTF8))
            {
                ProcessDefJsonSerializer.Serialize(ce.Process, sw);
            }
            ce.ReadDate = DateTime.Now;
            _processCache.AddOrUpdate(ce.Process.ShortDefinitionId.ToLower(), ce, (id, oc) => ce);
            return true;
        }



        public string Name
        {
            get { return Path.GetFileName(BaseDir);  }
        }

        public IEnumerable<string> ProcessNames
        {
            get 
            {
                EnsureProcessesLoaded();
                return _processCache.Where(x => x.Value.Process != null).Select(x => x.Value.Process.ShortDefinitionId);
            }
        }

        private PackageDef _pkgDef = null;

        public PackageDef GetPackageDef()
        {
            var pd = new PackageDef
            {
                Name = this.Name,
                ExternalResources = new List<string>(),
                PackageTypeSets = new List<ProcessModel.Data.TypeSet>(),
                ProcessDefinitions = new List<ProcessDef>()
            };
            if (_processCache != null)
            {
                pd.ProcessDefinitions.AddRange(this._processCache.Values.Where(x => x.Process != null).Select(x => x.Process));
            }
            return pd;
        }

        private bool ValidateProcessDef(ProcessDef pd, List<string> errors = null, List<string> warnings = null)
        {
            if (!pd.Validate(errors)) return false;
            var script = ProcessBooScriptGenerator.GenerateScriptString(pd);
            if (!_dsl.CheckSyntax(script, errors, warnings))
            {
                return false;
            }
            return true;
        }

        private PDCacheEntry TryReloadProcessDef(ProcessDef pd)
        {
            string ckey = pd.ShortDefinitionId.ToLower();
            var errs = new List<string>();
            if (!ValidateProcessDef(pd, errs))
            {
                throw new Exception("Process definition invalid: " + string.Join(";\n", errs));
            }
            string script = ProcessBooScriptGenerator.GenerateScriptString(pd);
            string surl = "PScript_" + Guid.NewGuid().ToString("N");
            _storage.AddScript(surl, script);
            try
            {
                var t = _dsl.TryRecompile(surl, RD.CompilationMode.Compile);
                var cd = new PDCacheEntry
                {
                    Process = pd,
                    ReadDate = DateTime.Now,
                    ScriptUrl = surl
                };
                return cd;
            }
            catch (Exception)
            {
                _storage.Invalidate(surl);
                throw;   
            }
        }

        protected ProcessDef LoadPDFromFile(string fileName)
        {
            string did = Path.GetFileNameWithoutExtension(fileName);
            string[] pts = did.Split('.');
            if (pts.Length != 2) throw new Exception("Invalid file name format");
            if (string.IsNullOrEmpty(pts[0].Trim())) throw new Exception("Invalid file name format");
            int v;
            if (!Int32.TryParse(pts[1], out v)) throw new Exception("Invalid file name format");
            var pname = char.ToUpper(pts[0][0]) + pts[0].Substring(1).ToLower();
            var pd = ProcessDefJsonSerializer.DeserializeFile(fileName);
            pd.ProcessName = pname;
            pd.Version = v;
            pd.Package = this.GetPackageDef();
            pd.FinishModelBuild();
            return pd;
        }

        private PDCacheEntry TryReloadProcessFile(string fileName)
        {
            log.Info("(Re) loading process file {0}", fileName);
            var pd = LoadPDFromFile(fileName);
            return TryReloadProcessDef(pd);
        }

        

        private IEnumerable<PDCacheEntry> InitialProcessLoad()
        {
            List<PDCacheEntry> entries = new List<PDCacheEntry>();
            foreach (string file in Directory.GetFiles(BaseDir, "*.npd"))
            {
                try
                {
                    log.Info("Loading process from {0}", file);
                    var pd = LoadPDFromFile(file);
                    var err = new List<string>();
                    if (!this.ValidateProcessDef(pd,err))
                    {
                        log.Warn("Process {0} in {1} is invalid: {2}", pd.DefinitionId, file, string.Join("|", err));
                        continue;
                    }
                    var script = ProcessBooScriptGenerator.GenerateScriptString(pd);
                    string surl = "PScript_" + Guid.NewGuid().ToString("N");
                    _storage.AddScript(surl, script);
                    var cd = new PDCacheEntry
                    {
                        Process = pd,
                        ReadDate = DateTime.Now,
                        ScriptUrl = surl
                    };
                    entries.Add(cd);
                }
                catch (Exception ex)
                {
                    log.Warn("Error loading process file {0}: {1}", file, ex);
                    continue;
                }
            }
            var all = _dsl.CreateAll();
            return entries;
        }

        public ProcessDef GetProcessDefinition(string definitionId)
        {
            EnsureProcessesLoaded();
            var did = definitionId.ToLower();
            var pe = _processCache.AddOrUpdate(did, 
            id =>
            {
                var file = Path.Combine(BaseDir, definitionId + ".npd");
                return TryReloadProcessFile(file);
            },
            (id, pd) =>
            {
                var file = Path.Combine(BaseDir, definitionId + ".npd");
                if (!File.Exists(file))
                {
                    return pd;
                }
                if (File.GetLastWriteTime(file) > pd.ReadDate)
                {
                    try
                    {
                        return TryReloadProcessFile(file);
                    }
                    catch (Exception ex)
                    {
                        log.Error("Error reloading process. Old version will be returned. File: {0}. Error: {1}", file, ex);
                    }
                }
                return pd;
            });
            return pe.Process;
        }

        public IProcessScriptRuntime GetScriptRuntime(string processDefinition)
        {
            var pd = GetProcessDefinition(processDefinition);
            string ckey = pd.ShortDefinitionId.ToLower();
            var cd = _processCache[ckey];
            var sr = _dsl.Create(cd.ScriptUrl);
            sr.Initialize(pd, this);
            return new BooProcessScriptRuntime(sr);
        }



        public bool ValidateAndSaveProcessDefinition(ProcessDef pd, bool save, out List<string> errors, out List<string> warnings)
        {
            EnsureProcessesLoaded();
            errors = new List<string>();
            warnings = new List<string>();
            if (!ValidateProcessDef(pd, errors, warnings)) {
                return false;
            }
            if (save)
            {
                if (!TryUpdateProcess(pd))
                {
                    return false;
                }
            }
            return true;
        }
    }

    
}
