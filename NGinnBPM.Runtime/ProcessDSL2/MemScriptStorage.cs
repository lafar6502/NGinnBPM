using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using RD = NGinnBPM.DSLServices;
using BLC = Boo.Lang.Compiler;

namespace NGinnBPM.Runtime.ProcessDSL2
{
    internal class MemScriptStorage : RD.ISimpleScriptStorage
    {
        private ConcurrentDictionary<string, string> _scripts = new ConcurrentDictionary<string, string>();
        private Func<string, string> _getScript;


        public MemScriptStorage(Func<string, string> getScript)
        {
            _getScript = getScript;
        }

        public Boo.Lang.Compiler.ICompilerInput CreateCompilerInput(string url)
        {
            var scr = _scripts.GetOrAdd(url, _getScript);
            return new BLC.IO.StringInput(url, scr);

        }

        public void DetectModification(Action<string[]> modifiedUrlCallback)
        {
            
        }

        public void Invalidate(string url)
        {
            string s;
            _scripts.TryRemove(url, out s);
        }

        public void AddScript(string url, string script)
        {
            if (!_scripts.TryAdd(url, script)) throw new Exception("Failed to add script with key " + url);
        }

        public DateTime GetLastModificationDate(string url)
        {
            return DateTime.Now;
        }

        public IEnumerable<string> GetScriptUrls()
        {
            return _scripts.Keys;
        }

        public string GetTypeNameFromUrl(string url)
        {
            return url;
        }

        public string Normalize(string url)
        {
            return url;
        }

        public void Dispose()
        {
            
        }
    }
}
