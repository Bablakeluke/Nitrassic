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
		/// Creates a new instance of FunctionExpression.
		/// </summary>
		public FunctionExpression()
		{}
		
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
		/// Gets the source code for the body of the function.
		/// </summary>
		public string BodyText
		{
			get { return this.context.BodyText; }
		}

		/// <summary>
		/// Gets the type that results from evaluating this expression.
		/// </summary>
		public override Type GetResultType(OptimizationInfo optimizationInfo)
		{
			return typeof(Library.ObjectInstance);
		}

		/// <summary>
		/// Generates CIL for the expression.
		/// </summary>
		/// <param name="generator"> The generator to output the CIL to. </param>
		/// <param name="optimizationInfo"> Information about any optimizations that should be performed. </param>
		public override void GenerateCode(ILGenerator generator, OptimizationInfo optimizationInfo)
		{
			// Generate a new method.
			this.context.GenerateCode(optimizationInfo);
			
			// Add the generated method to the nested function list.
			if (optimizationInfo.NestedFunctions == null)
				optimizationInfo.NestedFunctions = new List<UserDefinedFunction>();
			optimizationInfo.NestedFunctions.Add(this.context.GeneratedMethod);
			
			// Add all the nested methods to the parent list.
			// Note: it's used to compute which variables are being hoisted.
			if (this.context.GeneratedMethod.Dependencies != null)
			{
				foreach (var nestedFunctionExpression in this.context.GeneratedMethod.Dependencies)
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
			return string.Format("function {0}({1}) {{\n{2}\n}}", this.FunctionName, StringHelpers.Join(", ", this.Arguments), this.BodyText);
		}
	}

}