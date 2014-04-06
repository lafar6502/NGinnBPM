using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NGinnBPM.ProcessModel;
using RD = Rhino.DSL;
using System.IO;
using NLog;
using System.Collections.Concurrent;
using Newtonsoft.Json;


namespace NGinnBPM.Runtime.ProcessDSL2
{
    /// <summary>
    /// This class stores processes as JSON documents, with script snippets dynamically compiled in Boo
    /// </summary>
    public class BooProcessPackage : IProcessPackage
    {
        /// <summary>
        /// Package base directory.
        /// </summary>
        public string BaseDir { get; private set; }
        

        private static Logger log = LogManager.GetCurrentClassLogger();

        private RD.SimpleBaseClassDslCompiler<ProcessRuntimeDSLBase> _dsl;
        private MemScriptStorage _storage;
        private ConcurrentDictionary<string, PDCacheEntry> _processCache = new ConcurrentDictionary<string, PDCacheEntry>();
        private JsonSerializer _ser = null;

        private class PDCacheEntry
        {
            public DateTime ReadDate { get; set; }
            public ProcessDef Process { get; set; }
            public string ScriptUrl { get; set; }
        }

        public BooProcessPackage(string baseDir)
        {
            this.BaseDir = baseDir;
            JsonSerializerSettings sett = new JsonSerializerSettings
            {
                DateFormatHandling = Newtonsoft.Json.DateFormatHandling.IsoDateFormat,
                NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
                MissingMemberHandling = Newtonsoft.Json.MissingMemberHandling.Ignore,
                Formatting = Newtonsoft.Json.Formatting.Indented,
                TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto
            };
            _ser = JsonSerializer.Create(sett);
            InitializeIfNecessary();
        }

        protected void InitializeIfNecessary()
        {
            if (_dsl == null)
            {
                var st = new MemScriptStorage(id =>
                {
                    var pd = GetProcessDefinition(id);
                    var script = ProcessBooScriptGenerator.GenerateScriptString(pd);
                    log.Debug("Generated script for process {0}: {1}", id, script);
                    return script;
                });
                var dsl = new RD.SimpleBaseClassDslCompiler<ProcessRuntimeDSLBase>(st);
                _storage = st;
                _dsl = dsl;
            }
        }
        /// <summary>
        /// Attempts to update process definition. If the attempt fails current definition will not 
        /// be modified.
        /// </summary>
        /// <param name="pd"></param>
        /// <param name="errors"></param>
        /// <returns></returns>
        public bool TryUpdateProcess(ProcessDef pd, out IList<string> errors)
        {
            InitializeIfNecessary();
            List<string> err = new List<string>();
            List<string> warn = new List<string>();
            var scr = ProcessBooScriptGenerator.GenerateScriptString(pd);
            if (!_dsl.CheckSyntax(scr, err, warn))
            {
                errors = err;
                return false;
            }

            throw new NotImplementedException();
        }



        public string Name
        {
            get { return Path.GetDirectoryName(BaseDir); }
        }

        public IEnumerable<string> ProcessNames
        {
            get 
            {
                return Directory.GetFiles(BaseDir, "*.npd").Select(x => Path.GetFileNameWithoutExtension(x));
            }
        }

        public PackageDef GetPackageDef()
        {
            InitializeIfNecessary();
            throw new NotImplementedException();
        }

        private bool ValidateProcessDef(ProcessDef pd, List<string> errors)
        {
            if (!pd.Validate(errors)) return false;
            List<string> warns = new List<string>();
            var script = ProcessBooScriptGenerator.GenerateScriptString(pd);
            if (!_dsl.CheckSyntax(script, errors, warns))
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

        private PDCacheEntry TryReloadProcessFile(string fileName)
        {
            log.Info("(Re) loading process file {0}", fileName);
            using (var sr = new StreamReader(fileName, Encoding.UTF8))
            {
                var pd = _ser.Deserialize<ProcessDef>(new JsonTextReader(sr));
                return TryReloadProcessDef(pd);
            }
        }

        public ProcessDef GetProcessDefinition(string definitionId)
        {
            var pe = _processCache.AddOrUpdate(definitionId, 
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
            sr.Initialize(pd);
            return sr;
        }
        
    }

    
}
