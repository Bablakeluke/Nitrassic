using System;
using System.Collections;
using System.Collections.Generic;
using Nitrassic.Library;
using System.Reflection;


namespace Nitrassic.Compiler{
	
	/// <summary>
	/// Represents an argument of a function.
	/// </summary>
	
	public class ArgVariable : Variable
	{
		
		
		public ArgVariable(string name)
			:base(name)
			{}
		
		/// <summary>The function scope it's declared in.</summary>
		public Scope Scope;
		
		/// <summary>The argument ID.
		/// Note that hoisted variables can push this over; it is not considered constant until after the AST is complete.
		/// </summary>
		public int ArgumentID;
		
		/// <summary>The args type.</summary>
		private Type _Type;
		
		/// <summary>The set of properties on this argument that the function changes (optionally or otherwise).</summary>
		public Dictionary<string,Type> ChangesProperties;
		
		
		internal override bool IsHoisted(Scope relativeTo)
		{
			return Scope!=relativeTo;
		}
		
		internal override Type Get(ILGenerator generator)
		{
			
			// Load the value in the variable:
			generator.LoadArgument(ArgumentID);
			
			return _Type;
			
		}
		
		/// <summary>Mark the argument as changed by this function.
		/// This function sets the given property to something of the given type on this argument.</summary>
		public void Changed(string property,Type type)
		{
			
			if(ChangesProperties==null)
			{
				ChangesProperties=new Dictionary<string,Type>();
			}
			else
			{
				
				Type result;
				
				if(ChangesProperties.TryGetValue(property,out result))
				{
					
					if(result==type || result==typeof(object))
					{
						return;
					}
					
					// Sets it to more than one type.
					type=typeof(object);
					
				}
				
			}
			
			// Add it:
			ChangesProperties[property]=type;
			
		}
		
		internal override void Set(ILGenerator generator, OptimizationInfo optimizationInfo, Type valueType, SetValueMethod value)
		{
			
			if(_Type==null)
			{
				_Type=valueType;
			}
			else if(valueType!=_Type)
			{
				// Essentially declaring a new variable.
				// This overwrites the name reference in the scope to the new variable.
				
				Scope.DeclareVariable(Name,valueType,null).Set(generator,optimizationInfo,valueType,value);
				
				return;
			}
			
			// Load the value:
			value();
			
			// Store it in the arg:
			generator.StoreArgument(ArgumentID);
			
		}
		
		internal override Type Type
		{
			get
			{
				return _Type;
			}
			set
			{
				_Type=value;
			}
		}
		
	}
	
	
}