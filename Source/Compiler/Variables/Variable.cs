using System;
using Nitrassic.Library;


namespace Nitrassic.Compiler
{
	
	/// <summary>
	/// Represents a variable declared somewhere.
	/// </summary>
	public class Variable
	{
		
		/// <summary>Used by a variable before it's value has been set the first time.</summary>
		public static object DefaultConstant=new object();
		/// <summary>Used when a variable is known to be non-constant.</summary>
		public static object NonConstant=new object();
		
		/// <summary>The name of this variable.</summary>
		public string Name;
		
		/// <summary>The first value set to this variable. Used by the method call optimiser.</summary>
		public object ConstantValue=Variable.DefaultConstant;
		
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
		
		/// <summary>True if this variable is set to a constant value once. a=4; etc.</summary>
		public bool IsConstant{
			get{
				return (ConstantValue!=Variable.NonConstant && ConstantValue!=Variable.DefaultConstant);
			}
			set{
				
				if(value){
					// Resets:
					ConstantValue=Variable.DefaultConstant;
				}else{
					ConstantValue=Variable.NonConstant;
				}
				
			}
		}
		
		internal bool TryLoadConstant(Expression expr){
			
			object constantValue=expr.Evaluate();
			
			if(constantValue!=null){
				// Got one!
				return TrySetConstant(constantValue);
				
			}
			
			return false;
			
		}
		
		/// <summary>Marks a constant value of this variable.</summary>
		public bool TrySetConstant(object cv){
			
			if(ConstantValue==Variable.DefaultConstant){
				
				// Apply first to it:
				ConstantValue=cv;
				
				return true;
				
			}else if(ConstantValue.Equals(cv)){
				
				// No change.
				return true;
				
			}else{
				
				// It's been set more than once. It's no longer known to be constant.
				ConstantValue=Variable.NonConstant;
				
			}
			
			return false;
			
		}
		
		/// <summary>Puts the value of this variable onto the stack.</summary>
		internal virtual Type Get(ILGenerator generator)
		{
			return null;
		}
		
		/// <summary>Sets this variable with the given value generator method.</summary>
		internal virtual void Set(ILGenerator generator, OptimizationInfo optimizationInfo,bool rIU, Type valueType, SetValueMethod value)
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