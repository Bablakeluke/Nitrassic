using System;

namespace Nitrassic.Compiler
{
	/// <summary>
	/// Represents a ternary operator expression.
	/// </summary>
	internal sealed class TernaryExpression : OperatorExpression
	{
		
		/// <summary>Set if this statement has a static result.</summary>
		private object StaticResult;
		
		/// <summary>
		/// Creates a new instance of TernaryExpression.
		/// </summary>
		/// <param name="operator"> The ternary operator to base this expression on. </param>
		public TernaryExpression(Operator @operator)
			: base(@operator)
		{
		}
		
		internal override void ResolveVariables(OptimizationInfo optimizationInfo)
		{
			
			Expression condition=GetOperand(0);
			Expression operand2=GetOperand(1);
			Expression operand3=GetOperand(2);
			
			// Resolve conditions vars:
			condition.ResolveVariables(optimizationInfo);
			
			// Is condition static?
			StaticResult=condition.Evaluate();
			
			if(StaticResult==null){
				
				// Runtime resolve only.
				
				// Resolve both:
				operand2.ResolveVariables(optimizationInfo);
				operand3.ResolveVariables(optimizationInfo);
				
			}else{
				
				// Great - we have true?.. or false?.. Only resolve variables for true or false.
				// Note that resolving both will often collapse variables and we don't want that!
				
				// Convert it to a bool:
				bool result=TypeConverter.ToBoolean(StaticResult);
				
				if(result){
					operand2.ResolveVariables(optimizationInfo);
				}else{
					operand3.ResolveVariables(optimizationInfo);
				}
				
			}
			
		}
		
		/// <summary>
		/// Evaluates the expression, if possible.
		/// </summary>
		/// <returns> The result of evaluating the expression, or <c>null</c> if the expression can
		/// not be evaluated. </returns>
		public override object Evaluate()
		{
			// Evaluate the operands.
			var condition = this.GetOperand(0).Evaluate();
			if (condition == null)
				return null;
			
			var result1 = this.GetOperand(1).Evaluate();
			if (result1 == null)
				return null;
			var result2 = this.GetOperand(2).Evaluate();
			if (result2 == null)
				return null;

			// Apply the conditional operator logic.
			if (TypeConverter.ToBoolean(condition) == true)
				return result1;
			return result2;
		}

		/// <summary>
		/// Gets the type that results from evaluating this expression.
		/// </summary>
		public override Type GetResultType(OptimizationInfo optimizationInfo)
		{
			// The result is either the type of the second operand or the third operand.
			var a = this.GetOperand(1).GetResultType(optimizationInfo);
			var b = this.GetOperand(2).GetResultType(optimizationInfo);
			if (a == b)
				return a;
			if (PrimitiveTypeUtilities.IsNumeric(a) == true && PrimitiveTypeUtilities.IsNumeric(b) == true)
				return typeof(double);
			
			if(StaticResult!=null){
				
				// Convert it to a bool:
				bool result=TypeConverter.ToBoolean(StaticResult);
				
				if(result){
					return a;
				}
				
				return b;
				
			}
			
			// Don't know if it'll be a or b, and they're distinctive types.
			return typeof(object);
			
		}

		/// <summary>
		/// Generates CIL for the expression.
		/// </summary>
		/// <param name="generator"> The generator to output the CIL to. </param>
		/// <param name="optimizationInfo"> Information about any optimizations that should be performed. </param>
		public override void GenerateCode(ILGenerator generator, OptimizationInfo optimizationInfo)
		{
			
			// If our host is a root then we are also a root.
			Expression prevRoot=optimizationInfo.RootExpression;
			
			Expression operand2 = this.GetOperand(1);
			Expression operand3 = this.GetOperand(2);
			
			if(StaticResult!=null){
				
				// Going one way or the other statically!
				
				// Convert it to a bool:
				bool result=TypeConverter.ToBoolean(StaticResult);
				
				if(result){
					// if(true)
					
					if(prevRoot==this){
						// Host is a root therefore we are one now:
						optimizationInfo.RootExpression=operand2;
					}
					
					// Generate code for the if clause:
					operand2.GenerateCode(generator, optimizationInfo);
					
				}else{
					// if(false)
					
					if(prevRoot==this){
						// Host is a root therefore we are one now:
						optimizationInfo.RootExpression=operand3;
					}
					
					// Code for the else clause:
					operand3.GenerateCode(generator, optimizationInfo);
					
				}
				
				// Restore root:
				optimizationInfo.RootExpression=prevRoot;
				return;
				
			}
			
			// Emit the condition.
			var condition = this.GetOperand(0);
			condition.GenerateCode(generator, optimizationInfo);

			// Convert the condition to a boolean.
			EmitConversion.ToBool(generator, condition.GetResultType(optimizationInfo));
			
			// Branch if the condition is false.
			var startOfElse = generator.CreateLabel();
			generator.BranchIfFalse(startOfElse);

			// Calculate the result type.
			var outputType = this.GetResultType(optimizationInfo);

			if(prevRoot==this){
				// Host is a root therefore we are one now:
				optimizationInfo.RootExpression=operand2;
			}
			
			// Emit the second operand and convert it to the result type.
			operand2.GenerateCode(generator, optimizationInfo);
			EmitConversion.Convert(generator, operand2.GetResultType(optimizationInfo), outputType, optimizationInfo);

			// Branch to the end.
			var end = generator.CreateLabel();
			generator.Branch(end);
			generator.DefineLabelPosition(startOfElse);
			
			if(prevRoot==this){
				// Host is a root therefore we are one now:
				optimizationInfo.RootExpression=operand3;
			}
			
			// Emit the third operand and convert it to the result type.
			operand3.GenerateCode(generator, optimizationInfo);
			EmitConversion.Convert(generator, operand3.GetResultType(optimizationInfo), outputType, optimizationInfo);

			// Define the end label.
			generator.DefineLabelPosition(end);
			
			// Restore root:
			optimizationInfo.RootExpression=prevRoot;
			
		}
	}

}