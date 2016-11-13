using System;
using System.Collections.Generic;

namespace Nitrassic.Compiler
{

	/// <summary>
	/// Represents a javascript with statement.
	/// </summary>
	internal class WithStatement : Statement
	{
		
		/// <summary>
		/// Gets or sets the object scope inside the with statement.
		/// </summary>
		public ObjectScope Scope;

		/// <summary>
		/// Gets or sets the body of the with statement.
		/// </summary>
		public Statement Body;

		/// <summary>
		/// Generates CIL for the statement.
		/// </summary>
		/// <param name="generator"> The generator to output the CIL to. </param>
		/// <param name="optimizationInfo"> Information about any optimizations that should be performed. </param>
		public override void GenerateCode(ILGenerator generator, OptimizationInfo optimizationInfo)
		{
			
			// Get the previous root - we'll be changing it:
			Expression prevRoot=optimizationInfo.RootExpression;
			
			// Set as root:
			Body.SetRoot(optimizationInfo);
			
			// Generate code for the body statements.
			Body.GenerateCode(generator, optimizationInfo);
			
			// Restore root:
			optimizationInfo.RootExpression=prevRoot;
			
		}

		/// <summary>
		/// Gets an enumerable list of child nodes in the abstract syntax tree.
		/// </summary>
		public override IEnumerable<AstNode> ChildNodes
		{
			get
			{
				yield return this.Scope.ScopeObjectExpression;
				yield return this.Body;
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
			result.Append("with (");
			result.Append(this.Scope.ScopeObjectExpression);
			result.AppendLine(")");
			result.Append(this.Body.ToString(indentLevel + 1));
			return result.ToString();
		}
	}

}