using System;
using System.Collections.Generic;

namespace Nitrassic.Compiler
{

	/// <summary>
	/// Represents a break statement.
	/// </summary>
	internal class LabelledStatement : Statement
	{
		
		private List<string> labels;
		
		public Statement Labelled;
		
		public LabelledStatement(List<string> set,Statement labelled)
		{
			
			labels=set;
			Labelled=labelled;
			
		}
		
		public override void GenerateCode(ILGenerator generator, OptimizationInfo optimizationInfo)
		{
			
			// Get the previous root - we'll be changing it:
			Expression prevRoot=optimizationInfo.RootExpression;
			
			// Labelled is now the root:
			Labelled.SetRoot(optimizationInfo);
			
			if (Labelled.DefaultBreakStatementBehaviour)
			{
				
				// Set up the information needed by the break statement.
				ILLabel endOfStatement = generator.CreateLabel();
				optimizationInfo.PushBreakOrContinueInfo(labels, endOfStatement, null, true);
				
				Labelled.GenerateCode(generator,optimizationInfo);
				
				// Revert the information needed by the break statement.
				generator.DefineLabelPosition(endOfStatement);
				optimizationInfo.PopBreakOrContinueInfo();
				
			}else{
				
				// Loop, For-In, For-Of or Switch only.
				optimizationInfo.Labels=labels;
				Labelled.GenerateCode(generator,optimizationInfo);
				
			}
			
			// Restore root:
			optimizationInfo.RootExpression=prevRoot;
			
		}
		
		/// <summary>
		/// Gets an enumerable list of child nodes in the abstract syntax tree.
		/// </summary>
		public override IEnumerable<AstNode> ChildNodes
		{
			get { yield return this.Labelled; }
		}

		public override string ToString(int indentLevel)
		{
			return Labelled.ToString(indentLevel);
		}
		
	}
	
}