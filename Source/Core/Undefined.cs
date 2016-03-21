using System;
using System.Collections.Generic;
using System.Text;

namespace Nitrassic
{
	/// <summary>
	/// Represents the JavaScript "undefined" type and provides the one and only instance of that type.
	/// </summary>
	public sealed class Undefined
	{
		/// <summary>
		/// Gets the one and only "undefined" instance.
		/// </summary>
		public static readonly Undefined Value = new Undefined();
	
		/// <summary>
		/// Returns a string representing the current object.
		/// </summary>
		/// <returns> A string representing the current object. </returns>
		public override string ToString()
		{
			return "undefined";
		}
	}
}
