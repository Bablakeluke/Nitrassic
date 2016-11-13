using System;
using System.Reflection;
using Nitrassic.Compiler;
using System.Collections;
using System.Collections.Generic;

namespace Nitrassic.Library
{
	/// <summary>
	/// Represents a constructor.
	/// </summary>
	[JSProperties(Name="Function")]
	public class FunctionProto
	{
		/// <summary>
		/// Acts as the prototype for all the different method types.
		/// </summary>
		[JSProperties(Hidden=true)]
		public static readonly Type[] PrototypeFor=new Type[]{typeof(MethodInfo),typeof(ConstructorInfo),typeof(MethodGroup)};
		
		/// <summary>
		/// The prototype for the Function object.
		/// </summary>
		internal static MethodBase GetPrototype(){
			return typeof(FunctionProto).GetMethod("BasePrototypeImplementation");
		}
		
		/// <summary>
		/// Implements the behaviour of the function that is the prototype of the Function object.
		/// </summary>
		/// <param name="engine"></param>
		/// <param name="thisObj"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		private static void BasePrototypeImplementation(params object[] args){}
		
		/// <summary>
		/// Creates a new function instance.
		/// </summary>
		/// <param name="argumentsAndBody"> The argument names plus the function body. </param>
		/// <returns> A new function instance. </returns>
		public static FunctionInstance OnCall(ScriptEngine engine,params string[] argumentsAndBody)
		{
			return OnConstruct(engine,argumentsAndBody);
		}
		
		/// <summary>
		/// Creates a new function instance.
		/// </summary>
		/// <param name="argumentsAndBody"> The argument names plus the function body. </param>
		/// <returns> A new function instance. </returns>
		public static FunctionInstance OnConstruct(ScriptEngine engine,params string[] argumentsAndBody)
		{
			// Passing no arguments results in an empty function.
			if (argumentsAndBody.Length == 0)
				return UserDefinedFunction.Create(engine, "anonymous", null, string.Empty);

			// Split any comma-delimited names.
			List<ArgVariable> args = new List<ArgVariable>();
			args.Add(new ArgVariable("this"));
			
			for (int i = 0; i < argumentsAndBody.Length - 1; i++)
			{
				var splitNames = argumentsAndBody[i].Split(',');
				
				for (int j = 0; j < splitNames.Length; j++)
				{
					// Trim any whitespace from the start and end of the argument name.
					string argumentName = Nitrassic.Library.String.Trim(splitNames[j]);
					if (argumentName == string.Empty)
						throw new JavaScriptException(engine, "SyntaxError", "Unexpected ',' in argument");

					// Check the name is valid and resolve any escape sequences.
					argumentName = Compiler.Lexer.ResolveIdentifier(engine, argumentName);
					if (argumentName == null)
						throw new JavaScriptException(engine, "SyntaxError", "Expected identifier");
					
					args.Add(new ArgVariable(argumentName));
					
				}
				
			}

			// Create a new function.
			return UserDefinedFunction.Create(engine, "anonymous", args, argumentsAndBody[argumentsAndBody.Length - 1]);
		}
		
		public static string ToString(object thisObj)
		{
			return "function "+thisObj.ToString()+"() {{ [native code] }}";
		}
		
	}
}