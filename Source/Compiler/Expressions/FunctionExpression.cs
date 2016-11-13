using System;
using System.Collections.Generic;
using Nitrassic.Library;


namespace Nitrassic.Compiler
{
	/// <summary>
	/// Represents a function expression.
	/// </summary>
	internal sealed class FunctionExpression : Expression
	{
		private FunctionMethodGenerator context;
		
		/// <summary>
		/// How this function was declared.
		/// </summary>
		public FunctionDeclarationType DeclarationType;
		
		/// <summary>
		/// Creates a new instance of FunctionExpression.
		/// </summary>
		public FunctionExpression()
		{}
		
		internal FunctionMethodGenerator Context{
			get{
				return context;
			}
		}
		
		public override object Evaluate(){
			
			return Context;
			
		}
		
		internal override void ResolveVariables(OptimizationInfo optimizationInfo)
		{
			// Do nothing here. We should only resolve variables inside a function when it's actually getting compiled.
		}
		
		/// <summary>Forces the resolving of variables now.</summary>
		internal void ResolveVariablesNow(OptimizationInfo optimizationInfo){
			
			base.ResolveVariables(optimizationInfo);
			
		}
		
		/// <param name="functionContext"> The function context to base this expression on. </param>
		internal void SetContext(FunctionMethodGenerator functionContext)
		{
			if (functionContext == null)
				throw new ArgumentNullException("functionContext");
			this.context = functionContext;
		}
		
		/// <summary>
		/// The set of hoisted declarative variables that this function uses.
		/// Note that they can only come from the direct parent scope.
		/// </summary>
		public List<Variable> HoistValues;
		
		/// <summary>
		/// Gets or sets the scope the function is declared within.
		/// </summary>
		public Scope ParentScope{
			get{
				return context.InitialScope;
			}
		}
		
		/// <summary>
		/// Gets the name of the function.
		/// </summary>
		public string FunctionName
		{
			get { return this.context.Name; }
		}

		/// <summary>
		/// Gets a list of argument names.
		/// </summary>
		public IList<ArgVariable> Arguments
		{
			get { return this.context.Arguments; }
		}
		
		/// <summary>
		/// Gets the type that results from evaluating this expression.
		/// </summary>
		public override Type GetResultType(OptimizationInfo optimizationInfo)
		{	
			if(HoistValues!=null){
				return typeof(HoistFunctionReference);
			}
			
			return typeof(FunctionMethodGenerator);
		}

		/// <summary>
		/// Generates CIL for the expression.
		/// </summary>
		/// <param name="generator"> The generator to output the CIL to. </param>
		/// <param name="optimizationInfo"> Information about any optimizations that should be performed. </param>
		public override void GenerateCode(ILGenerator generator, OptimizationInfo optimizationInfo)
		{
			
			// Setup the method info.
			// this.context.SetupMethod(optimizationInfo);
			
			// Add the generated method to the nested function list.
			if (optimizationInfo.NestedFunctions == null)
				optimizationInfo.NestedFunctions = new List<FunctionMethodGenerator>();
			optimizationInfo.NestedFunctions.Add(context);
			
			// Add all the nested methods to the parent list.
			// Note: it's used to compute which variables are being hoisted.
			if (this.context.Dependencies != null)
			{
				foreach (var nestedFunctionExpression in this.context.Dependencies)
					optimizationInfo.NestedFunctions.Add(nestedFunctionExpression);
			}
			
			if(HoistValues!=null)
			{
				
				// Script engine:
				EmitHelpers.LoadEngine(generator);
				
				// Method ID:
				generator.LoadInt64(context.MethodID);
				
				generator.LoadInt32(HoistValues.Count);
				
				// Hoist vars array:
				generator.NewArray(typeof(object));
				
				// Load each one:
				for (int i = 0; i < HoistValues.Count; i++)
				{
					generator.Duplicate();
					generator.LoadInt32(i);
					
					// Load the variable value:
					Type type=HoistValues[i].Get(generator);
					
					// Box if necessary:
					EmitConversion.ToAny(generator,type);
					
					generator.StoreArrayElement(typeof(object));
				}
				
				generator.NewObject(ReflectionHelpers.HoistFunctionReference_Constructor);
				
			}else{
				
				// body
				generator.LoadInt64(context.MethodID);
				generator.Call(ReflectionHelpers.MethodLookup_Load);
				
			}
			
		}
		
		/// <summary>
		/// Hoist the given variable into the function being called here.
		/// </summary>
		public Variable Hoist(Variable dvar)
		{
			
			if(HoistValues==null)
			{
				HoistValues=new List<Variable>();
			}
			
			HoistValues.Add(dvar);
			
			// Declare it as hoisted in the function body:
			return context.DeclareHoisted(dvar.Name,dvar.Type);
			
		}
		
		/// <summary>
		/// Converts the expression to a string.
		/// </summary>
		/// <returns> A string representing this expression. </returns>
		public override string ToString()
		{
			return context.ToString();
		}
	}

}