using System;
using System.Collections.Generic;
using Nitrassic.Library;

namespace Nitrassic.Compiler
{
	/// <summary>
	/// Represents the unit of compilation.
	/// </summary>
	public abstract class MethodGenerator
	{
		/// <summary>
		/// Creates a new MethodGenerator instance.
		/// </summary>
		/// <param name="engine"> The script engine. </param>
		/// <param name="scope"> The initial scope. </param>
		/// <param name="source"> The source of javascript code. </param>
		/// <param name="options"> Options that influence the compiler. </param>
		protected MethodGenerator(ScriptEngine engine, Scope scope, ScriptSource source, CompilerOptions options)
		{
			if (engine == null)
				throw new ArgumentNullException("engine");
			if (source == null)
				throw new ArgumentNullException("source");
			if (options == null)
				throw new ArgumentNullException("options");
			this.Engine = engine;
			this.InitialScope = scope;
			this.Source = source;
			this.Options = options;
			this.StrictMode = this.Options.ForceStrictMode;
		}

		/// <summary>
		/// Gets a reference to the script engine.
		/// </summary>
		public ScriptEngine Engine;

		/// <summary>
		/// Gets a reference to any compiler options.
		/// </summary>
		public CompilerOptions Options;

		/// <summary>
		/// Gets the source of javascript code.
		/// </summary>
		public ScriptSource Source;

		/// <summary>
		/// Gets a value that indicates whether strict mode is enabled.
		/// </summary>
		public bool StrictMode;

		/// <summary>
		/// Gets the top-level scope associated with the context.
		/// </summary>
		public Scope InitialScope;
		
		/// <summary>
		/// The ID of the generated method in the method cache.
		/// </summary>
		public long MethodID=-1;
		
		/// <summary>
		/// Gets a list of dependent generated methods.
		/// </summary>
		public IList<FunctionMethodGenerator> Dependencies;
		
		/// <summary>
		/// Gets the root node of the abstract syntax tree.  This will be <c>null</c> until Parse()
		/// is called.
		/// </summary>
		public Statement AbstractSyntaxTree;

		/// <summary>
		/// Gets or sets optimization information.
		/// </summary>
		public MethodOptimizationHints MethodOptimizationHints;

		/// <summary>
		/// Gets the generated IL.  This will be <c>null</c> until GenerateCode is
		/// called.
		/// </summary>
		// public ILGenerator ILGenerator;

		/// <summary>
		/// The emitted dynamic method(s), plus any dependencies.
		/// These are created when GenerateCode is called with a specific set of args.
		/// </summary>
		public List<UserDefinedFunction> GeneratedMethods=new List<UserDefinedFunction>();
		
		/// <summary>
		/// Gets a name for the generated method.
		/// </summary>
		/// <returns> A name for the generated method. </returns>
		protected abstract string GetMethodName();

		/// <summary>
		/// Gets a name for the function, as it appears in the stack trace.
		/// </summary>
		/// <returns> A name for the function, as it appears in the stack trace, or <c>null</c> if
		/// this generator is generating code in the global scope. </returns>
		protected virtual string GetStackName()
		{
			return null;
		}
		
		/// <summary>
		/// Parses the source text into an abstract syntax tree.
		/// </summary>
		public abstract void Parse(OptimizationInfo optimizationInfo);
		
		/// <summary>
		/// Generates IL for a specific variant of the method. Arguments MUST include 'this' keyword.
		/// </summary>
		public UserDefinedFunction GenerateCode(ArgVariable[] arguments,OptimizationInfo optimizationInfo)
		{	
			
			// Generate the abstract syntax tree if it hasn't already been generated.
			if (this.AbstractSyntaxTree == null)
			{
				Parse(optimizationInfo);
				
				Dependencies=optimizationInfo.NestedFunctions;
				
			}
			
			// Create the param types, which is the args (as they already include 'this'):
			int argCount=arguments==null?0 : arguments.Length;
			
			Type[] parameters=new Type[argCount];
			
			for(int i=0;i<argCount;i++){
				
				// Transfer the type over:
				parameters[i]=arguments[i].Type;
				
			}
			
			string name=GetMethodName();
			
			// Initialize global code-gen information.
			optimizationInfo.AbstractSyntaxTree = this.AbstractSyntaxTree;
			optimizationInfo.StrictMode = this.StrictMode;
			optimizationInfo.MethodOptimizationHints = this.MethodOptimizationHints;
			optimizationInfo.FunctionName = this.GetStackName();
			optimizationInfo.Source = this.Source;
			
			ILGenerator generator;
			
			// Create the UDF now (must be before we generate the code as it would go recursive for recursive functions otherwise):
			UserDefinedFunction mtd=new UserDefinedFunction(name, arguments, this as FunctionMethodGenerator,StrictMode);
			
			// Add to created set:
			GeneratedMethods.Add(mtd);
			
			// Clear locals:
			if(InitialScope!=null){
				InitialScope.Reset();
			}
			
			// Resolve variables now:
			AbstractSyntaxTree.ResolveVariables(optimizationInfo);
			
			// Low trust path.
			ReflectionEmitModuleInfo reflectionEmitInfo = this.Engine.ReflectionEmitInfo;
			
			var typeBuilder=reflectionEmitInfo.MainType;
			
			// Create the method builder now:
			var methodBuilder = typeBuilder.DefineMethod(name,
				System.Reflection.MethodAttributes.HideBySig | System.Reflection.MethodAttributes.Static | System.Reflection.MethodAttributes.Public,
				optimizationInfo.ReturnType, parameters);
			
			mtd.body=methodBuilder;
			
			// Generate the IL for the method.
			generator = new ReflectionEmitILGenerator(Engine,methodBuilder);
			
			if (ScriptEngine.EnableILAnalysis)
			{
				// Replace the generator with one that logs.
				generator = new LoggingILGenerator(generator);
			}
			
			// optimizationInfo.MarkSequencePoint(generator, new SourceCodeSpan(1, 1, 1, 1));
			GenerateCode(arguments, generator, optimizationInfo);
			generator.Complete();
			
			if (ScriptEngine.EnableILAnalysis)
			{
				// Store the disassembled IL so it can be retrieved for analysis purposes.
				mtd.DisassembledIL = generator.ToString();
				
				string args="";
				
				if(arguments!=null){
					for(int i=0;i<arguments.Length;i++){
						if(i!=0){
							args+=",";
						}
						args+=parameters[i]+" "+arguments[i].Name.ToString();
					}
				}
				
				Engine.Log("-IL "+GetMethodName()+"("+args+") : "+methodBuilder.ReturnType+"-");
				Engine.Log(mtd.DisassembledIL);
				
			}
			
			return mtd;
			
		}

		/// <summary>
		/// Generates IL for the script.
		/// </summary>
		/// <param name="generator"> The generator to output the CIL to. </param>
		/// <param name="optimizationInfo"> Information about any optimizations that should be performed. </param>
		protected abstract void GenerateCode(ArgVariable[] arguments,ILGenerator generator, OptimizationInfo optimizationInfo);
		
		/// <summary>
		/// Retrieves a delegate for the generated method.
		/// </summary>
		/// <returns> The delegate type that matches the method parameters. </returns>
		protected virtual Type GetDelegate()
		{
			return typeof(Func<object, object>);
		}
	}

}