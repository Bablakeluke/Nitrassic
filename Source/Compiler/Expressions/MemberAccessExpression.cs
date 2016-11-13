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
		private PropertyVariable ResolvedProperty=null;
		private string propertyName = null;
		private bool isArrayIndex = false;
		
		/// <summary>
		/// The property being accessed.
		/// </summary>
		public PropertyVariable GetProperty(OptimizationInfo optimizationInfo){
			
			return ResolvedProperty;
			
		}
		
		/// <summary>
		/// Creates a new instance of MemberAccessExpression.
		/// </summary>
		/// <param name="operator"> The operator to base this expression on. </param>
		public MemberAccessExpression(Operator @operator)
			: base(@operator)
		{
		}
		
		public string Name{
			get{
				return propertyName;
			}
		}
		
		/// <summary>
		/// Gets an expression that evaluates to the object that is being accessed or modified.
		/// </summary>
		public Expression Base
		{
			get { return this.GetOperand(0); }
		}
		
		public override object Evaluate(){
			
			if(ResolvedProperty==null || ResolvedProperty.Type==null){
				
				// Undefined.
				return Undefined.Value;
			
			}
			
			// Is it constant?
			if(ResolvedProperty.IsConstant){
				
				if(isArrayIndex){
					// Array index; not actually constant (even though the ref itself is).
					return null;
				}
				
				return ResolvedProperty.ConstantValue;
				
			}
			
			return null;
		}
		
		/// <summary>
		/// Gets the type that results from evaluating this expression.
		/// </summary>
		public override Type GetResultType(OptimizationInfo optimizationInfo)
		{
			
			if(ResolvedProperty==null){
				// Runtime only property.
				return typeof(object);
			}
			
			Type result=ResolvedProperty.Type;
			
			if(result==null){
				// Unknown.
				return typeof(Nitrassic.Undefined);
			}
			
			return result;
			
		}
		
		internal override void ResolveVariables(OptimizationInfo optimizationInfo){
			
			if(ResolvedProperty!=null || propertyName!=null){
				// Already resolved.
				return;
			}
			
			// Resolve kids:
			base.ResolveVariables(optimizationInfo);
			
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
				var rhs = this.GetOperand(1);
				if (rhs != null)
				{
					
					Type rhsType=rhs.GetResultType(optimizationInfo);
					
					if(rhsType==typeof(string)){
						
						// Try a literal:
						LiteralExpression literalStr=rhs as LiteralExpression;
						
						if(literalStr!=null){
							propertyName = TypeConverter.ToString(literalStr.Value);
							
							// Could actually be numeric, so try that:
							if(propertyName != null && Nitrassic.Library.Array.ParseArrayIndex(propertyName) != uint.MaxValue){
								
								// Yep, it is!
								isArrayIndex=true;
								
							}
							
						}
						
					}else if(PrimitiveTypeUtilities.IsNumeric(rhsType)){
						
						// array index (a[0])
						isArrayIndex = true;
						
					}
					
				}
			}
			
			if (isArrayIndex == true)
			{
				// Array indexer
				// -------------
				// xxx = object[index]
				
				// Load the left-hand side and convert to an object instance.
				var lhs = this.GetOperand(0);
				
				// Get the type of the LHS (the object being read from):
				Type lhsType=lhs.GetResultType(optimizationInfo);
				
				// Get the proto for it:
				Nitrassic.Library.Prototype proto=optimizationInfo.Engine.Prototypes.Get(lhsType);
				
				// Does that type have an indexer method on it? (this[uint])
				ResolvedProperty=proto.Indexer(typeof(uint));
				
				if(ResolvedProperty==null){
					
					// Try [int] instead:
					ResolvedProperty=proto.Indexer(typeof(int));
					
				}
				
			}
			
			if (ResolvedProperty==null && propertyName != null)
			{
				
				// Load the left-hand side and convert to an object instance.
				var lhs = this.GetOperand(0);
				
				Type lhsType=lhs.GetResultType(optimizationInfo);
				
				// Get the prototype:
				Nitrassic.Library.Prototype proto=optimizationInfo.Engine.Prototypes.Get(lhsType);
				
				// Get the property:
				ResolvedProperty=proto.GetProperty(propertyName);
				
				if(ResolvedProperty==null){
					
					// Add it now (as undefined):
					ResolvedProperty=proto.AddProperty(propertyName,null,Nitrassic.Library.PropertyAttributes.FullAccess);
				
				}
				
			}
			
		}
		
		public object GetConstantValue(){
			
			// Try loading a constant now:
			Variable v=ResolvedProperty;
			
			if(v!=null && v.IsConstant){
				return v.ConstantValue;
			}
			
			return null;
			
		}
		
		public object TryApplyConstant(OptimizationInfo optimizationInfo,Expression expr){
			
			// Try loading a constant now:
			Variable v=ResolvedProperty;
			
			if(v!=null && v.TryLoadConstant(expr)){
				return v.ConstantValue;
			}
			
			return null;
			
		}
		
		public void TryApplyConstant(OptimizationInfo optimizationInfo,bool isConst){
			
			// Try loading a constant now:
			Variable v=ResolvedProperty;
			
			if(v!=null){
				v.IsConstant=isConst;
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
		
		/// <summary>Updates the type of the property variable. Note that this does nothing if the property
		/// is fixed; i.e. it's built in.</summary>
		public void ApplyType(OptimizationInfo optimizationInfo,Type type){
			
			if(ResolvedProperty==null){
				// Unable to set it.
				return;
			}
			
			// Set the type:
			ResolvedProperty.Type=type;
			
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
			
			if(ResolvedProperty==null){
				
				// Dynamic access.
				
				// optimizationInfo.TypeError("Attempted to get a property dynamically. Currently unsupported.");
				
				// Load the left-hand side:
				var lhs = this.GetOperand(0);
				
				// -- Begin args for Prototype.GetPropertyValue --
				
				// Script engine (engine):
				EmitHelpers.LoadEngine(generator);
				
				// Emit LHS to the stack (thisObj):
				lhs.GenerateCode(generator, optimizationInfo);
				
				// What type have we now got on the stack? Expected to be just 'object'.
				Type lhsType=lhs.GetResultType(optimizationInfo);
				
				// Ensure it's boxed (still thisObj):
				EmitConversion.ToAny(generator,lhsType);
				
				// Load the property name and convert to a string (property).
				var rhs = this.GetOperand(1);
				rhs.GenerateCode(generator, optimizationInfo);
				EmitConversion.ToString(generator, rhs.GetResultType(optimizationInfo));
				
				// Get the value:
				generator.Call(ReflectionHelpers.Object_GetPropertyValue);
				
				// Either it's now on the stack, or we threw a null ref.
				
			}else if(isArrayIndex){
			
				// Array indexer
				// -------------
				// xxx = object[index]

				// Load the left-hand side
				var lhs = this.GetOperand(0);
				lhs.GenerateCode(generator, optimizationInfo);
				
				// Load the right-hand side and convert to [theTypeHere] (typically int32).
				var rhs = this.GetOperand(1);
				rhs.GenerateCode(generator, optimizationInfo);
				
				// Convert the index:
				EmitConversion.Convert(generator, rhs.GetResultType(optimizationInfo),ResolvedProperty.FirstIndexType);
				
				// Emit a get for the indexer:
				ResolvedProperty.Get(generator);
				
			}else{
				
				// Load the left-hand side and convert to an object instance.
				var lhs = this.GetOperand(0);
				
				if(ResolvedProperty!=null){
					
					if(ResolvedProperty.HasEngine){
						
						// Emit the engine ref:
						EmitHelpers.LoadEngine(generator);
						
					}
					
					if(ResolvedProperty.HasAccessor){
						
						// Emit the 'this' obj:
						lhs.GenerateCode(generator, optimizationInfo);
						
					}
					
					// Emit a get:
					ResolvedProperty.Get(generator);
					
				}
				
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
		public void GenerateSet(ILGenerator generator, OptimizationInfo optimizationInfo,bool rIU, Type valueType, SetValueMethod value, bool throwIfUnresolvable)
		{
			
			if(ResolvedProperty==null){
				
				// Dynamic property access
				// -----------------------
				// xxx = object.Set(x)
				
				// Load the left-hand side:
				var lhs = this.GetOperand(0);
				
				// -- Begin args for Prototype.SetPropertyValue --
				
				// Script engine (engine):
				EmitHelpers.LoadEngine(generator);
				
				// Put LHS object onto stack now (thisObj):
				lhs.GenerateCode(generator, optimizationInfo);
				
				// What type have we now got on the stack? Typically expected to be 'object'.
				Type lhsType=lhs.GetResultType(optimizationInfo);
				
				// Ensure it's boxed (still thisObj):
				EmitConversion.ToAny(generator,lhsType);
				
				// Load the property name and convert to a string.
				var rhs = this.GetOperand(1);
				rhs.GenerateCode(generator, optimizationInfo);
				EmitConversion.ToString(generator, rhs.GetResultType(optimizationInfo));
				
				if(rIU){
					
					// Output the value now (twice):
					value(true);
					
					// We now have [obj][value][value] on the stack.
					// Calling the set method would fail (as it'll operate on the duplicated value).
					// So, we have to pop one off and re-add it after.
					
					// In order for SetValue to work, we need to shove the 2nd copy into a temp variable.
					ILLocalVariable localVar=generator.DeclareVariable(valueType);
					
					// Store into the local:
					generator.StoreVariable(localVar);
					
					// Set the value:
					generator.Call(ReflectionHelpers.Object_SetPropertyValue);
					
					// Load from the local:
					generator.LoadVariable(localVar);
					
				}else{
					
					// Output the value now:
					value(false);
					
					// Set the value:
					generator.Call(ReflectionHelpers.Object_SetPropertyValue);
					
				}
				
			}else if(isArrayIndex){
				
				// Array indexer
				// -------------
				// object[index] = x
				
				// Load the left-hand side and convert to an object instance.
				var lhs = this.GetOperand(0);
				lhs.GenerateCode(generator, optimizationInfo);
				
				// Load the right-hand side and convert to int32/uint32/whatever the indexer function wants.
				var rhs = this.GetOperand(1);
				rhs.GenerateCode(generator, optimizationInfo);
				
				// Convert the index:
				EmitConversion.Convert(generator, rhs.GetResultType(optimizationInfo),ResolvedProperty.FirstIndexType);
				
				// Call set:
				ResolvedProperty.Set(generator,optimizationInfo,rIU,valueType,value);
				
			}else{
				
				// Named property modification (e.g. x.property = y)
				// -------------------------------------------------
				
				if(ResolvedProperty.HasEngine){
					
					// Emit the engine ref:
					EmitHelpers.LoadEngine(generator);
					
				}
				
				if(ResolvedProperty.HasAccessor)
				{
					// Load the left-hand side:
					var lhs = GetOperand(0);
					lhs.GenerateCode(generator, optimizationInfo);
				}
				
				// Target object is now on the stack.
				
				// Set it:
				ResolvedProperty.Set(generator,optimizationInfo,rIU,valueType,value);
				
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
			
			throw new JavaScriptException(optimizationInfo.Engine, "SyntaxError", "Delete is not supported by Nitro", 1, optimizationInfo.Source.Path, optimizationInfo.FunctionName);
			
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