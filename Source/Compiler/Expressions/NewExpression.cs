using System;

namespace Nitrassic.Compiler
{
	/// <summary>
	/// Represents a "new" expression.
	/// </summary>
	internal sealed class NewExpression : OperatorExpression
	{
		/// <summary>
		/// Creates a new instance of NewExpression.
		/// </summary>
		/// <param name="operator"> The operator to base this expression on. </param>
		public NewExpression(Operator @operator)
			: base(@operator)
		{
		}

		/// <summary>
		/// Gets the precedence of the operator.
		/// </summary>
		public override int Precedence
		{
			get
			{
				// The expression "new String('').toString()" is parsed as
				// "new (String('').toString())" rather than "(new String('')).toString()".
				// There is no way to express this constraint properly using the standard operator
				// rules so we artificially boost the precedence of the operator if a function
				// call is encountered.  Note: GetRawOperand() is used instead of GetOperand(0)
				// because parentheses around the function call affect the result.
				return this.OperandCount == 1 && this.GetRawOperand(0) is FunctionCallExpression ?
					int.MaxValue : Operator.New.Precedence;
			}
		}
		
		internal override void ResolveVariables(OptimizationInfo optimizationInfo){
			
			// Mark as a ctr type:
			bool previous=optimizationInfo.IsConstructCall;
			optimizationInfo.IsConstructCall=true;
			
			// Resolve kids now:
			base.ResolveVariables(optimizationInfo);
			
			// Attempt a conversion next:
			
			// Get as a call expr:
			FunctionCallExpression function = GetRawOperand(0) as FunctionCallExpression;
			
			if (function==null){
				function=AttemptConversion(optimizationInfo);
			}
			
			optimizationInfo.IsConstructCall=previous;
			
		}
		
		/// <summary>
		/// Gets the type that results from evaluating this expression.
		/// </summary>
		public override Type GetResultType(OptimizationInfo optimizationInfo)
		{
			
			// Get the operand:
			var operand = this.GetRawOperand(0);
			
			// Get as a call expr:
			FunctionCallExpression function = operand as FunctionCallExpression;
			
			if(function!=null){
				// Set IsConstructCall so a potential resolve runs correctly:
				optimizationInfo.IsConstructCall=true;
			}
			
			Type fType=operand.GetResultType(optimizationInfo);
			
			if(function!=null){
				// Clear ICC:
				optimizationInfo.IsConstructCall=false;
			}
			
			if(function.IsUserDefined){
				// It's user defined which means the return type is actually the instance type:
				fType=function.InstanceType(optimizationInfo.Engine);
			}
			
			return fType;
			
		}
		
		private FunctionCallExpression AttemptConversion(OptimizationInfo optimizationInfo){
			
			var operand = this.GetRawOperand(0);
			
			// Try a NameExpression which refs a constructor instead.
			NameExpression nameExpr=operand as NameExpression;
			
			if(nameExpr==null){
				
				return null;
				
			}
			
			// Get the type:
			Type type=nameExpr.Variable.Type;
			
			// Get the prototype;
			Library.Prototype proto=optimizationInfo.Engine.Prototypes.Get(type);
			
			// Get ctr object, which may be a set:
			object onCtr=proto.OnConstruct;
			
			if(onCtr==null){
				
				throw new Exception("Didn't recognise that as a suitable constructable object");
				
			}
			
			// Ok! It could be a set - let's check:
			MethodGroup group=onCtr as MethodGroup;
			
			System.Reflection.MethodBase resolvedMethod=null;
	
			if(group==null)
			{
				// It must be MethodBase - it can't be anything else:
				resolvedMethod=onCtr as System.Reflection.MethodBase;
			}
			else
			{
				// We have a group! Find the overload that we're after (no args were passed here):
				resolvedMethod=group.Match(null);
			}
			
			// Create a FunctionCallExpr.
			FunctionCallExpression function=new FunctionCallExpression(Operator.FunctionCall);
			
			// Apply resolved:
			function.ResolvedMethod=resolvedMethod;
			
			// Apply target:
			function.Push(operand);
			
			// Overwrite local operand:
			SetRawOperand(0,function);
			
			return function;
			
		}
		
		/// <summary>
		/// Generates CIL for the expression.
		/// </summary>
		/// <param name="generator"> The generator to output the CIL to. </param>
		/// <param name="optimizationInfo"> Information about any optimizations that should be performed. </param>
		public override void GenerateCode(ILGenerator generator, OptimizationInfo optimizationInfo)
		{
			// Note: we use GetRawOperand() so that grouping operators are not ignored.
			var operand = this.GetRawOperand(0);
			
			// There is only one operand, and it can be either a reference or a function call.
			// We need to split the operand into a function and some arguments.
			// If the operand is a reference, it is equivalent to a function call with no arguments.
			
			// Get as a call expr:
			FunctionCallExpression function = operand as FunctionCallExpression;
			
			System.Reflection.MethodBase resolvedMethod=null;
	
			if(function!=null){
				
				// Did it resolve?
				resolvedMethod=function.ResolvedMethod;
				
			}
			
			if(resolvedMethod!=null)
			{
				// Awesome, we can pre-emit the construct call!
				
				// Set is constructor (immediately cleared by CallExpr's GenerateCode method):
				optimizationInfo.IsConstructCall=true;
				
				// Emit an ordinary call:
				function.GenerateCode(generator,optimizationInfo);
				return;
				
			}
			
			#warning incorrect.
			// Emit the function instance first.
			function.GenerateCode(generator, optimizationInfo);
			EmitConversion.ToAny(generator, function.GetResultType(optimizationInfo));
			
		}
		
	}

}