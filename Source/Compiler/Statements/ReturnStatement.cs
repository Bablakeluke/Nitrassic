using System;
using System.Collections.Generic;

namespace Nitrassic.Compiler
{

	/// <summary>
	/// Represents a return statement.
	/// </summary>
	internal class ReturnStatement : Statement
	{
		
		/// <summary>
		/// Gets or sets the expression to return.  Can be <c>null</c> to return "undefined".
		/// </summary>
		public Expression Value;

		/// <summary>
		/// Generates CIL for the statement.
		/// </summary>
		/// <param name="generator"> The generator to output the CIL to. </param>
		/// <param name="optimizationInfo"> Information about any optimizations that should be performed. </param>
		public override void GenerateCode(ILGenerator generator, OptimizationInfo optimizationInfo)
		{
			
			// Emit the return value.
			if (this.Value == null)
				EmitHelpers.EmitUndefined(generator);
			else
			{
				this.Value.GenerateCode(generator, optimizationInfo);
				EmitConversion.ToAny(generator, this.Value.GetResultType(optimizationInfo));
			}

			// Determine if this is the last statement in the function.
			bool lastStatement = optimizationInfo.AbstractSyntaxTree is BlockStatement &&
				((BlockStatement)optimizationInfo.AbstractSyntaxTree).Statements.Count > 0 &&
				((BlockStatement)optimizationInfo.AbstractSyntaxTree).Statements[((BlockStatement)optimizationInfo.AbstractSyntaxTree).Statements.Count - 1] == this;

			// The first return statement initializes the variable that holds the return value.
			if (optimizationInfo.ReturnVariable == null)
				optimizationInfo.ReturnVariable = generator.DeclareVariable(typeof(object), "returnValue");

			// Store the return value in a variable.
			generator.StoreVariable(optimizationInfo.ReturnVariable);

			// There is no need to jump to the end of the function if this is the last statement.
			if (lastStatement == false)
			{
				
				// The first return statement that needs to branch creates the return label.  This is
				// defined in FunctionmethodGenerator.GenerateCode at the end of the function.
				if (optimizationInfo.ReturnTarget == null)
					optimizationInfo.ReturnTarget = generator.CreateLabel();

				// Branch to the end of the function.  Note: the return statement might be branching
				// from inside a try { } or finally { } block to outside.  EmitLongJump() handles this.
				optimizationInfo.EmitLongJump(generator, optimizationInfo.ReturnTarget);

			}
			
		}

		/// <summary>
		/// Gets an enumerable list of child nodes in the abstract syntax tree.
		/// </summary>
		public override IEnumerable<AstNode> ChildNodes
		{
			get
			{
				if (this.Value != null)
					yield return this.Value;
			}
		}

		/// <summary>
		/// Converts the statement to a string.
		/// </summary>
		/// <param name="indentLevel"> The number of tabs to include before the statement. </param>
		/// <returns> A string representing this statement. </returns>
		public override string ToString(int indentLevel)
		{
			var result = new System.Text.StringBuilder();
			result.Append(new string('\t', indentLevel));
			result.Append("return");
			if (this.Value != null)
			{
				result.Append(" ");
				result.Append(this.Value);
			}
			result.Append(";");
			return result.ToString();
		}
	}

}