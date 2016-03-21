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
		
		#warning set me!
		/// <summary>
		/// Essentially the type of object that is the focus of this scope, if there is one.
		/// Note that the global scope does not have this.
		/// </summary>
		public Prototype ScopePrototype;
		
		/// <summary>
		/// Creates a new global object scope.
		/// </summary>
		/// <returns> A new ObjectScope instance. </returns>
		internal static ObjectScope CreateGlobalScope(ScriptEngine engine)
		{
			return new ObjectScope(engine);
		}

		/// <summary>
		/// Creates a new object scope for use inside a with statement.
		/// </summary>
		/// <param name="parentScope"> A reference to the parent scope.  Can not be <c>null</c>. </param>
		/// <param name="scopeObject"> An expression that evaluates to the object to use. </param>
		/// <returns> A new ObjectScope instance. </returns>
		internal static ObjectScope CreateWithScope(Scope parentScope, Expression scopeObject)
		{
			if (parentScope == null)
				throw new ArgumentException("With scopes must have a parent scope.");
			return new ObjectScope(parentScope) { ScopeObjectExpression = scopeObject, ProvidesImplicitThisValue = true, CanDeclareVariables = false };
		}
		
		/// <summary>
		/// Used when variables are declared whilst within an object scope.
		/// </summary>
		private DeclarativeScope Declared;
		
		/// <summary>
		/// Creates a new ObjectScope instance.
		/// </summary>
		private ObjectScope(Scope parentScope)
			: base(parentScope)
		{
			this.ScopeObjectExpression = null;
			this.ProvidesImplicitThisValue = false;
			CanDeclareVariables=true;
		}

		/// <summary>
		/// Creates a new ObjectScope instance.
		/// </summary>
		private ObjectScope(ScriptEngine engine)
			: base(engine)
		{
			this.ScopeObjectExpression = null;
			this.ProvidesImplicitThisValue = false;
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
		
		internal void GenerateDeclarations(ILGenerator generator, OptimizationInfo optimizationInfo)
		{
			
			// Only ever occurs on the global scope.
			
			if(Declared==null)
			{
				return;
			}
			
			float progress=0;
			float total=Declared.variables.Count;
			
			// Initialize the declared variables and functions.
			foreach (KeyValuePair<string,Variable> kvp in Declared.variables)
			{
				
				progress++;
				
				GlobalVariable variable=kvp.Value as GlobalVariable;
				
				if (variable == null)
					continue;

				//if (variable.ValueAtTopOfScope == null)
				//	continue;
				
				// Console.WriteLine(((progress/total)*100)+"% ["+variable.Name+", "+variable.Type+"]");
				
				// Variable should actually be a global.
				
				/*
				// bool DefineProperty(string propertyName, PropertyDescriptor descriptor, bool throwOnError)
				EmitHelpers.GlobalObject(generator);
				generator.LoadString(variable.Name);
				variable.ValueAtTopOfScope.GenerateCode(generator, optimizationInfo);
				EmitConversion.Convert(generator, variable.ValueAtTopOfScope.GetResultType(optimizationInfo), typeof(object), optimizationInfo);
				generator.LoadInt32((int)attributes);
				generator.NewObject(ReflectionHelpers.PropertyDescriptor_Constructor2);
				generator.LoadBoolean(false);
				generator.Call(ReflectionHelpers.ObjectInstance_DefineProperty);
				generator.Pop();
				*/
				
			}
			
		}
		
		internal override Variable DeclareVariable(string name,Type type, Expression valueAtTopOfScope)
		{
			
			if(Declared==null)
				Declared=new DeclarativeScope(this,1);
			
			if(ParentScope == null)
			{
				// This is the global scope. Create a global variable
				// and add it to the declared set so it gets initialised correctly.
				Variable _global=Engine.Global.Get(name);
				
				if(type!=null)
				{
					_global.Type=type;
				}
				
				Declared.Declare(_global);
				return _global;
			}
			
			return Declared.DeclareVariable(name,type, valueAtTopOfScope);
		}
		
	}

}