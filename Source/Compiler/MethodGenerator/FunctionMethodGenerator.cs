using System;
using System.Collections.Generic;

namespace Nitrassic.Compiler
{
	/// <summary>
	/// Represents the information needed to compile a function.
	/// </summary>
	internal class FunctionMethodGenerator : MethodGenerator
	{	
		
		/// <summary>
		/// Creates a new FunctionMethodGenerator instance.
		/// </summary>
		/// <param name="engine"> The script engine. </param>
		/// <param name="scope"> The function scope. </param>
		/// <param name="functionName"> The name of the function. </param>
		/// <param name="includeNameInScope"> Indicates whether the name should be included in the function scope. </param>
		/// <param name="args"> The names of the arguments. </param>
		/// <param name="bodyText"> The source code of the function. </param>
		/// <param name="body"> The root of the abstract syntax tree for the body of the function. </param>
		/// <param name="scriptPath"> The URL or file system path that the script was sourced from. </param>
		/// <param name="options"> Options that influence the compiler. </param>
		public FunctionMethodGenerator(ScriptEngine engine, DeclarativeScope scope, string functionName, bool includeNameInScope, IList<ArgVariable> args, string bodyText, Statement body, string scriptPath, CompilerOptions options)
			: base(engine, scope, new DummyScriptSource(scriptPath), options)
		{
			this.Name = functionName;
			this.IncludeNameInScope = includeNameInScope;
			this.Arguments = args;
			this.BodyRoot = body;
			this.BodyText = bodyText;
			
			// Claim an ID:
			MethodID = MethodLookup.GetNextID();
			
			Validate();
		}

		/// <summary>
		/// Dummy implementation of ScriptSource.
		/// </summary>
		private class DummyScriptSource : ScriptSource
		{
			private string path;

			public DummyScriptSource(string path)
			{
				this.path = path;
			}

			public override string Path
			{
				get { return this.path; }
			}

			public override System.IO.TextReader GetReader()
			{
				throw new NotImplementedException();
			}
		}

		/// <summary>
		/// Creates a new FunctionContext instance.
		/// </summary>
		/// <param name="engine"> The script engine. </param>
		/// <param name="scope"> The function scope. </param>
		/// <param name="functionName"> The name of the function. </param>
		/// <param name="args"> The names of the arguments. </param>
		/// <param name="body"> The source code for the body of the function. </param>
		/// <param name="options"> Options that influence the compiler. </param>
		public FunctionMethodGenerator(ScriptEngine engine, DeclarativeScope scope, string functionName, IList<ArgVariable> args, string body, CompilerOptions options)
			: base(engine, scope, new StringScriptSource(body), options)
		{
			this.Name = functionName;
			this.Arguments = args;
			this.BodyText = body;
		}

		/// <summary>
		/// Gets the name of the function.
		/// </summary>
		public string Name;
		
		/// <summary>
		/// Indicates whether the function name should be included in the function scope (i.e. if
		/// this is <c>false</c>, then you cannot reference the function by name from within the
		/// body of the function).
		/// </summary>
		internal bool IncludeNameInScope;

		/// <summary>
		/// Gets a list of arguments and their optional type.
		/// </summary>
		public IList<ArgVariable> Arguments;
		
		/// <summary>
		/// Gets the root of the abstract syntax tree for the body of the function.
		/// </summary>
		public Statement BodyRoot;

		/// <summary>
		/// Gets the source code for the body of the function.
		/// </summary>
		public string BodyText;

		/// <summary>
		/// Gets a name for the generated method.
		/// </summary>
		/// <returns> A name for the generated method. </returns>
		protected override string GetMethodName()
		{
			if (string.IsNullOrEmpty(this.Name))
				return "anonymous";
			else
				return this.Name;
		}
		
		/// <summary>Declare an arg variable at the start of the args set.</summary>
		internal ArgVariable DeclareHoisted(string name,Type type)
		{
			
			// Create it:
			ArgVariable arg=new ArgVariable(name);
			
			// Set the type:
			arg.Type=type;
			
			// Add a blank arg to the end of the set:
			Arguments.Add(null);
			
			// Time to shift all the args over!
			for(int i=Arguments.Count-1;i>=0;i--)
			{
				
				// Get it:
				ArgVariable currentArg=Arguments[i];
				
				// Update the index:
				currentArg.ArgumentID=i+1;
				
				// Set it back:
				Arguments[i+1]=currentArg;
				
			}
			
			// Declare the arg in the scope:
			(InitialScope as DeclarativeScope).Declare(arg);
			
			return arg;
		}
		
		internal void GenerateCode(OptimizationInfo optimizationInfo){
			
			GenerateCode(Arguments,optimizationInfo,BodyText);
			
		}
		
		/// <summary>
		/// Gets a name for the function, as it appears in the stack trace.
		/// </summary>
		/// <returns> A name for the function, as it appears in the stack trace, or <c>null</c> if
		/// this generator is generating code in the global scope. </returns>
		protected override string GetStackName()
		{
			return GetMethodName();
		}

		/// <summary>
		/// Gets an array of types - one for each parameter accepted by the method generated by
		/// this context.
		/// </summary>
		/// <returns> An array of parameter types. </returns>
		protected override Type[] GetParameterTypes()
		{
			return new Type[] {
				typeof(object),					 // The "this" object.
				typeof(object[])					// The argument values.
			};
		}
		
		public bool HasArgument(string name)
		{
			
			foreach(ArgVariable arg in Arguments)
			{
				if(arg.Name==name)
				{
					return true;
				}
			}
			
			return false;
		}
		/// <summary>
		/// Checks whether the function is valid (in strict mode the function cannot be named
		/// 'arguments' or 'eval' and the argument names cannot be duplicated).
		/// </summary>
		private void Validate()
		{
			if (this.StrictMode == true)
			{
				// If the function body is strict mode, then the function name cannot be 'eval' or 'arguments'.
				if (this.Name == "arguments" || this.Name == "eval")
					throw new JavaScriptException(this.Engine, "SyntaxError", "Functions cannot be named '"+Name+"' in strict mode.");

				// If the function body is strict mode, then the argument names cannot be 'eval' or 'arguments'.
				foreach (ArgVariable arg in Arguments)
					if (arg.Name == "arguments" || arg.Name == "eval")
						throw new JavaScriptException(this.Engine, "SyntaxError", "Arguments cannot be named '"+arg.Name+"' in strict mode.");

				/*
				// If the function body is strict mode, then the argument names cannot be duplicates.
				var duplicateCheck = new HashSet<string>();
				foreach (ArgVariable arg in Arguments)
				{
					if (duplicateCheck.Contains(arg.Name) == true)
						throw new JavaScriptException(this.Engine, "SyntaxError", "Duplicate argument name '"+arg.Name+"' is not allowed in strict mode.");
					duplicateCheck.Add(arg.Name);
				}
				*/
			}
		}

		/// <summary>
		/// Parses the source text into an abstract syntax tree.
		/// </summary>
		/// <returns> The root node of the abstract syntax tree. </returns>
		public override void Parse(OptimizationInfo optimizationInfo)
		{
			if (this.BodyRoot != null)
			{
				this.AbstractSyntaxTree = this.BodyRoot;
			}
			else
			{
				using (var lexer = new Lexer(this.Engine, this.Source))
				{
					var parser = new Parser(this.Engine, lexer, this.InitialScope, optimizationInfo, this.Options, CodeContext.Function);
					this.AbstractSyntaxTree = parser.Parse();
					this.StrictMode = parser.StrictMode;
					this.MethodOptimizationHints = parser.MethodOptimizationHints;
				}
				Validate();
			}
		}

		/// <summary>
		/// Retrieves a delegate for the generated method.
		/// </summary>
		/// <returns> The delegate type that matches the method parameters. </returns>
		protected override Type GetDelegate()
		{
			return typeof(Library.FunctionDelegate);
		}

		/// <summary>
		/// Generates IL for the script.
		/// </summary>
		/// <param name="generator"> The generator to output the CIL to. </param>
		/// <param name="optimizationInfo"> Information about any optimizations that should be performed. </param>
		protected override void GenerateCode(ILGenerator generator, OptimizationInfo optimizationInfo)
		{
			// Method signature: object FunctionDelegate(object thisObj, object[] arguments)
			
			// Transfer the function name into the scope.
			if (!string.IsNullOrEmpty(Name) &&
				IncludeNameInScope &&
				!HasArgument(Name) &&
				optimizationInfo.MethodOptimizationHints.HasVariable(Name))
			{
				
				var functionName = new NameExpression(InitialScope, Name);
				functionName.GenerateSet(generator, optimizationInfo, typeof(object), delegate()
				{
					
					generator.LoadInt64(MethodID);
					generator.Call(ReflectionHelpers.MethodLookup_Load);
					
				}, false);
				
			}
			
			// Transfer the arguments object into the scope.
			if (MethodOptimizationHints.HasArguments && !HasArgument("arguments"))
			{
				
				var arguments = new NameExpression(this.InitialScope, "arguments");
				arguments.GenerateSet(generator, optimizationInfo, typeof(object), delegate()
				{
					
					// argumentValues
					EmitHelpers.LoadArgumentsArray(generator);
					generator.NewObject(ReflectionHelpers.Arguments_Constructor);
					
				}, false);
			}
			
			// Initialize any declarations.
			(this.InitialScope as DeclarativeScope).GenerateDeclarations(generator, optimizationInfo);
			
			// Generate code for the body of the function.
			this.AbstractSyntaxTree.GenerateCode(generator, optimizationInfo);

			// Define the return target - this is where the return statement jumps to.
			// ReturnTarget can be null if there were no return statements.
			if (optimizationInfo.ReturnTarget != null)
				generator.DefineLabelPosition(optimizationInfo.ReturnTarget);

			// Load the return value.  If the variable is null, there were no return statements.
			if (optimizationInfo.ReturnVariable != null)
				// Return the value stored in the variable.  Will be null if execution hits the end
				// of the function without encountering any return statements.
				generator.LoadVariable(optimizationInfo.ReturnVariable);
			else
				// There were no return statements - return null.
				generator.LoadNull();
		}

		/// <summary>
		/// Converts this object to a string.
		/// </summary>
		/// <returns> A string representing this object. </returns>
		public override string ToString()
		{
			if (this.BodyRoot != null)
				return string.Format("function {0}({1}) {2}", this.Name, StringHelpers.Join(", ", this.Arguments), this.BodyRoot);
			return string.Format("function {0}({1}) {{\n{2}\n}}", this.Name, StringHelpers.Join(", ", this.Arguments), this.BodyText);
		}
	}

}