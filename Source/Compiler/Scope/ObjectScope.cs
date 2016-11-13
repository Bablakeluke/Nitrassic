using System;
using System.Collections;
using System.Collections.Generic;
using Nitrassic.Library;


namespace Nitrassic.Compiler
{

	/// <summary>
	/// Represents a scope which is backed by the properties of an object.
	/// </summary>
	[Serializable]
	public class ObjectScope : Scope
	{
		
		[NonSerialized]
		private Expression scopeObjectExpression;

		private bool providesImplicitThisValue;
		
		/// <summary>
		/// Essentially the type of object that is the focus of this scope, if there is one.
		/// Note that the global scope does not have this.
		/// </summary>
		public Prototype ScopePrototype;
		
		/// <summary>
		/// Creates a new object scope for use inside a with statement.
		/// </summary>
		/// <param name="parentScope"> A reference to the parent scope.  Can not be <c>null</c>. </param>
		/// <param name="scopeObject"> An expression that evaluates to the object to use. </param>
		/// <returns> A new ObjectScope instance. </returns>
		internal static ObjectScope CreateWithScope(Scope parentScope, Expression scopeObject,OptimizationInfo optimizationInfo)
		{
			
			if (parentScope == null)
				throw new ArgumentException("With scopes must have a parent scope.");
			
			// Get the type:
			Type resultType=scopeObject.GetResultType(optimizationInfo);
			
			// Get the prototype:
			Prototype proto=optimizationInfo.Engine.Prototypes.Get(resultType);
			
			// Create the scope:
			ObjectScope scope=new ObjectScope(parentScope,proto);
			scope.ScopeObjectExpression = scopeObject;
			scope.ProvidesImplicitThisValue = true;
			scope.CanDeclareVariables = false;
			
			return scope;
			
		}
		
		/// <summary>
		/// Creates a new ObjectScope instance.
		/// </summary>
		internal ObjectScope(Scope parentScope,Prototype scopePrototype)
			: base(parentScope)
		{
			this.ScopeObjectExpression = null;
			this.ProvidesImplicitThisValue = false;
			ScopePrototype=scopePrototype;
			CanDeclareVariables=true;
		}

		/// <summary>
		/// Creates a new ObjectScope instance.
		/// </summary>
		internal ObjectScope(ScriptEngine engine,Prototype scopePrototype)
			: base(engine)
		{
			this.ScopeObjectExpression = null;
			this.ProvidesImplicitThisValue = false;
			ScopePrototype=scopePrototype;
			CanDeclareVariables=true;
		}

		/// <summary>
		/// Gets an expression that evaluates to the scope object.  <c>null</c> if the scope object
		/// is the global object.
		/// </summary>
		internal Expression ScopeObjectExpression
		{
			get { return this.scopeObjectExpression; }
			set { this.scopeObjectExpression = value; }
		}

		/// <summary>
		/// Gets a value that indicates whether an implicit "this" value is supplied to function
		/// calls in this scope.
		/// </summary>
		public bool ProvidesImplicitThisValue
		{
			get { return this.providesImplicitThisValue; }
			set { this.providesImplicitThisValue = value; }
		}
		
		internal override Variable GetVariable(string name){
			
			// Get the property:
			return ScopePrototype.GetProperty(name);
			
		}
		
		internal override Variable AddVariable(string name,Type type, Expression valueAtTopOfScope)
		{
			
			// Get the property:
			PropertyVariable pv=ScopePrototype.GetProperty(name);
			
			if(pv!=null){
				
				// Already exists - try to change the type (may collapse it):
				pv.Type=type;
				
				return pv;
			}
			
			// Add it now:
			pv=ScopePrototype.AddProperty(name,null,Nitrassic.Library.PropertyAttributes.FullAccess);
			
			// Set the type:
			pv.Type=type;
			
			return pv;
			
		}
		
	}

}