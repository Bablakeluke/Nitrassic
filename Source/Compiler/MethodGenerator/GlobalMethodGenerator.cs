using System;
using System.Collections.Generic;

namespace Nitrassic.Compiler
{
	/// <summary>
	/// Represents the information needed to compile global code.
	/// </summary>
	public class GlobalMethodGenerator : MethodGenerator
	{
		/// <summary>
		/// Creates a new GlobalMethodGenerator instance.
		/// </summary>
		/// <param name="engine"> The script engine. </param>
		/// <param name="source"> The source of javascript code. </param>
		/// <param name="options"> Options that influence the compiler. </param>
		public GlobalMethodGenerator(ScriptEngine engine, ScriptSource source, CompilerOptions options)
			: base(engine, new ObjectScope(engine,engine.GlobalPrototype), source, options)
		{
			
			// Apply global scope:
			Engine.GlobalScope=InitialScope as ObjectScope;
			
			// Set the global 'this':
			Library.PropertyVariable pv=Engine.SetGlobalType("this",Engine.GlobalPrototype.Type,Library.PropertyAttributes.Sealed);
			
			// When loaded, we set 'this' to being our Global object:
			Engine.AwaitStart(new AwaitingStart(pv,null,true));
			
		}
		
		/// <summary>
		/// Gets a name for the generated method.
		/// </summary>
		/// <returns> A name for the generated method. </returns>
		protected override string GetMethodName()
		{
			return "__.main";
		}

		/// <summary>
		/// Parses the source text into an abstract syntax tree.
		/// </summary>
		public override void Parse(OptimizationInfo optimizationInfo)
		{
			using (var lexer = new Lexer(this.Engine, this.Source))
			{
				var parser = new Parser(this.Engine, lexer, this.InitialScope, optimizationInfo, this.Options, CodeContext.Global);
				this.AbstractSyntaxTree = parser.Parse();
				this.StrictMode = parser.StrictMode;
				this.MethodOptimizationHints = parser.MethodOptimizationHints;
			}
		}

		/// <summary>
		/// Generates IL for the script.
		/// </summary>
		/// <param name="generator"> The generator to output the CIL to. </param>
		/// <param name="optimizationInfo"> Information about any optimizations that should be performed. </param>
		protected override void GenerateCode(ArgVariable[] arguments,ILGenerator generator, OptimizationInfo optimizationInfo)
		{
			
			// Generate code for the source code.
			this.AbstractSyntaxTree.GenerateCode(generator, optimizationInfo);
			
		}

		/// <summary>
		/// Executes the script.
		/// </summary>
		/// <returns> The result of evaluating the script. </returns>
		public object Execute()
		{
			
			// Execute the compiled delegate and store the result.
			object result = GeneratedMethods[0].CallLateBound(this.Engine);
			
			// Ensure the abstract syntax tree is kept alive until the eval code finishes running.
			GC.KeepAlive(this);

			// Return the result.
			return result;
		}
		
	}

}