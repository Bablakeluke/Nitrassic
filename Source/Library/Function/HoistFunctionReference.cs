using System;
using System.Collections.Generic;
using Nitrassic.Compiler;


namespace Nitrassic.Library
{
	/// <summary>
	/// Used when an inline function is declared which uses hoisted variables from a parent declarative scope.
	/// The hoisted variables are passed as arguments into the function.
	/// </summary>
	public class HoistFunctionReference : FunctionInstance
	{
		
		/// <summary>The host function. That's the one being called.</summary>
		public UserDefinedFunction Host;
		/// <summary>The values being hoisted into the new scope.</summary>
		public object[] HoistValues;
		
		/// <summary>
		/// Creates a new instance of a user-defined function.
		/// </summary>
		/// <param name="prototype"> The next object in the prototype chain. </param>
		/// <param name="name"> The name of the function. </param>
		/// <param name="args"> The names of the arguments. </param>
		/// <param name="bodyText"> The source code for the function body. </param>
		/// <param name="body"> A delegate which represents the body of the function. </param>
		/// <param name="strictMode"> <c>true</c> if the function body is strict mode; <c>false</c> otherwise. </param>
		public HoistFunctionReference(ScriptEngine engine,long methodID,object[] hoistValues)
			: base(engine.Prototypes.FunctionPrototype)
		{
			Host=MethodLookup.Load(methodID);
			HoistValues=hoistValues;
		}
		
		
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
			// Call the function, passing the hoisted values too.
			int hoisted=HoistValues.Length;
			int argCount=argumentValues.Length;
			
			object[] set=new object[hoisted + argCount];
			
			// Copy hoist values in:
			System.Array.Copy(HoistValues,0,set,0,hoisted);
			
			// Copy arg values in:
			System.Array.Copy(argumentValues,0,set,hoisted,argCount);
			
			return Host.body.Invoke(thisObj, set);
		}
		
		/// <summary>
		/// Returns a string representing this object.
		/// </summary>
		/// <returns> A string representing this object. </returns>
		public override string ToString()
		{
			return Host.ToString();
		}
		
	}
}
