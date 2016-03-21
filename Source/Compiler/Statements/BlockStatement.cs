﻿using System;
using System.Collections.Generic;

namespace Nitrassic.Compiler
{

	/// <summary>
	/// Represents a javascript block statement.
	/// </summary>
	internal class BlockStatement : Statement
	{
		private List<Statement> statements = new List<Statement>();
		
		/// <summary>
		/// Gets a list of the statements in the block.
		/// </summary>
		public IList<Statement> Statements
		{
			get { return this.statements; }
		}

		/// <summary>
		/// Generates CIL for the statement.
		/// </summary>
		/// <param name="generator"> The generator to output the CIL to. </param>
		/// <param name="optimizationInfo"> Information about any optimizations that should be performed. </param>
		public override void GenerateCode(ILGenerator generator, OptimizationInfo optimizationInfo)
		{
			
			foreach (var statement in this.Statements)
			{
				// Generate code for the statement.
				statement.GenerateCode(generator, optimizationInfo);
			}
			
		}

		/// <summary>
		/// Gets an enumerable list of child nodes in the abstract syntax tree.
		/// </summary>
		public override IEnumerable<AstNode> ChildNodes
		{
			get
			{
				foreach (var statement in this.Statements)
					yield return statement;
			}
		}

		/// <summary>
		/// Converts the statement to a string.
		/// </summary>
		/// <param name="indentLevel"> The number of tabs to include before the statement. </param>
		/// <returns> A string representing this statement. </returns>
		public override string ToString(int indentLevel)
		{
			indentLevel = Math.Max(indentLevel - 1, 0);
			var result = new System.Text.StringBuilder();
			result.Append(new string('\t', indentLevel));
			result.AppendLine("{");
			foreach (var statement in this.Statements)
				result.AppendLine(statement.ToString(indentLevel + 1));
			result.Append(new string('\t', indentLevel));
			result.Append("}");
			return result.ToString();
		}
	}

}