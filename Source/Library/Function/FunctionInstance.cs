using System;
using System.Collections.Generic;
using System.Reflection;
using Nitrassic.Compiler;


namespace Nitrassic.Library
{
	/// <summary>
	/// Represents a JavaScript function.
	/// </summary>
	[Serializable]
	[JSProperties(Name="Function")]
	public abstract partial class FunctionInstance
	{
		
		//	 INITIALIZATION
		//_________________________________________________________________________________________
		
		internal FunctionMethodGenerator Generator;
		
		/// <summary>
		/// Creates a new instance of a built-in function object, with the default Function
		/// prototype.
		/// </summary>
		protected FunctionInstance(){
		}
		
		//	 .NET ACCESSOR PROPERTIES
		//_________________________________________________________________________________________
		
		public Type GetInstanceType(ScriptEngine engine){
			return GetInstancePrototype(engine).Type;
		}
		
		private Library.Prototype _RawInstancePrototype;
		
		/// <summary>
		/// Gets the prototype of objects constructed using this function.  Equivalent to
		/// the Function.prototype property.
		/// </summary>
		public Library.Prototype GetInstancePrototype(ScriptEngine engine){
			
			if(Generator!=null){
				return Generator.GetInstancePrototype(engine);
			}
			
			if(_RawInstancePrototype==null){
				
				// Create one now!
				_RawInstancePrototype=engine.Prototypes.Create();
				
			}
			
			return _RawInstancePrototype;
			
		}
		
		/// <summary>
		/// Gets the name of the function.
		/// </summary>
		public string Name;

		/// <summary>
		/// Gets the display name of the function.  This is equal to the displayName property, if
		/// it exists, or the name property otherwise.
		/// </summary>
		public string DisplayName;

		/// <summary>
		/// Gets the number of arguments expected by the function.
		/// </summary>
		public int Length;



		//	 JAVASCRIPT INTERNAL FUNCTIONS
		//_________________________________________________________________________________________

		/// <summary>
		/// Determines whether the given object inherits from this function.  More precisely, it
		/// checks whether the prototype chain of the object contains the prototype property of
		/// this function.  Used by the "instanceof" operator.
		/// </summary>
		/// <param name="instance"> The instance to check. </param>
		/// <returns> <c>true</c> if the object inherits from this function; <c>false</c>
		/// otherwise. </returns>
		public bool HasInstance(ScriptEngine engine,object instance)
		{
			
			if(instance==null){
				return false;
			}
			
			
			// Get the type of the instance:
			Type type=instance.GetType();
			
			// Check if it == or inherits from InstanceType.
			return GetInstanceType(engine).IsAssignableFrom(type);
		}

		/// <summary>
		/// Calls this function, passing in the given "this" value and zero or more arguments.
		/// </summary>
		/// <param name="thisAndArgumentValues"> The value of the "this" keyword within the function plus
		/// an array of argument values to pass to the function. </param>
		/// <returns> The value that was returned from the function. </returns>
		public abstract object CallLateBound(ScriptEngine engine,params object[] thisAndArgumentValues);
		
		//	 JAVASCRIPT FUNCTIONS
		//_________________________________________________________________________________________

		/// <summary>
		/// Calls the function, passing in parameters from the given array.
		/// </summary>
		/// <param name="thisObj"> The value of <c>this</c> in the context of the function. </param>
		/// <param name="arguments"> The arguments passed to the function, as an array. </param>
		/// <returns> The result from the function call. </returns>
		public object Apply(ScriptEngine engine,object thisObj, object arguments)
		{
			// Convert the arguments parameter into an array.
			object[] argumentsArray;
			if (arguments == null || arguments == Undefined.Value || arguments == Null.Value)
				argumentsArray = new object[0];
			else
			{
				if ((arguments is ObjectInstance) == false)
					throw new JavaScriptException(engine, "TypeError", "The second parameter of apply() must be an array or an array-like object.");
				ObjectInstance argumentsObject = (ObjectInstance)arguments;
				object arrayLengthObj = argumentsObject["length"];
				if (arrayLengthObj == null || arrayLengthObj == Undefined.Value || arrayLengthObj == Null.Value)
					throw new JavaScriptException(engine, "TypeError", "The second parameter of apply() must be an array or an array-like object.");
				uint arrayLength = TypeConverter.ToUint32(arrayLengthObj);
				if (arrayLength != TypeConverter.ToNumber(arrayLengthObj))
					throw new JavaScriptException(engine, "TypeError", "The second parameter of apply() must be an array or an array-like object.");
				argumentsArray = new object[arrayLength];
				for (uint i = 0; i < arrayLength; i++)
					argumentsArray[i] = argumentsObject[i];
			}

			return this.CallLateBound(engine, thisObj, argumentsArray);
		}

		/// <summary>
		/// Calls the function.
		/// </summary>
		/// <param name="thisAndArguments"> The value of <c>this</c> in the context of the function plus
		/// any number of arguments that will be passed to the function. </param>
		/// <returns> The result from the function call. </returns>
		public object Call(ScriptEngine engine, params object[] thisAndArguments)
		{
			
			if(thisAndArguments==null || thisAndArguments.Length<1){
				throw new JavaScriptException(engine, "TypeError", "Missing 'this' parameter.");
			}
			
			return this.CallLateBound(engine, thisAndArguments);
		}
		
		/// <summary>
		/// Returns a string representing this object.
		/// </summary>
		/// <returns> A string representing this object. </returns>
		public override string ToString()
		{
			return "function "+Name+"() {{ [native code] }}";
		}
		
		//	 CONSTRUCTORS
		//_________________________________________________________________________________________
		
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
	}
}
