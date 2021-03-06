﻿using System;
using System.Collections.Generic;
using Nitrassic.Library;

namespace Nitrassic.Compiler
{
	
	public delegate void OnLJCallback(ILGenerator g, ILLabel l);
	
	public delegate void CodeGenDelegate(ILGenerator generator, OptimizationInfo optimizationInfo);

	/// <summary>
	/// Represents information about one or more code generation optimizations.
	/// </summary>
	public class OptimizationInfo
	{
		/// <summary>
		/// Creates a new OptimizationInfo instance.
		/// </summary>
		/// <param name="engine"> The associated script engine. </param>
		public OptimizationInfo(ScriptEngine engine)
		{
			this.Engine = engine;
		}

		/// <summary>
		/// Gets the associated script engine.
		/// </summary>
		public ScriptEngine Engine;

		/// <summary>
		/// Gets or sets the root of the abstract syntax tree that is being compiled.
		/// </summary>
		public AstNode AbstractSyntaxTree;

		/// <summary>
		/// Gets or sets a value that indicates whether strict mode is enabled.
		/// </summary>
		public bool StrictMode;



		//	 DEBUGGING
		//_________________________________________________________________________________________

		/// <summary>
		/// Gets or sets the symbol store to write debugging information to.
		/// </summary>
		public System.Diagnostics.SymbolStore.ISymbolDocumentWriter DebugDocument;

		/// <summary>
		/// Gets or sets the source of javascript code.
		/// </summary>
		public ScriptSource Source;

		/// <summary>
		/// Gets or sets the name of the function that is being generated.
		/// </summary>
		public string FunctionName;
		
		/// <summary>
		/// Set to the root node of the current expression.
		/// Used to indicate if the return value is in use or not.
		/// </summary>
		internal Expression RootExpression;
		
		/// <summary>
		/// Method return type.
		/// </summary>
		public Type ReturnType;
		
		/*
		/// <summary>
		/// Emits a sequence point, and sets the SourceSpan property.
		/// </summary>
		/// <param name="generator"> The IL generator used to emit the sequence point. </param>
		/// <param name="span"> The source code span. </param>
		public void MarkSequencePoint(ILGenerator generator, SourceCodeSpan span)
		{
			if (span == null)
				throw new ArgumentNullException("span");
			if (this.DebugDocument != null)
				generator.MarkSequencePoint(this.DebugDocument, span);
			this.SourceSpan = span;
		}
		*/

		//	 NESTED FUNCTIONS
		//_________________________________________________________________________________________
		
		/// <summary>
		/// The latest set of labels from a LabelledStatement.
		/// Set when DefaultBreakStatementBehaviour is false.
		/// </summary>
		public List<string> Labels;
		
		/// <summary>
		/// Gets a list of generated methods that correspond to nested functions.
		/// This list is maintained so that the garbage collector does not prematurely collect
		/// the generated code for the nested functions.
		/// </summary>
		public IList<FunctionMethodGenerator> NestedFunctions;
		
		/// <summary>
		/// True if a FunctionCallExpression is actually being used by new.
		/// </summary>
		public bool IsConstructCall;

		/// <summary>
		/// True if the whole function being compiled is being used by new.
		/// </summary>
		public bool IsConstructor;

		//	 FUNCTION OPTIMIZATION
		//_________________________________________________________________________________________

		/// <summary>
		/// Gets or sets function optimization information.
		/// </summary>
		public MethodOptimizationHints MethodOptimizationHints;

		/// <summary>
		/// Gets a value that indicates whether the declarative scopes should be optimized away,
		/// so that the scope is not even created at runtime.
		/// </summary>
		public bool OptimizeDeclarativeScopes
		{
			get
			{
				return !MethodOptimizationHints.HasEval;
			}
		}

		/// <summary>
		/// Gets a value that indicates whether the variables should be optimized if the type of
		/// the variable can be inferred.
		/// </summary>
		public bool OptimizeInferredTypes
		{
			get
			{
				return !MethodOptimizationHints.HasEval;
			}
		}



		//	 VARIABLES
		//_________________________________________________________________________________________
		
		private Dictionary<RegularExpressionLiteral, ILLocalVariable> regularExpressionVariables;

		/// <summary>
		/// Retrieves a variable that can be used to store a shared instance of a regular
		/// expression.
		/// </summary>
		/// <param name="generator"> The IL generator used to create the variable. </param>
		/// <param name="literal"> The regular expression literal. </param>
		/// <returns> A varaible that can be used to store a shared instance of a regular
		/// expression. </returns>
		public ILLocalVariable GetRegExpVariable(ILGenerator generator, RegularExpressionLiteral literal)
		{
			if (literal == null)
				throw new ArgumentNullException("literal");

			// Create a new Dictionary if it hasn't been created before.
			if (this.regularExpressionVariables == null)
				this.regularExpressionVariables = new Dictionary<RegularExpressionLiteral, ILLocalVariable>();

			// Check if the literal already exists in the dictionary.
			ILLocalVariable variable;
			if (this.regularExpressionVariables.TryGetValue(literal, out variable) == false)
			{
				// The literal does not exist - add it.
				variable = generator.DeclareVariable(typeof(Library.RegExp));
				this.regularExpressionVariables.Add(literal, variable);
			}
			return variable;
		}



		//	 RETURN SUPPORT
		//_________________________________________________________________________________________

		/// <summary>
		/// Gets or sets the label the return statement should jump to (with the return value on
		/// top of the stack).  Will be <c>null</c> if code is being generated outside a function
		/// context.
		/// </summary>
		public ILLabel ReturnTarget;

		/// <summary>
		/// Gets or sets the variable that holds the return value for the function.  Will be
		/// <c>null</c> if code is being generated outside a function context or if no return
		/// statements have been encountered.
		/// </summary>
		public ILLocalVariable ReturnVariable;



		//	 BREAK AND CONTINUE SUPPORT
		//_________________________________________________________________________________________

		private class BreakOrContinueInfo
		{
			public IList<string> LabelNames;
			public bool LabelledOnly;
			public ILLabel BreakTarget;
			public ILLabel ContinueTarget;
		}

		private Stack<BreakOrContinueInfo> breakOrContinueStack = new Stack<BreakOrContinueInfo>();
		
		/// <summary>Throws a syntax error at the current function.</summary>
		public void SyntaxError(string message){
			throw new JavaScriptException(Engine, "SyntaxError", message, 1, this.Source.Path, this.FunctionName);
		}
		
		/// <summary>Throws a type error at the current function.</summary>
		public void TypeError(string message){
			throw new JavaScriptException(Engine, "TypeError", message, 1, this.Source.Path, this.FunctionName);
		}
		
		/// <summary>
		/// Pushes information about break or continue targets to a stack.
		/// </summary>
		/// <param name="labelNames"> The label names associated with the break or continue target.
		/// Can be <c>null</c>. </param>
		/// <param name="breakTarget"> The IL label to jump to if a break statement is encountered. </param>
		/// <param name="continueTarget"> The IL label to jump to if a continue statement is
		/// encountered.  Can be <c>null</c>. </param>
		/// <param name="labelledOnly"> <c>true</c> if break or continue statements without a label
		/// should ignore this entry; <c>false</c> otherwise. </param>
		public void PushBreakOrContinueInfo(IList<string> labelNames, ILLabel breakTarget, ILLabel continueTarget, bool labelledOnly)
		{
			if (breakTarget == null)
				throw new ArgumentNullException("breakTarget");

			// Check the label doesn't already exist.
			if (labelNames != null)
			{
				foreach (var labelName in labelNames)
					foreach (var info in this.breakOrContinueStack)
						if (info.LabelNames != null && info.LabelNames.Contains(labelName) == true)
							throw new JavaScriptException(this.Engine, "SyntaxError", string.Format("Label '{0}' has already been declared", labelName), 1, this.Source.Path, this.FunctionName);
			}

			// Push the info to the stack.
			this.breakOrContinueStack.Push(new BreakOrContinueInfo()
			{
				LabelNames = labelNames,
				LabelledOnly = labelledOnly,
				BreakTarget = breakTarget,
				ContinueTarget = continueTarget
			});
		}

		/// <summary>
		/// Removes the top-most break or continue information from the stack.
		/// </summary>
		public void PopBreakOrContinueInfo()
		{
			this.breakOrContinueStack.Pop();
		}
		
		public ILLabel GetBreakTarget()
		{
			return GetBreakTarget(null);
		}
		
		/// <summary>
		/// Returns the break target for the statement with the given label, if one is provided, or
		/// the top-most break target otherwise.
		/// </summary>
		/// <param name="labelName"> The label associated with the break target.  Can be
		/// <c>null</c>. </param>
		/// <returns> The break target for the statement with the given label. </returns>
		public ILLabel GetBreakTarget(string labelName)
		{
			if (labelName == null)
			{
				foreach (var info in this.breakOrContinueStack)
				{
					if (info.LabelledOnly == false)
						return info.BreakTarget;
				}
				throw new JavaScriptException(this.Engine, "SyntaxError", "Illegal break statement", 1, this.Source.Path, this.FunctionName);
			}
			else
			{
				foreach (var info in this.breakOrContinueStack)
				{
					if (info.LabelNames != null && info.LabelNames.Contains(labelName) == true)
						return info.BreakTarget;
				}
				throw new JavaScriptException(this.Engine, "SyntaxError", string.Format("Undefined label '{0}'", labelName), 1, this.Source.Path, this.FunctionName);
			}
		}
		
		public ILLabel GetContinueTarget()
		{
			return GetContinueTarget(null);
		}
		
		/// <summary>
		/// Returns the continue target for the statement with the given label, if one is provided, or
		/// the top-most continue target otherwise.
		/// </summary>
		/// <param name="labelName"> The label associated with the continue target.  Can be
		/// <c>null</c>. </param>
		/// <returns> The continue target for the statement with the given label. </returns>
		public ILLabel GetContinueTarget(string labelName)
		{
			if (labelName == null)
			{
				foreach (var info in this.breakOrContinueStack)
				{
					if (info.ContinueTarget != null && info.LabelledOnly == false)
						return info.ContinueTarget;
				}
				throw new JavaScriptException(this.Engine, "SyntaxError", "Illegal continue statement", 1, this.Source.Path, this.FunctionName);
			}
			else
			{
				foreach (var info in this.breakOrContinueStack)
				{
					if (info.LabelNames != null && info.LabelNames.Contains(labelName) == true)
					{
						if (info.ContinueTarget == null)
							throw new JavaScriptException(this.Engine, "SyntaxError", string.Format("The statement with label '{0}' is not a loop", labelName), 1, this.Source.Path, this.FunctionName);
						return info.ContinueTarget;
					}
				}
				throw new JavaScriptException(this.Engine, "SyntaxError", string.Format("Undefined label '{0}'", labelName), 1, this.Source.Path, this.FunctionName);
			}
		}

		/// <summary>
		/// Gets the number of available break or continue targets.  Used to support break or
		/// continue statements within finally blocks.
		/// </summary>
		public int BreakOrContinueStackSize
		{
			get { return this.breakOrContinueStack.Count; }
		}

		/// <summary>
		/// Searches for the given label in the break/continue stack.
		/// </summary>
		/// <param name="label"></param>
		/// <returns> The depth of the label in the stack.  Zero indicates the bottom of the stack.
		/// <c>-1</c> is returned if the label was not found. </returns>
		private int GetBreakOrContinueLabelDepth(ILLabel label)
		{
			if (label == null)
				throw new ArgumentNullException("label");

			int depth = this.breakOrContinueStack.Count - 1;
			foreach (var info in this.breakOrContinueStack)
			{
				if (info.BreakTarget == label)
					return depth;
				if (info.ContinueTarget == label)
					return depth;
				depth --;
			}
			return -1;
		}



		//	 FINALLY SUPPORT
		//_________________________________________________________________________________________

		/// <summary>
		/// Gets or sets a value that indicates whether code generation is occurring within a
		/// try, catch or finally block.
		/// </summary>
		public bool InsideTryCatchOrFinally;
		
		/// <summary>
		/// Gets or sets a delegate that is called when EmitLongJump() is called and the target
		/// label is outside the LongJumpStackSizeThreshold.
		/// </summary>
		public OnLJCallback LongJumpCallback;

		/// <summary>
		/// Gets or sets the depth of the break/continue stack at the start of the finally
		/// statement.
		/// </summary>
		public int LongJumpStackSizeThreshold;

		/// <summary>
		/// Emits code to branch between statements, even if code generation is within a finally
		/// block (where unconditional branches are not allowed).
		/// </summary>
		/// <param name="generator"> The generator to output the CIL to. </param>
		/// <param name="targetLabel"> The label to jump to. </param>
		public void EmitLongJump(ILGenerator generator, ILLabel targetLabel)
		{
			if (this.LongJumpCallback == null)
			{
				// Code generation is not inside a finally block.
				if (this.InsideTryCatchOrFinally == true)
					generator.Leave(targetLabel);
				else
					generator.Branch(targetLabel);
			}
			else
			{
				// The long jump is occurring within a finally block.
				// Check if the target is inside or outside the finally block.
				int depth = this.GetBreakOrContinueLabelDepth(targetLabel);
				if (depth < this.LongJumpStackSizeThreshold)
				{
					// The target label is outside the finally block.  Call the callback to emit
					// the jump.
					this.LongJumpCallback(generator, targetLabel);
				}
				else
				{
					// The target label is inside the finally block.
					if (this.InsideTryCatchOrFinally == true)
						generator.Leave(targetLabel);
					else
						generator.Branch(targetLabel);
				}
			}
		}

	}

}
