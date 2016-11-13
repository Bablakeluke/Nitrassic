using System;
using System.Collections.Generic;

namespace Nitrassic.Compiler
{

	/// <summary>
	/// Represents a return statement.
	/// </summary>
	internal class ReturnStatement : Statement
	{
		
		/// <summary>
		/// Gets or sets the expression to return.  Can be <c>null</c> to return "undefined".
		/// </summary>
		public Expression Value;
		/// <summary>
		/// The return type.
		/// </summary>
		public Type ReturnType;
		
		/// <summary>Collapsed var warning.</summary>
		private void Collapsed(){
			
			Wrench.Log.Add("Warning: Function return type has collapsed. This happens when you've got multiple return statements and they return different types.");
		
		}
		
		internal override void ResolveVariables(OptimizationInfo optimizationInfo)
		{
			
			if(optimizationInfo.IsConstructor){
				Wrench.Log.Add("You've got a return statement inside a function which is being used as a constructor. It's been ignored.");
				return;
			}
			
			if(Value==null){
				
				if(optimizationInfo.ReturnType!=null && optimizationInfo.ReturnType!=typeof(Nitrassic.Undefined)){
					
					// Collapsed.
					ReturnType=typeof(object);
					
					if(ScriptEngine.CollapseWarning){
						Collapsed();
					}
					
				}
				
				return;
			}
			
			// Resolve vars:
			Value.ResolveVariables(optimizationInfo);
			
			// Get return type:
			ReturnType=Value.GetResultType(optimizationInfo);
			
			if(optimizationInfo.ReturnType==null){
				
				optimizationInfo.ReturnType=ReturnType;
				
			}else if(!optimizationInfo.ReturnType.IsAssignableFrom(ReturnType)){
				
				// Return type has collapsed. Can only become 'object' now.
				ReturnType=typeof(object);
				
				if(ScriptEngine.CollapseWarning){
					Collapsed();
				}
				
				// Change the return type:
				optimizationInfo.ReturnType=typeof(object);
				
			}
			
		}
		
		/// <summary>
		/// Generates CIL for the statement.
		/// </summary>
		/// <param name="generator"> The generator to output the CIL to. </param>
		/// <param name="optimizationInfo"> Information about any optimizations that should be performed. </param>
		public override void GenerateCode(ILGenerator generator, OptimizationInfo optimizationInfo)
		{
			
			if(optimizationInfo.IsConstructor){
				return;
			}
			
			// Emit the return value.
			Type returnType=ReturnType;
			
			// Determine if this is the last statement in the function.
			BlockStatement hostBlock=optimizationInfo.AbstractSyntaxTree as BlockStatement;
			
			bool lastStatement;

			if(hostBlock!=null && hostBlock.Statements.Count > 0 &&
				hostBlock.Statements[hostBlock.Statements.Count - 1] == this){
					lastStatement=true;
			}else{
				lastStatement=false;
			}
			
			// Current return var:
			var returnVar=optimizationInfo.ReturnVariable;
			
			if (Value != null){
				
				// Emit the returned value:
				Value.GenerateCode(generator, optimizationInfo);
				
				// The first return statement initializes the variable that holds the return value.
				if (returnVar == null){
					
					if(!lastStatement){
						
						returnVar = generator.DeclareVariable(returnType, "returnValue");
						optimizationInfo.ReturnVariable=returnVar;
						
					}
					
				}
				
				
			}else if(returnVar!=null){
				
				// We need to return undefined here, which will require changing the return type to object.
				
				// No value being set:
				EmitHelpers.EmitUndefined(generator);
				
			}
			
			if(returnVar!=null){
				
				// the return type here must be a child type of the return type.
				// If it isn't then this function returns different things.
				
				// Store the return value in a variable.
				generator.StoreVariable(returnVar);
				
			}
			
			// There is no need to jump to the end of the function if this is the last statement.
			if (!lastStatement)
			{
				
				// The first return statement that needs to branch creates the return label.  This is
				// defined in FunctionmethodGenerator.GenerateCode at the end of the function.
				if (optimizationInfo.ReturnTarget == null)
					optimizationInfo.ReturnTarget = generator.CreateLabel();

				// Branch to the end of the function.  Note: the return statement might be branching
				// from inside a try { } or finally { } block to outside.  EmitLongJump() handles this.
				optimizationInfo.EmitLongJump(generator, optimizationInfo.ReturnTarget);

			}
			
		}

		/// <summary>
		/// Gets an enumerable list of child nodes in the abstract syntax tree.
		/// </summary>
		public override IEnumerable<AstNode> ChildNodes
		{
			get
			{
				if (this.Value != null)
					yield return this.Value;
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
			result.Append("return");
			if (this.Value != null)
			{
				result.Append(" ");
				result.Append(this.Value);
			}
			result.Append(";");
			return result.ToString();
		}
	}

}