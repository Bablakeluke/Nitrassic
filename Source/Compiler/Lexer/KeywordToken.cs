using System;
using System.Collections.Generic;

namespace Nitrassic.Compiler
{

	/// <summary>
	/// Represents a reserved word in the source code.
	/// </summary>
	internal class KeywordToken : Token
	{
		/// <summary>
		/// Creates a new KeywordToken instance.
		/// </summary>
		/// <param name="name"> The keyword name. </param>
		public KeywordToken(string name)
		{
			if (name == null)
				throw new ArgumentNullException("name");
			this.Name = name;
		}

		/// <summary>
		/// Gets the name of the identifier.
		/// </summary>
		public string Name;

		// Keywords.
		public readonly static KeywordToken Break = new BreakToken();
		public readonly static KeywordToken Case = new KeywordToken("case");
		public readonly static KeywordToken Catch = new KeywordToken("catch");
		public readonly static KeywordToken Continue = new ContinueToken();
		public readonly static KeywordToken Debugger = new DebuggerToken();
		public readonly static KeywordToken Default = new KeywordToken("default");
		public readonly static KeywordToken Delete = new KeywordToken("delete");
		public readonly static KeywordToken Do = new DoToken();
		public readonly static KeywordToken Else = new KeywordToken("else");
		public readonly static KeywordToken Finally = new KeywordToken("finally");
		public readonly static KeywordToken For = new ForToken();
		public readonly static KeywordToken Function = new FunctionToken();
		public readonly static KeywordToken If = new IfToken();
		public readonly static KeywordToken In = new KeywordToken("in");
		public readonly static KeywordToken InstanceOf = new KeywordToken("instanceof");
		public readonly static KeywordToken New = new KeywordToken("new");
		public readonly static KeywordToken Return = new ReturnToken();
		public readonly static KeywordToken Switch = new SwitchToken();
		public readonly static KeywordToken This = new KeywordToken("this");
		public readonly static KeywordToken Throw = new ThrowToken();
		public readonly static KeywordToken Try = new TryToken();
		public readonly static KeywordToken Typeof = new KeywordToken("typeof");
		public readonly static KeywordToken Var = new VarToken();
		public readonly static KeywordToken Void = new KeywordToken("void");
		public readonly static KeywordToken While = new WhileToken();
		public readonly static KeywordToken With = new WithToken();

		// ECMAScript 5 reserved words.
		public readonly static KeywordToken Class = new KeywordToken("class");
		public readonly static KeywordToken Const = new ConstToken();
		public readonly static KeywordToken Enum = new KeywordToken("enum");
		public readonly static KeywordToken Export = new KeywordToken("export");
		public readonly static KeywordToken Extends = new KeywordToken("extends");
		public readonly static KeywordToken Import = new KeywordToken("import");
		public readonly static KeywordToken Super = new KeywordToken("super");
		
		// Strict-mode reserved words.
		public readonly static KeywordToken Implements = new KeywordToken("implements");
		public readonly static KeywordToken Interface = new KeywordToken("interface");
		public readonly static KeywordToken Let = new LetToken();
		public readonly static KeywordToken Package = new KeywordToken("package");
		public readonly static KeywordToken Private = new KeywordToken("private");
		public readonly static KeywordToken Protected = new KeywordToken("protected");
		public readonly static KeywordToken Public = new KeywordToken("public");
		public readonly static KeywordToken Static = new KeywordToken("static");
		public readonly static KeywordToken Yield = new KeywordToken("yield");

		// ECMAScript 3 reserved words.
		public readonly static KeywordToken Abstract = new KeywordToken("abstract");
		public readonly static KeywordToken Boolean = new KeywordToken("boolean");
		public readonly static KeywordToken Byte = new KeywordToken("byte");
		public readonly static KeywordToken Char = new KeywordToken("char");
		public readonly static KeywordToken Double = new KeywordToken("double");
		public readonly static KeywordToken Final = new KeywordToken("final");
		public readonly static KeywordToken Float = new KeywordToken("float");
		public readonly static KeywordToken Goto = new KeywordToken("goto");
		public readonly static KeywordToken Int = new KeywordToken("int");
		public readonly static KeywordToken Long = new KeywordToken("long");
		public readonly static KeywordToken Native = new KeywordToken("native");
		public readonly static KeywordToken Short = new KeywordToken("short");
		public readonly static KeywordToken Synchronized = new KeywordToken("synchronized");
		public readonly static KeywordToken Throws = new KeywordToken("throws");
		public readonly static KeywordToken Transient = new KeywordToken("transient");
		public readonly static KeywordToken Volatile = new KeywordToken("volatile");

		// Base keywords.
		private readonly static Token[] keywords = new Token[]
		{
			Break,
			Case,
			Catch,
			Continue,
			Debugger,
			Default,
			Delete,
			Do,
			Else,
			Finally,
			For,
			Function,
			If,
			In,
			InstanceOf,
			Let,
			New,
			Return,
			Switch,
			This,
			Throw,
			Try,
			Typeof,
			Var,
			Void,
			While,
			With,

			// Literal keywords.
			LiteralToken.True,
			LiteralToken.False,
			LiteralToken.Null,

			// Reserved keywords.
			Class,
			Const,
			Enum,
			Export,
			Extends,
			Import,
			Super,
		};

		// Reserved words (in strict mode).
		private readonly static Token[] strictModeReservedWords = new Token[]
		{
			Implements,
			Interface,
			Package,
			Private,
			Protected,
			Public,
			Static,
			Yield,
		};
		
		// The actual lookup tables are the result of combining two of the lists above.
		private static Dictionary<string, Token> ecmaScript5LookupTable;
		private static Dictionary<string, Token> strictModeLookupTable;

		/// <summary>
		/// Creates a token from the given string.
		/// </summary>
		/// <param name="text"> The text. </param>
		/// <param name="strictMode"> <c>true</c> if the lexer is operating in strict mode;
		/// <c>false</c> otherwise. </param>
		/// <returns> The token corresponding to the given string, or <c>null</c> if the string
		/// does not represent a valid token. </returns>
		public static Token FromString(string text, bool strictMode)
		{
			// Determine the lookup table to use.
			Dictionary<string, Token> lookupTable;
			if (!strictMode)
			{
				// Initialize the ECMAScript 5 lookup table, if it hasn't already been intialized.
				if (ecmaScript5LookupTable == null)
				{
					lookupTable = InitializeLookupTable(new Token[0]);
					System.Threading.Thread.MemoryBarrier();
					ecmaScript5LookupTable = lookupTable;
				}
				lookupTable = ecmaScript5LookupTable;
			}
			else
			{
				// Initialize the strict mode lookup table, if it hasn't already been intialized.
				if (strictModeLookupTable == null)
				{
					lookupTable = InitializeLookupTable(strictModeReservedWords);
					System.Threading.Thread.MemoryBarrier();
					strictModeLookupTable = lookupTable;
				}
				lookupTable = strictModeLookupTable;
			}

			// Look up the keyword in the lookup table.
			Token result;
			if (lookupTable.TryGetValue(text, out result) == true)
				return result;

			// If the text wasn't found, it is an identifier instead.
			return IdentifierToken.Create(text);
		}

		/// <summary>
		/// Initializes a lookup table by combining the base list with a second list of keywords.
		/// </summary>
		/// <param name="additionalKeywords"> A list of additional keywords. </param>
		/// <returns> A lookup table. </returns>
		private static Dictionary<string, Token> InitializeLookupTable(Token[] additionalKeywords)
		{
			var result = new Dictionary<string, Token>(keywords.Length + additionalKeywords.Length);
			foreach (var token in keywords)
				result.Add(token.Text, token);
			foreach (var token in additionalKeywords)
				result.Add(token.Text, token);
			return result;
		}

		/// <summary>
		/// Gets a string that represents the token in a parseable form.
		/// </summary>
		public override string Text
		{
			get { return this.Name; }
		}
	}
	
	internal class DebuggerToken : KeywordToken
	{
		
		internal DebuggerToken():base("debugger")
		{}
		
		public override Statement ParseNoNewContext(Parser parser)
		{
			
			var result = parser.Labels(new DebuggerStatement());
			
			// Consume the debugger keyword.
			parser.Expect(KeywordToken.Debugger);

			// Consume the end of the statement.
			parser.ExpectEndOfStatement();
			
			return result;
			
		}
		
	}
	
	internal class ThrowToken : KeywordToken
	{
		
		internal ThrowToken():base("throw")
		{}
		
		public override Statement ParseNoNewContext(Parser parser)
		{
			var result = new ThrowStatement();
			var labelled = parser.Labels(result);
			
			// Consume the throw keyword.
			parser.Expect(KeywordToken.Throw);

			// A line terminator is not allowed here.
			if (parser.consumedLineTerminator == true)
				throw new JavaScriptException(parser.engine, "SyntaxError", "Illegal newline after throw", parser.LineNumber, parser.SourcePath);

			// Parse the expression to throw.
			result.Value = parser.ParseExpression(PunctuatorToken.Semicolon);

			// Consume the end of the statement.
			parser.ExpectEndOfStatement();
			
			return labelled;
		}
		
	}
	
	internal class FunctionToken : KeywordToken
	{
		
		internal FunctionToken():base("function")
		{}
		
		public override Statement ParseNoNewContext(Parser parser)
		{
			
			// Consume the function keyword.
            parser.Expect(KeywordToken.Function);

            // Read the function name.
            var functionName = parser.ExpectIdentifier();
            parser.ValidateVariableName(functionName);

            // Parse the function declaration.
            var expression = parser.ParseFunction(FunctionDeclarationType.Declaration, parser.initialScope, functionName);
			
			// Add the function to the top-level scope.
			Type vType=expression.GetResultType(parser.OptimizationInfo);
			
			Variable variable=parser.initialScope.AddVariable(expression.FunctionName, vType, expression);
			
			// Try setting a constant value:
			variable.TrySetConstant(expression.Context);
			
            // Function declarations do nothing at the point of declaration - everything happens
            // at the top of the function/global code.
            return parser.Labels(new EmptyStatement());
			
		}
		
	}
	
	internal class IfToken : KeywordToken
	{
		
		internal IfToken():base("if")
		{}
		
		public override Statement ParseNoNewContext(Parser parser)
		{
			
			var result = new IfStatement();
			var labelled = parser.Labels(result);
			
			// Consume the if keyword.
			parser.Expect(KeywordToken.If);

			// Read the left parenthesis.
			parser.Expect(PunctuatorToken.LeftParenthesis);
			
			// Parse the condition.
			result.Condition = parser.ParseExpression(PunctuatorToken.RightParenthesis);
			
			// Read the right parenthesis.
			parser.Expect(PunctuatorToken.RightParenthesis);

			// Read the statements that will be executed when the condition is true.
			result.IfClause = parser.ParseStatement();

			// Optionally, read the else statement.
			if (parser.nextToken == KeywordToken.Else)
			{
				// Consume the else keyword.
				parser.Consume();

				// Read the statements that will be executed when the condition is false.
				result.ElseClause = parser.ParseStatement();
			}

			return labelled;
		}
		
	}
	
	internal class DoToken : KeywordToken
	{
		
		internal DoToken():base("do")
		{}
		
		public override Statement ParseNoNewContext(Parser parser)
		{
			var result = new DoWhileStatement();
			var labelled = parser.Labels(result);
			
			// Consume the do keyword.
			parser.Expect(KeywordToken.Do);

			// Read the statements that will be executed in the loop body.
			result.Body = parser.ParseStatement();

			// Read the while keyword.
			parser.Expect(KeywordToken.While);

			// Read the left parenthesis.
			parser.Expect(PunctuatorToken.LeftParenthesis);
			
			result.ConditionStatement = new ExpressionStatement(parser.ParseExpression(PunctuatorToken.RightParenthesis));
			
			// Read the right parenthesis.
			parser.Expect(PunctuatorToken.RightParenthesis);

			// Consume the end of the statement.
			parser.ExpectEndOfStatement();

			return labelled;
		}
		
	}
	
	internal class ContinueToken : KeywordToken
	{
		
		internal ContinueToken():base("continue")
		{}
		
		public override Statement ParseNoNewContext(Parser parser)
		{
			var result = new ContinueStatement();
			var labelled = parser.Labels(result);
			
			// Consume the continue keyword.
			parser.Expect(KeywordToken.Continue);

			// The continue statement can have an optional label to jump to.
			if (!parser.AtValidEndOfStatement())
			{
				// continue [label]

				// Read the label name.
				result.Label = parser.ExpectIdentifier();
			}

			// Consume the semi-colon, if there was one.
			parser.ExpectEndOfStatement();
			
			return labelled;
		}
		
	}
	
	internal class BreakToken : KeywordToken
	{
		
		internal BreakToken():base("break")
		{}
		
		public override Statement ParseNoNewContext(Parser parser)
		{
			
			var result = new BreakStatement();
			var labelled = parser.Labels(result);
			
			// Consume the break keyword.
			parser.Expect(KeywordToken.Break);

			// The break statement can have an optional label to jump to.
			if (parser.AtValidEndOfStatement() == false)
			{
				// break [label]

				// Read the label name.
				result.Label = parser.ExpectIdentifier();
			}

			// Consume the semi-colon, if there was one.
			parser.ExpectEndOfStatement();
			
			return labelled;
			
		}
		
	}
	
	internal class TryToken : KeywordToken
	{
		
		internal TryToken():base("try")
		{}
		
		public override Statement ParseNoNewContext(Parser parser)
		{
			
			var result = new TryCatchFinallyStatement();
			var labelled = parser.Labels(result);
			
			// Consume the try keyword.
			parser.Expect(KeywordToken.Try);

			// Parse the try block.
			result.TryBlock = parser.ParseBlock();

			// The next token is either 'catch' or 'finally'.
			if (parser.nextToken == KeywordToken.Catch)
			{
				// Consume the catch token.
				parser.Expect(KeywordToken.Catch);

				// Read the left parenthesis.
				parser.Expect(PunctuatorToken.LeftParenthesis);

				// Read the name of the variable to assign the exception to.
				result.CatchVariableName = parser.ExpectIdentifier();
				parser.ValidateVariableName(result.CatchVariableName);

				// Read the right parenthesis.
				parser.Expect(PunctuatorToken.RightParenthesis);

				// Create a new scope for the catch variable.
				result.CatchScope = DeclarativeScope.CreateCatchScope(parser.currentLetScope, result.CatchVariableName);
				using (parser.CreateScopeContext(result.CatchScope, result.CatchScope))
				{
					// Parse the statements inside the catch block.
					result.CatchBlock = parser.ParseBlock();
				}
			}

			if (parser.nextToken == KeywordToken.Finally)
			{
				// Consume the finally token.
				parser.Expect(KeywordToken.Finally);

				// Read the finally statements.
				result.FinallyBlock = parser.ParseBlock();
			}

			// There must be a catch or finally block.
			if (result.CatchBlock == null && result.FinallyBlock == null)
				throw new JavaScriptException(parser.engine, "SyntaxError", "Missing catch or finally after try", parser.LineNumber, parser.SourcePath);

			return labelled;
					
		}
		
	}
	
	internal class ReturnToken : KeywordToken
	{
		
		internal ReturnToken():base("return")
		{}
		
		public override Statement ParseNoNewContext(Parser parser)
		{
			
			if (parser.context != CodeContext.Function)
				throw new JavaScriptException(parser.engine, "SyntaxError", "Return statements are only allowed inside functions", parser.LineNumber, parser.SourcePath);

			var result = new ReturnStatement();
			var labelled = parser.Labels(result);
			
			// Consume the return keyword.
			parser.Expect(KeywordToken.Return);

			if (parser.AtValidEndOfStatement() == false)
			{
				// Parse the return value expression.
				result.Value = parser.ParseExpression(PunctuatorToken.Semicolon);
			}

			// Consume the end of the statement.
			parser.ExpectEndOfStatement();
			
			return labelled;
			
		}
		
	}
	
	internal class SwitchToken : KeywordToken
	{
		
		internal SwitchToken():base("switch")
		{}
		
		public override Statement ParseNoNewContext(Parser parser)
		{
			
			var result = new SwitchStatement();
			var labelled = parser.Labels(result);
			
			// Consume the switch keyword.
			parser.Expect(KeywordToken.Switch);

			// Read the left parenthesis.
			parser.Expect(PunctuatorToken.LeftParenthesis);
			
			// Parse the switch expression.
			result.Value = parser.ParseExpression(PunctuatorToken.RightParenthesis);

			// Read the right parenthesis.
			parser.Expect(PunctuatorToken.RightParenthesis);

			// Consume the start brace ({).
			parser.Expect(PunctuatorToken.LeftBrace);

			SwitchCase defaultClause = null;
			Token next=parser.nextToken;
			
			while (true)
			{
				if (next == KeywordToken.Case)
				{
					var caseClause = new SwitchCase();

					// Read the case keyword.
					parser.Expect(KeywordToken.Case);
				
					// Parse the case expression.
					caseClause.Value = parser.ParseExpression(PunctuatorToken.Colon);

					// Consume the colon.
					parser.Expect(PunctuatorToken.Colon);
					
					next=parser.nextToken;
					
					// Zero or more statements can be added to the case statement.
					while (next != KeywordToken.Case && next != KeywordToken.Default && next != PunctuatorToken.RightBrace)
					{
						caseClause.BodyStatements.Add(parser.ParseStatement());
						next=parser.nextToken;
					}

					// Add the case clause to the switch statement.
					result.CaseClauses.Add(caseClause);
				}
				else if (next == KeywordToken.Default)
				{
					// Make sure this is the only default clause.
					if (defaultClause != null)
						throw new JavaScriptException(parser.engine, "SyntaxError", "Only one default clause is allowed.", parser.LineNumber, parser.SourcePath);

					defaultClause = new SwitchCase();

					// Read the case keyword.
					parser.Expect(KeywordToken.Default);

					// Consume the colon.
					parser.Expect(PunctuatorToken.Colon);
					
					next=parser.nextToken;
					
					// Zero or more statements can be added to the case statement.
					while (next != KeywordToken.Case && next != KeywordToken.Default && next != PunctuatorToken.RightBrace)
					{
						defaultClause.BodyStatements.Add(parser.ParseStatement());
						next=parser.nextToken;
					}

					// Add the default clause to the switch statement.
					result.CaseClauses.Add(defaultClause);
				}
				else if (next == PunctuatorToken.RightBrace)
				{
					break;
				}
				else
				{
					// Statements cannot be added directly after the switch.
					throw new JavaScriptException(parser.engine, "SyntaxError", "Expected 'case' or 'default'.", parser.LineNumber, parser.SourcePath);
				}
				
			}

			// Consume the end brace.
			parser.Consume();

			return labelled;
			
		}
		
	}
	
	internal class WithToken : KeywordToken
	{
		
		internal WithToken():base("with")
		{}
		
		public override Statement ParseNoNewContext(Parser parser)
		{
			
			// This statement is not allowed in strict mode.
			if (parser.StrictMode == true)
				throw new JavaScriptException(parser.engine, "SyntaxError", "The with statement is not supported in strict mode", parser.LineNumber, parser.SourcePath);

			var result = new WithStatement();
			var labelled = parser.Labels(result);
			
			// Read past the "with" token.
			parser.Expect(KeywordToken.With);

			// Read a left parenthesis token "(".
			parser.Expect(PunctuatorToken.LeftParenthesis);

			// Read an object reference.
			var objectEnvironment = parser.ParseExpression(PunctuatorToken.RightParenthesis);
			
			// Read a right parenthesis token ")".
			parser.Expect(PunctuatorToken.RightParenthesis);

			// Create a new scope and assign variables within the with statement to the scope.
			result.Scope = ObjectScope.CreateWithScope(parser.currentLetScope, objectEnvironment,parser.OptimizationInfo);
			
			using (parser.CreateScopeContext(result.Scope, result.Scope))
			{
				// Read the body of the with statement.
				result.Body = parser.ParseStatement();
			}

			return labelled;
			
		}
		
	}
	
	internal class WhileToken : KeywordToken
	{
		
		internal WhileToken():base("while")
		{}
		
		public override Statement ParseNoNewContext(Parser parser)
		{
			
			var result = new WhileStatement();
			var labelled = parser.Labels(result);
			
			// Consume the while keyword.
			parser.Expect(KeywordToken.While);

			// Read the left parenthesis.
			parser.Expect(PunctuatorToken.LeftParenthesis);

			// Parse the condition.
			result.ConditionStatement = new ExpressionStatement(parser.ParseExpression(PunctuatorToken.RightParenthesis));
			
			// Read the right parenthesis.
			parser.Expect(PunctuatorToken.RightParenthesis);

			// Read the statements that will be executed in the loop body.
			result.Body = parser.ParseStatement();

			return labelled;
			
		}
	
	}
	
	internal class LetToken : VarToken
	{
		internal LetToken():base("let")
		{}
	}
	
	internal class ConstToken : VarToken
	{
		internal ConstToken():base("const")
		{}
	}
	
	internal class VarToken : KeywordToken
	{
		
		internal VarToken() : base("var")
		{}
		
		internal VarToken(string val) : base(val)
		{}
		
		public override Statement ParseNoNewContext(Parser parser)
		{
			
			var result = new VarStatement(this == KeywordToken.Var ? parser.currentVarScope : parser.currentLetScope);
			var labelled = parser.Labels(result);
			
			// Read past the first token (var, let or const).
			parser.Expect(this);
		
			// There can be multiple declarations.
			while (true)
			{
				var declaration = new VariableDeclaration();

				// The next token must be a variable name.
				declaration.VariableName = parser.ExpectIdentifier();
				parser.ValidateVariableName(declaration.VariableName);

				// Add the variable to the current function's list of local variables.
				parser.currentVarScope.AddVariable(declaration.VariableName,null,
					parser.context == CodeContext.Function ? null : new LiteralExpression(Undefined.Value));

				// The next token is either an equals sign (=), a semi-colon or a comma.
				if (parser.nextToken == PunctuatorToken.Assignment)
				{
					// Read past the equals token (=).
					parser.Expect(PunctuatorToken.Assignment);

					// Read the setter expression.
					declaration.InitExpression = parser.ParseExpression(PunctuatorToken.Semicolon, PunctuatorToken.Comma);
					
				}

				// Add the declaration to the result.
				result.Declarations.Add(declaration);

				// Check if we are at the end of the statement.
				if (parser.AtValidEndOfStatement() && parser.nextToken != PunctuatorToken.Comma)
					break;

				// Read past the comma token.
				parser.Expect(PunctuatorToken.Comma);
				
			}

			// Consume the end of the statement.
			parser.ExpectEndOfStatement();

			return labelled;
			
		}
		
	}
	
	/// <summary>
	/// When parsing a for statement, this is used to keep track of what type it is.
	/// </summary>
	internal enum ForStatementType
	{
		Unknown,
		For,
		ForIn,
		ForOf,
	}
	
	internal class ForToken : KeywordToken
	{
		
		internal ForToken():base("for")
		{}
		
		public override Statement ParseNoNewContext(Parser parser)
		{
			
			// Consume the for keyword.
			parser.Expect(KeywordToken.For);

			// Read the left parenthesis.
			parser.Expect(PunctuatorToken.LeftParenthesis);

			// The initialization statement.
			Statement initializationStatement = null;

            // The type of for statement.
            ForStatementType type = ForStatementType.Unknown;
			
			// The for-in and for-of expressions need a variable to assign to.  Is null for a regular for statement.
			IReferenceExpression forInOfReference = null;
			
			if (parser.nextToken == KeywordToken.Var || parser.nextToken==KeywordToken.Let || parser.nextToken==KeywordToken.Const)
			{
				
				bool isVar=(parser.nextToken == KeywordToken.Var);
				
				// Read past the var/let/const token.
				parser.Expect(parser.nextToken);
				
				Scope scope=isVar?parser.currentVarScope : parser.currentLetScope;
				
				// There can be multiple initializers (but not for for-in statements).
				var varLetConstStatement = new VarStatement(scope);
				initializationStatement = parser.Labels(varLetConstStatement);
				
				while (true)
				{
					var declaration = new VariableDeclaration();

					// The next token must be a variable name.
					declaration.VariableName = parser.ExpectIdentifier();
					parser.ValidateVariableName(declaration.VariableName);

					// Add the variable to the current function's list of local variables.
					parser.currentVarScope.AddVariable(declaration.VariableName,null,
						parser.context == CodeContext.Function ? null : new LiteralExpression(Undefined.Value));

					// The next token is either an equals sign (=), a semi-colon, a comma, or the "in" keyword.
					if (parser.nextToken == PunctuatorToken.Assignment)
					{
						// Read past the equals token (=).
						parser.Expect(PunctuatorToken.Assignment);

						// Read the setter expression.
						declaration.InitExpression = parser.ParseExpression(PunctuatorToken.Semicolon, PunctuatorToken.Comma);
						
                        // This must be a regular for statement.
                        type = ForStatementType.For;
					}

					// Add the declaration to the initialization statement.
					varLetConstStatement.Declarations.Add(declaration);
					
					if (parser.nextToken == PunctuatorToken.Semicolon)
					{
						// This is a regular for statement.
						break;
					}
					else if (parser.nextToken == KeywordToken.In && type == ForStatementType.Unknown)
                    {
                        // This is a for-in statement.
                        forInOfReference = new NameExpression(scope, declaration.VariableName);
                        type = ForStatementType.ForIn;
                        break;
                    }
					else if (parser.nextToken == IdentifierToken.Of && type == ForStatementType.Unknown)
                    {
                        // This is a for-of statement.
                        forInOfReference = new NameExpression(scope, declaration.VariableName);
                        type = ForStatementType.ForOf;
                        break;
                    }
					else if (parser.nextToken != PunctuatorToken.Comma)
						throw new JavaScriptException(parser.engine, "SyntaxError", string.Format("Unexpected token {0}", Token.ToText(parser.nextToken)), parser.LineNumber, parser.SourcePath);

					// Read past the comma token.
					parser.Expect(PunctuatorToken.Comma);
					
                    // Multiple initializers are not allowed in for-in statements.
                    type = ForStatementType.For;
				}
				
			}
			else
			{
				// Not a var initializer - can be a simple variable name then "in" or any expression ending with a semi-colon.
				// The expression can be empty.
				if (parser.nextToken != PunctuatorToken.Semicolon)
				{
					// Parse an expression.
					var initializationExpression = parser.ParseExpression(PunctuatorToken.Semicolon, KeywordToken.In, IdentifierToken.Of);

					// Record debug info for the expression.
					initializationStatement = new ExpressionStatement(initializationExpression);
					
					if (parser.nextToken == KeywordToken.In)
					{
						// This is a for-in statement.
						if ((initializationExpression is IReferenceExpression) == false)
							throw new JavaScriptException(parser.engine, "SyntaxError", "Invalid left-hand side in for-in", parser.LineNumber, parser.SourcePath);
						forInOfReference = (IReferenceExpression)initializationExpression;
					}
					else if (parser.nextToken == IdentifierToken.Of)
                    {
                        // This is a for-of statement.
                        if ((initializationExpression is IReferenceExpression) == false)
                            throw new JavaScriptException(parser.engine, "SyntaxError", "Invalid left-hand side in for-of", parser.LineNumber, parser.SourcePath);
                        forInOfReference = (IReferenceExpression)initializationExpression;
                        type = ForStatementType.ForOf;
                    }
				}
			}

			if (type == ForStatementType.ForIn)
			{
				// for (x in y)
				// for (var x in y)
				var result = new ForInStatement();
				var labelled = parser.Labels(result);
				
				result.Variable = forInOfReference;
				
				// Consume the "in".
				parser.Expect(KeywordToken.In);

				// Parse the right-hand-side expression.
				result.TargetObject = parser.ParseExpression(PunctuatorToken.RightParenthesis);
				
				// Read the right parenthesis.
				parser.Expect(PunctuatorToken.RightParenthesis);

				// Read the statements that will be executed in the loop body.
				result.Body = parser.ParseStatement();

				return labelled;
			}
			else if (type == ForStatementType.ForOf)
            {
                // for (x of y)
                // for (var x of y)
                var result = new ForOfStatement();
				var labelled = parser.Labels(result);
				
                result.Variable = forInOfReference;
                
                // Consume the "of".
                parser.Expect(IdentifierToken.Of);

                // Parse the right-hand-side expression.
                result.TargetObject = parser.ParseExpression(PunctuatorToken.RightParenthesis, PunctuatorToken.Comma); // Comma is not allowed.
                
                // Read the right parenthesis.
                parser.Expect(PunctuatorToken.RightParenthesis);

                // Read the statements that will be executed in the loop body.
                result.Body = parser.ParseStatement();
				
                return labelled;
            }
			else
			{
				var result = new ForStatement();
				var labelled = parser.Labels(result);
				
				// Set the initialization statement.
				if (initializationStatement != null)
					result.InitStatement = initializationStatement;

				// Read the semicolon.
				parser.Expect(PunctuatorToken.Semicolon);

				// Parse the optional condition expression.
				// Note: if the condition is omitted then it is considered to always be true.
				if (parser.nextToken != PunctuatorToken.Semicolon)
				{
					result.ConditionStatement = new ExpressionStatement(parser.ParseExpression(PunctuatorToken.Semicolon));
				}

				// Read the semicolon.
				// Note: automatic semicolon insertion never inserts a semicolon in the header of a
				// for statement.
				parser.Expect(PunctuatorToken.Semicolon);

				// Parse the optional increment expression.
				if (parser.nextToken != PunctuatorToken.RightParenthesis)
				{
					result.IncrementStatement = new ExpressionStatement(parser.ParseExpression(PunctuatorToken.RightParenthesis));
				}
				
				// Read the right parenthesis.
				parser.Expect(PunctuatorToken.RightParenthesis);

				// Read the statements that will be executed in the loop body.
				result.Body = parser.ParseStatement();

				return labelled;
			}
		}
		
	}
	
}