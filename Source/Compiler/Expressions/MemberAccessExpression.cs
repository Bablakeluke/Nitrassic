using System;
using Nitrassic.Library;

namespace Nitrassic.Compiler
{
	
	/// <summary>
	/// Represents a variable or member access.
	/// </summary>
	internal sealed class MemberAccessExpression : OperatorExpression, IReferenceExpression
	{
		
		/// <summary>
		/// The property being accessed.
		/// </summary>
		internal PropertyVariable ResolvedProperty=null;
		private string propertyName = null;
		private bool isArrayIndex = false;
		
		/// <summary>
		/// Creates a new instance of MemberAccessExpression.
		/// </summary>
		/// <param name="operator"> The operator to base this expression on. </param>
		public MemberAccessExpression(Operator @operator)
			: base(@operator)
		{
		}

		/// <summary>
		/// Gets an expression that evaluates to the object that is being accessed or modified.
		/// </summary>
		public Expression Base
		{
			get { return this.GetOperand(0); }
		}

		/// <summary>
		/// Gets the type that results from evaluating this expression.
		/// </summary>
		public override Type GetResultType(OptimizationInfo optimizationInfo)
		{
			if(ResolvedProperty==null)
			{
				Resolve(optimizationInfo);
			}
			
			return ResolvedProperty.Type;
		}
		
		/// <summary>
		/// Figures out which property is being referenced.
		/// </summary>
		internal void Resolve(OptimizationInfo optimizationInfo)
		{
			
			// Right-hand-side can be a property name (a.b)
			if (this.OperatorType == OperatorType.MemberAccess)
			{
				var rhs = this.GetOperand(1) as NameExpression;
				if (rhs == null)
					throw new JavaScriptException(optimizationInfo.Engine, "SyntaxError", "Invalid member access", 1, optimizationInfo.Source.Path, optimizationInfo.FunctionName);
				propertyName = rhs.Name;
			}
			
			// Or a constant indexer (a['b'])
			if (this.OperatorType == OperatorType.Index)
			{
				var rhs = this.GetOperand(1) as LiteralExpression;
				if (rhs != null && (PrimitiveTypeUtilities.IsNumeric(rhs.GetResultType(optimizationInfo)) || rhs.GetResultType(optimizationInfo) == typeof(string)))
				{
					propertyName = TypeConverter.ToString(rhs.Value);

					// Or a array index (a[0])
					if (rhs.GetResultType(optimizationInfo) == typeof(int) || (propertyName != null && Nitrassic.Library.Array.ParseArrayIndex(propertyName) != uint.MaxValue))
						isArrayIndex = true;
				}
			}

			if (isArrayIndex == true)
			{
				// Array indexer
				// -------------
				// xxx = object[index]
				
			}
			else if (propertyName != null)
			{
				
				// Load the left-hand side and convert to an object instance.
				var lhs = this.GetOperand(0);
				
				// Get the prototype:
				Nitrassic.Library.Prototype proto=optimizationInfo.Engine.Prototypes.Get(lhs.GetResultType(optimizationInfo));
				
				// Get the property:
				ResolvedProperty=proto.GetProperty(propertyName);
				
			}
			
		}
		
		/// <summary>
		/// Generates CIL for the expression.
		/// </summary>
		/// <param name="generator"> The generator to output the CIL to. </param>
		/// <param name="optimizationInfo"> Information about any optimizations that should be performed. </param>
		public override void GenerateCode(ILGenerator generator, OptimizationInfo optimizationInfo)
		{
			// NOTE: this is a get reference because assignment expressions do not call this method.
			GenerateGet(generator, optimizationInfo, false);
		}

		/// <summary>
		/// Pushes the value of the reference onto the stack.
		/// </summary>
		/// <param name="generator"> The generator to output the CIL to. </param>
		/// <param name="optimizationInfo"> Information about any optimizations that should be performed. </param>
		/// <param name="throwIfUnresolvable"> <c>true</c> to throw a ReferenceError exception if
		/// the name is unresolvable; <c>false</c> to output <c>null</c> instead. </param>
		public void GenerateGet(ILGenerator generator, OptimizationInfo optimizationInfo, bool throwIfUnresolvable)
		{
			
			if(ResolvedProperty==null)
			{
				Resolve(optimizationInfo);
			}
			
			if (isArrayIndex == true)
			{
				// Array indexer
				// -------------
				// xxx = object[index]

				// Load the left-hand side and convert to an object instance.
				var lhs = this.GetOperand(0);
				lhs.GenerateCode(generator, optimizationInfo);
				EmitConversion.ToObject(generator, lhs.GetResultType(optimizationInfo), optimizationInfo);

				// Load the right-hand side and convert to a uint32.
				var rhs = this.GetOperand(1);
				rhs.GenerateCode(generator, optimizationInfo);
				EmitConversion.ToUInt32(generator, rhs.GetResultType(optimizationInfo));

				// Call the indexer.
				generator.Call(ReflectionHelpers.ObjectInstance_GetPropertyValue_Int);
			}
			else if (propertyName != null)
			{
				
				// Load the left-hand side and convert to an object instance.
				var lhs = this.GetOperand(0);
				
				if(ResolvedProperty==null || ResolvedProperty.HasAccessor)
				{
					lhs.GenerateCode(generator, optimizationInfo);
				}
				
				if(ResolvedProperty==null){
					// It's undefined:
					EmitHelpers.EmitUndefined(generator);
				}else{
					
					// Emit a get:
					ResolvedProperty.Get(generator);
					
				}
				
			}
			else
			{
				// Dynamic property access
				// -----------------------
				// xxx = object.Get(x)

				// Load the left-hand side and convert to an object instance.
				var lhs = this.GetOperand(0);
				lhs.GenerateCode(generator, optimizationInfo);
				EmitConversion.ToObject(generator, lhs.GetResultType(optimizationInfo), optimizationInfo);

				// Load the property name and convert to a string.
				var rhs = this.GetOperand(1);
				rhs.GenerateCode(generator, optimizationInfo);
				EmitConversion.ToString(generator, rhs.GetResultType(optimizationInfo));

				// Call Get(string)
				generator.Call(ReflectionHelpers.ObjectInstance_GetPropertyValue_String);
			}
		}

		/// <summary>
		/// Stores the value on the top of the stack in the reference.
		/// </summary>
		/// <param name="generator"> The generator to output the CIL to. </param>
		/// <param name="optimizationInfo"> Information about any optimizations that should be performed. </param>
		/// <param name="valueType"> The primitive type of the value that is on the top of the stack. </param>
		/// <param name="throwIfUnresolvable"> <c>true</c> to throw a ReferenceError exception if
		/// the name is unresolvable; <c>false</c> to create a new property instead. </param>
		public void GenerateSet(ILGenerator generator, OptimizationInfo optimizationInfo, Type valueType, SetValueMethod value, bool throwIfUnresolvable)
		{
			
			if(ResolvedProperty==null)
			{
				Resolve(optimizationInfo);
			}
			
			if (isArrayIndex == true)
			{
				// Array indexer
				// -------------
				// xxx = object[index]

				// Load the left-hand side and convert to an object instance.
				var lhs = this.GetOperand(0);
				lhs.GenerateCode(generator, optimizationInfo);
				EmitConversion.ToObject(generator, lhs.GetResultType(optimizationInfo), optimizationInfo);

				// Load the right-hand side and convert to a uint32.
				var rhs = this.GetOperand(1);
				rhs.GenerateCode(generator, optimizationInfo);
				EmitConversion.ToUInt32(generator, rhs.GetResultType(optimizationInfo));

				// Call the indexer.
				value();
				EmitConversion.ToAny(generator, valueType);
				generator.LoadBoolean(optimizationInfo.StrictMode);
				generator.Call(ReflectionHelpers.ObjectInstance_SetPropertyValue_Int);
			}
			else if (propertyName != null)
			{
				
				// Named property modification (e.g. x.property = y)
				// -------------------------------------------------
				
				// Load the left-hand side and convert to an object instance.
				var lhs = this.GetOperand(0);
				lhs.GenerateCode(generator, optimizationInfo);
				
				// Get the type:
				Type parentType=lhs.GetResultType(optimizationInfo);
				
				EmitConversion.ToObject(generator, parentType, optimizationInfo);
				// Target object is now on the stack.
				
				generator.LoadString(propertyName);
				value();
				generator.LoadBoolean(optimizationInfo.StrictMode);
				generator.Call(ReflectionHelpers.ObjectInstance_SetPropertyValue_String);
				
			}
			else
			{
				// Dynamic property access
				// -----------------------
				// xxx = object.Get(x)

				// Load the left-hand side and convert to an object instance.
				var lhs = this.GetOperand(0);
				lhs.GenerateCode(generator, optimizationInfo);
				EmitConversion.ToObject(generator, lhs.GetResultType(optimizationInfo), optimizationInfo);

				// Load the property name and convert to a string.
				var rhs = this.GetOperand(1);
				rhs.GenerateCode(generator, optimizationInfo);
				EmitConversion.ToString(generator, rhs.GetResultType(optimizationInfo));

				// Call the indexer.
				value();
				generator.LoadBoolean(optimizationInfo.StrictMode);
				generator.Call(ReflectionHelpers.ObjectInstance_SetPropertyValue_String);
			}
			
		}

		/// <summary>
		/// Deletes the reference and pushes <c>true</c> if the delete succeeded, or <c>false</c>
		/// if the delete failed.
		/// </summary>
		/// <param name="generator"> The generator to output the CIL to. </param>
		/// <param name="optimizationInfo"> Information about any optimizations that should be performed. </param>
		public void GenerateDelete(ILGenerator generator, OptimizationInfo optimizationInfo)
		{
			// Load the left-hand side and convert to an object instance.
			var lhs = this.GetOperand(0);
			lhs.GenerateCode(generator, optimizationInfo);
			EmitConversion.ToObject(generator, lhs.GetResultType(optimizationInfo), optimizationInfo);

			// Load the property name and convert to a string.
			var rhs = this.GetOperand(1);
			if (this.OperatorType == OperatorType.MemberAccess && rhs is NameExpression)
				generator.LoadString((rhs as NameExpression).Name);
			else
			{
				rhs.GenerateCode(generator, optimizationInfo);
				EmitConversion.ToString(generator, rhs.GetResultType(optimizationInfo));
			}

			// Call Delete()
			generator.LoadBoolean(optimizationInfo.StrictMode);
			generator.Call(ReflectionHelpers.ObjectInstance_Delete);

			// If the return value is not wanted then pop it from the stack.
			//if (optimizationInfo.SuppressReturnValue == true)
			//	generator.Pop();
		}

		/// <summary>
		/// Converts the expression to a string.
		/// </summary>
		/// <returns> A string representing this expression. </returns>
		public override string ToString()
		{
			if (this.OperatorType == OperatorType.MemberAccess)
				return string.Format("{0}.{1}", this.GetRawOperand(0), this.OperandCount >= 2 ? this.GetRawOperand(1).ToString() : "?");
			return base.ToString();
		}
	}
}