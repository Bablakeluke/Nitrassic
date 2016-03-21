using System;
using System.Collections.Generic;
using Nitrassic.Compiler;
using System.Reflection;


namespace Nitrassic.Library
{
	/// <summary>
	/// Represents a JavaScript function implemented in javascript.
	/// </summary>
	public class UserDefinedFunction : FunctionInstance
	{
		
		internal MethodInfo body;
		
		/// <summary>
		/// Creates a new instance of a user-defined function.
		/// </summary>
		/// <param name="prototype"> The next object in the prototype chain. </param>
		/// <param name="name"> The name of the function. </param>
		/// <param name="args"> The names of the arguments. </param>
		/// <param name="bodyText"> The source code for the body of the function. </param>
		internal static UserDefinedFunction Create(ScriptEngine engine, string name, IList<ArgVariable> args, string bodyText){
			
			if (name == null)
				throw new ArgumentNullException("name");
			if (args == null)
				throw new ArgumentNullException("args");
			if (bodyText == null)
				throw new ArgumentNullException("bodyText");

			// Set up a new function scope.
			var scope = DeclarativeScope.CreateFunctionScope(engine.GlobalScope, name, args);
			
			// Setup new optimization info:
			OptimizationInfo info=new OptimizationInfo(engine);
			
			// Compile the code.
			var context = new FunctionMethodGenerator(engine, scope, name, args, bodyText, new CompilerOptions());
			context.GenerateCode(info);
			
			return context.GeneratedMethod;
			
		}
		
		/// <summary>
		/// Creates a new instance of a user-defined function.
		/// </summary>
		/// <param name="prototype"> The next object in the prototype chain. </param>
		/// <param name="name"> The name of the function. </param>
		/// <param name="args"> The names of the arguments. </param>
		/// <param name="bodyText"> The source code for the function body. </param>
		/// <param name="body"> A delegate which represents the body of the function. </param>
		/// <param name="strictMode"> <c>true</c> if the function body is strict mode; <c>false</c> otherwise. </param>
		internal UserDefinedFunction(Prototype prototype, string name, IList<ArgVariable> args, string bodyText, MethodInfo body, bool strictMode)
			: base(prototype)
		{
			Init(name, args, bodyText, body, null, strictMode, true);
		}

		/// <summary>
		/// Creates a new instance of a user-defined function.
		/// </summary>
		/// <param name="prototype"> The next object in the prototype chain. </param>
		/// <param name="name"> The name of the function. </param>
		/// <param name="args"> The names of the arguments. </param>
		/// <param name="bodyText"> The source code for the function body. </param>
		/// <param name="generatedMethod"> A delegate which represents the body of the function plus any dependencies. </param>
		/// <param name="strictMode"> <c>true</c> if the function body is strict mode; <c>false</c> otherwise. </param>
		public UserDefinedFunction(Prototype prototype, string name, IList<ArgVariable> args, string bodyText, MethodInfo delegateToMethod, IList<UserDefinedFunction> dependencies, bool strictMode)
			: base(prototype)
		{
			Init(name, args, bodyText, delegateToMethod, dependencies, strictMode, true);
		}

		/// <summary>
		/// Initializes a user-defined function.
		/// </summary>
		/// <param name="name"> The name of the function. </param>
		/// <param name="args"> The names of the arguments. </param>
		/// <param name="bodyText"> The source code for the function body. </param>
		/// <param name="generatedMethod"> A delegate which represents the body of the function, plus any dependencies. </param>
		/// <param name="strictMode"> <c>true</c> if the function body is strict mode; <c>false</c> otherwise. </param>
		/// <param name="hasInstancePrototype"> <c>true</c> if the function should have a valid
		/// "prototype" property; <c>false</c> if the "prototype" property should be <c>null</c>. </param>
		private void Init(string name, IList<ArgVariable> args, string bodyText, MethodInfo delegateToMethod, IList<UserDefinedFunction> dependencies, bool strictMode, bool hasInstancePrototype)
		{
			if (name == null)
				throw new ArgumentNullException("name");
			if (args == null){
				args=new ArgVariable[0];
			}
			if (bodyText == null)
				throw new ArgumentNullException("bodyText");
			this.Arguments = args;
			this.BodyText = bodyText;
			this.Dependencies = dependencies;
			this.body = delegateToMethod;
			this.StrictMode = strictMode;

			// Add function properties.
			this.FastSetProperty("name", name, PropertyAttributes.Configurable);
			this.FastSetProperty("length", args.Count, PropertyAttributes.Configurable);

			// The empty function doesn't have an instance prototype.
			if (hasInstancePrototype == true)
			{
				this.FastSetProperty("prototype",ObjectInstance.OnConstruct(Engine), PropertyAttributes.Writable);
				#warning this is incorrect; instanceProto is actually the (globally shared) Object proto.
				// this.InstancePrototype.AddProperty("constructor", this, PropertyAttributes.NonEnumerable);
			}
		}
		
		//	 PROPERTIES
		//_________________________________________________________________________________________

		/// <summary>
		/// Gets a list containing the names of the function arguments, in order of definition.
		/// This list can contain duplicate names.
		/// </summary>
		internal IList<ArgVariable> Arguments;

		/// <summary>
		/// Gets a value that indicates whether the function was declared within strict mode code
		/// -or- the function contains a strict mode directive within the function body.
		/// </summary>
		public bool StrictMode;
		
		/// <summary>
		/// Gets the source code for the body of the function.
		/// </summary>
		public string BodyText;
		
		/// <summary>
		/// Gets a list of dependent generated methods.
		/// </summary>
		public IList<UserDefinedFunction> Dependencies;

		/// <summary>
		/// Gets the body of the method in the form of disassembled IL code.  Will be <c>null</c>
		/// unless ScriptEngine.EnableILAnalysis has been set to <c>true</c>.
		/// </summary>
		public string DisassembledIL;



		//	 OVERRIDES
		//_________________________________________________________________________________________

		/// <summary>
		/// Calls this function, passing in the given "this" value and zero or more arguments.
		/// </summary>
		/// <param name="thisObj"> The value of the "this" keyword within the function. </param>
		/// <param name="argumentValues"> An array of argument values to pass to the function. </param>
		/// <returns> The value that was returned from the function. </returns>
		public override object CallLateBound(object thisObj, params object[] argumentValues)
		{
			
			if(body is System.Reflection.Emit.MethodBuilder)
			{
				// Close the type.
				Close();
			}
			
			// Call the function.
			return body.Invoke(thisObj,argumentValues);
		}
		
		/// <summary>Closes the type builder as it's still open.</summary>
		private void Close(){
			
			// Get the builder:
			var builder=(body.DeclaringType as System.Reflection.Emit.TypeBuilder);
			
			// Get the host type:
			Type hostType=builder.CreateType();
			
			if(builder==Engine.ReflectionEmitInfo.MainType)
			{
				// Clear the main type:
				Engine.ReflectionEmitInfo.MainType=null;
			}
			
			// Get the new field:
			body=hostType.GetMethod(body.Name);
			
		}
		
		/// <summary>
		/// Returns a string representing this object.
		/// </summary>
		/// <returns> A string representing this object. </returns>
		public override string ToString()
		{
			string str="function "+Name+"("+StringHelpers.Join(", ", Arguments)+") {{\n";
			
			if(BodyText==null){
				str+="[Native code]";
			}else{
				str+=BodyText;
			}
			
			return str+"\n}}";
			
		}
	}
}
