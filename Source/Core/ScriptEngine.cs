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
		
		// Compatibility mode.
		private CompatibilityMode compatibilityMode;
		
		public PrototypeLookup Prototypes;
		
		/// <summary>
		/// Gets the built-in global object.  This object is implicitly accessed when creating
		/// global variables and functions.
		/// </summary>
		internal Window Global;
		
		// The global scope.
		public Compiler.ObjectScope GlobalScope;
		
		// Mono check.
		internal static bool IsMonoRuntime = Type.GetType("Mono.Runtime") != null;
		
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
				
				// Create all the built-in objects.
				Global = new Window(this);
				
				// Setup the global cache:
				ReflectionEmitInfo.SetupGlobalCache();
				
			}
			
			// Global scope:
			GlobalScope=Compiler.ObjectScope.CreateGlobalScope(this);
			
		}
		
		//	 PROPERTIES
		//_________________________________________________________________________________________

		/// <summary>
		/// Gets or sets a value that indicates whether to force ECMAScript 5 strict mode, even if
		/// the code does not contain a strict mode directive ("use strict").  The default is
		/// <c>false</c>.
		/// </summary>
		public bool ForceStrictMode;
		
		/// <summary>
		/// A value that indicates if warnings about collapsed global variables should be triggered.
		/// A collapsing global has performance implications, so they're best avoided. The default is <c>true</c>.
		/// </summary>
		public bool CollapseWarning=true;
		
		/// <summary>
		/// Logs the given message.
		/// </summary>
		public void Log(string message){
			Wrench.Log.Add(message);
		}
		
		/// <summary>
		/// Gets or sets a value that indicates whether the script engine should run in
		/// compatibility mode.
		/// </summary>
		public CompatibilityMode CompatibilityMode
		{
			get { return this.compatibilityMode; }
			set
			{
				this.compatibilityMode = value;

				// Infinity, NaN and undefined are writable in ECMAScript 3 and read-only in ECMAScript 5.
				var attributes = PropertyAttributes.Sealed;
				if (this.CompatibilityMode == CompatibilityMode.ECMAScript3)
					attributes = PropertyAttributes.Writable;
				SetGlobal("Infinity", double.PositiveInfinity, attributes);
				SetGlobal("NaN", double.NaN, attributes);
				SetGlobal("undefined", Undefined.Value, attributes);
			}
		}

		/// <summary>
		/// Gets or sets a value that indicates whether to disassemble any generated IL and store it
		/// in the associated function.
		/// </summary>
		public bool EnableILAnalysis;
		
		/// <summary>
		/// Gets a value that indicates whether the script engine must run in a low privilege environment.
		/// </summary>
		internal static bool LowPrivilegeEnvironment
		{
			get { return true; }
		}

		/// <summary>
		/// Indicates that the current AppDomain is a low privilege environment.
		/// </summary>
		internal static void SetLowPrivilegeEnvironment()
		{
		}
		
		[ThreadStatic]
		private static ScriptEngine deserializationEnvironment;

		/// <summary>
		/// Gets or sets the script engine to use when deserializing objects.  This property is a
		/// per-thread setting; it must be set on the thread that is doing the deserialization.
		/// </summary>
		public static ScriptEngine DeserializationEnvironment
		{
			get { return deserializationEnvironment; }
			set { deserializationEnvironment = value; }
		}
		
		//	 DEBUGGING SUPPORT
		//_________________________________________________________________________________________

		/// <summary>
		/// Gets or sets a value which indicates whether debug information should be generated.  If
		/// this is set to <c>true</c> performance and memory usage are negatively impacted.
		/// </summary>
		public bool EnableDebugging;
		
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
			if (this.ParsingStarted != null)
				this.ParsingStarted(this, EventArgs.Empty);
			methodGen.Parse(info);

			// Optimize
			if (this.OptimizationStarted != null)
				this.OptimizationStarted(this, EventArgs.Empty);
			methodGen.Optimize();

			// Generate code
			if (this.CodeGenerationStarted != null)
				this.CodeGenerationStarted(this, EventArgs.Empty);
			methodGen.GenerateCode(null,info,"");
			VerifyGeneratedCode();
			
			// Bake the type now:
			reflectionEmitInfo.MainType.CreateType();
			
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
			if (this.ExecutionStarted != null)
				this.ExecutionStarted(this, EventArgs.Empty);
			compiledScript.Execute();
		}

		/// <summary>
		/// Verifies the generated byte code.
		/// </summary>
		[System.Diagnostics.Conditional("DEBUG")]
		private void VerifyGeneratedCode()
		{
#if false
			if (this.EnableDebugging == false)
				return;

			var filePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "NitroDebug.dll");

			// set the entry point for the application and save it
			this.ReflectionEmitInfo.AssemblyBuilder.Save(System.IO.Path.GetFileName(filePath));

			// Copy this DLL there as well.
			var assemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
			System.IO.File.Copy(assemblyPath, System.IO.Path.Combine(System.IO.Path.GetDirectoryName(filePath), System.IO.Path.GetFileName(assemblyPath)), true);

			var startInfo = new System.Diagnostics.ProcessStartInfo();
			startInfo.FileName = @"C:\Program Files (x86)\Microsoft SDKs\Windows\v7.0A\Bin\NETFX 4.0 Tools\x64\PEVerify.exe";
			startInfo.Arguments = string.Format("\"{0}\" /nologo /verbose /unique", filePath);
			startInfo.CreateNoWindow = true;
			startInfo.RedirectStandardOutput = true;
			startInfo.UseShellExecute = false;
			var verifyProcess = System.Diagnostics.Process.Start(startInfo);
			string output = verifyProcess.StandardOutput.ReadToEnd();

			if (verifyProcess.ExitCode != 0)
			{
				throw new InvalidOperationException(output);
			}
			//else
			//{
			//	System.Diagnostics.Process.Start(@"C:\Program Files\Reflector\Reflector.exe", string.Format("\"{0}\" /select:JavaScriptClass", filePath));
			//	Environment.Exit(0);
			//}

			// The assembly can no longer be modified - so don't use it again.
			this.ReflectionEmitInfo = null;
#endif
		}

		/// <summary>
		/// Creates a CompilerOptions instance using the script engine properties.
		/// </summary>
		/// <returns> A populated CompilerOptions instance. </returns>
		private Compiler.CompilerOptions CreateOptions()
		{
			return new Compiler.CompilerOptions() { ForceStrictMode = this.ForceStrictMode, EnableDebugging = this.EnableDebugging };
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
			return Global.HasProperty(variableName);
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
			return TypeUtilities.NormalizeValue(Global.GetValue(variableName));
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
			return TypeConverter.ConvertTo<T>(this, TypeUtilities.NormalizeValue(Global.GetValue(variableName)));
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
		/// <param name="value"> The desired value of the variable.  This must be of a supported
		/// type (bool, int, double, string, Null, Undefined or a ObjectInstance-derived type). </param>
		/// <param name="attribs">The attributes of the value.</param>
		/// <exception cref="JavaScriptException"> The property is read-only or the property does
		/// not exist and the object is not extensible. </exception>
		public void SetGlobal(string variableName, object value,PropertyAttributes attribs)
		{
			
			if(value==null){
				value=Null.Value;
			}
			
			Nitrassic.Compiler.GlobalVariable globalVariable=Global.SetValue(variableName, value);
			globalVariable.Attributes=attribs;
			
		}
		
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
			var value = Global.GetValue(functionName) as System.Reflection.MethodInfo;
			if (value == null)
				throw new InvalidOperationException(string.Format("'{0}' is not a function.", functionName));
			return value.Invoke(null, argumentValues);
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




		//	 TIMING EVENTS
		//_________________________________________________________________________________________

		/// <summary>
		/// Fires when the compiler starts parsing javascript source code.
		/// </summary>
		public event EventHandler ParsingStarted;

		/// <summary>
		/// Fires when the compiler starts optimizing.
		/// </summary>
		public event EventHandler OptimizationStarted;

		/// <summary>
		/// Fires when the compiler starts generating byte code.
		/// </summary>
		public event EventHandler CodeGenerationStarted;

		/// <summary>
		/// Fires when the compiler starts running javascript code.
		/// </summary>
		public event EventHandler ExecutionStarted;
		
		//	 STACK TRACE SUPPORT
		//_________________________________________________________________________________________
		
		/// <summary>
		/// Creates a stack trace.
		/// </summary>
		/// <param name="errorName"> The name of the error (e.g. "ReferenceError"). </param>
		/// <param name="message"> The error message. </param>
		/// <param name="path"> The path of the javascript source file that is currently executing. </param>
		/// <param name="function"> The name of the currently executing function. </param>
		/// <param name="line"> The line number of the statement that is currently executing. </param>
		internal string FormatStackTrace(string errorName, string message, string path, string function, int line, int depth)
		{
			System.Text.StringBuilder result = new System.Text.StringBuilder(errorName);
			
			if (string.IsNullOrEmpty(message) == false)
			{
				result.Append(": ");
				result.Append(message);
			}
			
			if (path != null || function != null || line != 0)
				AppendStackFrame(result, path, function, line, 0);
			
			System.Diagnostics.StackTrace trace=new System.Diagnostics.StackTrace(depth+1,true);
			
			for(int f=0; f<trace.FrameCount;f++){
				
				System.Diagnostics.StackFrame frame=trace.GetFrame(f);
				
				AppendStackFrame(result, frame.GetFileName(), frame.GetMethod().Name, frame.GetFileLineNumber(), frame.GetFileColumnNumber());
				
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
			result.AppendLine();
			result.Append("	");
			result.Append("at ");
			if (string.IsNullOrEmpty(function) == false)
			{
				result.Append(function);
				result.Append(" (");
			}
			result.Append(path ?? "unknown");
			if (line > 0)
			{
				result.Append(":");
				result.Append(line);
				
				if(column > 0){
					result.Append(", ");
					result.Append(column);
				}
			}
			if (string.IsNullOrEmpty(function) == false)
				result.Append(")");
		}
		
	}
}
