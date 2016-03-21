using System;
using System.Collections.Generic;

namespace Nitrassic.Compiler
{

	/// <summary>
	/// Represents a javascript expression statement.
	/// </summary>
	internal class ExpressionStatement : Statement
	{
		/// <summary>
		/// Creates a new ExpressionStatement instance.
		/// </summary>
		/// <param name="expression"> The underlying expression. </param>
		public ExpressionStatement(Expression expression)
		{
			if (expression == null)
				throw new ArgumentNullException("expression");
			this.Expression = expression;
		}
		
		/// <summary>
		/// Gets or sets the underlying expression.
		/// </summary>
		public Expression Expression;
		
		/// <summary>
		/// Generates CIL for the statement.
		/// </summary>
		/// <param name="generator"> The generator to output the CIL to. </param>
		/// <param name="optimizationInfo"> Information about any optimizations that should be performed. </param>
		public override void GenerateCode(ILGenerator generator, OptimizationInfo optimizationInfo)
		{
			
			// Emit the expression.
			Expression.GenerateCode(generator, optimizationInfo);
			generator.Pop();
			
		}

		/// <summary>
		/// Gets an enumerable list of child nodes in the abstract syntax tree.
		/// </summary>
		public override IEnumerable<AstNode> ChildNodes
		{
			get { yield return this.Expression; }
		}

		/// <summary>
		/// Converts the statement to a string.
		/// </summary>
		/// <param name="indentLevel"> The number of tabs to include before the statement. </param>
		/// <returns> A string representing this statement. </returns>
		public override string ToString(int indentLevel)
		{
			return string.Format("{0}{1};", new string('\t', indentLevel), this.Expression);
		}
	}

}