using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Boo.Lang.Compiler.Ast;
using Boo.Lang.Compiler.Steps;
using Boo.Lang.Compiler.Util;
using Module = Boo.Lang.Compiler.Ast.Module;

//RG: borrowed the file from Rhino.DSL
namespace NGinnBPM.DSLServices
{
    /// <summary>
    /// Provides a base class for classes that provide DSL via
    /// substituting code into one or more members of a class
    /// created at runtime inheriting from a BaseClassCompilerStep.
    /// </summary>
    public abstract class BaseClassCompilerStep : AbstractCompilerStep
    {
        private readonly Type _baseClass;
        private readonly string[] _namespaces;

        /// <summary>
        /// Create new instance of <seealso cref="BaseClassCompilerStep"/>
        /// </summary>
        /// <param name="baseClass">The base class that will be used</param>
        /// <param name="namespaces">Namespaces that will be automatically imported into all modules</param>
        protected BaseClassCompilerStep(Type baseClass, params string[] namespaces)
        {
            this._baseClass = baseClass;
            this._namespaces = namespaces;
        }

        /// <summary>
        /// Run this compiler step
        /// </summary>
        public override void Run()
        {
            if (Context.References.Contains(_baseClass.Assembly) == false)
                Context.Parameters.References.Add(_baseClass.Assembly);

            foreach (var module in CompileUnit.Modules)
            {
                foreach (var ns in _namespaces)
                {
                    module.Imports.Add(new Import(module.LexicalInfo, ns));
                }

                var definition = new ClassDefinition { Name = module.FullName };
                definition.BaseTypes.Add(new SimpleTypeReference(_baseClass.FullName));

                GenerateConstructors(definition);

                // This is called before the module.Globals is set to a new block so that derived classes may retrieve the
                // block from the module.
                ExtendBaseClass(module, definition);

                module.Globals = new Block();
                module.Members.Add(definition);
            }
        }

        /// <summary>
        /// Base class that this BaseClassCompilerStep builds a derived instance of.
        /// </summary>
        protected Type BaseClass
        {
            get { return _baseClass; }
        }

        private void GenerateConstructors(TypeDefinition definition)
        {
            var ctors =
                _baseClass.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var ctor in ctors)
            {
                if (ctor.IsPrivate)
                    continue;
                var constructor = new Constructor(definition.LexicalInfo);
                definition.Members.Add(constructor);
                var super = new MethodInvocationExpression(new SuperLiteralExpression());
                constructor.Body.Add(super);
                foreach (var info in ctor.GetParameters())
                {
                    var typeReference =
                        new SimpleTypeReference(TypeUtilities.GetFullName(info.ParameterType));
                    constructor.Parameters.Add(new ParameterDeclaration(info.Name,
                                                                        typeReference)
                        );
                    super.Arguments.Add(new ReferenceExpression(info.Name));
                }
            }
        }

        /// <summary>
        /// Allow a derived class to perform additional operations on the newly created type definition.
        /// </summary>
        protected virtual void ExtendBaseClass(Module module, ClassDefinition definition)
        {
        }
    }
}
