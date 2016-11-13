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
		
		/// <summary>Set if this statement has a static result.</summary>
		private object StaticResult;
		
		/// <summary>
		/// Resolves which function is being called where possible.
		/// </summary>
		internal override void ResolveVariables(OptimizationInfo optimizationInfo)
		{
			
			// Resolve condition:
			Condition.ResolveVariables(optimizationInfo);
			
			// Is the condition constant?
			StaticResult=Condition.Evaluate();
			
			if(StaticResult==null){
				
				// Resolve both:
				IfClause.ResolveVariables(optimizationInfo);
				
				if(ElseClause!=null){
					// Only resolve else.
					ElseClause.ResolveVariables(optimizationInfo);
				}
				
			}else{
				
				// Convert it to a bool:
				bool result=TypeConverter.ToBoolean(StaticResult);
				
				if(result){
					
					// Only resolve if.
					IfClause.ResolveVariables(optimizationInfo);
					
				}else{
					
					if(ElseClause!=null){
						// Only resolve else.
						ElseClause.ResolveVariables(optimizationInfo);
					}
					
				}
				
			}
			
		}
		
		/// <summary>
		/// Generates CIL for the statement.
		/// </summary>
		/// <param name="generator"> The generator to output the CIL to. </param>
		/// <param name="optimizationInfo"> Information about any optimizations that should be performed. </param>
		public override void GenerateCode(ILGenerator generator, OptimizationInfo optimizationInfo)
		{
			
			// Get the root:
			Expression prevRoot=optimizationInfo.RootExpression;
			
			// Is the condition constant?
			if(StaticResult!=null){
				
				// We have a compile-time constant (i.e. if(true) )
				
				// Convert it to a bool:
				bool result=TypeConverter.ToBoolean(StaticResult);
				
				if(result){
					// if(true)
					
					// Generate code for the if clause which is now the root:
					IfClause.SetRoot(optimizationInfo);
					
					IfClause.GenerateCode(generator, optimizationInfo);
					
				}else{
					// if(false)
					
					if(ElseClause!=null){
						
						// It's now the root:
						ElseClause.SetRoot(optimizationInfo);
						
						// Code for the else clause:
						ElseClause.GenerateCode(generator, optimizationInfo);
						
					}
					
				}
				
				// Restore root:
				optimizationInfo.RootExpression=prevRoot;
				
				return;
				
			}
			
			// Generate code for the condition and coerce to a boolean.
			this.Condition.GenerateCode(generator, optimizationInfo);
			EmitConversion.ToBool(generator, this.Condition.GetResultType(optimizationInfo));
			
			// We will need a label at the end of the if statement.
			var endOfEverything = generator.CreateLabel();

			if (this.ElseClause == null)
			{
				// Jump to the end if the condition is false.
				generator.BranchIfFalse(endOfEverything);
				
				// If clause is now the root:
				IfClause.SetRoot(optimizationInfo);
				
				// Generate code for the if clause.
				IfClause.GenerateCode(generator, optimizationInfo);
			}
			else
			{
				// Branch to the else clause if the condition is false.
				var startOfElseClause = generator.CreateLabel();
				generator.BranchIfFalse(startOfElseClause);

				// If clause is now the root:
				IfClause.SetRoot(optimizationInfo);
				
				// Generate code for the if clause.
				IfClause.GenerateCode(generator, optimizationInfo);

				// Branch to the end of the if statement.
				generator.Branch(endOfEverything);

				// Else clause is now the root:
				ElseClause.SetRoot(optimizationInfo);
				
				// Generate code for the else clause.
				generator.DefineLabelPosition(startOfElseClause);
				ElseClause.GenerateCode(generator, optimizationInfo);
				
			}

			// Define the label at the end of the if statement.
			generator.DefineLabelPosition(endOfEverything);
			
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