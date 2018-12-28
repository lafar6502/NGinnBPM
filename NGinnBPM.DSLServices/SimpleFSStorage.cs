using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Boo.Lang.Compiler;

namespace NGinnBPM.DSLServices
{
    /// <summary>
    /// 
    /// </summary>
    public class SimpleFSStorage : ISimpleScriptStorage
    {
        private FileSystemWatcher _watcher;
        private DateTime _lastModificationReported;
        private Action<string[]> _modificationCallback;

        public string FileExtension { get; set; }  = ".boo";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="baseDir"></param>
        /// <param name="detectModification">detect file modifications</param>
        public SimpleFSStorage(string baseDir, bool detectModification)
        {
            BaseDirectory = baseDir;
            if (detectModification)
            {
                _watcher = new FileSystemWatcher(BaseDirectory, "*" + FileExtension);
                _watcher.Changed += new FileSystemEventHandler(_watcher_Changed);
                _lastModificationReported = DateTime.Now;
            }
        }

        

        void  _watcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (_modificationCallback == null) return;
 	            
        }

        
        
        /// <summary>
        /// 
        /// </summary>
        string BaseDirectory { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<string> GetScriptUrls()
        {
            return Directory.GetFiles(BaseDirectory, "*" + FileExtension).Select(x => Path.GetFileName(x));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public virtual string GetTypeNameFromUrl(string url)
        {
            if (url.EndsWith(FileExtension)) return url.Substring(0, url.Length - 4);
            return url;
        }
        
        public virtual string MapUrlToFilePath(string url, string extension = null)
        {
            var ex = extension ?? FileExtension;
            if (!url.EndsWith(ex, StringComparison.InvariantCultureIgnoreCase)) url +=  ex;
            return Path.Combine(BaseDirectory, url);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public virtual ICompilerInput CreateCompilerInput(string url)
        {
            string path = MapUrlToFilePath(url);
            if (!File.Exists(path)) throw new Exception("File doesn't exist: " + path);

            //here it's going wrong if the type name is not same as the file name
            //because the type name will be taken from the module name (see BaseClassCompilerStep) and the module name
            //is taken from the file name (compiler input name). 
            return new  Boo.Lang.Compiler.IO.FileInput(path);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="modifiedUrlCallback"></param>
        public virtual void DetectModification(Action<string[]> modifiedUrlCallback)
        {
            _modificationCallback = modifiedUrlCallback;
            //not implemented
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            if (_watcher != null)
            {
                _watcher.Dispose();
                _watcher = null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public virtual DateTime GetLastModificationDate(string url)
        {
            string pth = MapUrlToFilePath(url);
            if (!File.Exists(pth)) return DateTime.MinValue;
            return File.GetLastWriteTime(pth);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public string Normalize(string url)
        {
            return url;
        }
    }
}
