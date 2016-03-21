using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace Nitrassic.Compiler
{

	internal class MethodGroup
	{
		
		/// <summary>
		/// True if this group is representing a jump table.
		/// The method to jump to is defined as an integer instance property.
		/// </summary>
		public bool JumpTable;
		
		/// <summary>
		/// The methods in this group.
		/// Note that they always have the same name.
		/// </summary>
		public List<MethodBase> Methods=new List<MethodBase>();
		
		/// <summary>
		/// The number of methods in the group.
		/// </summary>
		public int Length{
			get{
				return Methods.Count;
			}
		}
		
		/// <summary>
		/// Emits a jump table for this method group. The integer index must be on the stack.
		/// Each index in the table is simply a Call instruction.
		/// </summary>
		public void GenerateJumpTable(ILGenerator generator)
		{
			
			// The number of methods:
			int methodCount=Methods.Count;
			
			// Create the set of switch labels, one for each method:
			ILLabel[] switchLabels = new ILLabel[Methods.Count];
			for (int i = 0; i < methodCount; i++)
			{
				switchLabels[i] = generator.CreateLabel();
			}
			
			// Emit the switch instruction now:
			generator.Switch(switchLabels);
			
			// Create the end label:
			ILLabel end=generator.CreateLabel();
			
			// For each method, mark the label and emit the call.
			for (int i = 0; i < methodCount; i++)
			{
				// Mark the label:
				generator.DefineLabelPosition(switchLabels[i]);
				
				// Get the method:
				MethodBase method=Methods[i];
				
				// Emit the call now:
				if(method.IsConstructor)
				{
					// Actual constructor call:
					generator.NewObject(method as System.Reflection.ConstructorInfo);
				}
				else
				{
					// Call it now:
					generator.Call(method);
				}
				
				generator.Branch(end);
			}
			
			generator.DefineLabelPosition(end);
			
		}
		
		/// <summary>
		/// The property attributes for these methods.
		/// </summary>
		public Nitrassic.Library.PropertyAttributes PropertyAttributes;
		
		/// <summary>
		/// Adds a given method into this group.
		/// </summary>
		public void Add(MethodBase method)
		{
			Methods.Add(method);
		}
		
		/// <summary>
		/// Finds the best method for the given parameter set.
		/// </summary>
		public MethodBase MatchObjects(object[] parameters)
		{
			#warning complete
			int count=Methods.Count;
			
			for(int i=0;i<count;i++)
			{
				MethodBase method=Methods[i];
			}
			
			return Methods[0];
		}
		
		public MethodBase Match(Type[] types)
		{
			
			int typeCount=types==null?0:types.Length;
			
			int count=Methods.Count;
			
			for(int i=0;i<count;i++)
			{
				MethodBase method=Methods[i];
				ParameterInfo[] parameters=method.GetParameters();
				
				if(parameters.Length==typeCount)
				{
					return method;
				}
			}
			
			return Methods[0];
		}
		
		/// <summary>
		/// Invokes a method in this set which matches the parameters.
		/// </summary>
		public object Invoke(object thisObj,object[] parameters)
		{
			// Find the method which matches the parameters. Error otherwise.
			MethodBase method=MatchObjects(parameters);
			
			if(method==null)
			{
				// throw new JavaScriptException();
			}
			
			return null;
		}
		
		public override string ToString()
		{
			return Methods[0].Name;
		}
		
	}
	
	
}