using System;
using System.Collections.Generic;
using Nitrassic.Compiler;
using System.Reflection;

namespace Nitrassic.Library
{
	/// <summary>
	/// Represents an arguments object in non-strict mode.
	/// </summary>
	[JSProperties(Name="Arguments")]
	public class ArgumentsInstance
	{
		
		//	 INITIALIZATION
		//_________________________________________________________________________________________
		
		private object[] Arguments;
		
		/// <summary>
		/// Creates a new Arguments instance.
		/// </summary>
		/// <param name="prototype"> The next object in the prototype chain. </param>
		/// <param name="argumentValues"> The argument values that were passed to the function. </param>
		public ArgumentsInstance(object[] argumentValues)
		{
			Arguments=argumentValues;
		}
		
		/// <summary>
		/// The number of args.
		/// </summary>
		public int Length
		{
			get
			{
				return Arguments.Length;
			}
		}
		
		/// <summary>
		/// Gets or sets the given arg.
		/// </summary>
		public object this[int index]
		{
			get
			{
				if(index<0 || index>=Arguments.Length)
				{
					return Undefined.Value;
				}
				
				return Arguments[index];
			}
			set
			{
				Arguments[index]=value;
			}
		}
		
		/// <summary>Used when enumerating the arg values.</summary>
		[JSProperties(Hidden=true)]
		public IEnumerable<object> PropertyValues
		{
			get
			{
				
				// Enumerate dense array indices.
				for (uint i = 0; i < Arguments.Length; i++)
				{
					object arrayElementValue = Arguments[i];
					if (arrayElementValue != null)
						yield return arrayElementValue;
				}
				
			}
		}
		
		/// <summary>Used when enumerating the args.</summary>
		[JSProperties(Hidden=true)]
		public IEnumerable<string> Properties
		{
			get
			{
				
				// Enumerate dense array indices.
				for (uint i = 0; i < Arguments.Length; i++)
				{
					object arrayElementValue = Arguments[i];
					if (arrayElementValue != null)
						yield return i.ToString();
				}
				
			}
		}
		
		/// <summary>
		/// The function caller.
		/// </summary>
		public UserDefinedFunction caller{
			get
			{
				return ResolveFunctionInstance(1);
			}
		}
		
		/// <summary>
		/// 'This' function - the callee.
		/// </summary>
		public UserDefinedFunction callee{
			get
			{
				return ResolveFunctionInstance(0);
			}
		}
		
		/// <summary>
		/// Resolves a function instance via the .NET stack.
		/// </summary>
		private static UserDefinedFunction ResolveFunctionInstance(int count){
			
			// First, get the stack:
			System.Diagnostics.StackTrace trace=new System.Diagnostics.StackTrace(5+count,true);
			
			for(int i=0;i<trace.FrameCount;i++){
				System.Diagnostics.StackFrame frame=trace.GetFrame(i);
				
				UserDefinedFunction func=MethodLookup.Search(frame.GetMethod() as MethodInfo);
				
				if(func!=null){
					return func;
				}
				
			}
			
			return null;
		}
		
	}
}
