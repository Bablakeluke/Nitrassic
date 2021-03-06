﻿using System;
using System.Collections.Generic;

namespace Nitrassic.Compiler
{

    /// <summary>
    /// Represents a javascript for-of statement.
    /// </summary>
    internal class ForOfStatement : Statement
    {
        
        /// <summary>
        /// Gets or sets a reference to mutate on each iteration of the loop.
        /// </summary>
        public IReferenceExpression Variable;
		
        /// <summary>
        /// Gets or sets an expression that evaluates to the object to enumerate.
        /// </summary>
        public Expression TargetObject;
		
        /// <summary>
        /// Gets or sets the loop body.
        /// </summary>
        public Statement Body;

		public override bool DefaultBreakStatementBehaviour{
			get{
				return false;
			}
		}
		
		internal override void ResolveVariables(OptimizationInfo optimizationInfo){
			
			// Apply var:
			Variable.ApplyType(optimizationInfo,typeof(object));
			
			base.ResolveVariables(optimizationInfo);
			
		}
		
        /// <summary>
        /// Generates CIL for the statement.
        /// </summary>
        /// <param name="generator"> The generator to output the CIL to. </param>
        /// <param name="optimizationInfo"> Information about any optimizations that should be performed. </param>
        public override void GenerateCode(ILGenerator generator, OptimizationInfo optimizationInfo)
        {
            
			// Get the previous root - we'll be changing it:
			Expression prevRoot=optimizationInfo.RootExpression;
			
			List<string> labels=optimizationInfo.Labels;
			optimizationInfo.Labels=null;
			
			// Construct a loop expression.
			// var enumerator = TypeUtilities.EnumeratePropertyNames(rhs).GetEnumerator();
			// while (true) {
			//   continue-target:
			//   if (enumerator.MoveNext() == false)
			//	 goto break-target;
			//   lhs = enumerator.Current;
			//
			//   <body statements>
			// }
			// break-target:

			// Call IEnumerable<string> EnumeratePropertyNames(ScriptEngine engine, object obj)
			// optimizationInfo.MarkSequencePoint(generator, this.TargetObjectSourceSpan);
			EmitHelpers.LoadEngine(generator);
			this.TargetObject.GenerateCode(generator, optimizationInfo);
			EmitConversion.ToAny(generator, this.TargetObject.GetResultType(optimizationInfo));
			generator.Call(ReflectionHelpers.TypeUtilities_EnumeratePropertyValues);
			
			// Store the enumerator in a temporary variable.
			var enumerator = generator.CreateTemporaryVariable(typeof(IEnumerator<object>));
			generator.StoreVariable(enumerator);

			var breakTarget = generator.CreateLabel();
			var continueTarget = generator.DefineLabelPosition();

			// Emit debugging information.
			// if (optimizationInfo.DebugDocument != null)
			// 	generator.MarkSequencePoint(optimizationInfo.DebugDocument, this.VariableSourceSpan);

			//   if (enumerator.MoveNext() == false)
			//	 goto break-target;
			generator.LoadVariable(enumerator);
			generator.Call(ReflectionHelpers.IEnumerator_MoveNext);
			generator.BranchIfFalse(breakTarget);

			// lhs = enumerator.Current;
			this.Variable.GenerateSet(generator, optimizationInfo,false, typeof(object), delegate(bool two){
				
				generator.LoadVariable(enumerator);
				generator.Call(ReflectionHelpers.IEnumerator_Current);
				
				if(two){
					generator.Duplicate();
				}
				
			}, false);
			
			
			
			// Emit the body statement(s).
			optimizationInfo.PushBreakOrContinueInfo(labels, breakTarget, continueTarget, false);
			
			// Mark the body as root:
			Body.SetRoot(optimizationInfo);
			
			Body.GenerateCode(generator, optimizationInfo);
			optimizationInfo.PopBreakOrContinueInfo();

			generator.Branch(continueTarget);
			generator.DefineLabelPosition(breakTarget);
			
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
                yield return this.TargetObject;

				// Fake a string assignment to the target variable so it gets the correct type.
				var fakeAssignment = new AssignmentExpression(Operator.Assignment);
				fakeAssignment.operand0=(Expression)this.Variable;
				fakeAssignment.operand1=new LiteralExpression("");
				fakeAssignment.OperandCount=2;
				yield return fakeAssignment;

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
            result.AppendFormat("for ({0} of {1})",
                this.Variable.ToString(),
                this.TargetObject.ToString());
            result.AppendLine();
            result.Append(this.Body.ToString(indentLevel + 1));
            return result.ToString();
        }
    }

}