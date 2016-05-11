using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Boo.Lang.Compiler;
using System.Reflection;


namespace NGinnBPM.DSLServices
{
    /// <summary>
    /// Simple script storage
    /// </summary>
    public interface ISimpleScriptStorage : IDisposable
    {
        /// <summary>
        /// List of available script urls
        /// </summary>
        /// <returns></returns>
        IEnumerable<string> GetScriptUrls();
        /// <summary>
        /// Map script url to type name
        /// </summary>
        /// <param name="url">url</param>
        /// <returns></returns>
        string GetTypeNameFromUrl(string url);
        /// <summary>
        /// Create compiler input
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        Boo.Lang.Compiler.ICompilerInput CreateCompilerInput(string url);
        /// <summary>
        /// provide callback for detecting script modification
        /// </summary>
        /// <param name="modifiedUrlCallback"></param>
        void DetectModification(Action<string[]> modifiedUrlCallback);
        /// <summary>
        /// Gets last script modification date
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        DateTime GetLastModificationDate(string url);
        /// <summary>
        /// normalize an url (return a canonical id)
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        string Normalize(string url);
    }

    /// <summary>
    /// script compilation mode
    /// </summary>
    public enum CompilationMode
    {
        /// <summary>
        /// Compile and if successful, replace previous version in the type cache
        /// </summary>
        Compile,
        /// <summary>
        /// Compile but don't replace previous version of the code in the cache
        /// </summary>
        CompileNoReplace,
        /// <summary>
        /// Check for errors only without generating any executable.
        /// </summary>
        CheckErrors
    }
    /// <summary>
    /// Simplified DSL version
    /// </summary>
    public class SimpleBaseClassDslCompiler<T>
    {
        protected Type _actualBaseType = typeof(T);
        /// <summary>
        /// 
        /// </summary>
        protected ISimpleScriptStorage _storage;
        /// <summary>
        /// 
        /// </summary>
        protected Dictionary<string, TypeCacheEntry> _typeCache = new System.Collections.Generic.Dictionary<string, TypeCacheEntry>();
        /// <summary>
        /// 
        /// </summary>
        protected class TypeCacheEntry
        {
            /// <summary>
            /// 
            /// </summary>
            public string Url { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public bool Modified { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public Type DslType { get; set; }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="storage"></param>
        public SimpleBaseClassDslCompiler(ISimpleScriptStorage storage)
        {
            _storage = storage;
            _storage.DetectModification(ReportScriptsModified);
            Namespaces = new List<string>(new[] {
                "System",
                "System.Text"
            });
            WhitespaceAgnostic = false;
            DSLMethodName = "Prepare";
        }

        public SimpleBaseClassDslCompiler(ISimpleScriptStorage storage, Type actualBaseType) : this(storage)
        {
            if (!typeof(T).IsAssignableFrom(actualBaseType)) throw new Exception("actualBaseType does not inherit from T");
            _actualBaseType = actualBaseType;
        }

        /// <summary>
        /// dsl import namespaces
        /// </summary>
        public List<string> Namespaces { get; set; }
        /// <summary>
        /// switch compiler to whitespace agnostic mode
        /// </summary>
        public bool WhitespaceAgnostic { get; set; }
        /// <summary>
        /// Name of the abstract method that is overridden by the dynamically
        /// generated class. 'void Prepare()' by default
        /// </summary>
        public string DSLMethodName { get; set; }

        /// <summary>
        /// storage
        /// </summary>
        public ISimpleScriptStorage Storage
        {
            get { return _storage; }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<string> GetScriptUrls()
        {
            return _storage.GetScriptUrls();
        }

        /// <summary>
        /// URLS of scripts that have been compiled
        /// </summary>
        public virtual IEnumerable<string> CompiledScriptUrls
        {
            get
            {
                lock (_typeCache)
                {
                    return _typeCache.Where(kv => kv.Value.DslType != null).Select(kv => kv.Key);
                }
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public virtual T Create(string url)
        {
            var tp = GetCompiledDslType(url);
            var ci = tp.GetConstructor(new Type[] { typeof(string) });
            if (ci != null)
            {
                return (T) ci.Invoke(new object[] { url });
            }
            return (T) Activator.CreateInstance(tp);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<T> CreateAll()
        {
            return GetScriptUrls().Select(x => Create(x)).ToList();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public virtual Type GetCompiledDslType(string url)
        {
            url = _storage.Normalize(url);
            if (_typeCache.Count == 0)
            {
                try
                {
                    var urls = _storage.GetScriptUrls().ToArray();
                    if (urls.Contains(url))
                    {
                        TryRecompile(urls, CompilationMode.Compile);
                    }
                }
                catch (Exception ex)
                {
                    lock (_typeCache)
                    {
                        _typeCache["_123compile_error"] = new TypeCacheEntry { Url = ex.Message };
                    }
                    Console.WriteLine("Error when compiling all urls - will try to recompile only {1}: {0}", ex, url);
                }
            }
            lock (_typeCache)
            {
                TypeCacheEntry tp;
                if (_typeCache.TryGetValue(url, out tp)) return tp.DslType;
            }
            Console.WriteLine($"type not found in cache - compiling only {url}");
            var t2 = TryRecompile(url, CompilationMode.Compile);
            return t2;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="urls"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        protected virtual Assembly TryRecompile(string[] urls, CompilationMode mode)
        {

            var cc = TryCompile(urls, mode == CompilationMode.CheckErrors ? true : false);
            if (cc.GeneratedAssembly == null && (mode == CompilationMode.Compile || mode == CompilationMode.CompileNoReplace)) throw new Exception("Generated assembly missing");
            if (mode != CompilationMode.Compile) return cc.GeneratedAssembly;
            foreach (var url in urls)
            {
                var tn = _storage.GetTypeNameFromUrl(url);
                var tp = cc.GeneratedAssembly.GetType(tn);
                if (tp == null)
                {
                    foreach (var t in cc.GeneratedAssembly.GetTypes())
                    {
                        Console.WriteLine($"Type: {t.FullName}");
                    }

                    throw new Exception("Type not found for url: " + url + ", type name: " + tn);
                }
                var tce = new TypeCacheEntry
                {
                    DslType = tp,
                    Modified = false,
                    Url = url
                };
                lock (_typeCache)
                {
                    _typeCache.Remove(url);
                    _typeCache[url] = tce;
                }
            }
            return cc.GeneratedAssembly;
        }
        
        

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public virtual Type TryRecompile(string url, CompilationMode mode)
        {
            url = _storage.Normalize(url);
            var asm = TryRecompile(new string[] { url }, mode);
            var typeName = _storage.GetTypeNameFromUrl(url);
            var tp = asm.GetType(typeName);
            if (tp != null) return tp;
            var sb = new StringBuilder();
            throw new Exception(
                $"Type {typeName} not found in generated assembly. List of types: {asm.GetTypes().Aggregate(sb, (l, s) => l.AppendLine(s.FullName), l => l.ToString())}");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="urls"></param>
        public virtual void ReportScriptsModified(string[] urls)
        {
            if (urls == null) return;
            foreach (string url in urls)
            {
                lock (_typeCache)
                {
                    _typeCache.Remove(url);
                    _typeCache.Remove(_storage.Normalize(url));
                }
                
                //TypeCacheEntry tce;
                //if (_typeCache.TryGetValue(url, out tce)) tce.Modified = true;
                //if (_typeCache.TryGetValue(_storage.Normalize(url), out tce)) tce.Modified = true;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tp"></param>
        /// <returns></returns>
        public virtual string GetUrlForType(Type tp)
        {
            lock (_typeCache)
            {
                var e = _typeCache.Values.FirstOrDefault(x => x.DslType == tp);
                return e?.Url;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public virtual DateTime? GetLastModificationDate(string url)
        {
            var dt = _storage.GetLastModificationDate(_storage.Normalize(url));
            return dt > DateTime.MinValue ? dt : (DateTime?)null;
        }

        /// <summary>
        /// check script syntax
        /// </summary>
        /// <param name="script"></param>
        /// <param name="errors">provide a list for adding errors</param>
        /// <param name="warnings">provide a list for adding warnings</param>
        /// <returns></returns>
        public virtual bool CheckSyntax(string script, IList<string> errors, IList<string> warnings)
        {
            //Console.WriteLine("CheckSyntax {0}", script);
            var compiler = new BooCompiler();
            compiler.Parameters.OutputType = CompilerOutputType.Library;
            compiler.Parameters.GenerateInMemory = true;
            compiler.Parameters.Pipeline = new Boo.Lang.Compiler.Pipelines.CheckForErrors();
            compiler.Parameters.WhiteSpaceAgnostic = this.WhitespaceAgnostic;

            CustomizeCompiler(compiler, compiler.Parameters.Pipeline, new string[] { });
            compiler.Parameters.Input.Add(new Boo.Lang.Compiler.IO.StringInput("the_script", script));
            var compilerContext = compiler.Run();
            if (warnings != null)
            {
                foreach (var w in compilerContext.Warnings)
                {
                    warnings.Add(w.ToString());
                }
            }
            if (errors == null) return compilerContext.Errors.Count == 0;
            foreach (var e in compilerContext.Errors)
            {
                errors.Add(e.ToString(true));
            }
            return compilerContext.Errors.Count == 0;
        }

        /// <summary>
        /// Try re-compiling specified urls
        /// </summary>
        /// <param name="urls"></param>
        /// <param name="checkSyntaxOnly">Don't compile, check syntax only.</param>
        /// <returns></returns>
        protected virtual CompilerContext TryCompile(string[] urls, bool checkSyntaxOnly = false)
        {
            //Console.WriteLine("TryCompile {0}, syntaxcheck={1}", string.Join(",", urls), checkSyntaxOnly);
            var compiler = new BooCompiler();
            compiler.Parameters.OutputType = CompilerOutputType.Library;
            compiler.Parameters.GenerateInMemory = true;
            compiler.Parameters.Pipeline = checkSyntaxOnly ? (CompilerPipeline) new Boo.Lang.Compiler.Pipelines.CheckForErrors() : new Boo.Lang.Compiler.Pipelines.CompileToMemory();
            compiler.Parameters.WhiteSpaceAgnostic = this.WhitespaceAgnostic;
            CustomizeCompiler(compiler, compiler.Parameters.Pipeline, urls);
            foreach (var url in urls)
            {
                compiler.Parameters.Input.Add(_storage.CreateCompilerInput(url));
            }
            var compilerContext = compiler.Run();
            _compilationCallback?.Invoke(compilerContext, urls);

            if (compilerContext.Errors.Count != 0)
                throw CreateCompilerException(compilerContext);
            HandleWarnings(compilerContext.Warnings);

            return compilerContext;
        }

        /// <summary>
        /// Customise the compiler to fit this DSL engine.
        /// This is the most commonly overriden method.
        /// </summary>
        protected virtual void CustomizeCompiler(BooCompiler compiler, CompilerPipeline pipeline, string[] urls)
        {
            compiler.Parameters.Ducky = true;
            compiler.Parameters.Debug = true;
            /*
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    string loc = asm.Location;
                    if (!compiler.Parameters.References.Contains(asm)) compiler.Parameters.References.Add(asm);
                }
                catch (Exception) {  }
            }*/

            pipeline.Insert(1, new ImplicitBaseClassCompilerStep(_actualBaseType, DSLMethodName, Namespaces.ToArray()));
        }

        private Action<CompilerContext, string[]> _compilationCallback;

        /// <summary>
        /// compilation callback function
        /// </summary>
        /// <param name="compilationCompleted">will be invoked after every compilation</param>
        public virtual void CompilationCallback(Action<CompilerContext, string[]> compilationCompleted)
        {
            _compilationCallback = compilationCompleted;
        }

        /// <summary>
        /// Allow a derived class to get access to the warnings that occured during 
        /// compilation
        /// </summary>
        /// <param name="warnings">The warnings.</param>
        protected virtual void HandleWarnings(CompilerWarningCollection warnings)
        {
        }

        /// <summary>
        /// Create an exception that would be raised on compilation errors.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        protected virtual Exception CreateCompilerException(CompilerContext context)
        {
            return new CompilerError(context.Errors.ToString(true));
        }

    }
}
