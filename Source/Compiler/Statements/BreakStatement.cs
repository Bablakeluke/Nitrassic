﻿using System;
using System.Collections.Generic;

namespace Nitrassic.Compiler
{

	/// <summary>
	/// Represents a break statement.
	/// </summary>
	internal class BreakStatement : Statement
	{
		
		/// <summary>
		/// Gets or sets the name of the label that identifies the block to break out of.  Can be
		/// <c>null</c>.
		/// </summary>
		public string Label;

		/// <summary>
		/// Generates CIL for the statement.
		/// </summary>
		/// <param name="generator"> The generator to output the CIL to. </param>
		/// <param name="optimizationInfo"> Information about any optimizations that should be performed. </param>
		public override void GenerateCode(ILGenerator generator, OptimizationInfo optimizationInfo)
		{
			
			// Emit an unconditional branch.
			// Note: the break statement might be branching from inside a try { } or finally { }
			// block to outside.  EmitLongJump() handles this.
			optimizationInfo.EmitLongJump(generator, optimizationInfo.GetBreakTarget(this.Label));
			
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
			result.Append("break");
			if (this.Label != null)
			{
				result.Append(" ");
				result.Append(this.Label);
			}
			result.Append(";");
			return result.ToString();
		}
	}

}