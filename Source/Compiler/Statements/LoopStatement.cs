using System;
using System.Collections.Generic;

namespace Nitrassic.Compiler
{

	/// <summary>
	/// Represents a javascript loop statement (for, for-in, while and do-while).
	/// </summary>
	internal abstract class LoopStatement : Statement
	{
		
		/// <summary>
		/// Gets or sets the statement that initializes the loop variable.
		/// </summary>
		public Statement InitStatement;

		/// <summary>
		/// Gets the var statement that initializes the loop variable.
		/// </summary>
		public VarStatement InitVarStatement
		{
			get { return this.InitStatement as VarStatement; }
		}

		/// <summary>
		/// Gets the expression that initializes the loop variable.
		/// </summary>
		public Expression InitExpression
		{
			get { return (this.InitStatement is ExpressionStatement) == true ? ((ExpressionStatement)this.InitStatement).Expression : null; }
		}

		/// <summary>
		/// Gets or sets the statement that checks whether the loop should terminate.
		/// </summary>
		public ExpressionStatement ConditionStatement;

		/// <summary>
		/// Gets the expression that checks whether the loop should terminate.
		/// </summary>
		public Expression Condition
		{
			get { return this.ConditionStatement.Expression; }
		}

		/// <summary>
		/// Gets or sets the statement that increments (or decrements) the loop variable.
		/// </summary>
		public ExpressionStatement IncrementStatement;

		/// <summary>
		/// Gets the expression that increments (or decrements) the loop variable.
		/// </summary>
		public Expression Increment
		{
			get { return this.IncrementStatement.Expression; }
		}

		/// <summary>
		/// Gets or sets the loop body.
		/// </summary>
		public Statement Body;

		/// <summary>
		/// Gets a value that indicates whether the condition should be checked at the end of the
		/// loop.
		/// </summary>
		protected virtual bool CheckConditionAtEnd
		{
			get { return false; }
		}
		
		public override bool DefaultBreakStatementBehaviour{
			get{
				return false;
			}
		}
		
		/// <summary>
		/// Generates CIL for the statement.
		/// </summary>
		/// <param name="generator"> The generator to output the CIL to. </param>
		/// <param name="optimizationInfo"> Information about any optimizations that should be performed. </param>
		public override void GenerateCode(ILGenerator generator, OptimizationInfo optimizationInfo)
		{
			List<string> labels=optimizationInfo.Labels;
			optimizationInfo.Labels=null;
			
			// Emit the initialization statement.
			if (this.InitStatement != null)
				this.InitStatement.GenerateCode(generator, optimizationInfo);
			
			var startOfLoop = generator.DefineLabelPosition();
			var continueTarget = generator.CreateLabel();
			var endOfLoop = generator.CreateLabel();
			
			bool conditionAtEnd=CheckConditionAtEnd;
			
			// AT END..
			// top:
			// <body statements>
			// continue:
			// <increment>
			// if(condition)
			// <goto top>
			// break:
			
			// top:
			// if(!condition)
				// <goto break>
			// <body statements>
			// continue:
			// <increment>
			// <goto top>
			// break:
			
			if(!conditionAtEnd)
			{
			
				if(ConditionStatement != null)
				{
					Condition.GenerateCode(generator, optimizationInfo);
					EmitConversion.ToBool(generator, Condition.GetResultType(optimizationInfo));
					generator.BranchIfFalse(endOfLoop);
				}
				
			}
			
			// Emit the loop body.
			optimizationInfo.PushBreakOrContinueInfo(labels, endOfLoop, continueTarget, false);
			this.Body.GenerateCode(generator, optimizationInfo);
			optimizationInfo.PopBreakOrContinueInfo();
			
			generator.DefineLabelPosition(continueTarget);
			
			// Increment the loop variable.
			if (this.IncrementStatement != null)
				this.IncrementStatement.GenerateCode(generator, optimizationInfo);
			
			if(conditionAtEnd)
			{
				
				if(ConditionStatement != null)
				{
					Condition.GenerateCode(generator, optimizationInfo);
					EmitConversion.ToBool(generator, Condition.GetResultType(optimizationInfo));
					generator.BranchIfTrue(startOfLoop);
				}
				
			}else{
			
				generator.Branch(startOfLoop);
				
			}
			
			generator.DefineLabelPosition(endOfLoop);
			
		}
		
		/// <summary>
		/// Gets an enumerable list of child nodes in the abstract syntax tree.
		/// </summary>
		public override IEnumerable<AstNode> ChildNodes
		{
			get
			{
				if (this.InitStatement != null)
					yield return this.InitStatement;
				if (this.CheckConditionAtEnd == false && this.ConditionStatement != null)
					yield return this.ConditionStatement;
				yield return this.Body;
				if (this.IncrementStatement != null)
					yield return this.IncrementStatement;
				if (this.CheckConditionAtEnd == true && this.ConditionStatement != null)
					yield return this.ConditionStatement;
			}
		}
	}

}