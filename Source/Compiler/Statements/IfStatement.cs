using System;
using System.Collections.Generic;

namespace Nitrassic.Compiler
{

	/// <summary>
	/// Represents an if statement.
	/// </summary>
	internal class IfStatement : Statement
	{
		
		/// <summary>
		/// Gets or sets the condition that determines which path the code should proceed.
		/// </summary>
		public Expression Condition;

		/// <summary>
		/// Gets or sets the statement that is executed if the condition is true.
		/// </summary>
		public Statement IfClause;

		/// <summary>
		/// Gets or sets the statement that is executed if the condition is false.
		/// </summary>
		public Statement ElseClause;

		/// <summary>
		/// Generates CIL for the statement.
		/// </summary>
		/// <param name="generator"> The generator to output the CIL to. </param>
		/// <param name="optimizationInfo"> Information about any optimizations that should be performed. </param>
		public override void GenerateCode(ILGenerator generator, OptimizationInfo optimizationInfo)
		{
			
			// Generate code for the condition and coerce to a boolean.
			this.Condition.GenerateCode(generator, optimizationInfo);
			EmitConversion.ToBool(generator, this.Condition.GetResultType(optimizationInfo));

			// We will need a label at the end of the if statement.
			var endOfEverything = generator.CreateLabel();

			if (this.ElseClause == null)
			{
				// Jump to the end if the condition is false.
				generator.BranchIfFalse(endOfEverything);

				// Generate code for the if clause.
				this.IfClause.GenerateCode(generator, optimizationInfo);
			}
			else
			{
				// Branch to the else clause if the condition is false.
				var startOfElseClause = generator.CreateLabel();
				generator.BranchIfFalse(startOfElseClause);

				// Generate code for the if clause.
				this.IfClause.GenerateCode(generator, optimizationInfo);

				// Branch to the end of the if statement.
				generator.Branch(endOfEverything);

				// Generate code for the else clause.
				generator.DefineLabelPosition(startOfElseClause);
				this.ElseClause.GenerateCode(generator, optimizationInfo);
				
			}

			// Define the label at the end of the if statement.
			generator.DefineLabelPosition(endOfEverything);
			
		}

		/// <summary>
		/// Gets an enumerable list of child nodes in the abstract syntax tree.
		/// </summary>
		public override IEnumerable<AstNode> ChildNodes
		{
			get
			{
				yield return this.Condition;
				yield return this.IfClause;
				if (this.ElseClause != null)
					yield return this.ElseClause;
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
			result.Append("if (");
			result.Append(this.Condition.ToString());
			result.AppendLine(")");
			result.AppendLine(this.IfClause.ToString(indentLevel + 1));
			if (this.ElseClause != null)
			{
				result.Append(new string('\t', indentLevel));
				result.AppendLine("else");
				result.AppendLine(this.ElseClause.ToString(indentLevel + 1));
			}
			return result.ToString();
		}
	}

}