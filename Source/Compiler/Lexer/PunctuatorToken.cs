using System;
using System.Collections.Generic;

namespace Nitrassic.Compiler
{

	/// <summary>
	/// Represents punctuation or an operator in the source code.
	/// </summary>
	internal class PunctuatorToken : Token
	{
		private string text;

		/// <summary>
		/// Creates a new PunctuatorToken instance.
		/// </summary>
		/// <param name="text"> The punctuator text. </param>
		internal PunctuatorToken(string text)
		{
			if (text == null)
				throw new ArgumentNullException("text");
			this.text = text;
		}

		// The full list of punctuators.
		public readonly static PunctuatorToken LeftBrace = new LeftBraceToken();
		public readonly static PunctuatorToken RightBrace = new PunctuatorToken("}");
		public readonly static PunctuatorToken LeftParenthesis = new PunctuatorToken("(");
		public readonly static PunctuatorToken RightParenthesis = new PunctuatorToken(")");
		public readonly static PunctuatorToken LeftBracket = new PunctuatorToken("[");
		public readonly static PunctuatorToken RightBracket = new PunctuatorToken("]");
		public readonly static PunctuatorToken Semicolon = new SemicolonToken();
		public readonly static PunctuatorToken Comma = new PunctuatorToken(",");
		public readonly static PunctuatorToken LessThan = new PunctuatorToken("<");
		public readonly static PunctuatorToken GreaterThan = new PunctuatorToken(">");
		public readonly static PunctuatorToken LessThanOrEqual = new PunctuatorToken("<=");
		public readonly static PunctuatorToken GreaterThanOrEqual = new PunctuatorToken(">=");
		public readonly static PunctuatorToken Equality = new PunctuatorToken("==");
		public readonly static PunctuatorToken Inequality = new PunctuatorToken("!=");
		public readonly static PunctuatorToken StrictEquality = new PunctuatorToken("===");
		public readonly static PunctuatorToken StrictInequality = new PunctuatorToken("!==");
		public readonly static PunctuatorToken Plus = new PunctuatorToken("+");
		public readonly static PunctuatorToken Minus = new PunctuatorToken("-");
		public readonly static PunctuatorToken Multiply = new PunctuatorToken("*");
		public readonly static PunctuatorToken Modulo = new PunctuatorToken("%");
		public readonly static PunctuatorToken Increment = new PunctuatorToken("++");
		public readonly static PunctuatorToken Decrement = new PunctuatorToken("--");
		public readonly static PunctuatorToken LeftShift = new PunctuatorToken("<<");
		public readonly static PunctuatorToken SignedRightShift = new PunctuatorToken(">>");
		public readonly static PunctuatorToken UnsignedRightShift = new PunctuatorToken(">>>");
		public readonly static PunctuatorToken BitwiseAnd = new PunctuatorToken("&");
		public readonly static PunctuatorToken BitwiseOr = new PunctuatorToken("|");
		public readonly static PunctuatorToken BitwiseXor = new PunctuatorToken("^");
		public readonly static PunctuatorToken LogicalNot = new PunctuatorToken("!");
		public readonly static PunctuatorToken BitwiseNot = new PunctuatorToken("~");
		public readonly static PunctuatorToken LogicalAnd = new PunctuatorToken("&&");
		public readonly static PunctuatorToken LogicalOr = new PunctuatorToken("||");
		public readonly static PunctuatorToken Conditional = new PunctuatorToken("?");
		public readonly static PunctuatorToken Colon = new PunctuatorToken(":");
		public readonly static PunctuatorToken Assignment = new PunctuatorToken("=");
		public readonly static PunctuatorToken CompoundAdd = new PunctuatorToken("+=");
		public readonly static PunctuatorToken CompoundSubtract = new PunctuatorToken("-=");
		public readonly static PunctuatorToken CompoundMultiply = new PunctuatorToken("*=");
		public readonly static PunctuatorToken CompoundModulo = new PunctuatorToken("%=");
		public readonly static PunctuatorToken CompoundLeftShift = new PunctuatorToken("<<=");
		public readonly static PunctuatorToken CompoundSignedRightShift = new PunctuatorToken(">>=");
		public readonly static PunctuatorToken CompoundUnsignedRightShift = new PunctuatorToken(">>>=");
		public readonly static PunctuatorToken CompoundBitwiseAnd = new PunctuatorToken("&=");
		public readonly static PunctuatorToken CompoundBitwiseOr = new PunctuatorToken("|=");
		public readonly static PunctuatorToken CompoundBitwiseXor = new PunctuatorToken("^=");

		// These are treated specially by the lexer.
		public readonly static PunctuatorToken Dot = new PunctuatorToken(".");
		public readonly static PunctuatorToken Divide = new PunctuatorToken("/");
		public readonly static PunctuatorToken CompoundDivide = new PunctuatorToken("/=");

		// Mapping from text -> punctuator.
		internal readonly static Dictionary<int, PunctuatorToken> LookupTable = new Dictionary<int, PunctuatorToken>();
		
		private static void Add(PunctuatorToken p)
		{
			
			string b=p.Text;
			
			int id=0;
			
			// Create the ID from the string:
			for(int i=0;i<b.Length;i++)
			{
				id=(id<<8) | (int)b[i];
			}
			
			LookupTable[id]=p;
		}
		
		public static void Setup()
		{
			
			if(LookupTable.Count!=0)
				return;
			
			Add(LeftBrace);
			Add(RightBrace);
			Add(LeftParenthesis);
			Add(RightParenthesis);
			Add(LeftBracket);
			Add(RightBracket);
			Add(Semicolon);
			Add(Comma);
			Add(LessThan);
			Add(GreaterThan);
			Add(LessThanOrEqual);
			Add(GreaterThanOrEqual);
			Add(Equality);
			Add(Inequality);
			Add(StrictEquality);
			Add(StrictInequality);
			Add(Plus);
			Add(Minus);
			Add(Multiply);
			Add(Modulo);
			Add(Increment);
			Add(Decrement);
			Add(LeftShift);
			Add(SignedRightShift);
			Add(UnsignedRightShift);
			Add(BitwiseAnd);
			Add(BitwiseOr);
			Add(BitwiseXor);
			Add(LogicalNot);
			Add(BitwiseNot);
			Add(LogicalAnd);
			Add(LogicalOr);
			Add(Conditional);
			Add(Colon);
			Add(Assignment);
			Add(CompoundAdd);
			Add(CompoundSubtract);
			Add(CompoundMultiply);
			Add(CompoundModulo);
			Add(CompoundLeftShift);
			Add(CompoundSignedRightShift);
			Add(CompoundUnsignedRightShift);
			Add(CompoundBitwiseAnd);
			Add(CompoundBitwiseOr);
			Add(CompoundBitwiseXor);

			// These are treated specially by the lexer.
			Add(Dot);
			Add(Divide);
			Add(CompoundDivide);
		}

		/// <summary>
		/// Gets a punctuator token from the given ID.
		/// </summary>
		/// <param name="id"> The punctuator ID. The ID originates from the characters of the string.</param>
		/// <returns> The punctuator corresponding to the given string, or <c>null</c> if the ID
		/// does not represent a valid punctuator. </returns>
		public static PunctuatorToken FromID(int id)
		{
			PunctuatorToken result;
			LookupTable.TryGetValue(id, out result);
			return result;
		}

		/// <summary>
		/// Gets a string that represents the token in a parseable form.
		/// </summary>
		public override string Text
		{
			get { return this.text; }
		}
	}
	
	internal class LeftBraceToken : PunctuatorToken{
		
		internal LeftBraceToken():base("{"){}
		
		public override Statement ParseNoNewContext(Parser parser){
			
			return parser.ParseBlock();
			
		}
		
	}
	
	internal class SemicolonToken : PunctuatorToken{
		
		internal SemicolonToken():base(";"){}
		
		public override Statement ParseNoNewContext(Parser parser){
			
			var result = parser.Labels(new EmptyStatement());

			// Read past the semicolon.
			parser.Expect(PunctuatorToken.Semicolon);
			
			return result;
			
		}
		
	}
	
}