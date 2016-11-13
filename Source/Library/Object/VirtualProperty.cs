using System;
using System.Reflection;
using Nitrassic.Compiler;

namespace Nitrassic.Library
{
	
	/// <summary>
	/// Handles a pair of methods which get/set some value.
	/// </summary>
	
	public class VirtualProperty{
		
		public MethodBase GetMethod;
		public MethodBase SetMethod;
		
		
		public VirtualProperty(){}
		
		
		public VirtualProperty(PropertyInfo property){
			
			// Apply get/set:
			GetMethod=property.GetGetMethod();
			SetMethod=property.GetSetMethod();
			
			LoadMeta();
		}
		
		/// <summary>True if this property requires a ScriptEngine pass.</summary>
		public bool RequiresEngine;
		/// <summary>True if this property requires 'this'.</summary>
		public bool RequiresThis;
		
		
		public void LoadMeta(){
			
			if(GetMethod==null){
				return;
			}
			
			ParameterInfo[] paraSet=GetMethod.GetParameters();
			
			if(paraSet.Length>0)
			{
				
				int parameterOffset=0;
				
				if(paraSet[0].ParameterType==typeof(ScriptEngine))
				{
					RequiresEngine=true;
					
					parameterOffset=1;
				}
				
				if(paraSet.Length>parameterOffset && paraSet[parameterOffset].Name=="thisObj")
				{
					// Acts like an instance method.
					RequiresThis=true;
				}
				
			}else if(!GetMethod.IsStatic){
				// It's an instance method.
				RequiresThis=true;
			}
			
		}
		
		/// <summary>The type of this property.</summary>
		public Type PropertyType{
			get{
				return (GetMethod as MethodInfo).ReturnType;
			}
		}
		
	}
	
}