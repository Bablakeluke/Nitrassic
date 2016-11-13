using System;
using System.Collections.Generic;

namespace Nitrassic.Compiler
{

	/// <summary>
	/// Represents a throw statement.
	/// </summary>
	internal class ThrowStatement : Statement
	{
		
		/// <summary>
		/// Gets or sets an expression which evaluates to the value to throw.
		/// </summary>
		public Expression Value;

		/// <summary>
		/// Generates CIL for the statement.
		/// </summary>
		/// <param name="generator"> The generator to output the CIL to. </param>
		/// <param name="optimizationInfo"> Information about any optimizations that should be performed. </param>
		public override void GenerateCode(ILGenerator generator, OptimizationInfo optimizationInfo)
		{
			
			// Emit code to throw the given value.
			EmitHelpers.LoadEngine(generator);
			this.Value.GenerateCode(generator, optimizationInfo);
			EmitConversion.ToAny(generator, this.Value.GetResultType(optimizationInfo));
			generator.LoadInt32(1);
			generator.LoadStringOrNull(optimizationInfo.Source.Path);
			generator.LoadStringOrNull(optimizationInfo.FunctionName);
			generator.NewObject(ReflectionHelpers.JavaScriptException_Constructor_Object);
			generator.Throw();
			
		}

		/// <summary>
		/// Gets an enumerable list of child nodes in the abstract syntax tree.
		/// </summary>
		public override IEnumerable<AstNode> ChildNodes
		{
			get { yield return this.Value; }
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
			result.Append("throw ");
			result.Append(this.Value);
			result.Append(";");
			return result.ToString();
		}
	}

}