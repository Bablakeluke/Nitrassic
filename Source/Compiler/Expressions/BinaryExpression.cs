using System;
using System.Diagnostics;

namespace Nitrassic.Compiler
{
	/// <summary>
	/// Represents a binary operator expression.
	/// </summary>
	internal class BinaryExpression : OperatorExpression
	{
		/// <summary>
		/// Creates a new instance of BinaryExpression.
		/// </summary>
		/// <param name="operator"> The binary operator to base this expression on. </param>
		public BinaryExpression(Operator @operator)
			: base(@operator)
		{
		}

		/// <summary>
		/// Creates a new instance of BinaryJSExpression.
		/// </summary>
		/// <param name="operator"> The binary operator to base this expression on. </param>
		/// <param name="left"> The operand on the left side of the operator. </param>
		/// <param name="right"> The operand on the right side of the operator. </param>
		public BinaryExpression(Operator @operator, Expression left, Expression right)
			: base(@operator)
		{
			this.operand0=left;
			this.operand1=right;
			OperandCount=2;
		}

		/// <summary>
		/// Gets the expression on the left side of the operator.
		/// </summary>
		public Expression Left
		{
			get { return this.GetOperand(0); }
		}

		/// <summary>
		/// Gets the expression on the right side of the operator.
		/// </summary>
		public Expression Right
		{
			get { return this.GetOperand(1); }
		}

		/// <summary>
		/// Evaluates the expression, if possible.
		/// </summary>
		/// <returns> The result of evaluating the expression, or <c>null</c> if the expression can
		/// not be evaluated. </returns>
		public override object Evaluate()
		{
			// Evaluate the operands.
			var left = this.Left.Evaluate();
			if (left == null)
				return null;
			var right = this.Right.Evaluate();
			if (right == null)
				return null;

			// Apply the binary operator logic.
			switch (this.OperatorType)
			{
				case OperatorType.Add:
					{
						var leftPrimitive = TypeConverter.ToPrimitive(left, PrimitiveTypeHint.None);
						var rightPrimitive = TypeConverter.ToPrimitive(right, PrimitiveTypeHint.None);
						if (TypeUtilities.IsString(leftPrimitive) == true || TypeUtilities.IsString(rightPrimitive) == true)
							return TypeConverter.ToString(leftPrimitive) + TypeConverter.ToString(rightPrimitive);
						return TypeConverter.ToNumber(leftPrimitive) + TypeConverter.ToNumber(rightPrimitive);
					}

				// Arithmetic operations.
				case OperatorType.Subtract:
					return TypeConverter.ToNumber(left) - TypeConverter.ToNumber(right);
				case OperatorType.Multiply:
					return TypeConverter.ToNumber(left) * TypeConverter.ToNumber(right);
				case OperatorType.Divide:
					return TypeConverter.ToNumber(left) / TypeConverter.ToNumber(right);
				case OperatorType.Modulo:
					return TypeConverter.ToNumber(left) % TypeConverter.ToNumber(right);

				// Bitwise operations.
				case OperatorType.BitwiseAnd:
					return TypeConverter.ToInt32(left) & TypeConverter.ToInt32(right);
				case OperatorType.BitwiseOr:
					return TypeConverter.ToInt32(left) | TypeConverter.ToInt32(right);
				case OperatorType.BitwiseXor:
					return TypeConverter.ToInt32(left) ^ TypeConverter.ToInt32(right);
				case OperatorType.LeftShift:
					return TypeConverter.ToInt32(left) << (int)(TypeConverter.ToUint32(right) & 0x1F);
				case OperatorType.SignedRightShift:
					return TypeConverter.ToInt32(left) >> (int)(TypeConverter.ToUint32(right) & 0x1F);
				case OperatorType.UnsignedRightShift:
					return (uint)TypeConverter.ToInt32(left) >> (int)(TypeConverter.ToUint32(right) & 0x1F);

				// Relational operations.
				case OperatorType.LessThan:
				case OperatorType.LessThanOrEqual:
				case OperatorType.GreaterThan:
				case OperatorType.GreaterThanOrEqual:
					return typeof(bool);

				// Equality operations.
				case OperatorType.Equal:
					return TypeComparer.Equals(left, right) == true;
				case OperatorType.StrictlyEqual:
					return TypeComparer.StrictEquals(left, right) == true;
				case OperatorType.NotEqual:
					return TypeComparer.Equals(left, right) == false;
				case OperatorType.StrictlyNotEqual:
					return TypeComparer.StrictEquals(left, right) == false;

				// Logical operations.
				case OperatorType.LogicalAnd:
					if (TypeConverter.ToBoolean(left) == false)
						return left;
					return right;

				case OperatorType.LogicalOr:
					if (TypeConverter.ToBoolean(left) == true)
						return left;
					return right;

				// Misc
				case OperatorType.InstanceOf:
				case OperatorType.In:
					return null;

				default:
					throw new NotImplementedException(string.Format("Unsupported operator {0}", this.OperatorType));
			}
		}

		/// <summary>
		/// Gets the type that results from evaluating this expression.
		/// </summary>
		public override Type GetResultType(OptimizationInfo optimizationInfo)
		{
			var type = this.OperatorType;
			switch (this.OperatorType)
			{
				// Add
				case OperatorType.Add:
					{
						var lhs = this.Left.GetResultType(optimizationInfo);
						var rhs = this.Right.GetResultType(optimizationInfo);
						if (lhs == typeof(string) || rhs == typeof(string))
							return typeof(ConcatenatedString);
						if (lhs == typeof(ConcatenatedString) || rhs == typeof(ConcatenatedString))
							return typeof(ConcatenatedString);
						if (lhs == typeof(object) || lhs == typeof(Library.ObjectInstance) ||
							rhs == typeof(object) || rhs == typeof(Library.ObjectInstance))
							return typeof(object);
						return typeof(double);
					}

				// Arithmetic operations.
				case OperatorType.Subtract:
				case OperatorType.Multiply:
				case OperatorType.Divide:
				case OperatorType.Modulo:
					return typeof(double);

				// Bitwise operations.
				case OperatorType.BitwiseAnd:
				case OperatorType.BitwiseOr:
				case OperatorType.BitwiseXor:
				case OperatorType.LeftShift:
				case OperatorType.SignedRightShift:
					return typeof(int);
				case OperatorType.UnsignedRightShift:
					return typeof(double);

				// Relational operations.
				case OperatorType.LessThan:
				case OperatorType.LessThanOrEqual:
				case OperatorType.GreaterThan:
				case OperatorType.GreaterThanOrEqual:
					return typeof(bool);

				// Equality operations.
				case OperatorType.Equal:
				case OperatorType.StrictlyEqual:
				case OperatorType.NotEqual:
				case OperatorType.StrictlyNotEqual:
					return typeof(bool);
				
				// Logical operations.
				case OperatorType.LogicalAnd:
				case OperatorType.LogicalOr:
					{
						// The result is either the left-hand side or the right-hand side.
						var lhs = this.Left.GetResultType(optimizationInfo);
						var rhs = this.Right.GetResultType(optimizationInfo);
						if (lhs == rhs)
							return lhs;
						if (PrimitiveTypeUtilities.IsNumeric(lhs) == true && PrimitiveTypeUtilities.IsNumeric(rhs) == true)
							return typeof(double);
						return typeof(object);
					}

				// Misc
				case OperatorType.InstanceOf:
				case OperatorType.In:
					return typeof(bool);
			}
			throw new NotImplementedException();
		}

		/// <summary>
		/// Generates CIL for the expression.
		/// </summary>
		/// <param name="generator"> The generator to output the CIL to. </param>
		/// <param name="optimizationInfo"> Information about any optimizations that should be performed. </param>
		public override void GenerateCode(ILGenerator generator, OptimizationInfo optimizationInfo)
		{
			// If a return value is not expected, generate only the side-effects.
			/*if (optimizationInfo.SuppressReturnValue == true)
			{
				this.GenerateSideEffects(generator, optimizationInfo);
				return;
			}*/

			// Special case the addition operator.
			if (this.OperatorType == OperatorType.Add)
			{
				GenerateAdd(generator, optimizationInfo);
				return;
			}

			// Special case the instanceof operator.
			if (this.OperatorType == OperatorType.InstanceOf)
			{
				GenerateInstanceOf(generator, optimizationInfo);
				return;
			}

			// Special case the in operator.
			if (this.OperatorType == OperatorType.In)
			{
				GenerateIn(generator, optimizationInfo);
				return;
			}

			// Special case the relational operators.
			if (this.OperatorType == OperatorType.LessThan ||
				this.OperatorType == OperatorType.LessThanOrEqual ||
				this.OperatorType == OperatorType.GreaterThan ||
				this.OperatorType == OperatorType.GreaterThanOrEqual)
			{
				GenerateRelational(generator, optimizationInfo);
				return;
			}

			// Special case the logical operators.
			if (this.OperatorType == OperatorType.LogicalAnd ||
				this.OperatorType == OperatorType.LogicalOr)
			{
				GenerateLogical(generator, optimizationInfo);
				return;
			}

			// Load the left hand side onto the stack.
			this.Left.GenerateCode(generator, optimizationInfo);

			// Convert the left argument.
			switch (this.OperatorType)
			{
				// Arithmetic operations.
				case OperatorType.Subtract:
				case OperatorType.Multiply:
				case OperatorType.Divide:
				case OperatorType.Modulo:
					EmitConversion.ToNumber(generator, this.Left.GetResultType(optimizationInfo));
					break;

				// Bitwise operations.
				case OperatorType.BitwiseAnd:
				case OperatorType.BitwiseOr:
				case OperatorType.BitwiseXor:
				case OperatorType.LeftShift:
				case OperatorType.SignedRightShift:
				case OperatorType.UnsignedRightShift:
					EmitConversion.ToInt32(generator, this.Left.GetResultType(optimizationInfo));
					break;

				// Equality operations.
				case OperatorType.Equal:
				case OperatorType.StrictlyEqual:
				case OperatorType.NotEqual:
				case OperatorType.StrictlyNotEqual:
					EmitConversion.ToAny(generator, this.Left.GetResultType(optimizationInfo));
					break;
			}

			// Load the right hand side onto the stack.
			this.Right.GenerateCode(generator, optimizationInfo);

			// Convert the right argument.
			switch (this.OperatorType)
			{
				// Arithmetic operations.
				case OperatorType.Subtract:
				case OperatorType.Multiply:
				case OperatorType.Divide:
				case OperatorType.Modulo:
					EmitConversion.ToNumber(generator, this.Right.GetResultType(optimizationInfo));
					break;

				// Bitwise operations.
				case OperatorType.BitwiseAnd:
				case OperatorType.BitwiseOr:
				case OperatorType.BitwiseXor:
					EmitConversion.ToInt32(generator, this.Right.GetResultType(optimizationInfo));
					break;
				case OperatorType.LeftShift:
				case OperatorType.SignedRightShift:
				case OperatorType.UnsignedRightShift:
					EmitConversion.ToUInt32(generator, this.Right.GetResultType(optimizationInfo));
					generator.LoadInt32(0x1F);
					generator.BitwiseAnd();
					break;

				// Equality operations.
				case OperatorType.Equal:
				case OperatorType.StrictlyEqual:
				case OperatorType.NotEqual:
				case OperatorType.StrictlyNotEqual:
					EmitConversion.ToAny(generator, this.Right.GetResultType(optimizationInfo));
					break;
			}

			// Apply the operator.
			switch (this.OperatorType)
			{
				// Arithmetic operations.
				case OperatorType.Subtract:
					generator.Subtract();
					break;
				case OperatorType.Multiply:
					generator.Multiply();
					break;
				case OperatorType.Divide:
					generator.Divide();
					break;
				case OperatorType.Modulo:
					generator.Remainder();
					break;

				// Bitwise operations.
				case OperatorType.BitwiseAnd:
					generator.BitwiseAnd();
					break;
				case OperatorType.BitwiseOr:
					generator.BitwiseOr();
					break;
				case OperatorType.BitwiseXor:
					generator.BitwiseXor();
					break;

				// Shift operations.
				case OperatorType.LeftShift:
					generator.ShiftLeft();
					break;
				case OperatorType.SignedRightShift:
					generator.ShiftRight();
					break;
				case OperatorType.UnsignedRightShift:
					generator.ShiftRightUnsigned();
					EmitConversion.ToNumber(generator, typeof(uint));
					break;

				// Equality operations.
				case OperatorType.Equal:
					generator.Call(ReflectionHelpers.TypeComparer_Equals);
					break;
				case OperatorType.StrictlyEqual:
					generator.Call(ReflectionHelpers.TypeComparer_StrictEquals);
					break;
				case OperatorType.NotEqual:
					generator.Call(ReflectionHelpers.TypeComparer_Equals);
					generator.LoadBoolean(false);
					generator.CompareEqual();
					break;
				case OperatorType.StrictlyNotEqual:
					generator.Call(ReflectionHelpers.TypeComparer_StrictEquals);
					generator.LoadBoolean(false);
					generator.CompareEqual();
					break;

				default:
					throw new NotImplementedException(string.Format("Unsupported operator {0}", this.OperatorType));
			}
		}

		/// <summary>
		/// Generates CIL for the addition operation.
		/// </summary>
		/// <param name="generator"> The generator to output the CIL to. </param>
		/// <param name="optimizationInfo"> Information about any optimizations that should be performed. </param>
		private void GenerateAdd(ILGenerator generator, OptimizationInfo optimizationInfo)
		{
			// Get the statically-determined types of the left and right operands.
			Type leftType = this.Left.GetResultType(optimizationInfo);
			Type rightType = this.Right.GetResultType(optimizationInfo);

			// The add operator adds two strings together if at least one of the operands
			// is a string, otherwise it adds two numbers.
			if (PrimitiveTypeUtilities.IsString(leftType) || PrimitiveTypeUtilities.IsString(rightType))
			{
				// If at least one of the operands is a string, then the add operator concatenates.

				// Load the left-hand side onto the stack.
				this.Left.GenerateCode(generator, optimizationInfo);

				// Convert the operand to a concatenated string.
				EmitConversion.ToPrimitive(generator, leftType, PrimitiveTypeHint.None);
				EmitConversion.ToConcatenatedString(generator, leftType);

				// Load the right-hand side onto the stack.
				this.Right.GenerateCode(generator, optimizationInfo);

				if (rightType == typeof(string))
				{
					// Concatenate the two strings.
					generator.Call(ReflectionHelpers.ConcatenatedString_Concatenate_String);
				}
				else if (rightType == typeof(ConcatenatedString))
				{
					// Concatenate the two strings.
					generator.Call(ReflectionHelpers.ConcatenatedString_Concatenate_ConcatenatedString);
				}
				else
				{
					// Convert the operand to an object.
					EmitConversion.ToPrimitive(generator, rightType, PrimitiveTypeHint.None);
					EmitConversion.ToAny(generator, rightType);

					// Concatenate the two strings.
					generator.Call(ReflectionHelpers.ConcatenatedString_Concatenate_Object);
				}
			}
			else if (leftType != typeof(object) && leftType != typeof(Library.ObjectInstance) &&
				rightType != typeof(object) && rightType != typeof(Library.ObjectInstance))
			{
				// Neither of the operands are strings.

				// Load the left hand side onto the stack.
				this.Left.GenerateCode(generator, optimizationInfo);

				// Convert the operand to a number.
				EmitConversion.ToNumber(generator, leftType);

				// Load the right hand side onto the stack.
				this.Right.GenerateCode(generator, optimizationInfo);

				// Convert the operand to a number.
				EmitConversion.ToNumber(generator, rightType);

				// Add the two numbers.
				generator.Add();
			}
			else
			{
				// It is unknown whether the operands are strings.

				// Load the left hand side onto the stack.
				this.Left.GenerateCode(generator, optimizationInfo);
				EmitConversion.ToAny(generator, leftType);

				// Load the right hand side onto the stack.
				this.Right.GenerateCode(generator, optimizationInfo);
				EmitConversion.ToAny(generator, rightType);

				// Add the two objects.
				generator.Call(ReflectionHelpers.TypeUtilities_Add);
			}
		}

		/// <summary>
		/// Generates CIL for the relational operators.
		/// </summary>
		/// <param name="generator"> The generator to output the CIL to. </param>
		/// <param name="optimizationInfo"> Information about any optimizations that should be performed. </param>
		private void GenerateRelational(ILGenerator generator, OptimizationInfo optimizationInfo)
		{
			// Get the statically-determined types of the left and right operands.
			Type leftType = this.Left.GetResultType(optimizationInfo);
			Type rightType = this.Right.GetResultType(optimizationInfo);

			// The relational operators compare strings if both of the operands are strings.
			if (leftType == typeof(string) && rightType == typeof(string))
			{
				// Both of the operands are strings.

				// Load the left hand side operand onto the stack.
				this.Left.GenerateCode(generator, optimizationInfo);

				// Load the right hand side operand onto the stack.
				this.Right.GenerateCode(generator, optimizationInfo);

				// Compare the two strings.
				generator.Call(ReflectionHelpers.String_CompareOrdinal);
				switch (this.OperatorType)
				{
					case OperatorType.LessThan:
						generator.LoadInt32(0);
						generator.CompareLessThan();
						break;
					case OperatorType.LessThanOrEqual:
						generator.LoadInt32(1);
						generator.CompareLessThan();
						break;
					case OperatorType.GreaterThan:
						generator.LoadInt32(0);
						generator.CompareGreaterThan();
						break;
					case OperatorType.GreaterThanOrEqual:
						generator.LoadInt32(-1);
						generator.CompareGreaterThan();
						break;
				}
			}
			else if (leftType == typeof(int) && rightType == typeof(int))
			{
				// Both of the operands are integers.

				// Load the left hand side operand onto the stack.
				this.Left.GenerateCode(generator, optimizationInfo);

				// Load the right hand side operand onto the stack.
				this.Right.GenerateCode(generator, optimizationInfo);

				// Compare the two numbers.
				switch (this.OperatorType)
				{
					case OperatorType.LessThan:
						generator.CompareLessThan();
						break;
					case OperatorType.GreaterThan:
						generator.CompareGreaterThan();
						break;

					case OperatorType.LessThanOrEqual:
						// a <= b   <-->   (a > b) == false
						generator.CompareGreaterThan();
						generator.LoadBoolean(false);
						generator.CompareEqual();
						break;

					case OperatorType.GreaterThanOrEqual:
						// a >= b   <-->   (a < b) == false
						generator.CompareLessThan();
						generator.LoadBoolean(false);
						generator.CompareEqual();
						break;
				}
			}
			else if (PrimitiveTypeUtilities.IsNumeric(leftType) || PrimitiveTypeUtilities.IsNumeric(rightType))
			{
				// At least one of the operands is a number.

				// Load the left hand side operand onto the stack.
				this.Left.GenerateCode(generator, optimizationInfo);

				// Convert the operand to a number.
				EmitConversion.ToNumber(generator, leftType);

				// Load the right hand side operand onto the stack.
				this.Right.GenerateCode(generator, optimizationInfo);

				// Convert the operand to a number.
				EmitConversion.ToNumber(generator, rightType);

				// Compare the two numbers.
				switch (this.OperatorType)
				{
					case OperatorType.LessThan:
						generator.CompareLessThan();
						break;
					case OperatorType.GreaterThan:
						generator.CompareGreaterThan();
						break;

					case OperatorType.LessThanOrEqual:
						// a <= b   <-->   (a > b) == false
						generator.CompareGreaterThanUnsigned();
						generator.LoadBoolean(false);
						generator.CompareEqual();
						break;

					case OperatorType.GreaterThanOrEqual:
						// a >= b   <-->   (a < b) == false
						generator.CompareLessThanUnsigned();
						generator.LoadBoolean(false);
						generator.CompareEqual();
						break;
				}
			}
			else
			{
				// It is unknown whether one of the operands is a string.

				// Load the left hand side operand onto the stack.
				this.Left.GenerateCode(generator, optimizationInfo);
				EmitConversion.ToAny(generator, leftType);

				// Load the right hand side operand onto the stack.
				this.Right.GenerateCode(generator, optimizationInfo);
				EmitConversion.ToAny(generator, rightType);

				switch (this.OperatorType)
				{
					case OperatorType.LessThan:
						generator.Call(ReflectionHelpers.TypeComparer_LessThan);
						break;

					case OperatorType.LessThanOrEqual:
						generator.Call(ReflectionHelpers.TypeComparer_LessThanOrEqual);
						break;

					case OperatorType.GreaterThan:
						generator.Call(ReflectionHelpers.TypeComparer_GreaterThan);
						break;

					case OperatorType.GreaterThanOrEqual:
						generator.Call(ReflectionHelpers.TypeComparer_GreaterThanOrEqual);
						break;
				}
			}
		}

		/// <summary>
		/// Generates CIL for the logical operators.
		/// </summary>
		/// <param name="generator"> The generator to output the CIL to. </param>
		/// <param name="optimizationInfo"> Information about any optimizations that should be performed. </param>
		private void GenerateLogical(ILGenerator generator, OptimizationInfo optimizationInfo)
		{
			// Get the statically-determined types of the left and right operands.
			Type leftType = this.Left.GetResultType(optimizationInfo);
			Type rightType = this.Right.GetResultType(optimizationInfo);

			// Load the left-hand side operand.
			this.Left.GenerateCode(generator, optimizationInfo);

			// Make sure the output type is consistant.
			if (leftType != rightType)
			{
				if (PrimitiveTypeUtilities.IsNumeric(leftType) == true && PrimitiveTypeUtilities.IsNumeric(rightType) == true)
				{
					EmitConversion.ToNumber(generator, leftType);
					leftType = typeof(double);
				}
				else
				{
					EmitConversion.ToAny(generator, leftType);
					leftType = typeof(object);
				}
			}

			// Duplicate and convert to a Boolean.
			generator.Duplicate();
			EmitConversion.ToBool(generator, leftType);

			// Stack contains "left, (bool)left"
			var endOfIf = generator.CreateLabel();
			if (this.OperatorType == OperatorType.LogicalAnd)
				generator.BranchIfFalse(endOfIf);
			else
				generator.BranchIfTrue(endOfIf);

			// Stack contains "left".  Load the right-hand side operand.
			generator.Pop();
			this.Right.GenerateCode(generator, optimizationInfo);

			// Make sure the output type is consistant.
			if (leftType != rightType)
			{
				if (PrimitiveTypeUtilities.IsNumeric(leftType) == true && PrimitiveTypeUtilities.IsNumeric(rightType) == true)
					EmitConversion.ToNumber(generator, rightType);
				else
					EmitConversion.ToAny(generator, rightType);
			}

			// Define the label used above.
			generator.DefineLabelPosition(endOfIf);
		}

		/// <summary>
		/// Generates CIL for the instanceof operator.
		/// </summary>
		/// <param name="generator"> The generator to output the CIL to. </param>
		/// <param name="optimizationInfo"> Information about any optimizations that should be performed. </param>
		private void GenerateInstanceOf(ILGenerator generator, OptimizationInfo optimizationInfo)
		{
			// Emit the left-hand side expression and convert it to an object.
			this.Left.GenerateCode(generator, optimizationInfo);
			EmitConversion.ToAny(generator, this.Left.GetResultType(optimizationInfo));

			// Store the left-hand side expression in a temporary variable.
			var temp = generator.CreateTemporaryVariable(typeof(object));
			generator.StoreVariable(temp);

			// Emit the right-hand side expression.
			this.Right.GenerateCode(generator, optimizationInfo);
			EmitConversion.ToAny(generator, this.Right.GetResultType(optimizationInfo));

			// Check the right-hand side is a function - if not, throw an exception.
			generator.IsInstance(typeof(Library.FunctionInstance));
			generator.Duplicate();
			var endOfTypeCheck = generator.CreateLabel();
			generator.BranchIfNotNull(endOfTypeCheck);

			// Throw an nicely formatted exception.
			var rightValue = generator.CreateTemporaryVariable(typeof(object));
			generator.StoreVariable(rightValue);
			EmitHelpers.LoadEngine(generator);
			generator.LoadString("TypeError");
			generator.LoadString("The instanceof operator expected a function, but found '{0}' instead");
			generator.LoadInt32(1);
			generator.NewArray(typeof(object));
			generator.Duplicate();
			generator.LoadInt32(0);
			generator.LoadVariable(rightValue);
			generator.Call(ReflectionHelpers.TypeUtilities_TypeOf);
			generator.StoreArrayElement(typeof(object));
			generator.Call(ReflectionHelpers.String_Format);
			generator.LoadInt32(1);
			generator.LoadStringOrNull(optimizationInfo.Source.Path);
			generator.LoadStringOrNull(optimizationInfo.FunctionName);
			generator.NewObject(ReflectionHelpers.JavaScriptException_Constructor_Error);
			generator.Throw();
			generator.DefineLabelPosition(endOfTypeCheck);
			generator.ReleaseTemporaryVariable(rightValue);

			// Load the left-hand side expression from the temporary variable.
			generator.LoadVariable(temp);

			// Call FunctionInstance.HasInstance(object)
			generator.Call(ReflectionHelpers.FunctionInstance_HasInstance);

			// Allow the temporary variable to be reused.
			generator.ReleaseTemporaryVariable(temp);
		}

		/// <summary>
		/// Generates CIL for the in operator.
		/// </summary>
		/// <param name="generator"> The generator to output the CIL to. </param>
		/// <param name="optimizationInfo"> Information about any optimizations that should be performed. </param>
		private void GenerateIn(ILGenerator generator, OptimizationInfo optimizationInfo)
		{
			// Emit the left-hand side expression and convert it to a string.
			this.Left.GenerateCode(generator, optimizationInfo);
			EmitConversion.ToString(generator, this.Left.GetResultType(optimizationInfo));

			// Store the left-hand side expression in a temporary variable.
			var temp = generator.CreateTemporaryVariable(typeof(string));
			generator.StoreVariable(temp);

			// Emit the right-hand side expression.
			this.Right.GenerateCode(generator, optimizationInfo);
			EmitConversion.ToAny(generator, this.Right.GetResultType(optimizationInfo));

			// Check the right-hand side is a javascript object - if not, throw an exception.
			generator.IsInstance(typeof(Library.ObjectInstance));
			generator.Duplicate();
			var endOfTypeCheck = generator.CreateLabel();
			generator.BranchIfNotNull(endOfTypeCheck);

			// Throw an nicely formatted exception.
			var rightValue = generator.CreateTemporaryVariable(typeof(object));
			generator.StoreVariable(rightValue);
			EmitHelpers.LoadEngine(generator);
			generator.LoadString("TypeError");
			generator.LoadString("The in operator expected an object, but found '{0}' instead");
			generator.LoadInt32(1);
			generator.NewArray(typeof(object));
			generator.Duplicate();
			generator.LoadInt32(0);
			generator.LoadVariable(rightValue);
			generator.Call(ReflectionHelpers.TypeUtilities_TypeOf);
			generator.StoreArrayElement(typeof(object));
			generator.Call(ReflectionHelpers.String_Format);
			generator.LoadInt32(1);
			generator.LoadStringOrNull(optimizationInfo.Source.Path);
			generator.LoadStringOrNull(optimizationInfo.FunctionName);
			generator.NewObject(ReflectionHelpers.JavaScriptException_Constructor_Error);
			generator.Throw();
			generator.DefineLabelPosition(endOfTypeCheck);
			generator.ReleaseTemporaryVariable(rightValue);

			// Load the left-hand side expression from the temporary variable.
			generator.LoadVariable(temp);

			// Call ObjectInstance.HasProperty(object)
			generator.Call(ReflectionHelpers.ObjectInstance_HasProperty);

			// Allow the temporary variable to be reused.
			generator.ReleaseTemporaryVariable(temp);
		}
	}
}