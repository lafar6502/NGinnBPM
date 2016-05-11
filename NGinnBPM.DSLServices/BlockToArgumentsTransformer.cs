using System;
using System.Collections.Generic;
using Boo.Lang.Compiler.Ast;

//RG: borrowed the file from Rhino.DSL
namespace NGinnBPM.DSLServices
{
	/// <summary>
	/// Transforms the contents of macro blocks specified in the ctor to parameters.
	/// <example>
	/// <code>
	/// mymacro:
	///		1
	///		"something"
	/// 
	/// is transformed into
	/// 
	/// mymacro(1, "something")
	/// </code>
	/// </example>
	/// </summary>
	public class BlockToArgumentsTransformer : DepthFirstTransformer
	{
		private readonly string[] methods;

		/// <summary>
		/// Creates an instance of BlockToArgumentsTransformer
		/// </summary>
		/// <param name="methods">Methods that should have blocks transformed into arguments.</param>
		public BlockToArgumentsTransformer(params string[] methods)
		{
			if (methods == null ||
				methods.Length == 0)
				throw new ArgumentNullException("methods");
			this.methods = methods;
		}

		/// <summary>
		/// Handles macro statements.
		/// </summary>
		/// <param name="node">The node.</param>
		public override void OnMacroStatement(MacroStatement node)
		{
			if (Array.Exists(methods,
			                 name => name.Equals(node.Name)))
			{
				if (node.Body != null)
				{
					var expressions = GetExpressionsFromBlock(node.Body);
					foreach (var expression in expressions)
						node.Arguments.Add(expression);
					node.Body.Clear();
				}
			}
			base.OnMacroStatement(node);
		}

		private static Expression[] GetExpressionsFromBlock(Block block)
		{
			var expressions = new List<Expression>(block.Statements.Count);
			foreach (var statement in block.Statements)
			{
				if (statement is ExpressionStatement)
					expressions.Add((statement as ExpressionStatement).Expression);
				else if (statement is MacroStatement)
				{
					var macroStatement = statement as MacroStatement;
					if (macroStatement.Arguments.Count == 0 &&
						macroStatement.Body.IsEmpty)
					{
						// Assume it is a reference expression
						var refExp = new ReferenceExpression(macroStatement.LexicalInfo);
						refExp.Name = macroStatement.Name;
						expressions.Add(refExp);
					}
					else
					{
						// Assume it is a MethodInvocation
					    var mie = new MethodInvocationExpression(macroStatement.LexicalInfo)
					    {
					        Target = new ReferenceExpression(macroStatement.LexicalInfo, macroStatement.Name),
					        Arguments = macroStatement.Arguments
					    };

					    if (macroStatement.Body.IsEmpty == false)
						{
							// If the macro statement has a block,                      
							// transform it into a block expression and pass that as the last argument                     
							// to the method invocation.
							BlockExpression be = new BlockExpression(macroStatement.LexicalInfo);
							be.Body = macroStatement.Body.CloneNode();

							mie.Arguments.Add(be);
						}

						expressions.Add(mie);
					}
				}
				else
				{
					throw new InvalidOperationException($"Can not transform block with {statement.GetType()} into argument.");
				}
			}
			return expressions.ToArray();
		}
	}
}
