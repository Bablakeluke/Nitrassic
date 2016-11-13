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
		internal FunctionMethodGenerator Host;
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
		{
			Host=MethodLookup.LoadGenerator(methodID);
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
		public override object CallLateBound(ScriptEngine engine,params object[] thisAndArgumentValues)
		{
			// Call the function, passing the hoisted values too.
			int hoisted=HoistValues.Length;
			int argCount=thisAndArgumentValues.Length;
			
			object[] set=new object[hoisted + argCount];
			
			// Transfer 'this':
			set[0]=thisAndArgumentValues;
			
			// Copy hoist values in:
			System.Array.Copy(HoistValues,0,set,1,hoisted);
			
			// Copy arg values in:
			System.Array.Copy(thisAndArgumentValues,1,set,hoisted+1,argCount-1);
			
			// Need to find the correct compiled version of the host:
			UserDefinedFunction udf=Host.GetCompiled(engine,set);
			
			return udf.body.Invoke(null, set);
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
