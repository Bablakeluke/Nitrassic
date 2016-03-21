using System;
using System.Collections.Generic;

namespace Nitrassic.Compiler
{

	/// <summary>
	/// Represents the debugger statement.
	/// </summary>
	internal class DebuggerStatement : Statement
	{
		
		/// <summary>
		/// Generates CIL for the statement.
		/// </summary>
		/// <param name="generator"> The generator to output the CIL to. </param>
		/// <param name="optimizationInfo"> Information about any optimizations that should be performed. </param>
		public override void GenerateCode(ILGenerator generator, OptimizationInfo optimizationInfo)
		{
			
			// Inserts a breakpoint into the IL.
			generator.Breakpoint();

			// When the debugger stops, it stops at the first instruction after the breakpoint.  By
			// inserting a no-op operation the debugger will highlight the "debugger" statement
			// instead of the statement after the "debugger" statement.
			generator.NoOperation();
			
		}

		/// <summary>
		/// Converts the statement to a string.
		/// </summary>
		/// <param name="indentLevel"> The number of tabs to include before the statement. </param>
		/// <returns> A string representing this statement. </returns>
		public override string ToString(int indentLevel)
		{
			return new string('\t', indentLevel) + "debugger;";
		}
	}

}