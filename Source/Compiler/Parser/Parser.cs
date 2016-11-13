using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Nitrassic.Compiler
{
	/// <summary>
	/// Converts a series of tokens into an abstract syntax tree.
	/// </summary>
	internal sealed class Parser
	{
		internal ScriptEngine engine;
		internal Lexer lexer;
		internal Token nextToken;
		internal bool consumedLineTerminator;
		internal ParserExpressionState expressionState;
		internal Scope initialScope;
		internal Scope currentVarScope;
		internal Scope currentLetScope;
		internal MethodOptimizationHints methodOptimizationHints;
		internal List<string> labelsForCurrentStatement;
		internal Token endToken;
		internal CompilerOptions options;
		internal CodeContext context;
		internal bool strictMode;



		//	 INITIALIZATION
		//_________________________________________________________________________________________

		/// <summary>
		/// Creates a Parser instance with the given lexer supplying the tokens.
		/// </summary>
		/// <param name="engine"> The associated script engine. </param>
		/// <param name="lexer"> The lexical analyser that provides the tokens. </param>
		/// <param name="initialScope"> The initial variable scope. </param>
		/// <param name="options"> Options that influence the compiler. </param>
		/// <param name="context"> The context of the code (global, function or eval). </param>
		public Parser(ScriptEngine engine, Lexer lexer, Scope initialScope,OptimizationInfo optimizationInfo, CompilerOptions options, CodeContext context)
		{
			if (engine == null)
				throw new ArgumentNullException("engine");
			if (lexer == null)
				throw new ArgumentNullException("lexer");
			if (initialScope == null)
				throw new ArgumentNullException("initialScope");
			this.engine = engine;
			this.lexer = lexer;
			this.lexer.ParserExpressionState = ParserExpressionState.Literal;
			SetInitialScope(initialScope);
			this.methodOptimizationHints = new MethodOptimizationHints();
			this.options = options;
			this.context = context;
			this.StrictMode = options.ForceStrictMode;
			OptimizationInfo = optimizationInfo;
			this.Consume();
		}
		
		/// <summary>
		/// Holds current optimization info.
		/// </summary>
		public OptimizationInfo OptimizationInfo;
		
		/// <summary>
		/// Creates a parser that can read the body of a function.
		/// </summary>
		/// <param name="parser"> The parser for the parent context. </param>
		/// <param name="scope"> The function scope. </param>
		/// <returns> A new parser. </returns>
		private static Parser CreateFunctionBodyParser(Parser parser, Scope scope, MethodOptimizationHints methodOptimizationHints)
		{
			var result = (Parser)parser.MemberwiseClone();
			result.SetInitialScope(scope);
			result.context = CodeContext.Function;
			result.endToken = PunctuatorToken.RightBrace;
			return result;
		}



		//	 PROPERTIES
		//_________________________________________________________________________________________

		/// <summary>
		/// Gets the line number of the next token.
		/// </summary>
		public int LineNumber
		{
			get { return this.lexer.LineNumber; }
		}
		
		/// <summary>
		/// Gets the path or URL of the source file.  Can be <c>null</c>.
		/// </summary>
		public string SourcePath
		{
			get { return this.lexer.Source.Path; }
		}

		/// <summary>
		/// Gets or sets a value that indicates whether the parser is operating in strict mode.
		/// </summary>
		public bool StrictMode
		{
			get { return this.strictMode; }
			set
			{
				this.strictMode = value;
				this.lexer.StrictMode = value;
			}
		}

		/// <summary>
		/// The top-level scope.
		/// </summary>
		public Scope BaseScope
		{
			get { return this.initialScope; }
		}

		/// <summary>
		/// Gets optimization information about the code that was parsed (Parse() must be called
		/// first).
		/// </summary>
		public MethodOptimizationHints MethodOptimizationHints
		{
			get { return this.methodOptimizationHints; }
		}



		//	 VARIABLES
		//_________________________________________________________________________________________
	 
		/// <summary>
		/// Throws an exception if the variable name is invalid.
		/// </summary>
		/// <param name="name"> The name of the variable to check. </param>
		internal void ValidateVariableName(string name)
		{
			// In strict mode, the variable name cannot be "eval" or "arguments".
			if (this.StrictMode == true && (name == "eval" || name == "arguments"))
				throw new JavaScriptException(this.engine, "SyntaxError", string.Format("The variable name cannot be '{0}' in strict mode.", name), this.LineNumber, this.SourcePath);

			// Record each occurance of a variable name.
			this.methodOptimizationHints.EncounteredVariable(name);
		}



		//	 TOKEN HELPERS
		//_________________________________________________________________________________________
		
		internal void Consume()
		{
			Consume(ParserExpressionState.Literal);
		}
		
		/// <summary>
		/// Discards the current token and reads the next one.
		/// </summary>
		/// <param name="expressionState"> Indicates whether the next token can be a literal or an
		/// operator. </param>
		internal void Consume(ParserExpressionState expressionState)
		{
			this.expressionState = expressionState;
			this.lexer.ParserExpressionState = expressionState;
			this.consumedLineTerminator = false;
			
			while (true)
			{
				if (expressionState == ParserExpressionState.TemplateContinuation)
                    this.nextToken = this.lexer.ReadStringLiteral('`');
                else
                    this.nextToken = this.lexer.NextToken();
				if ((this.nextToken is WhiteSpaceToken) == false)
					break;
				if (((WhiteSpaceToken)this.nextToken).LineTerminatorCount > 0)
					this.consumedLineTerminator = true;
			}
		}

		/// <summary>
		/// Indicates that the next token is identical to the given one.  Throws an exception if
		/// this is not the case.  Consumes the token.
		/// </summary>
		/// <param name="token"> The expected token. </param>
		internal void Expect(Token token)
		{
			if (this.nextToken == token)
				Consume();
			else
				throw new JavaScriptException(this.engine, "SyntaxError", string.Format("Expected '{0}' but found {1}", token.Text, Token.ToText(this.nextToken)), this.LineNumber, this.SourcePath);
		}

		/// <summary>
		/// Indicates that the next token should be an identifier.  Throws an exception if this is
		/// not the case.  Consumes the token.
		/// </summary>
		/// <returns> The identifier name. </returns>
		internal string ExpectIdentifier()
		{
			var token = this.nextToken;
			if (token is IdentifierToken)
			{
				Consume();
				return ((IdentifierToken)token).Name;
			}
			else
			{
				throw new JavaScriptException(this.engine, "SyntaxError", string.Format("Expected identifier but found {0}", Token.ToText(this.nextToken)), this.LineNumber, this.SourcePath);
			}
		}

		/// <summary>
		/// Returns a value that indicates whether the current position is a valid position to end
		/// a statement.
		/// </summary>
		/// <returns> <c>true</c> if the current position is a valid position to end a statement;
		/// <c>false</c> otherwise. </returns>
		internal bool AtValidEndOfStatement()
		{
			// A statement can be terminator in four ways: by a semi-colon (;), by a right brace (}),
			// by the end of a line or by the end of the program.
			return this.nextToken == PunctuatorToken.Semicolon ||
				this.nextToken == PunctuatorToken.RightBrace ||
				this.consumedLineTerminator == true ||
				this.nextToken == null;
		}

		/// <summary>
		/// Indicates that the next token should end the current statement.  This implies that the
		/// next token is a semicolon, right brace or a line terminator.
		/// </summary>
		internal void ExpectEndOfStatement()
		{
			if (this.nextToken == PunctuatorToken.Semicolon)
				Consume();
			else
			{
				// Automatic semi-colon insertion.
				// If an illegal token is found then a semicolon is automatically inserted before
				// the offending token if one or more of the following conditions is true: 
				// 1. The offending token is separated from the previous token by at least one LineTerminator.
				// 2. The offending token is '}'.
				if (this.consumedLineTerminator == true || this.nextToken == PunctuatorToken.RightBrace)
					return;

				// If the end of the input stream of tokens is encountered and the parser is unable
				// to parse the input token stream as a single complete ECMAScript Program, then a
				// semicolon is automatically inserted at the end of the input stream.
				if (this.nextToken == null)
					return;

				// Otherwise, throw an error.
				throw new JavaScriptException(this.engine, "SyntaxError", string.Format("Expected ';' but found {0}", Token.ToText(this.nextToken)), this.LineNumber, this.SourcePath);
			}
		}

		//	 SCOPE HELPERS
		//_________________________________________________________________________________________

		/// <summary>
		/// Sets the initial scope.
		/// </summary>
		/// <param name="initialScope"> The initial scope </param>
		internal void SetInitialScope(Scope initialScope)
		{
			if (initialScope == null)
				throw new ArgumentNullException("initialScope");
			this.currentLetScope = this.currentVarScope = this.initialScope = initialScope;
		}

		/// <summary>
		/// Helper class to help manage scopes.
		/// </summary>
		internal class ScopeContext : IDisposable
		{
			private readonly Parser parser;
			private readonly Scope previousLetScope;
			private readonly Scope previousVarScope;

			public ScopeContext(Parser parser)
			{
				this.parser = parser;
				previousLetScope = parser.currentLetScope;
				previousVarScope = parser.currentVarScope;
			}

			public void Dispose()
			{
				parser.currentLetScope = previousLetScope;
				parser.currentVarScope = previousVarScope;
			}
		}
		
		internal ScopeContext CreateScopeContext(Scope letScope)
		{
			return CreateScopeContext(letScope,null);
		}
		/// <summary>
		/// Sets the current scope and returns an object which can be disposed to restore the
		/// previous scope.
		/// </summary>
		/// <param name="letScope"> The new let scope. </param>
		/// <param name="varScope"> The new var scope. </param>
		/// <returns> An object which can be disposed to restore the previous scope. </returns>
		internal ScopeContext CreateScopeContext(Scope letScope, Scope varScope)
		{
			if (letScope == null)
				throw new ArgumentNullException("letScope");
			var result = new ScopeContext(this);
			this.currentLetScope = letScope;
			if (varScope != null)
				this.currentVarScope = varScope;
			return result;
		}



		//	 PARSE METHODS
		//_________________________________________________________________________________________

		/// <summary>
		/// Parses javascript source code.
		/// </summary>
		/// <returns> An expression that can be executed to run the program represented by the
		/// source code. </returns>
		public Statement Parse()
		{
			// Read the directive prologue.
			var result = new BlockStatement();
			while (true)
			{
				// Check if we should stop parsing.
				if (this.nextToken == this.endToken)
					break;

				// A directive must start with a string literal token.  Record it now so that the
				// escape sequence and line continuation information is not lost.
				var directiveToken = this.nextToken as StringLiteralToken;
				if (directiveToken == null)
					break;

				// Directives cannot have escape sequences or line continuations.
				if (directiveToken.EscapeSequenceCount != 0 || directiveToken.LineContinuationCount != 0)
					break;

				// If the statement starts with a string literal, it must be an expression.
				var expression = ParseExpression(PunctuatorToken.Semicolon);

				// The statement must be added to the AST so that eval("'test'") works.
				var initialStatement = Labels(new ExpressionStatement(expression));
				result.Statements.Add(initialStatement);

				// In order for the expression to be part of the directive prologue, it must
				// consist solely of a string literal.
				if ((expression is LiteralExpression) == false)
					break;

				// Strict mode directive.
				if (directiveToken.Value == "use strict")
					this.StrictMode = true;

				// Read the end of the statement.  This must happen last so that the lexer has a
				// chance to act on the strict mode flag.
				ExpectEndOfStatement();
			}

			// If this is an eval, and strict mode is on, redefine the scope.
			if (this.StrictMode == true && this.context == CodeContext.Eval)
				SetInitialScope(DeclarativeScope.CreateEvalScope(this.initialScope));

			// Read zero or more regular statements.
			while (true)
			{
				// Check if we should stop parsing.
				if (this.nextToken == this.endToken)
					break;

				// Parse a single statement.
				result.Statements.Add(ParseStatement());
			}

			return result;
		}
		
		internal Statement Labels(Statement labelled)
		{
			
			if(labelsForCurrentStatement!=null)
			{
				labelled=new LabelledStatement(labelsForCurrentStatement, labelled);
				labelsForCurrentStatement = null;
			}
			
			return labelled;
		}
		
		/// <summary>
		/// Parses any statement other than a function declaration.
		/// </summary>
		/// <returns> An expression that represents the statement. </returns>
		internal Statement ParseStatement()
		{
			// Parse the statement.
			return nextToken.ParseNoNewContext(this);
		}
		
		/// <summary>
		/// Parses a block of statements.
		/// </summary>
		/// <returns> A BlockStatement containing the statements. </returns>
		/// <remarks> The value of a block statement is the value of the last statement in the block,
		/// or undefined if there are no statements in the block. </remarks>
		internal Statement ParseBlock()
		{
			// Consume the start brace ({).
			this.Expect(PunctuatorToken.LeftBrace);

			// Read zero or more statements.
			var result = new BlockStatement();
			var labelled = Labels(result);
			
			while (true)
			{
				// Check for the end brace (}).
				if (this.nextToken == PunctuatorToken.RightBrace)
					break;

				// Parse a single statement.
				result.Statements.Add(ParseStatement());
			}

			// Consume the end brace.
			this.Expect(PunctuatorToken.RightBrace);
			return labelled;
		}
		
		/// <summary>
        /// Parses a comma-separated list of function arguments.
        /// </summary>
        /// <param name="endToken"> The token that ends parsing. </param>
        /// <returns> A list of parsed arguments. </returns>
        internal List<ArgVariable> ParseFunctionArguments(Token endToken){
			
			// Read zero or more argument names.
			List<ArgVariable> argumentNames = new List<ArgVariable>();
			
			// always add 'this':
			argumentNames.Add(new ArgVariable("this"));
			
			// Read the first argument name.
			if (this.nextToken != endToken)
			{
				string argumentName = this.ExpectIdentifier();
				ValidateVariableName(argumentName);
				// Arg 1:
				ArgVariable firstArg=new ArgVariable(argumentName);
				firstArg.ArgumentID=1;
				
				argumentNames.Add(firstArg);
			}

			while (true)
			{
				if (this.nextToken == PunctuatorToken.Comma)
				{
					// Consume the comma.
					this.Consume();

					// Read and validate the argument name.
					string argumentName = this.ExpectIdentifier();
					ValidateVariableName(argumentName);
					ArgVariable arg=new ArgVariable(argumentName);
					arg.ArgumentID=argumentNames.Count;
					argumentNames.Add(arg);
				}
				else if (this.nextToken == PunctuatorToken.Colon)
				{
					
					// Consume the colon.
					this.Consume();
					
					string argumentType = this.ExpectIdentifier();
					
					// Get the type by name:
					Library.Prototype proto=this.engine.Prototypes.Get(argumentType);
					
					if(proto==null){
						throw new JavaScriptException(this.engine, "SyntaxError", "Type was not found: '"+argumentType+"'", this.LineNumber, this.SourcePath);
					}
					
					argumentNames[argumentNames.Count-1].Type=proto.Type;
					
				}
				else if (this.nextToken == endToken)
					break;
				else
					throw new JavaScriptException(this.engine, "SyntaxError", "Expected ',' or ')'", this.LineNumber, this.SourcePath);
			}
			
			return argumentNames;
		}
		
		/// <summary>
		/// Parses a function declaration or a function expression.
		/// </summary>
		/// <param name="functionType"> The type of function to parse. </param>
		/// <param name="parentScope"> The parent scope for the function. </param>
		/// <returns> A function expression. </returns>
		internal FunctionExpression ParseFunction(FunctionDeclarationType functionType, Scope parentScope, string functionName)
        {
            // Read the left parenthesis.
            this.Expect(PunctuatorToken.LeftParenthesis);
			
            // Replace scope and methodOptimizationHints.
            var originalScope = this.currentVarScope;
            var originalMethodOptimizationHints = this.methodOptimizationHints;
            var newMethodOptimizationHints = new MethodOptimizationHints();
            this.methodOptimizationHints = newMethodOptimizationHints;
            
            // Read zero or more arguments.
            var arguments = ParseFunctionArguments(PunctuatorToken.RightParenthesis);

			// Create a new scope and assign variables within the function body to the scope.
			bool includeNameInScope = functionType != FunctionDeclarationType.Getter && functionType != FunctionDeclarationType.Setter;
            DeclarativeScope scope = DeclarativeScope.CreateFunctionScope(parentScope, includeNameInScope ? functionName : string.Empty, arguments);
			this.currentVarScope = scope;
			
            // Restore scope and methodOptimizationHints.
            this.methodOptimizationHints = originalMethodOptimizationHints;
            this.currentVarScope = originalScope;

            // Getters must have zero arguments.
            if (functionType == FunctionDeclarationType.Getter && arguments.Count != 0)
                throw new JavaScriptException(this.engine, "SyntaxError", "Getters cannot have arguments", this.LineNumber, this.SourcePath);

            // Setters must have one argument.
            if (functionType == FunctionDeclarationType.Setter && arguments.Count != 1)
                throw new JavaScriptException(this.engine, "SyntaxError", "Setters must have a single argument", this.LineNumber, this.SourcePath);

            // Read the right parenthesis.
            this.Expect(PunctuatorToken.RightParenthesis);
			
            // Since the parser reads one token in advance, start capturing the function body here.
            var bodyTextBuilder = new System.Text.StringBuilder();
            var originalBodyTextBuilder = this.lexer.InputCaptureStringBuilder;
            this.lexer.InputCaptureStringBuilder = bodyTextBuilder;

            // Read the start brace.
            this.Expect(PunctuatorToken.LeftBrace);

            // This context has a nested function.
            this.methodOptimizationHints.HasNestedFunction = true;
			
			// Creat the expression and apply it to the scope:
			FunctionExpression expr=new FunctionExpression();
			scope.Function=expr;
			expr.DeclarationType=functionType;
			
			// Read the function body.
			var functionParser = Parser.CreateFunctionBodyParser(this, scope, methodOptimizationHints);
			var body = functionParser.Parse();
			
            // Transfer state back from the function parser.
            this.nextToken = functionParser.nextToken;
            this.lexer.StrictMode = this.StrictMode;
            this.lexer.InputCaptureStringBuilder = originalBodyTextBuilder;
            if (originalBodyTextBuilder != null)
                originalBodyTextBuilder.Append(bodyTextBuilder);

            
			if (functionType == FunctionDeclarationType.Expression)
            {
                // The end token '}' will be consumed by the parent function.
                if (this.nextToken != PunctuatorToken.RightBrace)
                    throw new JavaScriptException(this.engine, "SyntaxError", "Expected '}'", this.LineNumber, this.SourcePath);
			}
            else
            {
                // Consume the '}'.
                this.Expect(PunctuatorToken.RightBrace);
			}
			
			// Create a new function expression.
			var options = this.options.Clone();
			options.ForceStrictMode = functionParser.StrictMode;
			FunctionMethodGenerator context = new FunctionMethodGenerator(this.engine, scope, functionName,
				includeNameInScope, arguments, body,
				SourcePath, options);
			context.MethodOptimizationHints = functionParser.methodOptimizationHints;
			expr.SetContext(context);
			
			return expr;
        }

		/// <summary>
		/// Parses a statement consisting of an expression or starting with a label.  These two
		/// cases are disambiguated here.
		/// </summary>
		/// <returns> A statement. </returns>
		internal Statement ParseLabelOrExpressionStatement()
		{
			
			// Parse the statement as though it was an expression - but stop if there is an unexpected colon.
			var expression = ParseExpression(PunctuatorToken.Semicolon, PunctuatorToken.Colon);

			if (this.nextToken == PunctuatorToken.Colon && expression is NameExpression)
			{
				// The expression is actually a label.

				// Extract the label name.
				var labelName = ((NameExpression)expression).Name;
				
				if(labelsForCurrentStatement == null)
				{
					labelsForCurrentStatement = new List<string>();
				}
				
				this.labelsForCurrentStatement.Add(labelName);

				// Read past the colon.
				this.Expect(PunctuatorToken.Colon);

				// Read the rest of the statement.
				return nextToken.ParseNoNewContext(this);
			}
			else
			{

				// Consume the end of the statement.
				this.ExpectEndOfStatement();

				// Create a new expression statement.
				var result = Labels(new ExpressionStatement(expression));
				
				return result;
			}
		}
		
		//	 EXPRESSION PARSER
		//_________________________________________________________________________________________
		
		/// <summary>
		/// Parses a javascript expression.
		/// </summary>
		/// <param name="endTokens"> One or more tokens that indicate the end of the expression. </param>
		/// <returns> An expression tree that represents the expression. </returns>
		internal Expression ParseExpression(params Token[] endTokens)
		{
			// The root of the expression tree.
			Expression root = null;

			// The active operator, i.e. the one last encountered.
			OperatorExpression unboundOperator = null;

			while (this.nextToken != null)
			{
				if ((this.nextToken is LiteralToken && (!(this.nextToken is TemplateLiteralToken) || this.expressionState == ParserExpressionState.Literal)) ||
					this.nextToken is IdentifierToken ||
					this.nextToken == KeywordToken.Function ||
					this.nextToken == KeywordToken.This ||
					this.nextToken == PunctuatorToken.LeftBrace ||
					(this.nextToken == PunctuatorToken.LeftBracket && this.expressionState == ParserExpressionState.Literal) ||
					(this.nextToken is KeywordToken && unboundOperator != null && unboundOperator.OperatorType == OperatorType.MemberAccess && this.expressionState == ParserExpressionState.Literal))
				{
					// If a literal was found where an operator was expected, insert a semi-colon
					// automatically (if this would fix the error and a line terminator was
					// encountered) or throw an error.
					if (this.expressionState != ParserExpressionState.Literal)
					{
						// Check for automatic semi-colon insertion.
						if (Array.IndexOf(endTokens, PunctuatorToken.Semicolon) >= 0 && this.consumedLineTerminator == true)
							break;
                        // Check for "of" contextual keyword.
                        if (Array.IndexOf(endTokens, IdentifierToken.Of) >= 0)
                            break;
						throw new JavaScriptException(this.engine, "SyntaxError", string.Format("Expected operator but found {0}", Token.ToText(this.nextToken)), this.LineNumber, this.SourcePath);
					}

					// New in ECMAScript 5 is the ability to use keywords as property names.
					if ((this.nextToken is KeywordToken || (this.nextToken is LiteralToken && ((LiteralToken)this.nextToken).IsKeyword == true)) &&
						unboundOperator != null &&
						unboundOperator.OperatorType == OperatorType.MemberAccess &&
						this.expressionState == ParserExpressionState.Literal)
					{
						this.nextToken = IdentifierToken.Create(this.nextToken.Text);
					}
					
					Expression terminal;
					if (this.nextToken is LiteralToken)
					{
						if (this.nextToken is TemplateLiteralToken && ((TemplateLiteralToken)this.nextToken).SubstitutionFollows)
                        {
                            // Handle template literals with substitutions (template literals
                            // without substitutions are parsed the same as regular strings).
                            terminal = ParseTemplateLiteral();
                        }
                        else
                        {
                            // Otherwise, it's a simple literal.
                            terminal = new LiteralExpression(((LiteralToken)this.nextToken).Value);
                        }
					}
					else if (this.nextToken is IdentifierToken)
					{
						// If the token is an identifier, convert it to a NameExpression.
						var identifierName = ((IdentifierToken)this.nextToken).Name;
						terminal = new NameExpression(this.currentVarScope, identifierName);

						// Record each occurance of a variable name.
						if (unboundOperator == null || unboundOperator.OperatorType != OperatorType.MemberAccess)
							this.methodOptimizationHints.EncounteredVariable(identifierName);
					}
					else if (this.nextToken == KeywordToken.This)
					{
						// Convert "this" to an expression.
						terminal = new NameExpression(this.currentVarScope, "this");
						
						// Add method optimization info.
						this.methodOptimizationHints.HasThis = true;
					}
					else if (this.nextToken == PunctuatorToken.LeftBracket)
						// Array literal.
						terminal = ParseArrayLiteral();
					else if (this.nextToken == PunctuatorToken.LeftBrace)
						// Object literal.
						terminal = ParseObjectLiteral();
					else if (this.nextToken == KeywordToken.Function)
						terminal = ParseFunctionExpression();
					else
						throw new InvalidOperationException("Unsupported literal type.");

					// Push the literal to the most recent unbound operator, or, if there is none, to
					// the root of the tree.
					if (root == null)
					{
						// This is the first term in an expression.
						root = terminal;
					}
					else
					{
						Debug.Assert(unboundOperator != null && unboundOperator.AcceptingOperands == true);
						unboundOperator.Push(terminal);
					}
				}
				else if (this.nextToken is PunctuatorToken ||
                         this.nextToken is KeywordToken ||
                        (this.nextToken is TemplateLiteralToken && this.expressionState == ParserExpressionState.Operator))
                {
					// The token is an operator (o1).
					Operator newOperator = null;
					if(expressionState == ParserExpressionState.Operator)
					{
						newOperator=nextToken.PostfixOperator;
					}
					else
					{
						newOperator=nextToken.PrefixOperator;
					}
					
					// Make sure the token is actually an operator and not just a random keyword.
					if (newOperator == null)
					{
						// Check if the token is an end token, for example a semi-colon.
						if (Array.IndexOf(endTokens, this.nextToken) >= 0)
							break;
						// Check for automatic semi-colon insertion.
						if (Array.IndexOf(endTokens, PunctuatorToken.Semicolon) >= 0 && (this.consumedLineTerminator == true || this.nextToken == PunctuatorToken.RightBrace))
							break;
						throw new JavaScriptException(this.engine, "SyntaxError", string.Format("Unexpected token {0} in expression.", Token.ToText(this.nextToken)), this.LineNumber, this.SourcePath);
					}

					// Post-fix increment and decrement cannot have a line terminator in between
					// the operator and the operand.
					if (this.consumedLineTerminator == true && (newOperator == Operator.PostIncrement || newOperator == Operator.PostDecrement))
						break;

					// There are four possibilities:
					// 1. The token is the second of a two-part operator (for example, the ':' in a
					//	conditional operator.  In this case, we need to go up the tree until we find
					//	an instance of the operator and make that the active unbound operator.
					if (this.nextToken == newOperator.SecondaryToken)
					{
						// Traverse down the tree looking for the parent operator that corresponds to
						// this token.
						OperatorExpression parentExpression = null;
						var node = root as OperatorExpression;
						while (node != null)
						{
							if (node.Operator.Token == newOperator.Token && node.SecondTokenEncountered == false)
								parentExpression = node;
							if (node == unboundOperator)
								break;
							node = node.RightBranch;
						}

						// If the operator was not found, then this is a mismatched token, unless
						// it is the end token.  For example, if an unbalanced right parenthesis is
						// found in an if statement then it is merely the end of the test expression.
						if (parentExpression == null)
						{
							// Check if the token is an end token, for example a right parenthesis.
							if (Array.IndexOf(endTokens, this.nextToken) >= 0)
								break;
							// Check for automatic semi-colon insertion.
							if (Array.IndexOf(endTokens, PunctuatorToken.Semicolon) >= 0 && this.consumedLineTerminator == true)
								break;
							throw new JavaScriptException(this.engine, "SyntaxError", "Mismatched closing token in expression.", this.LineNumber, this.SourcePath);
						}

						// Mark that we have seen the closing token.
						unboundOperator = parentExpression;
						unboundOperator.SecondTokenEncountered = true;
					}
					else
					{
						// Check if the token is an end token, for example the comma in a variable
						// declaration.
						if (Array.IndexOf(endTokens, this.nextToken) >= 0)
						{
							// But make sure the token isn't inside an operator.
							// For example, in the expression "var x = f(a, b)" the comma does not
							// indicate the start of a new variable clause because it is inside the
							// function call operator.
							bool insideOperator = false;
							var node = root as OperatorExpression;
							while (node != null)
							{
								if (node.Operator.SecondaryToken != null && node.SecondTokenEncountered == false)
									insideOperator = true;
								if (node == unboundOperator)
									break;
								node = node.RightBranch;
							}
							if (insideOperator == false)
								break;
						}

						// All the other situations involve the creation of a new operator.
						var newExpression = OperatorExpression.FromOperator(newOperator);

						// 2. The new operator is a prefix operator.  The new operator becomes an operand
						//	of the previous operator.
						if (newOperator.HasLHSOperand == false)
						{
							if (root == null)
								// "!"
								root = newExpression;
							else if (unboundOperator != null && unboundOperator.AcceptingOperands == true)
							{
								// "5 + !"
								unboundOperator.Push(newExpression);
							}
							else
							{
								// "5 !" or "5 + 5 !"
								// Check for automatic semi-colon insertion.
								if (Array.IndexOf(endTokens, PunctuatorToken.Semicolon) >= 0 && this.consumedLineTerminator == true)
									break;
								throw new JavaScriptException(this.engine, "SyntaxError", "Invalid use of prefix operator.", this.LineNumber, this.SourcePath);
							}
						}
						else
						{
							// Search up the tree for an operator that has a lower precedence.
							// Because we don't store the parent link, we have to traverse down the
							// tree and take the last one we find instead.
							OperatorExpression lowPrecedenceOperator = null;
							if (unboundOperator == null ||
								(newOperator.Associativity == OperatorAssociativity.LeftToRight && unboundOperator.Precedence < newOperator.Precedence) ||
								(newOperator.Associativity == OperatorAssociativity.RightToLeft && unboundOperator.Precedence <= newOperator.Precedence))
							{
								// Performance optimization: look at the previous operator first.
								lowPrecedenceOperator = unboundOperator;
							}
							else
							{
								// Search for a lower precedence operator by traversing the tree.
								var node = root as OperatorExpression;
								while (node != null && node != unboundOperator)
								{
									if ((newOperator.Associativity == OperatorAssociativity.LeftToRight && node.Precedence < newOperator.Precedence) ||
										(newOperator.Associativity == OperatorAssociativity.RightToLeft && node.Precedence <= newOperator.Precedence))
										lowPrecedenceOperator = node;
									node = node.RightBranch;
								}
							}

							if (lowPrecedenceOperator == null)
							{
								// 3. The new operator has a lower precedence (or if the associativity is left to
								//	right, a lower or equal precedence) than all the parent operators.  The new
								//	operator goes to the root of the tree and the previous operator becomes the
								//	first operand for the new operator.
								if (root != null)
									newExpression.Push(root);
								root = newExpression;
							}
							else
							{
								// 4. Otherwise, the new operator can steal the last operand from the previous
								//	operator and then put itself in the place of that last operand.
								if (lowPrecedenceOperator.OperandCount == 0)
								{
									// "! ++"
									// Check for automatic semi-colon insertion.
									if (Array.IndexOf(endTokens, PunctuatorToken.Semicolon) >= 0 && this.consumedLineTerminator == true)
										break;
									throw new JavaScriptException(this.engine, "SyntaxError", "Invalid use of prefix operator.", this.LineNumber, this.SourcePath);
								}
								newExpression.Push(lowPrecedenceOperator.Pop());
								lowPrecedenceOperator.Push(newExpression);
							}
						}

						unboundOperator = newExpression;
					}
				}
				else
				{
					throw new JavaScriptException(this.engine, "SyntaxError", string.Format("Unexpected token {0} in expression", Token.ToText(this.nextToken)), this.LineNumber, this.SourcePath);
				}

				// Read the next token.
				this.Consume(root != null && (unboundOperator == null || unboundOperator.AcceptingOperands == false) ? ParserExpressionState.Operator : ParserExpressionState.Literal);
			}

			// Empty expressions are invalid.
			if (root == null)
				throw new JavaScriptException(this.engine, "SyntaxError", string.Format("Expected an expression but found {0} instead", Token.ToText(this.nextToken)), this.LineNumber, this.SourcePath);
			
			// A literal is the next valid expression token.
			this.expressionState = ParserExpressionState.Literal;
			this.lexer.ParserExpressionState = expressionState;

			// Resolve all the unbound operators into real operators.
			return root;
		}
		
        /// <summary>
        /// Checks the given AST is valid.
        /// </summary>
        /// <param name="root"> The root of the AST. </param>
        private void CheckASTValidity(Expression root)
        {
            // Push the root expression onto a stack.
            Stack<Expression> stack = new Stack<Expression>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                // Pop the next expression from the stack.
                var expression = stack.Pop() as OperatorExpression;
                
                // Only operator expressions are checked for validity.
                if (expression == null)
                    continue;

                // Check the operator expression has the right number of operands.
                if (expression.Operator.IsValidNumberOfOperands(expression.OperandCount) == false)
                    throw new JavaScriptException(this.engine, "SyntaxError", "Wrong number of operands", this.LineNumber, this.SourcePath);

                // Check the operator expression is closed.
                if (expression.Operator.SecondaryToken != null && expression.SecondTokenEncountered == false)
                    throw new JavaScriptException(this.engine, "SyntaxError", string.Format("Missing closing token '{0}'", expression.Operator.SecondaryToken.Text), this.LineNumber, this.SourcePath);

                // Check the child nodes.
                for (int i = 0; i < expression.OperandCount; i++)
                    stack.Push(expression.GetRawOperand(i));
            }
        }

		/// <summary>
		/// Parses an array literal (e.g. "[1, 2]").
		/// </summary>
		/// <returns> A literal expression that represents the array literal. </returns>
		private LiteralExpression ParseArrayLiteral()
		{
			// Read past the initial '[' token.
			Debug.Assert(this.nextToken == PunctuatorToken.LeftBracket);
			this.Consume();

			var items = new List<Expression>();
			while (true)
			{
				// If the next token is ']', then the array literal is complete.
				if (this.nextToken == PunctuatorToken.RightBracket)
					break;

				// If the next token is ',', then the array element is undefined.
				if (this.nextToken == PunctuatorToken.Comma)
					items.Add(null);
				else
					// Otherwise, read the next item in the array.
					items.Add(ParseExpression(PunctuatorToken.Comma, PunctuatorToken.RightBracket));

				// Read past the comma.
				Debug.Assert(this.nextToken == PunctuatorToken.Comma || this.nextToken == PunctuatorToken.RightBracket);
				if (this.nextToken == PunctuatorToken.Comma)
					this.Consume();
			}

			// The end token ']' will be consumed by the parent function.
			Debug.Assert(this.nextToken == PunctuatorToken.RightBracket);

			return new LiteralExpression(items);
		}
		
		/// <summary>
		/// Parses an object literal (e.g. "{a: 5}").
		/// </summary>
		/// <returns> A literal expression that represents the object literal. </returns>
		private LiteralExpression ParseObjectLiteral()
        {
            // Read past the initial '{' token.
            Debug.Assert(this.nextToken == PunctuatorToken.LeftBrace);
            this.Consume();

            var properties = new List<KeyValuePair<Expression, Expression>>();
            while (true)
            {
                // If the next token is '}', then the object literal is complete.
                if (this.nextToken == PunctuatorToken.RightBrace)
                    break;

                // Read the next property name.
                PropertyNameType nameType;
                Expression propertyName = ReadPropertyNameExpression(out nameType);

                // Check if this is a getter or setter.
                Expression propertyValue;
                if (this.nextToken != PunctuatorToken.Colon && (nameType == PropertyNameType.Get || nameType == PropertyNameType.Set))
                {
                    // Getters and setters can have any name that is allowed of a property.
                    PropertyNameType nameType2;
                    propertyName = ReadPropertyNameExpression(out nameType2);

                    // Parse the function name and body.
                    string nameAsString = propertyName is LiteralExpression ? (string)((LiteralExpression)propertyName).Value : string.Empty;
                    var function = ParseFunction(nameType == PropertyNameType.Get ? FunctionDeclarationType.Getter : FunctionDeclarationType.Setter, this.currentVarScope, nameAsString);

                    // Add the getter or setter to the list of properties to set.
                    properties.Add(new KeyValuePair<Expression, Expression>(propertyName, function));
                }
                else if (nameType != PropertyNameType.Expression && (this.nextToken == PunctuatorToken.Comma || this.nextToken == PunctuatorToken.RightBrace))
                {
                    // This is a shorthand property e.g. "var a = 1, b = 2, c = { a, b }" is the
                    // same as "var a = 1, b = 2, c = { a: a, b: b }".
                    string nameAsString = (string)((LiteralExpression)propertyName).Value;
                    properties.Add(new KeyValuePair<Expression, Expression>(propertyName, new NameExpression(this.currentVarScope, nameAsString)));
                }
                else if (this.nextToken == PunctuatorToken.LeftParenthesis)
                {
                    // This is a shorthand function e.g. "var a = { b() { return 2; } }" is the
                    // same as "var a = { b: function() { return 2; } }".

                    // Parse the function.
                    string nameAsString = propertyName is LiteralExpression ? (string)((LiteralExpression)propertyName).Value : string.Empty;
                    var function = ParseFunction(FunctionDeclarationType.Expression, this.currentVarScope, nameAsString);

                    // Strangely enough, if declarationType is Expression then the last right
                    // brace ('}') is not consumed.
                    this.Expect(PunctuatorToken.RightBrace);

                    // Add the function to the list of properties to set.
                    properties.Add(new KeyValuePair<Expression, Expression>(propertyName, function));
                }
                else
                {
                    // This is a regular property.
                    
                    // Read the colon.
                    this.Expect(PunctuatorToken.Colon);

                    // Now read the property value.
                    propertyValue = ParseExpression(PunctuatorToken.Comma, PunctuatorToken.RightBrace);

                    // Add the property to the list.
                    properties.Add(new KeyValuePair<Expression, Expression>(propertyName, propertyValue));
                }

                // Read past the comma.
                Debug.Assert(this.nextToken == PunctuatorToken.Comma || this.nextToken == PunctuatorToken.RightBrace);
                if (this.nextToken == PunctuatorToken.Comma)
                    this.Consume();
            }

            // The end token '}' will be consumed by the parent function.
            Debug.Assert(this.nextToken == PunctuatorToken.RightBrace);

            return new LiteralExpression(properties);
        }
		
        /// <summary>
        /// Distinguishes between the different ways of specifying a property name.
        /// </summary>
        internal enum PropertyNameType
        {
            Name,
            Get,
            Set,
            Expression,
        }
		
        /// <summary>
        /// Reads a property name, used in object literals.
        /// </summary>
        /// <param name="nameType"> Identifies the particular way the property was specified. </param>
        /// <returns> An expression that evaluates to the property name. </returns>
        internal Expression ReadPropertyNameExpression(out PropertyNameType nameType)
        {
            Expression result;
            if (this.nextToken is LiteralToken)
            {
                // The property name can be a string or a number or (in ES5) a keyword.
                if (((LiteralToken)this.nextToken).IsKeyword == true)
                {
                    // false, true or null.
                    result = new LiteralExpression(this.nextToken.Text);
                }
                else
                {
                    object literalValue = ((LiteralToken)this.nextToken).Value;
                    if ((literalValue is string || literalValue is double || literalValue is int) == false)
                        throw new JavaScriptException(this.engine, "SyntaxError", string.Format("Expected property name but found {0}", Token.ToText(this.nextToken)), this.LineNumber, this.SourcePath);
                    result = new LiteralExpression(((LiteralToken)this.nextToken).Value.ToString());
                }
                nameType = PropertyNameType.Name;
            }
            else if (this.nextToken is IdentifierToken)
            {
                // An identifier is also okay.
                var name = ((IdentifierToken)this.nextToken).Name;
                if (name == "get")
                    nameType = PropertyNameType.Get;
                else if (name == "set")
                    nameType = PropertyNameType.Set;
                else
                    nameType = PropertyNameType.Name;
                result = new LiteralExpression(name);
            }
            else if (this.nextToken is KeywordToken)
            {
                // In ES5 a keyword is also okay.
                result = new LiteralExpression(((KeywordToken)this.nextToken).Name);
                nameType = PropertyNameType.Name;
            }
            else if (this.nextToken == PunctuatorToken.LeftBracket)
            {
                // ES6 computed property.
                this.Consume();
                result = ParseExpression(PunctuatorToken.RightBracket);
                nameType = PropertyNameType.Expression;
            }
            else
                throw new JavaScriptException(this.engine, "SyntaxError", string.Format("Expected property name but found {0}", Token.ToText(this.nextToken)), this.LineNumber, this.SourcePath);

            // Consume the token.
            this.Consume();

            // Return the property name.
            return result;
        }
		
		/// <summary>
		/// Parses a function expression.
		/// </summary>
		/// <returns> A function expression. </returns>
		internal FunctionExpression ParseFunctionExpression()
        {
            // Consume the function keyword.
            this.Expect(KeywordToken.Function);

            // The function name is optional for function expressions.
            var functionName = string.Empty;
            if (this.nextToken is IdentifierToken)
            {
                functionName = this.ExpectIdentifier();
                ValidateVariableName(functionName);
            }

            // Parse the rest of the function.
            return ParseFunction(FunctionDeclarationType.Expression, this.currentVarScope, functionName);
        }
		
        /// <summary>
        /// Parses a template literal (e.g. `Bought ${count} items`).
        /// </summary>
        /// <returns> An expression that represents the template literal. </returns>
        internal Expression ParseTemplateLiteral()
        {
            Debug.Assert(this.nextToken is TemplateLiteralToken);

            var strings = new List<string>();
            var values = new List<Expression>();
            var rawStrings = new List<string>();
            while (true)
            {
                // Record the template literal token value.
                var templateLiteralToken = (TemplateLiteralToken)this.nextToken;
                strings.Add(templateLiteralToken.Value);
                rawStrings.Add(templateLiteralToken.RawText);

                // Check if we are at the end of the template literal.
                if (templateLiteralToken.SubstitutionFollows == false)
                    break;

                // Parse the substitution.
                this.Consume();     // Consume the template literal token.
                values.Add(ParseExpression(PunctuatorToken.RightBrace));

                // Consume the right brace, and continue scanning the template literal.
                // The TemplateContinuation option here indicates that the lexer should immediately
                // start constructing a template literal token.
                this.Consume(ParserExpressionState.TemplateContinuation);
            }

            // If this is an untagged template literal, return a UntaggedTemplateExpression.
            return new TemplateLiteralExpression(strings, values, rawStrings);
        }
		
	}

}