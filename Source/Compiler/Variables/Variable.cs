using System;
using Nitrassic.Library;


namespace Nitrassic.Compiler
{
	
	/// <summary>
	/// Represents a variable declared somewhere.
	/// </summary>
	public class Variable
	{
		
		/// <summary>The name of this variable.</summary>
		public string Name;
		
		/// <summary>
		/// Gets the property attributes.  These attributes describe how the property can
		/// be modified.
		/// </summary>
		public PropertyAttributes Attributes;
		
		
		public Variable(string name)
		{
			Name=name;
		}
		
		public Variable()
		{}
		
		/// <summary>
		/// Gets a boolean value indicating whether the property value can be set.
		/// </summary>
		public bool IsWritable
		{
			get { return (this.Attributes & PropertyAttributes.Writable) != 0; }
		}

		/// <summary>Puts the value of this variable onto the stack.</summary>
		internal virtual Type Get(ILGenerator generator)
		{
			return null;
		}
		
		/// <summary>Sets this variable with the given value generator method.</summary>
		internal virtual void Set(ILGenerator generator, OptimizationInfo optimizationInfo, Type valueType, SetValueMethod value)
		{}
		
		/// <summary>Checks if this variable is hoisted from some other scope.</summary>
		internal virtual bool IsHoisted(Scope relativeTo)
		{
			return false;
		}
		
		/// <summary>The type of this variable.
		/// Can be null if it has not been initialised yet.</summary>
		internal virtual Type Type
		{
			get
			{
				return null;
			}
			set {}
		}
		
	}
	
}