using System;
using System.Collections.Generic;

namespace Nitrassic.Library
{
	/// <summary>
	/// Represents an instance of the JavaScript Boolean object.
	/// </summary>
	public static class Boolean
	{
		
		[JSProperties(Hidden=true)]
		public static readonly Type PrototypeFor=typeof(bool);
		
		//	 JAVASCRIPT INTERNAL FUNCTIONS
		//_________________________________________________________________________________________
		
		/// <summary>
		/// Called when the Boolean object is invoked like a function, e.g. var x = Boolean("5").
		/// Converts the given argument into a boolean value (not a Boolean object).
		/// </summary>
		public static bool OnCall(bool value)
		{
			// Note: the parameter conversion machinery handles the required conversion.
			return value;
		}
		
		public static bool OnConstruct(bool value){
			// Note: the parameter conversion machinery handles the required conversion.
			return value;
		}
		
		//	 JAVASCRIPT FUNCTIONS
		//_________________________________________________________________________________________
		
		/// <summary>
		/// Returns the underlying primitive value of the current object.
		/// </summary>
		/// <returns> The underlying primitive value of the current object. </returns>
		public static bool ValueOf(bool thisObj)
		{
			return thisObj;
		}
		
		/// <summary>
		/// Returns a string representing this object.
		/// </summary>
		/// <returns> A string representing this object. </returns>
		public static string ToString(bool thisObj)
		{
			return thisObj ? "true" : "false";
		}
		
	}
}
