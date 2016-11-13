using System;
using System.Collections.Generic;

namespace Nitrassic.Compiler
{
	/// <summary>
	/// Represents an enclosing context where variables are uniquely defined.
	/// </summary>
	[Serializable]
	public abstract class Scope
	{
		
		/// <summary>
		/// Creates a new Scope instance.
		/// </summary>
		/// <param name="parentScope"> A reference to the parent scope, or <c>null</c> if this is
		/// the global scope. </param>
		protected Scope(Scope parentScope)
		{
			this.ParentScope = parentScope;
			Engine = parentScope.Engine;
		}
		
		/// <summary>
		/// Creates a new Scope instance.
		/// </summary>
		protected Scope(ScriptEngine engine)
		{
			Engine = engine;
		}
		
		/// <summary>
		/// Gets a reference to the parent scope.  Can be <c>null</c> if this is the global scope.
		/// </summary>
		public Scope ParentScope;
		/// <summary>
		/// The script engine this is a scope from.
		/// </summary>
		public ScriptEngine Engine;
		
		public bool CanDeclareVariables;
		
		
		/// <summary>Called before recompiling a function with different args.</summary>
		public virtual void Reset(){}
		
		/// <summary>
		/// Declares a variable or function in this scope.  This will be initialized with the value
		/// of the given expression.
		/// </summary>
		/// <param name="name"> The name of the variable. </param>
		/// <param name="valueAtTopOfScope"> The value of the variable at the top of the scope.
		/// Can be <c>null</c> to indicate the variable does not need initializing. </param>
		/// <param name="writable"> <c>true</c> if the variable can be modified; <c>false</c>
		/// otherwise.  Defaults to <c>true</c>. </param>
		/// <param name="deletable"> <c>true</c> if the variable can be deleted; <c>false</c>
		/// otherwise.  Defaults to <c>true</c>. </param>
		/// <returns> A reference to the variable that was declared. </returns>
		internal abstract Variable AddVariable(string name,Type type,Expression valueAtTopOfScope);
		
		internal abstract Variable GetVariable(string variableName);
		
		internal Variable AddVariable(string name)
		{
			return AddVariable(name,null,null);
		}
		
		internal Variable AddVariable(string name, Type type){
			
			return AddVariable(name,type,null);
			
		}
		
	}

}