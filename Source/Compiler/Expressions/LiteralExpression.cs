using System;
using System.Collections.Generic;

namespace Nitrassic.Compiler
{

	/// <summary>
	/// Represents a literal expression.
	/// </summary>
	internal sealed class LiteralExpression : Expression
	{
		/// <summary>
		/// Creates a new instance of LiteralJSExpression.
		/// </summary>
		/// <param name="value"> The literal value. </param>
		public LiteralExpression(object value)
		{
			this.Value = value;
		}

		/// <summary>
		/// Gets the literal value.
		/// </summary>
		public object Value;

		/// <summary>
		/// Evaluates the expression, if possible.
		/// </summary>
		/// <returns> The result of evaluating the expression, or <c>null</c> if the expression can
		/// not be evaluated. </returns>
		public override object Evaluate()
		{
			// Array literal.
			if (this.Value is List<Expression>)
				return null;

			// Object literal.
			if (this.Value is Dictionary<string, object>)
				return null;

			// RegExp literal.
			if (this.Value is RegularExpressionLiteral)
				return null;

			// Everything else.
			return this.Value;
		}

		/// <summary>
		/// Gets the type that results from evaluating this expression.
		/// </summary>
		public override Type GetResultType(OptimizationInfo optimizationInfo)
		{
			// Array literal.
			if (this.Value is List<Expression>)
				return typeof(Library.Array);

			// Object literal.
			if (this.Value is Dictionary<string, object>){
				return CreatedPrototype.Type;
			}
			
			// RegExp literal.
			if (this.Value is RegularExpressionLiteral)
				return typeof(Library.RegExp);
			
			// Everything else.
			return Value.GetType();
		}
		
		private Library.Prototype CreatedPrototype;
		
		internal override void ResolveVariables(OptimizationInfo optimizationInfo){
			
			// Resolve kids too:
			base.ResolveVariables(optimizationInfo);
			
			// Is it an object literal?
			var properties =this.Value as List<KeyValuePair<Expression, Expression>>;
			
			if(properties==null){
				
				// Not an object literal! How about an array?
				
				// Get the expressions:
				List<Expression> exprs=this.Value as List<Expression>;
				
				if(exprs==null){
					// Not an array either! Quit there.
					return;
				}
				
				// For each one..
				foreach(Expression expr in exprs){
					
					// Resolve it:
					expr.ResolveVariables(optimizationInfo);
					
				}
				
				return;
			}
			
			if(CreatedPrototype!=null){
				// Already made it!
				return;
			}
			
			// We'll generate a prototype and a custom object type for it:
			CreatedPrototype=optimizationInfo.Engine.Prototypes.Create();
			
			// We'll generate a prototype and a custom object type for it:
			Library.Prototype proto=CreatedPrototype;
			
			foreach (var keyValuePair in properties)
			{
				
				string propertyName = keyValuePair.Key.ToString();
				Expression propertyValue = keyValuePair.Value;
				
				if (propertyValue is Expression)
				{
					// Add a new property to the object.
					
					var dataPropertyValue = (Expression)propertyValue;
					
					// Resolve it:
					dataPropertyValue.ResolveVariables(optimizationInfo);
					
					Type valueType=dataPropertyValue.GetResultType(optimizationInfo);
					
					// Create the property now:
					Library.PropertyVariable pv=proto.AddProperty(propertyName,Library.PropertyAttributes.FullAccess,valueType);
					
					// If it's a function value then it might be constant.
					// It might be a constant string/ number too, so track them as well.
					pv.TryLoadConstant(dataPropertyValue);
					
				}
				else{
					throw new InvalidOperationException("Invalid property value type in object literal.");
				}
				
			}
		}
		
		/// <summary>
		/// Generates CIL for the expression.
		/// </summary>
		/// <param name="generator"> The generator to output the CIL to. </param>
		/// <param name="optimizationInfo"> Information about any optimizations that should be performed. </param>
		public override void GenerateCode(ILGenerator generator, OptimizationInfo optimizationInfo)
		{
			// Literals cannot have side-effects so if a return value is not expected then generate
			// nothing.
			if (optimizationInfo.RootExpression==this) 
				return;
			
			if (this.Value is int)
				generator.LoadInt32((int)this.Value);
			else if (this.Value is double)
				generator.LoadDouble((double)this.Value);
			else if (this.Value is string)
				generator.LoadString((string)this.Value);
			else if (this.Value is bool)
				generator.LoadBoolean((bool)this.Value);
			else if (this.Value is RegularExpressionLiteral)
			{
				// RegExp
				var sharedRegExpVariable = optimizationInfo.GetRegExpVariable(generator, (RegularExpressionLiteral)this.Value);
				var label1 = generator.CreateLabel();
				var label2 = generator.CreateLabel();

				// if (sharedRegExp == null) {
				generator.LoadVariable(sharedRegExpVariable);
				generator.LoadNull();
				generator.BranchIfNotEqual(label1);

				// sharedRegExp = Global.RegExp.OnConstruct(source, flags)
				EmitHelpers.LoadPrototypes(generator);
				generator.LoadField(ReflectionHelpers.PrototypeLookup_RegExp);
				generator.LoadString(((RegularExpressionLiteral)this.Value).Pattern);
				generator.LoadString(((RegularExpressionLiteral)this.Value).Flags);
				generator.Call(ReflectionHelpers.RegExp_Construct);
				generator.Duplicate();
				generator.StoreVariable(sharedRegExpVariable);

				// } else {
				generator.Branch(label2);
				generator.DefineLabelPosition(label1);

				// Global.RegExp.OnConstruct(sharedRegExp, flags)
				EmitHelpers.LoadPrototypes(generator);
				generator.LoadField(ReflectionHelpers.PrototypeLookup_RegExp);
				generator.LoadVariable(sharedRegExpVariable);
				generator.LoadNull();
				generator.Call(ReflectionHelpers.RegExp_Construct);

				// }
				generator.DefineLabelPosition(label2);
			}
			else if (this.Value == Null.Value)
			{
				// Null.
				EmitHelpers.EmitNull(generator);
			}
			else if (this.Value == Undefined.Value)
			{
				// Undefined.
				EmitHelpers.EmitUndefined(generator);
			}
			else if (this.Value is List<Expression>)
			{
				// Construct an array literal.
				var arrayLiteral = (List<Expression>)this.Value;

				// Operands for ArrayConstructor.New() are: an ArrayConstructor instance (ArrayConstructor), an array (object[])
				// ArrayConstructor
				EmitHelpers.LoadEngine(generator);
				
				// object[]
				generator.LoadInt32(arrayLiteral.Count);
				generator.NewArray(typeof(object));
				
				for (int i = 0; i < arrayLiteral.Count; i ++)
				{
					// Operands for StoreArrayElement() are: an array (object[]), index (int), value (object).
					// Array
					generator.Duplicate();

					// Index
					generator.LoadInt32(i);

					// Value
					var elementExpression = arrayLiteral[i];
					if (elementExpression == null)
						generator.LoadNull();
					else
					{
						
						elementExpression.GenerateCode(generator, optimizationInfo);
						EmitConversion.ToAny(generator, elementExpression.GetResultType(optimizationInfo));
					}

					// Store the element value.
					generator.StoreArrayElement(typeof(object));
				}

				// ArrayConstructor.New(object[])
				generator.Call(ReflectionHelpers.Array_New);
				
			}
			else if (this.Value is List<KeyValuePair<Expression, Expression>>)
			{
				// This is an object literal.
				var properties = (List<KeyValuePair<Expression, Expression>>)this.Value;
				
				// We'll generate a prototype and a custom object type for it:
				Library.Prototype proto=CreatedPrototype;
				
				// Create a new object.
				generator.NewObject(proto.TypeConstructor);
				
				foreach (var keyValuePair in properties)
				{
					
					string propertyName = keyValuePair.Key.ToString();
					Expression propertyValue = keyValuePair.Value;
					
					// Duplicate the object ref:
					generator.Duplicate();
					
					// Add a new property to the object.
					
					Type valueType=propertyValue.GetResultType(optimizationInfo);
					
					// Get the property:
					Library.PropertyVariable pv=proto.GetProperty(propertyName);
					
					// Set it:
					pv.Set(generator,optimizationInfo,false,valueType,delegate(bool two){
						
						if(pv.IsConstant){
							
							// Emit the constant:
							EmitHelpers.EmitValue(generator,pv.ConstantValue);
							
						}else{
						
							// Write the value to set now:
							propertyValue.GenerateCode(generator, optimizationInfo);
							
						}
						
						// Note: This one always ignores 'two'
						
					});
					
				}
				
			}
			else
				throw new NotImplementedException("Unknown literal type.");
		}

		/// <summary>
		/// Converts the expression to a string.
		/// </summary>
		/// <returns> A string representing this expression. </returns>
		public override string ToString()
		{
			// Array literal.
			if (this.Value is List<Expression>)
			{
				var result = new System.Text.StringBuilder("[");
				foreach (var item in (List<Expression>)this.Value)
				{
					if (result.Length > 1)
						result.Append(", ");
					result.Append(item);
				}
				result.Append("]");
				return result.ToString();
			}

			// Object literal.
			if (this.Value is List<KeyValuePair<Expression, Expression>>)
			{
				var result = new System.Text.StringBuilder("{");
				foreach (var keyValuePair in (List<KeyValuePair<Expression, Expression>>)this.Value)
				{
					if (result.Length > 1){
						result.Append(", ");
						
						result.Append(keyValuePair.Key);
						result.Append(": ");
						result.Append(keyValuePair.Value);

					}
				}
				result.Append("}");
				return result.ToString();
			}

			// RegExp literal.
			if (this.Value is RegularExpressionLiteral)
				return this.Value.ToString();

			// String literal.
			if (this.Value is string)
				return Nitrassic.Library.String.Quote((string)this.Value);

			// Everything else.
			return TypeConverter.ToString(this.Value);
		}
	}


}