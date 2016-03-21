using System;

namespace Nitrassic.Compiler
{
	/// <summary>
	/// Represents a ternary operator expression.
	/// </summary>
	internal sealed class TernaryExpression : OperatorExpression
	{
		/// <summary>
		/// Creates a new instance of TernaryExpression.
		/// </summary>
		/// <param name="operator"> The ternary operator to base this expression on. </param>
		public TernaryExpression(Operator @operator)
			: base(@operator)
		{
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
			return typeof(object);
		}

		/// <summary>
		/// Generates CIL for the expression.
		/// </summary>
		/// <param name="generator"> The generator to output the CIL to. </param>
		/// <param name="optimizationInfo"> Information about any optimizations that should be performed. </param>
		public override void GenerateCode(ILGenerator generator, OptimizationInfo optimizationInfo)
		{
			// If a return value is not expected, generate only the side-effects.
			/*if (optimizationInfo.SuppressReturnValue == true)
			{
				this.GenerateSideEffects(generator, optimizationInfo);
				return;
			}*/

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

			// Emit the second operand and convert it to the result type.
			var operand2 = this.GetOperand(1);
			operand2.GenerateCode(generator, optimizationInfo);
			EmitConversion.Convert(generator, operand2.GetResultType(optimizationInfo), outputType, optimizationInfo);

			// Branch to the end.
			var end = generator.CreateLabel();
			generator.Branch(end);
			generator.DefineLabelPosition(startOfElse);

			// Emit the third operand and convert it to the result type.
			var operand3 = this.GetOperand(2);
			operand3.GenerateCode(generator, optimizationInfo);
			EmitConversion.Convert(generator, operand3.GetResultType(optimizationInfo), outputType, optimizationInfo);

			// Define the end label.
			generator.DefineLabelPosition(end);
		}
	}

}