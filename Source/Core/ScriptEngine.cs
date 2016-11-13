using System;
using System.Collections.Generic;
using Nitrassic.Library;

namespace Nitrassic
{
	/// <summary>
	/// Represents the JavaScript script engine.  This is the first object that needs to be
	/// instantiated in order to execute javascript code.
	/// </summary>
	public partial class ScriptEngine{
		
		/// <summary>
		/// Gets or sets a value that indicates whether to disassemble any generated IL and store it
		/// in the associated function.
		/// </summary>
		public static bool EnableILAnalysis;
		
		/// <summary>
		/// Gets or sets a value which indicates whether debug information should be generated.  If
		/// this is set to <c>true</c> performance and memory usage are negatively impacted.
		/// </summary>
		public static bool EnableDebugging;
		
		/// <summary>
		/// Gets or sets a value that indicates whether to force ECMAScript 5 strict mode, even if
		/// the code does not contain a strict mode directive ("use strict").  The default is
		/// <c>false</c>.
		/// </summary>
		public static bool ForceStrictMode;
		
		/// <summary>
		/// A value that indicates if warnings about collapsed global variables should be triggered.
		/// A collapsing global has performance implications, so they're best avoided. The default is <c>true</c>.
		/// </summary>
		public static bool CollapseWarning=true;
		
		
		public PrototypeLookup Prototypes;
		
		/// <summary>
		/// The built-in global prototype.
		/// </summary>
		internal Prototype GlobalPrototype;
		
		/// <summary>
		/// An instance of the global object. Note that virtually all the fields are static.
		/// It only exists as an object for use by 'this'.</summary>
		internal object Global;
		
		/// <summary>The global scope.</summary>
		internal Compiler.ObjectScope GlobalScope;
		
		/// <summary>
		/// Initializes a new scripting environment.
		/// </summary>
		public ScriptEngine()
		{
			
			Compiler.Operator.Setup();
			
			// Create the lookup or use the global one:
			if(PrototypeLookup.All==null){
				Prototypes=new PrototypeLookup(this);
				Prototypes.Setup();
				PrototypeLookup.All=Prototypes;
			}else{
				
				// Share the lookup:
				Prototypes=PrototypeLookup.All;
				
				// Setup the global cache:
				ReflectionEmitInfo.SetupGlobalCache();
				
			}
			
		}
		
		/// <summary>
		/// Logs the given message.
		/// </summary>
		public void Log(string message){
			Wrench.Log.Add(message);
		}
		
		//	 EXECUTION
		//_________________________________________________________________________________________
		
		/// <summary>
		/// Compiles the given source code and returns it in a form that can be executed many
		/// times.
		/// </summary>
		/// <param name="source"> The javascript source code to execute. </param>
		/// <returns> A CompiledScript instance, which can be executed as many times as needed. </returns>
		/// <exception cref="ArgumentNullException"> <paramref name="source"/> is a <c>null</c> reference. </exception>
		public CompiledScript Compile(ScriptSource source)
		{
			
			// Create the new main type:
			reflectionEmitInfo.CreateMainType();
			
			var methodGen = new Nitrassic.Compiler.GlobalMethodGenerator(
				this,							   // The script engine
				source,							 // The source code.
				CreateOptions());				   // The compiler options.
			
			// Optimization info:
			Nitrassic.Compiler.OptimizationInfo info=new Nitrassic.Compiler.OptimizationInfo(this);
			
			// Parse
			methodGen.Parse(info);
			
			// Generate code
			methodGen.GenerateCode(null,info);
			
			// Bake all the prototypes:
			Prototypes.CompleteAll();
			
			// Bake the types now:
			reflectionEmitInfo.Close();
			
			// Instance a global scope object (for use by this):
			Global=GlobalPrototype.Instance();
			
			// Started!
			Started();
			
			return new CompiledScript(methodGen);
		}
		
		/// <summary>
		/// Executes the given source code.  Execution is bound to the global scope.
		/// </summary>
		/// <param name="code"> The javascript source code to execute. </param>
		public void Execute(string code)
		{
			Execute(new StringScriptSource(code));
		}

		/// <summary>
		/// Executes the given file.  If the file does not have a BOM then it is assumed to be UTF8.
		/// Execution is bound to the global scope.
		/// </summary>
		/// <param name="path"> The path to a javascript file.  This can be a local file path or a
		/// UNC path. </param>
		/// <exception cref="ArgumentNullException"> <paramref name="path"/> is a <c>null</c> reference. </exception>
		public void ExecuteFile(string path)
		{
			ExecuteFile(path, null);
		}

		/// <summary>
		/// Executes the given file.  Execution is bound to the global scope.
		/// </summary>
		/// <param name="path"> The path to a javascript file.  This can be a local file path or a
		/// UNC path. </param>
		/// <param name="encoding"> The character encoding to use if the file lacks a byte order
		/// mark (BOM). </param>
		/// <exception cref="ArgumentNullException"> <paramref name="path"/> is a <c>null</c> reference. </exception>
		public void ExecuteFile(string path, System.Text.Encoding encoding)
		{
			Execute(new FileScriptSource(path, encoding));
		}

		/// <summary>
		/// Executes the given source code.  Execution is bound to the global scope.
		/// </summary>
		/// <param name="source"> The javascript source code to execute. </param>
		/// <exception cref="ArgumentNullException"> <paramref name="source"/> is a <c>null</c> reference. </exception>
		public void Execute(ScriptSource source)
		{
			// Compile the script.
			var compiledScript = Compile(source);

			// ...and execute it.
			compiledScript.Execute();
		}
		
		/// <summary>
		/// Creates a CompilerOptions instance using the script engine properties.
		/// </summary>
		/// <returns> A populated CompilerOptions instance. </returns>
		private Compiler.CompilerOptions CreateOptions()
		{
			return new Compiler.CompilerOptions() { ForceStrictMode = ScriptEngine.ForceStrictMode, EnableDebugging = ScriptEngine.EnableDebugging };
		}



		//	 GLOBAL HELPERS
		//_________________________________________________________________________________________

		/// <summary>
		/// Gets a value that indicates whether the global variable with the given name is defined.
		/// </summary>
		/// <param name="variableName"> The name of the variable to check. </param>
		/// <returns> <c>true</c> if the given variable has a value; <c>false</c> otherwise. </returns>
		/// <remarks> Note that a variable that has been set to <c>undefined</c> is still
		/// considered to have a value. </remarks>
		public bool HasGlobalValue(string variableName)
		{
			if (variableName == null)
				throw new ArgumentNullException("variableName");
			
			return (GlobalPrototype[variableName]!=null);
		}

		/// <summary>
		/// Gets the value of the global variable with the given name.
		/// </summary>
		/// <param name="variableName"> The name of the variable to retrieve the value for. </param>
		/// <returns> The value of the global variable, or <c>null</c> otherwise. </returns>
		public object GetGlobal(string variableName)
		{
			if (variableName == null)
				throw new ArgumentNullException("variableName");
			
			PropertyVariable prop=GlobalPrototype[variableName];
			
			if(prop==null){
				throw new ArgumentNullException("'"+variableName+"' is not a global.");
			}
			
			object value=prop.GetValue(Global);
			
			return TypeUtilities.NormalizeValue(value);
		}

		/// <summary>
		/// Gets the value of the global variable with the given name and coerces it to the given
		/// type.
		/// </summary>
		/// <typeparam name="T"> The type to coerce the value to. </typeparam>
		/// <param name="variableName"> The name of the variable to retrieve the value for. </param>
		/// <returns> The value of the global variable, or <c>null</c> otherwise. </returns>
		/// <remarks> Note that <c>null</c> is coerced to the following values: <c>false</c> (if
		/// <typeparamref name="T"/> is <c>bool</c>), 0 (if <typeparamref name="T"/> is <c>int</c>
		/// or <c>double</c>), string.Empty (if <typeparamref name="T"/> is <c>string</c>). </remarks>
		public T GetGlobal<T>(string variableName)
		{
			if (variableName == null)
				throw new ArgumentNullException("variableName");
			return TypeConverter.ConvertTo<T>(this, TypeUtilities.NormalizeValue(GlobalPrototype.GetProperty(variableName).GetValue(Global)));
		}

		/// <summary>
		/// Sets the value of the global variable with the given name.  If the property does not
		/// exist, it will be created.
		/// </summary>
		/// <param name="variableName"> The name of the variable to set. </param>
		/// <param name="value"> The desired value of the variable.  This must be of a supported
		/// type (bool, int, double, string, Null, Undefined or a ObjectInstance-derived type). </param>
		/// <exception cref="JavaScriptException"> The property is read-only or the property does
		/// not exist and the object is not extensible. </exception>
		public void SetGlobal(string variableName, object value)
		{
			SetGlobal(variableName, value,PropertyAttributes.FullAccess);
		}
		
		/// <summary>
		/// Sets the value of the global variable with the given name.  If the property does not
		/// exist, it will be created.
		/// </summary>
		/// <param name="variableName"> The name of the variable to set. </param>
		/// <param name="value"> The desired value of the variable. </param>
		/// <param name="attribs">The attributes of the value.</param>
		/// <exception cref="JavaScriptException"> The property is read-only or the property does
		/// not exist and the object is not extensible. </exception>
		public void SetGlobal(string variableName, object value,PropertyAttributes attribs)
		{
			
			if(value==null){
				value=Null.Value;
			}
			
			// Try getting the property:
			PropertyVariable pv=GlobalPrototype[variableName];
			
			if(pv==null){
				// Create it:
				pv=GlobalPrototype.AddProperty(variableName,value,attribs);
			}
			
			// Await/ set now:
			AwaitStart(new AwaitingStart(pv,value));
			
		}
		
		internal void AwaitStart(AwaitingStart aws){
			
			if(HasStarted){
				
				// Global is currently settable.
				aws.OnStart(this);
				
			}else{
			
				if(AwaitingSet==null){
					AwaitingSet=new List<AwaitingStart>();
				}
				
				// Add to group of vars awaiting set
				// (this happens because we can't actually set the globals value yet - we must wait for it to be fully compiled):
				AwaitingSet.Add(aws);
				
			}
			
		}
		
		/// <summary>Creates a global of the given type.</summary>
		public PropertyVariable SetGlobalType(string variableName,Type type,PropertyAttributes attribs){
			
			// Create it:
			return GlobalPrototype.AddProperty(variableName,attribs,type);
			
		}
		
		/// <summary>True if the code has been compiled and the global scope is ready to go.</summary>
		public bool HasStarted{
			get{
				return (Global!=null);
			}
		}
		
		public void Started(){
			
			// Run everything in the waiting set:
			if(AwaitingSet==null){
				return;
			}
			
			for(int i=0;i<AwaitingSet.Count;i++){
				
				// Start it:
				AwaitingSet[i].OnStart(this);
				
			}
			
			// Clear set:
			AwaitingSet=null;
			
		}
		
		internal List<AwaitingStart> AwaitingSet;
		
		/// <summary>
		/// Calls a global function and returns the result.
		/// </summary>
		/// <param name="functionName"> The name of the function to call. </param>
		/// <param name="argumentValues"> The argument values to pass to the function. </param>
		/// <returns> The return value from the function. </returns>
		public object CallGlobalFunction(string functionName, params object[] argumentValues)
		{
			if (functionName == null)
				throw new ArgumentNullException("functionName");
			if (argumentValues == null)
				throw new ArgumentNullException("argumentValues");
				
			return GlobalPrototype.CallMemberFunctionOn(null,functionName,argumentValues);
			
		}

		/// <summary>
		/// Calls a global function and returns the result.
		/// </summary>
		/// <typeparam name="T"> The type to coerce the value to. </typeparam>
		/// <param name="functionName"> The name of the function to call. </param>
		/// <param name="argumentValues"> The argument values to pass to the function. </param>
		/// <returns> The return value from the function, coerced to the given type. </returns>
		public T CallGlobalFunction<T>(string functionName, params object[] argumentValues)
		{
			if (functionName == null)
				throw new ArgumentNullException("functionName");
			if (argumentValues == null)
				throw new ArgumentNullException("argumentValues");
			return TypeConverter.ConvertTo<T>(this, CallGlobalFunction(functionName, argumentValues));
		}

		/// <summary>
		/// Sets the global variable with the given name to a function implemented by the provided
		/// delegate.
		/// </summary>
		/// <param name="functionName"> The name of the global variable to set. </param>
		/// <param name="functionDelegate"> The delegate that will implement the function. </param>
		public void SetGlobalFunction(string functionName, Delegate functionDelegate)
		{
			if (functionName == null)
				throw new ArgumentNullException("functionName");
			if (functionDelegate == null)
				throw new ArgumentNullException("functionDelegate");
			SetGlobal(functionName, functionDelegate);
		}




		//	 STACK TRACE SUPPORT
		//_________________________________________________________________________________________
		
		/// <summary>
		/// Creates a stack trace.
		/// </summary>
		/// <param name="depth"> The number of stack frames to ignore. </param>
		internal string FormatStackTrace(int depth)
		{
			System.Text.StringBuilder result = new System.Text.StringBuilder();
			
			System.Diagnostics.StackTrace trace=new System.Diagnostics.StackTrace(depth+1,true);
			
			for(int f=0; f<trace.FrameCount;f++){
				
				System.Diagnostics.StackFrame frame=trace.GetFrame(f);
				
				string methodName=frame.GetMethod().Name;
				
				AppendStackFrame(result, frame.GetFileName(), methodName, frame.GetFileLineNumber(), frame.GetFileColumnNumber());
				
				if(methodName=="__.main"){
					break;
				}
				
			}
			
			return result.ToString();
		}

		/// <summary>
		/// Appends a stack frame to the end of the given StringBuilder instance.
		/// </summary>
		/// <param name="result"> The StringBuilder to append to. </param>
		/// <param name="path"> The path of the javascript source file. </param>
		/// <param name="function"> The name of the function. </param>
		/// <param name="line"> The line number of the statement. </param>
		private void AppendStackFrame(System.Text.StringBuilder result, string path, string function, int line, int column)
		{
			
			if(function=="__.main"){
				
			}else if (!string.IsNullOrEmpty(function))
			{
				result.Append(function);
			}else{
				result.Append("anonymous");
			}
			
			result.Append("@");
			
			result.Append(path ?? "unknown path");
			
			if (line > 0)
			{
				result.Append(":");
				result.Append(line);
				
				if(column > 0){
					result.Append(":");
					result.Append(column);
				}
			}
			
			result.AppendLine();
			
		}
		
	}
}
