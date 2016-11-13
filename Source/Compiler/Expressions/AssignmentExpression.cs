using System;
using System.Collections.Generic;

namespace Nitrassic.Compiler
{
	/// <summary>
	/// Represents an assignment expression (++, --, =, +=, -=, *=, /=, %=, &amp;=, |=, ^=, &lt;&lt;=, &gt;&gt;=, &gt;&gt;&gt;=).
	/// </summary>
	internal class AssignmentExpression : OperatorExpression
	{
		/// <summary>
		/// Creates a new instance of AssignmentExpression.
		/// </summary>
		/// <param name="operator"> The operator to base this expression on. </param>
		public AssignmentExpression(Operator @operator)
			: base(@operator)
		{
		}

		/// <summary>
		/// Creates a simple variable assignment expression.
		/// </summary>
		/// <param name="scope"> The scope the variable is defined within. </param>
		/// <param name="name"> The name of the variable to set. </param>
		/// <param name="value"> The value to set the variable to. </param>
		public AssignmentExpression(Scope scope, string name, Expression value)
			: base(Operator.Assignment)
		{
			operand0=new NameExpression(scope, name);
			operand1=value;
			OperandCount=2;
		}

		/// <summary>
		/// Gets the target of the assignment.
		/// </summary>
		public IReferenceExpression Target
		{
			get { return this.GetOperand(0) as IReferenceExpression; }
		}

		/// <summary>
		/// Gets the underlying base operator for the given compound operator.
		/// </summary>
		/// <param name="compoundOperatorType"> The type of compound operator. </param>
		/// <returns> The underlying base operator, or <c>null</c> if the type is not a compound
		/// operator. </returns>
		private static Operator GetCompoundBaseOperator(OperatorType compoundOperatorType)
		{
			switch (compoundOperatorType)
			{
				case OperatorType.CompoundAdd:
					return Operator.Add;
				case OperatorType.CompoundBitwiseAnd:
					return Operator.BitwiseAnd;
				case OperatorType.CompoundBitwiseOr:
					return Operator.BitwiseOr;
				case OperatorType.CompoundBitwiseXor:
					return Operator.BitwiseXor;
				case OperatorType.CompoundDivide:
					return Operator.Divide;
				case OperatorType.CompoundLeftShift:
					return Operator.LeftShift;
				case OperatorType.CompoundModulo:
					return Operator.Modulo;
				case OperatorType.CompoundMultiply:
					return Operator.Multiply;
				case OperatorType.CompoundSignedRightShift:
					return Operator.SignedRightShift;
				case OperatorType.CompoundSubtract:
					return Operator.Subtract;
				case OperatorType.CompoundUnsignedRightShift:
					return Operator.UnsignedRightShift;
			}
			return null;
		}
		
		internal override void ResolveVariables(OptimizationInfo optimizationInfo){
			
			// First, resolve kids:
			base.ResolveVariables(optimizationInfo);
			
			// Next, get the target:
			var target = this.GetOperand(0) as IReferenceExpression;
			
			if (target == null)
			{
				// Emit an error message.
				switch (this.OperatorType)
				{
					case OperatorType.PostIncrement:
					case OperatorType.PostDecrement:
						
						throw new JavaScriptException(
							optimizationInfo,
							"ReferenceError",
							"Invalid left-hand side in postfix operation"
						);
						
					case OperatorType.PreIncrement:
					case OperatorType.PreDecrement:
						
						throw new JavaScriptException(
							optimizationInfo,
							"ReferenceError",
							"Invalid left-hand side in prefix operation"
						);
						
					case OperatorType.Assignment:
					default:
						
						throw new JavaScriptException(
							optimizationInfo,
							"ReferenceError",
							"Invalid left-hand side in assignment"
						);
						
				}
				
			}
			
			// The left hand side cannot be "arguments" or "eval" in strict mode.
			if (optimizationInfo.StrictMode){
				
				NameExpression expr=target as NameExpression;
				
				if(expr!=null && (expr.Name == "eval" || expr.Name=="arguments")){
					
					throw new JavaScriptException(
						optimizationInfo.Engine,
						"SyntaxError",
						"The variable '"+expr.Name+"' cannot be modified in strict mode.",
						1,
						optimizationInfo.Source.Path,
						optimizationInfo.FunctionName
					);
					
				}
				
			}
			
			
			if(this.OperatorType == OperatorType.Assignment){
				
				// Potentially transferring a constant here.
				// Note that all of the others affect the source value in some way
				// so this is the only one which can potentially transfer a constant like this.
				target.TryApplyConstant(optimizationInfo,GetOperand(1));
			
			}else{
			
				// Ensure it's not constant:
				target.TryApplyConstant(optimizationInfo,false);
				
			}
			
			// Get the type being set:
			Type resultType=GetResultType(optimizationInfo);
			
			// Apply it to the target:
			target.ApplyType(optimizationInfo,resultType);
			
		}
		
		/// <summary>
		/// Gets the type that results from evaluating this expression.
		/// </summary>
		public override Type GetResultType(OptimizationInfo optimizationInfo)
		{
			var type = this.OperatorType;
			if (type == OperatorType.PostIncrement ||
				type == OperatorType.PostDecrement ||
				type == OperatorType.PreIncrement ||
				type == OperatorType.PreDecrement){
					
				// If the input var is an int, the out is an int too:
				Type input=GetOperand(0).GetResultType(optimizationInfo);
				
				if(input==typeof(int) || input==typeof(uint) || input==typeof(short) || input==typeof(ushort)){
					return input;
				}else if(input==typeof(double)){
					return typeof(double);
				}else{
					throw new Exception("Unable to increment/ decrement that (not a suitable type). "+input);
				}
				
			}
			
			if (type == OperatorType.Assignment){
				return this.GetOperand(1).GetResultType(optimizationInfo);
			}
			
			var compoundOperator = new BinaryExpression(GetCompoundBaseOperator(type), this.GetOperand(0), this.GetOperand(1));
			return compoundOperator.GetResultType(optimizationInfo);
		}

		/// <summary>
		/// Generates CIL for the expression.
		/// </summary>
		/// <param name="generator"> The generator to output the CIL to. </param>
		/// <param name="optimizationInfo"> Information about any optimizations that should be performed. </param>
		public override void GenerateCode(ILGenerator generator, OptimizationInfo optimizationInfo)
		{
			// The left hand side needs to be a variable reference or member access.
			var target = this.GetOperand(0) as IReferenceExpression;
			
			switch (this.OperatorType)
			{
				case OperatorType.Assignment:
					// Standard assignment operator.
					GenerateAssignment(generator, optimizationInfo, target);
					break;

				case OperatorType.PostIncrement:
					GenerateIncrementOrDecrement(generator, optimizationInfo, target, true, true);
					break;
				case OperatorType.PostDecrement:
					GenerateIncrementOrDecrement(generator, optimizationInfo, target, true, false);
					break;
				case OperatorType.PreIncrement:
					GenerateIncrementOrDecrement(generator, optimizationInfo, target, false, true);
					break;
				case OperatorType.PreDecrement:
					GenerateIncrementOrDecrement(generator, optimizationInfo, target, false, false);
					break;
				default:
					// All other compound operators.
					GenerateCompoundAssignment(generator, optimizationInfo, target);
					break;
			}
		}

		/// <summary>
		/// Generates CIL for an assignment expression.
		/// </summary>
		/// <param name="generator"> The generator to output the CIL to. </param>
		/// <param name="optimizationInfo"> Information about any optimizations that should be performed. </param>
		/// <param name="target"> The target to modify. </param>
		private void GenerateAssignment(ILGenerator generator, OptimizationInfo optimizationInfo, IReferenceExpression target)
		{
			// Load the value to assign.
			var rhs = GetOperand(1);
			
			// Potentially transferring a constant here.
			// Note that all of the others affect the source value in some way
			// so this is the only one which can potentially transfer a constant like this.
			object constValue=target.GetConstantValue();
			
			// Store the value.
			target.GenerateSet(generator, optimizationInfo,optimizationInfo.RootExpression!=this, rhs.GetResultType(optimizationInfo), delegate(bool two)
			{
				
				// Generate the code:
				
				if(constValue!=null){
					
					// Straight emit the constant value (Note that it could be a function - it handles that for us):
					EmitHelpers.EmitValue(generator,constValue);
					
				}else{
					
					rhs.GenerateCode(generator, optimizationInfo);
					
				}
				
				if(two){
					
					// Duplicate the value so it remains on the stack afterwards.
					generator.Duplicate();
					
				}
				
			}, optimizationInfo.StrictMode);
		}

		/// <summary>
		/// Generates CIL for an increment or decrement expression.
		/// </summary>
		/// <param name="generator"> The generator to output the CIL to. </param>
		/// <param name="optimizationInfo"> Information about any optimizations that should be performed. </param>
		/// <param name="target"> The target to modify. </param>
		/// <param name="postfix"> <c>true</c> if this is the postfix version of the operator;
		/// <c>false</c> otherwise. </param>
		/// <param name="increment"> <c>true</c> if this is the increment operator; <c>false</c> if
		/// this is the decrement operator. </param>
		private void GenerateIncrementOrDecrement(ILGenerator generator, OptimizationInfo optimizationInfo, IReferenceExpression target, bool postfix, bool increment)
		{
			// Note: increment and decrement can produce a number that is out of range if the
			// target is of type Int32.  The only time this should happen is for a loop variable
			// where the range has been carefully checked to make sure an out of range condition
			// cannot happen.
			
			// Store the value.
			target.GenerateSet(generator, optimizationInfo,optimizationInfo.RootExpression!=this, target.GetResultType(optimizationInfo) == typeof(int) ? typeof(int) : typeof(double), delegate(bool two)
			{
				
				// Get the target value.
				target.GenerateGet(generator, optimizationInfo, true);

				// Convert it to a number.
				if (target.GetResultType(optimizationInfo) != typeof(int))
					EmitConversion.ToNumber(generator, target.GetResultType(optimizationInfo));

				// If this is PostIncrement or PostDecrement, duplicate the value so it can be produced as the return value.
				if (postfix && two)
					generator.Duplicate();

				// Load the increment constant.
				if (target.GetResultType(optimizationInfo) == typeof(int))
					generator.LoadInt32(1);
				else
					generator.LoadDouble(1.0);

				// Add or subtract the constant to the target value.
				if (increment == true)
					generator.Add();
				else
					generator.Subtract();

				// If this is PreIncrement or PreDecrement, duplicate the value so it can be produced as the return value.
				if (!postfix && two)
					generator.Duplicate();
				
			}, optimizationInfo.StrictMode);
		}
		
		/// <summary>
		/// Generates CIL for a compound assignment expression.
		/// </summary>
		/// <param name="generator"> The generator to output the CIL to. </param>
		/// <param name="optimizationInfo"> Information about any optimizations that should be performed. </param>
		/// <param name="target"> The target to modify. </param>
		private void GenerateCompoundAssignment(ILGenerator generator, OptimizationInfo optimizationInfo, IReferenceExpression target)
		{
			
			// Load the value to assign.
			var compoundOperator = new BinaryExpression(GetCompoundBaseOperator(this.OperatorType), this.GetOperand(0), this.GetOperand(1));
			
			// Store the value.
			target.GenerateSet(generator, optimizationInfo,optimizationInfo.RootExpression!=this, compoundOperator.GetResultType(optimizationInfo), delegate(bool two)
			{
				
				// Generate the code:
				compoundOperator.GenerateCode(generator, optimizationInfo);
				
				if(two){
					// Duplicate the value so it remains on the stack afterwards.
					generator.Duplicate();
				}
				
			}, optimizationInfo.StrictMode);
		}
	}

}