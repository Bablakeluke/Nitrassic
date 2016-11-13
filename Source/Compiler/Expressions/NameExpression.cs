using System;
using Nitrassic.Library;


namespace Nitrassic.Compiler
{

	/// <summary>
	/// Represents a variable or part of a member reference.
	/// </summary>
	internal sealed class NameExpression : Expression, IReferenceExpression
	{
		/// <summary>
		/// Creates a new NameExpression instance.
		/// </summary>
		/// <param name="scope"> The current scope. </param>
		/// <param name="name"> The name of the variable or member that is being referenced. </param>
		public NameExpression(Scope scope, string name)
		{
			if (scope == null)
				throw new ArgumentNullException("scope");
			if (name == null)
				throw new ArgumentNullException("name");
			this.Scope = scope;
			this.Name = name;
		}

		/// <summary>
		/// Gets or sets the scope the name is contained within.
		/// </summary>
		public Scope Scope;

		/// <summary>
		/// Gets the name of the variable or member.
		/// </summary>
		public string Name;
		
		/// <summary>
		/// The variable that this has been matched to.
		/// </summary>
		private Variable _Variable;
		
		public Variable Variable{
			get{
				return _Variable;
			}
			set{
				_Variable=value;
			}
		}
		
		/// <summary>
		/// Gets the type that results from evaluating this expression.
		/// </summary>
		public override Type GetResultType(OptimizationInfo optimizationInfo)
		{
			if(Variable==null)
			{
				return typeof(object);
			}
			
			Type type=Variable.Type;
			
			if(type==null){
				return typeof(Nitrassic.Undefined);
			}
			
			return type;
			
		}
		
		public override object Evaluate(){
			
			// Try loading a constant now:
			Variable v=Variable;
			
			if(v==null){
				// Undefined.
				return Undefined.Value;
			}
			
			if(v.IsConstant){
				
				return v.ConstantValue;
				
			}
			
			return null;
			
		}
		
		public object GetConstantValue(){
			
			// Try loading a constant now:
			Variable v=Variable;
			
			if(v!=null && v.IsConstant){
				return v.ConstantValue;
			}
			
			return null;
			
		}
		
		public object TryApplyConstant(OptimizationInfo optimizationInfo,Expression expr){
			
			// Try loading a constant now:
			Variable v=Variable;
			
			if(v!=null&& v.TryLoadConstant(expr)){
				return v.ConstantValue;
			}
			
			return null;
			
		}
		
		public void TryApplyConstant(OptimizationInfo optimizationInfo,bool isConst){
			
			Variable v=Variable;
			
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
			GenerateGet(generator, optimizationInfo, true);
		}
		
		/// <summary>Resolves the type of variable this is referencing.</summary>
		internal override void ResolveVariables(OptimizationInfo optimizationInfo)
		{
			
			if(_Variable!=null){
				return;
			}
			
			// Resolve kids:
			base.ResolveVariables(optimizationInfo);
			
			var scope = this.Scope;
			
			while (scope != null){
				
				// Is the variable in this scope?
				_Variable = scope.GetVariable(Name);
				
				if (_Variable != null)
				{
					// The scope has been optimized away.  The value of the variable is stored
					// in an ILVariable.
					
					// The variable was found - no need to search any more parent scopes.
					return;
					
				}
				
				scope = scope.ParentScope;

			}
			
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
			
			if(Variable==null)
			{
				
				// It's undefined:
				EmitHelpers.EmitUndefined(generator);
				
				return;
			}
			
			if(Variable.IsHoisted(Scope))
			{
				
				// Hoist the variable:
				Variable=(Scope as DeclarativeScope).Function.Hoist(Variable);
				
			}
			
			// Output a get now:
			Variable.Get(generator);
			
		}

		public void ApplyType(OptimizationInfo optimizationInfo,Type type){
			
			if(Variable==null)
			{
				
				// Create a new variable:
				Variable=Scope.AddVariable(Name,type,null);
				
			}else{
				
				// Set type:
				Variable.Type=type;
				
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
			
			if(Variable==null)
			{
				
				// Create a new variable:
				Variable=Scope.AddVariable(Name,valueType,null);
				
			}
			else if(Variable.IsHoisted(Scope))
			{
				
				Wrench.Log.Add("Attempted to set a hoisted variable. This is slow and is usually unnecessary. Ignoring it by treating it as a local.");
				
				// This happens in this situation:
				// function a(){
				//  var b="hello!";
				//  var test=function(){
				//     b="overwriting a hoisted variable here (Nitrassic treats it as a new local in test's scope)";
				//   };
				//  // Nitrassic is wrong if this happens:
				//  test();
				//  console.log(b); // *should* be the value set from inside test
				// }
				
				// Create a new variable:
				Variable=Scope.AddVariable(Name,valueType,null);
				
			}
			
			// Output a set now:
			Variable.Set(generator,optimizationInfo,rIU,valueType,value);
			
		}

		/// <summary>
		/// Deletes the reference and pushes <c>true</c> if the delete succeeded, or <c>false</c>
		/// if the delete failed.
		/// </summary>
		/// <param name="generator"> The generator to output the CIL to. </param>
		/// <param name="optimizationInfo"> Information about any optimizations that should be performed. </param>
		public void GenerateDelete(ILGenerator generator, OptimizationInfo optimizationInfo)
		{
			// Deleting a variable is not allowed in strict mode.
			if (optimizationInfo.StrictMode == true)
				throw new JavaScriptException(optimizationInfo.Engine, "SyntaxError", string.Format("Cannot delete {0} because deleting a variable or argument is not allowed in strict mode", this.Name), 1, optimizationInfo.Source.Path, optimizationInfo.FunctionName);
			
			// Always just false:
			generator.LoadBoolean(false);
		}

		/// <summary>
		/// Generates code to push the "this" value for a function call.
		/// </summary>
		/// <param name="generator"> The generator to output the CIL to. </param>
		public void GenerateThis(ILGenerator generator)
		{
			// Optimization: if there are no with scopes, simply emit undefined.
			bool scopeChainHasWithScope = false;
			var scope = this.Scope;
			do
			{
				if (scope is ObjectScope && ((ObjectScope)scope).ProvidesImplicitThisValue == true)
				{
					scopeChainHasWithScope = true;
					break;
				}
				scope = scope.ParentScope;
			} while (scope != null);

			if (scopeChainHasWithScope == false)
			{
				// No with scopes in the scope chain, use undefined as the "this" value.
				EmitHelpers.EmitUndefined(generator);
				return;
			}
			
			scope = this.Scope;
			
			do
			{
				DeclarativeScope dScope = scope as DeclarativeScope;
				
				if (dScope!=null)
				{
					if (dScope.HasDeclaredVariable(this.Name))
					{
						// The variable exists but declarative scopes always produce undefined for
						// the "this" value.
						EmitHelpers.EmitUndefined(generator);
						break;
					}
				}
				else
				{
					
					// Object scope - what object type is it for?
					Prototype proto=(scope as ObjectScope).ScopePrototype;
					
					_Variable=proto.GetProperty(Name);
					
					if(_Variable!=null)
					{
						
						// Output a get now:
						_Variable.Get(generator);
						
						break;
					}
					
				}
				
				scope = scope.ParentScope;

			} while (scope != null);
			
		}

		/// <summary>
		/// Calculates the hash code for this object.
		/// </summary>
		/// <returns> The hash code for this object. </returns>
		public override int GetHashCode()
		{
			return this.Name.GetHashCode() ^ this.Scope.GetHashCode();
		}

		/// <summary>
		/// Determines if the given object is equal to this one.
		/// </summary>
		/// <param name="obj"> The object to compare. </param>
		/// <returns> <c>true</c> if the given object is equal to this one; <c>false</c> otherwise. </returns>
		public override bool Equals(object obj)
		{
			if ((obj is NameExpression) == false)
				return false;
			return this.Name == ((NameExpression)obj).Name && this.Scope == ((NameExpression)obj).Scope;
		}

		/// <summary>
		/// Converts the expression to a string.
		/// </summary>
		/// <returns> A string representing this expression. </returns>
		public override string ToString()
		{
			return this.Name;
		}
	}
}