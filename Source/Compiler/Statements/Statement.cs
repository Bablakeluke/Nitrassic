using System;
using System.Collections.Generic;

namespace Nitrassic.Compiler
{

	/// <summary>
	/// Represents a javascript statement.
	/// </summary>
	public abstract class Statement : AstNode
	{
		
		public virtual bool DefaultBreakStatementBehaviour{
			get{
				return true;
			}
		}
		
		/// <summary>Sets the current statement root.</summary>
		public virtual void SetRoot(OptimizationInfo optimizationInfo){
			
		}
		
		/// <summary>
		/// Converts the statement to a string.
		/// </summary>
		/// <returns> A string representing this statement. </returns>
		public override string ToString()
		{
			return this.ToString(0);
		}

		/// <summary>
		/// Converts the statement to a string.
		/// </summary>
		/// <param name="indentLevel"> The number of tabs to include before the statement. </param>
		/// <returns> A string representing this statement. </returns>
		public abstract string ToString(int indentLevel);
	}

}