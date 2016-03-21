﻿using System;
using System.Collections.Generic;

namespace Nitrassic.Compiler
{

	/// <summary>
	/// Represents a javascript while statement.
	/// </summary>
	internal class WhileStatement : LoopStatement
	{
		
		/// <summary>
		/// Converts the statement to a string.
		/// </summary>
		/// <param name="indentLevel"> The number of tabs to include before the statement. </param>
		/// <returns> A string representing this statement. </returns>
		public override string ToString(int indentLevel)
		{
			var result = new System.Text.StringBuilder();
			result.Append(new string('\t', indentLevel));
			result.Append("while (");
			result.Append(this.Condition);
			result.AppendLine(")");
			result.Append(this.Body.ToString(indentLevel + 1));
			return result.ToString();
		}
	}

}