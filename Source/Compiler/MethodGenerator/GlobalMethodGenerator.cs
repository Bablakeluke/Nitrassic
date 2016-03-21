using System;

namespace Nitrassic.Compiler
{
	/// <summary>
	/// Represents the information needed to compile global code.
	/// </summary>
	internal class GlobalMethodGenerator : MethodGenerator
	{
		/// <summary>
		/// Creates a new GlobalMethodGenerator instance.
		/// </summary>
		/// <param name="engine"> The script engine. </param>
		/// <param name="source"> The source of javascript code. </param>
		/// <param name="options"> Options that influence the compiler. </param>
		public GlobalMethodGenerator(ScriptEngine engine, ScriptSource source, CompilerOptions options)
			: base(engine, engine.GlobalScope, source, options)
		{
		}
		
		/// <summary>
		/// Gets a name for the generated method.
		/// </summary>
		/// <returns> A name for the generated method. </returns>
		protected override string GetMethodName()
		{
			// Take the path of the script and replace the non-alphanumeric characters with
			// underscores.
			var sanitizedPath = new System.Text.StringBuilder(this.Source.Path);
			for (int i = 0; i < sanitizedPath.Length; i++)
			{
				char c = sanitizedPath[i];
				if ((c < '0' || c > '9') && (c < 'a' || c > 'z') && (c < 'A' || c > 'Z'))
					sanitizedPath[i] = '_';
			}
			return "global_"+sanitizedPath.ToString();
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
		protected override void GenerateCode(ILGenerator generator, OptimizationInfo optimizationInfo)
		{
			
			// Initialize any function or variable declarations.
			(this.InitialScope as ObjectScope).GenerateDeclarations(generator, optimizationInfo);
			
			// Generate code for the source code.
			this.AbstractSyntaxTree.GenerateCode(generator, optimizationInfo);

			// Code in the global context always returns undefined.
			EmitHelpers.EmitUndefined(generator);
		}

		/// <summary>
		/// Executes the script.
		/// </summary>
		/// <returns> The result of evaluating the script. </returns>
		public object Execute()
		{
			
			// Execute the compiled delegate and store the result.
			object result = GeneratedMethod.Call(this.Engine.Global,new object[1]);
			
			// Ensure the abstract syntax tree is kept alive until the eval code finishes running.
			GC.KeepAlive(this);

			// Return the result.
			return result;
		}
		
	}

}