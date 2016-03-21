using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Nitrassic.Library;

namespace Nitrassic.Compiler
{

	/// <summary>
	/// Represents a single method that the JS function binder can call.
	/// </summary>
	internal class JSBinderMethod : BinderMethod
	{

		//	 INITIALIZATION
		//_________________________________________________________________________________________
		
		/// <summary>
		/// Creates a new FunctionBinderMethod instance.
		/// </summary>
		/// <param name="method"> The method to call. </param>
		/// <param name="flags"> Flags that modify the binding process. </param>
		public JSBinderMethod(MethodInfo method)
			: base(method)
		{
			Init(method.GetParameters());
		}
		
		/// <summary>
		/// Creates a new FunctionBinderMethod instance.
		/// </summary>
		/// <param name="method"> The method to call. </param>
		/// <param name="flags"> Flags that modify the binding process. </param>
		public JSBinderMethod(ConstructorInfo method)
			: base(method)
		{
			Init(method.GetParameters());
		}
		
		private void Init(ParameterInfo[] parameters)
		{
			
			if(parameters.Length>0)
			{
				
				HasEngineParameter=(parameters[0].ParameterType==typeof(ScriptEngine));
				
				if(parameters.Length>1)
				{
					
					ParameterInfo thisParam=parameters[HasEngineParameter?1:0];
					HasExplicitThisParameter=(thisParam.Name=="thisObj");
					
					if(HasExplicitThisParameter)
					{
						ExplicitThisType=thisParam.ParameterType;
					}
					
				}
				
			}
			
			/*
			if (this.HasEngineParameter && this.Method.IsStatic == false)
				throw new InvalidOperationException("engine cannot be used on the instance method '"+Name+"'.");
			if (this.HasExplicitThisParameter == true && this.Method.IsStatic == false)
				throw new InvalidOperationException("thisObj cannot be used on the instance method '"+Name+"'.");
			*/
			
		}
		
		//	 PROPERTIES
		//_________________________________________________________________________________________
		
		/// <summary>
		/// True if a null value should be converted to undefined.
		/// </summary>
		// public bool ConvertNullToUndefined;
		
		/// <summary>
		/// Gets a value that indicates whether the script engine should be passed as the first
		/// parameter.  Always false for instance methods.
		/// </summary>
		public bool HasEngineParameter;

		/// <summary>
		/// Gets a value that indicates whether the "this" object should be passed as the first
		/// parameter (or the second parameter if HasEngineParameter is <c>true</c>).  Always false
		/// for instance methods.
		/// </summary>
		public bool HasExplicitThisParameter;

		/// <summary>
		/// Gets the type of the explicit "this" value passed to this method.  Will be <c>null</c>
		/// if there is no explicit this value.
		/// </summary>
		public Type ExplicitThisType;

		///// <summary>
		///// Gets a value that indicates whether the "this" object should be passed to the method.
		///// Always true for instance methods.
		///// </summary>
		//public bool HasThisParameter
		//{
		//	get { return this.ThisType != null; }
		//}

		/// <summary>
		/// Gets the maximum number of parameters that this method requires (excluding the implicit
		/// this parameter).
		/// </summary>
		public int MaxParameterCount
		{
			get { return this.HasParamArray ? int.MaxValue : this.RequiredParameterCount + this.OptionalParameterCount; }
		}

		


		//	 METHODS
		//_________________________________________________________________________________________

		/// <summary>
		/// Gets an array of method parameters.
		/// </summary>
		/// <returns> An array of ParameterInfo instances describing the method parameters. </returns>
		protected override ParameterInfo[] GetParameters()
		{
			// Pull out the first and/or second parameters.
			var result = base.GetParameters();
			int offset = (this.HasEngineParameter ? 1 : 0) + (this.HasExplicitThisParameter ? 1 : 0);
			if (offset == 0)
				return result;
			ParameterInfo[] newArray = new ParameterInfo[result.Length - offset];
			System.Array.Copy(result, offset, newArray, 0, result.Length - offset);
			return newArray;
		}

		/// <summary>
		/// Gets an enumerable list of argument objects, equal in size to
		/// <paramref name="argumentCount"/>.
		/// </summary>
		/// <param name="argumentCount"> The number of arguments to return. </param>
		/// <returns> An enumerable list of argument objects. </returns>
		public override IEnumerable<BinderArgument> GetArguments(int argumentCount)
		{
			// If there is an engine parameter, return that first.
			if (this.HasEngineParameter == true)
				yield return new BinderArgument(BinderArgumentSource.ScriptEngine, typeof(ScriptEngine));

			// If there is an explicit this parameter, return that next.
			if (this.HasExplicitThisParameter == true){
				yield return new BinderArgument(BinderArgumentSource.ThisValue, this.ExplicitThisType);
			}
			
			// Delegate to the base class.
			foreach (var arg in GetHostArguments(argumentCount))
				yield return arg;
		}
		
		public IEnumerable<BinderArgument> GetHostArguments(int ac){
			return base.GetArguments(ac);
		}
	}

}
