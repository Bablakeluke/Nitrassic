using System;
using System.Collections.Generic;

namespace Nitrassic.Compiler
{
	
	/// <summary>
    /// Represents how the function was defined.
    /// </summary>
    internal enum FunctionDeclarationType
    {
        /// <summary>
        /// The function was declared as a statement.
        /// </summary>
        Declaration,

        /// <summary>
        /// The function was declared as an expression.
        /// </summary>
        Expression,

        /// <summary>
        /// The function is a getter in an object literal.
        /// </summary>
        Getter,

        /// <summary>
        /// The function is a setter in an object literal.
        /// </summary>
        Setter,
	}
	
	/// <summary>
	/// Represents the information needed to compile a function.
	/// </summary>
	public class FunctionMethodGenerator : MethodGenerator
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
		public FunctionMethodGenerator(ScriptEngine engine, DeclarativeScope scope, string functionName, bool includeNameInScope, List<ArgVariable> args, Statement body, string scriptPath, CompilerOptions options)
			: base(engine, scope, new DummyScriptSource(scriptPath), options)
		{
			this.Name = functionName;
			this.IncludeNameInScope = includeNameInScope;
			this.Arguments = args;
			this.BodyRoot = body;
			
			// Claim an ID:
			MethodID = MethodLookup.GetNextID();
			
			// Store the generated method in the cache:
			MethodLookup.SaveAs(MethodID,this);
			
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
		public FunctionMethodGenerator(ScriptEngine engine, DeclarativeScope scope, string functionName, List<ArgVariable> args, string body, CompilerOptions options)
			: base(engine, scope, new StringScriptSource(body), options)
		{
			this.Name = functionName;
			this.Arguments = args;
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
		public List<ArgVariable> Arguments;
		
		/// <summary>
		/// Gets the root of the abstract syntax tree for the body of the function.
		/// </summary>
		public Statement BodyRoot;
		
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
		
		/// <summary>True if the given method info is from this generator.</summary>
		internal Library.UserDefinedFunction Find(System.Reflection.MethodInfo mtd){
			
			for(int i=0;i<GeneratedMethods.Count;i++){
				
				if(GeneratedMethods[i].body==mtd){
					return GeneratedMethods[i];
				}
				
			}
			
			return null;
			
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
			
			// Time to shift all the args over except for 'this' (arg 0)!
			for(int i=Arguments.Count-1;i>0;i--)
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
			
			// Bake the args into a fixed length array:
			GenerateCode(Arguments.ToArray(),optimizationInfo);
			
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
		
		private Library.Prototype _RawInstancePrototype;
		
		/// <summary>
		/// Gets the prototype of objects constructed using this function.  Equivalent to
		/// the Function.prototype property.
		/// </summary>
		public Library.Prototype GetInstancePrototype(ScriptEngine engine){
			
			if(_RawInstancePrototype==null){
				
				// Create one now!
				_RawInstancePrototype=engine.Prototypes.Create();
				
			}
			
			return _RawInstancePrototype;
			
		}
		
		/// <summary>Gets the compiled function for the given args set. Note that this args set
		/// is the actual types and always includes the 'this' keywords type.</summary>
		public Library.UserDefinedFunction GetCompiled(Type[] args,ScriptEngine engine,bool isConstructor){
			
			int genCount=GeneratedMethods.Count;
			
			for(int i=0;i<genCount;i++){
				
				if(GeneratedMethods[i].ArgsEqual(args)){
					
					// Got a match! Already compiled it.
					return GeneratedMethods[i];
					
				}
				
			}
			
			// Need to compile it now with our given arg types.
			int argCount=args==null?0:args.Length;
			
			// The source contains a fixed number of args; it goes up to this:
			int maxArg=Arguments.Count;
			
			// First, map args to a block of ArgVariable objects:
			ArgVariable[] argsSet=new ArgVariable[argCount];
			
			for(int i=0;i<argCount;i++){
				
				// Setup the arg variable:
				ArgVariable curArg;
				
				if(i<maxArg){
					
					// Use the existing args object so it correctly updates in the scope:
					curArg=Arguments[i];
				
				}else{
					
					// Need to create a fake one:
					curArg=new ArgVariable("@unused-arg-"+i);
					
				}
				
				// Apply type (a little different here as we have to account for null too):
				curArg.RawType=args[i];
				
				// Apply to set:
				argsSet[i]=curArg;
				
			}
			
			// If we're passing less args than the source supports, update the types of those unused args to being 'Undefined':
			for(int i=argCount;i<maxArg;i++){
				
				Arguments[i].Type=typeof(Nitrassic.Undefined);
				
			}
			
			// Create info:
			OptimizationInfo info=new OptimizationInfo(engine);
			info.IsConstructor=isConstructor;
			
			// Generate it!
			return GenerateCode(argsSet,info);
			
		}
		
		/// <summary>Gets the compiled function for the given args set. Note that this args set
		/// is the actual values and always includes the 'this' keywords value.</summary>
		public Library.UserDefinedFunction GetCompiled(ScriptEngine engine,object[] args){
			
			int genCount=GeneratedMethods.Count;
			
			for(int i=0;i<genCount;i++){
				
				if(GeneratedMethods[i].ArgsEqual(args)){
					
					// Got a match! Already compiled it.
					return GeneratedMethods[i];
					
				}
				
			}
			
			// Need to compile it now with our given arg types.
			int argCount=args.Length;
			
			if(argCount>Arguments.Count){
				// The actual source can only handle so many args.
				argCount=Arguments.Count;
			}
			
			// First, map args to Arguments:
			ArgVariable[] argsSet=new ArgVariable[argCount];
			
			for(int i=0;i<argCount;i++){
				
				// Setup the arg variable:
				ArgVariable curArg=new ArgVariable(Arguments[i].Name);
				
				// Apply type:
				curArg.Type=args[i].GetType();
				
				// Apply to set:
				argsSet[i]=curArg;
				
			}
			
			// Generate it!
			return GenerateCode(argsSet,new OptimizationInfo(engine));
			
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
		protected override void GenerateCode(ArgVariable[] arguments,ILGenerator generator, OptimizationInfo optimizationInfo)
		{
			// Method signature: object FunctionDelegate(object thisObj, object[] arguments)
			
			// Transfer the function name into the scope.
			if (!string.IsNullOrEmpty(Name) &&
				IncludeNameInScope &&
				!HasArgument(Name) &&
				optimizationInfo.MethodOptimizationHints.HasVariable(Name))
			{
				
				var functionName = new NameExpression(InitialScope, Name);
				
				functionName.ApplyType(optimizationInfo,typeof(object));
				
				functionName.GenerateSet(generator, optimizationInfo,false, typeof(object), delegate(bool two)
				{
					
					generator.LoadInt64(MethodID);
					generator.Call(ReflectionHelpers.MethodLookup_Load);
					
					if(two){
						// Duplicate it:
						generator.Duplicate();
					}
					
				}, false);
				
			}
			
			// Transfer the arguments object into the scope.
			if (MethodOptimizationHints.HasArguments && !HasArgument("arguments"))
			{
				
				var argsSet = new NameExpression(this.InitialScope, "arguments");
				
				argsSet.ApplyType(optimizationInfo,typeof(object));
				
				argsSet.GenerateSet(generator, optimizationInfo,false, typeof(object), delegate(bool two)
				{
					
					// argumentValues
					
					// Build an object[] from the arg values.
					
					// Define an array:
					int argCount=(arguments==null)?0 : arguments.Length;
					
					generator.LoadInt32(argCount);
					generator.NewArray(typeof(object));
					
					for(int a=0;a<argCount;a++)
					{
						
						// One of many args:
						ArgVariable currentArg=arguments[a];
						
						generator.Duplicate();
						generator.LoadInt32(a);
						currentArg.Get(generator);
						EmitConversion.ToAny(generator,currentArg.Type);
						generator.StoreArrayElement(typeof(object));
					}
					
					
					generator.NewObject(ReflectionHelpers.Arguments_Constructor);
					
					if(two){
						generator.Duplicate();
					}
					
				}, false);
			}
			
			// Temp cache return var/target:
			var retVar=optimizationInfo.ReturnVariable;
			var retTarg=optimizationInfo.ReturnTarget;
			
			optimizationInfo.ReturnVariable=null;
			optimizationInfo.ReturnTarget=null;
			
			// Initialize any declarations.
			(this.InitialScope as DeclarativeScope).GenerateDeclarations(generator, optimizationInfo);
			
			// Generate code for the body of the function.
			this.AbstractSyntaxTree.GenerateCode(generator, optimizationInfo);

			// Define the return target - this is where the return statement jumps to.
			// ReturnTarget can be null if there were no return statements.
			if (optimizationInfo.ReturnTarget != null)
				generator.DefineLabelPosition(optimizationInfo.ReturnTarget);
			
			// Load the return value.  If the variable is null, there were no return statements.
			if (optimizationInfo.ReturnVariable != null){
				// Return the value stored in the variable.  Will be null if execution hits the end
				// of the function without encountering any return statements.
				generator.LoadVariable(optimizationInfo.ReturnVariable);
			}
			
			// Restore:
			optimizationInfo.ReturnVariable=retVar;
			optimizationInfo.ReturnTarget=retTarg;
			
		}

		/// <summary>
		/// Converts this object to a string.
		/// </summary>
		/// <returns> A string representing this object. </returns>
		public override string ToString()
		{
			
			string str="function "+Name+"("+StringHelpers.Join(", ", Arguments)+") {{\n";
			
			if(BodyRoot==null){
				str+="[Native code]";
			}else{
				str+=this.BodyRoot.ToString();
			}
			
			return str+"\n}}";
			
		}
	}

}