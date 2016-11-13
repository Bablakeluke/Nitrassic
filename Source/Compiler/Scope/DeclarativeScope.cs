using System;
using System.Collections.Generic;

namespace Nitrassic.Compiler
{
	
	/// <summary>
	/// Represents a scope where the variables are statically known.
	/// </summary>
	[Serializable]
	public class DeclarativeScope : Scope
	{
		// A dictionary containing the variables declared in this scope. Always either arguments or declaredVars.
		internal Dictionary<string, Variable> variables;

		// The expression of the function (if any) where this scope exists.
		internal FunctionExpression Function;
		
		/// <summary>
		/// Creates a new declarative scope for use inside a function body.
		/// </summary>
		/// <param name="parentScope"> A reference to the parent scope.  Can not be <c>null</c>. </param>
		/// <param name="functionName"> The name of the function.  Can be empty for an anonymous function. </param>
		/// <param name="argumentNames"> The names of each of the function arguments. </param>
		/// <returns> A new DeclarativeScope instance. </returns>
		internal static DeclarativeScope CreateFunctionScope(Scope parentScope, string functionName, List<ArgVariable> args)
		{
			if (functionName == null)
				throw new ArgumentNullException("functionName");
			if (args == null)
				throw new ArgumentNullException("args");
			DeclarativeScope result = new DeclarativeScope(parentScope, 0);
			if (string.IsNullOrEmpty(functionName) == false)
				result.AddVariable(functionName);
			
			// At this point, the 'this' keyword is already in args.
			
			result.AddVariable("arguments");
			
			foreach (ArgVariable arg in args)
			{
				result.Declare(arg);
				arg.Scope=result;
			}
			
			return result;
		}
		
		/// <summary>Called before recompiling a function with different args.</summary>
		public override void Reset(){
			
			foreach(KeyValuePair<string,Variable> kvp in variables){
				
				DeclaredVariable dec=kvp.Value as DeclaredVariable;
				
				if(dec==null){
					continue;
				}
				
				// Ensure store is cleared out:
				dec.Store=null;
				
			}
			
		}
		
		/// <summary>
		/// Creates a new declarative scope for use inside a catch statement.
		/// </summary>
		/// <param name="parentScope"> A reference to the parent scope.  Can not be <c>null</c>. </param>
		/// <param name="catchVariableName"> The name of the catch variable. </param>
		/// <returns> A new DeclarativeScope instance. </returns>
		internal static DeclarativeScope CreateCatchScope(Scope parentScope, string catchVariableName)
		{
			if (parentScope == null)
				throw new ArgumentNullException("parentScope", "Catch scopes must have a parent scope.");
			if (catchVariableName == null)
				throw new ArgumentNullException("catchVariableName");
			DeclarativeScope result = new DeclarativeScope(parentScope, 0);
			result.AddVariable(catchVariableName);
			result.CanDeclareVariables = false;	// Only the catch variable can be declared in this scope.
			return result;
		}

		/// <summary>
		/// Creates a new declarative scope for use inside a strict mode eval statement.
		/// </summary>
		/// <param name="parentScope"> A reference to the parent scope.  Can not be <c>null</c>. </param>
		/// <returns> A new DeclarativeScope instance. </returns>
		internal static DeclarativeScope CreateEvalScope(Scope parentScope)
		{
			if (parentScope == null)
				throw new ArgumentNullException("parentScope", "Eval scopes must have a parent scope.");
			return new DeclarativeScope(parentScope, 0);
		}
		
		/// <summary>
		/// Creates a new DeclarativeScope instance.
		/// </summary>
		/// <param name="parentScope"> A reference to the parent scope, or <c>null</c> if this is
		/// the global scope. </param>
		/// <param name="declaredVariableCount"> The number of variables declared in this scope. </param>
		internal DeclarativeScope(Scope parentScope, int declaredVariableCount)
			: base(parentScope)
		{
		
			this.variables = new Dictionary<string, Variable>(declaredVariableCount);
			this.CanDeclareVariables = true;
			
		}
		
		/// <summary>
		/// Creates a new DeclarativeScope instance.
		/// </summary>
		/// <param name="declaredVariableCount"> The number of variables declared in this scope. </param>
		internal DeclarativeScope(ScriptEngine engine, int declaredVariableCount)
			: base(engine)
		{
		
			this.variables = new Dictionary<string, Variable>(declaredVariableCount);
			this.CanDeclareVariables = false;
			
		}
		
		/// <summary>
		/// Returns <c>true</c> if the given variable has been declared in this scope.
		/// </summary>
		/// <param name="name"> The name of the variable. </param>
		/// <returns> <c>true</c> if the given variable has been declared in this scope;
		/// <c>false</c> otherwise. </returns>
		internal bool HasDeclaredVariable(string name)
		{
			return this.variables.ContainsKey(name);
		}
		
		/// <summary>Directly declare a new variable.</summary>
		internal void Declare(Variable variable)
		{
			variables[variable.Name]=variable;
		}
		
		internal override Variable AddVariable(string name,Type type,Expression valueAtTopOfScope)
		{
			if (name == null)
				throw new ArgumentNullException("name");

			// If variables cannot be declared in the scope, try the parent scope instead.
			if (this.CanDeclareVariables == false)
			{
				if (this.ParentScope == null)
					throw new InvalidOperationException("Invalid scope chain.");
				return this.ParentScope.AddVariable(name, type, valueAtTopOfScope);
			}

			Variable variable;
			this.variables.TryGetValue(name, out variable);
			DeclaredVariable dVariable;
			
			if (variable == null || !(variable is DeclaredVariable))
			{
				// This is a local variable that has not been declared before (or is an arg being overwritten).
				dVariable = new DeclaredVariable() { Name = name, Scope = this };
				variable = dVariable;
				this.variables[name]=dVariable;
			}
			else
			{
				dVariable=(DeclaredVariable)variable;
			}
			
			// Set the initial value, if one was provided.
			if (valueAtTopOfScope != null)
			{
				// Function expressions override literals.
				if ((valueAtTopOfScope is LiteralExpression && dVariable.ValueAtTopOfScope is FunctionExpression) == false)
					dVariable.ValueAtTopOfScope = valueAtTopOfScope;
			}
			
			variable.Type=type;
			return variable;
		}
		
		/// <summary>
		/// Removes a declared variable from the scope.
		/// </summary>
		/// <param name="name"> The name of the variable. </param>
		internal void RemovedDeclaredVariable(string name)
		{
			this.variables.Remove(name);
		}
		
		/// <summary>
		/// Generates code that initializes the variable and function declarations.
		/// </summary>
		/// <param name="generator"> The generator to output the CIL to. </param>
		/// <param name="optimizationInfo"> Information about any optimizations that should be performed. </param>
		internal void GenerateDeclarations(ILGenerator generator, OptimizationInfo optimizationInfo)
		{
			
			// Parent root:
			Expression rootExpression=optimizationInfo.RootExpression;
			
			// Initialize the declared variables and functions.
			foreach (var variable in this.variables.Values)
			{
				
				DeclaredVariable dVariable = variable as DeclaredVariable;
				
				// When a scope is reused, i.e. with an eval(), do not reinitialize the variables.
				if ( dVariable==null || dVariable.Initialized)
					continue;

				if (dVariable.ValueAtTopOfScope != null)
				{
					// Emit the initialization code.
					
					var name = new NameExpression(this, dVariable.Name);
					
					Type rType=dVariable.ValueAtTopOfScope.GetResultType(optimizationInfo);
					
					name.ApplyType(optimizationInfo,rType);
					
					name.GenerateSet(generator, optimizationInfo,false, rType, delegate(bool two)
					{
						
						optimizationInfo.RootExpression=dVariable.ValueAtTopOfScope;
						
						dVariable.ValueAtTopOfScope.GenerateCode(generator, optimizationInfo);
						
						if(two){
							// Dupe it:
							generator.Duplicate();
						}
						
					}, false);
					
					// Mark the variable as having been initialized.
					dVariable.Initialized = true;
				}
			}
			
			// Restore root:
			optimizationInfo.RootExpression=rootExpression;
			
		}
	
		
		/// <summary>
		/// Gets an enumerable list of the declared variables, in no particular order.
		/// </summary>
		internal IEnumerable<Variable> DeclaredVariables
		{
			get { return this.variables.Values; }
		}

		/// <summary>
		/// Gets the index of the given variable.
		/// </summary>
		/// <param name="variableName"> The name of the variable. </param>
		/// <returns> The index of the given variable, or <c>-1</c> if the variable doesn't exist
		/// in the scope. </returns>
		internal override Variable GetVariable(string variableName)
		{
			if (variableName == null)
				throw new ArgumentNullException("variableName");
				
			Variable variable;
			variables.TryGetValue(variableName, out variable);
			return variable;
		}
		
	}

}