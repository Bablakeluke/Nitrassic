using System;
using System.Globalization;

namespace Nitrassic.Library
{
	/// <summary>
	/// Represents an instance of the Number object.
	/// </summary>
	/// <remarks>
	/// None of the methods of the Number prototype are generic; they should throw <c>TypeError</c>
	/// if the <c>this</c> value is not a Number object or a number primitive.
	/// </remarks>
	public static class Number
	{
		
		[JSProperties(Hidden=true)]
		public static readonly Type PrototypeFor=typeof(double);
		
		//	 JAVASCRIPT FUNCTIONS
		//_________________________________________________________________________________________

		/// <summary>
		/// Returns a string representing a number represented in exponential notation.
		/// </summary>
		/// <param name="fractionDigits"> Number of digits after the decimal point. Must be in the
		/// range 0 – 20, inclusive.  Defaults to the number of digits necessary to specify the
		/// number. </param>
		/// <returns> A string representation of a number in exponential notation. The string
		/// contains one digit before the significand's decimal point, and may contain
		/// fractionDigits digits after it. </returns>
		public static string ToExponential(ScriptEngine engine,double thisObj,object fractionDigits)
		{
			// If precision is undefined, the number of digits is dependant on the number.
			if (TypeUtilities.IsUndefined(fractionDigits))
				return NumberFormatter.ToString(thisObj, 10, NumberFormatter.Style.Exponential, -1);

			// Convert the parameter to an integer.
			int fractionDigits2 = TypeConverter.ToInteger(fractionDigits);

			// Check the parameter is within range.
			if (fractionDigits2 < 0 || fractionDigits2 > 20)
				throw new JavaScriptException(engine, "RangeError", "toExponential() argument must be between 0 and 20.");

			// NumberFormatter does the hard work.
			return NumberFormatter.ToString(thisObj, 10, NumberFormatter.Style.Exponential, fractionDigits2);
		}

		/// <summary>
		/// Returns a string representing a number in fixed-point notation.
		/// </summary>
		/// <param name="fractionDigits"> Number of digits after the decimal point. Must be in the
		/// range 0 – 20, inclusive. </param>
		/// <returns> A string representation of a number in fixed-point notation. The string
		/// contains one digit before the significand's decimal point, and must contain
		/// fractionDigits digits after it.
		/// If fractionDigits is not supplied or undefined, the toFixed method assumes the value
		/// is zero. </returns>
		public static string ToFixed(ScriptEngine engine,double thisObj,int fractionDigits)
		{
			// Check the parameter is within range.
			if (fractionDigits < 0 || fractionDigits > 20)
				throw new JavaScriptException(engine, "RangeError", "toFixed() argument must be between 0 and 20.");

			// NumberFormatter does the hard work.
			return NumberFormatter.ToString(thisObj, 10, NumberFormatter.Style.Fixed, fractionDigits);
		}

		/// <summary>
		/// Returns a string containing a locale-dependant version of the number.
		/// </summary>
		/// <returns> A string containing a locale-dependant version of the number. </returns>
		public static string ToLocaleString(double thisObj)
		{
			// NumberFormatter does the hard work.
			return NumberFormatter.ToString(thisObj, 10, CultureInfo.CurrentCulture.NumberFormat, NumberFormatter.Style.Regular);
		}

		/// <summary>
		/// Returns a string containing a number represented either in exponential or fixed-point
		/// notation with a specified number of digits.
		/// </summary>
		/// <param name="precision"> The number of significant digits. Must be in the range 1 – 21,
		/// inclusive. </param>
		/// <returns> A string containing a number represented either in exponential or fixed-point
		/// notation with a specified number of digits. </returns>
		/// <remarks>
		/// For numbers in exponential notation, precision - 1 digits are returned after the
		/// decimal point. For numbers in fixed notation, precision significant digits are
		/// returned.
		/// If precision is not supplied or is undefined, the toString method is called instead.
		/// </remarks>
		public static string ToPrecision(ScriptEngine engine,double thisObj,object precision)
		{
			// If precision is undefined, delegate to "toString()".
			if (TypeUtilities.IsUndefined(precision))
				return ToString(engine,thisObj,10);

			// Convert the parameter to an integer.
			int precision2 = TypeConverter.ToInteger(precision);

			// Check the precision is in range.
			if (precision2 < 1 || precision2 > 21)
				throw new JavaScriptException(engine, "RangeError", "toPrecision() argument must be between 0 and 21.");

			// NumberFormatter does the hard work.
			return NumberFormatter.ToString(thisObj, 10, NumberFormatter.Style.Precision, precision2);
		}

		/// <summary>
		/// Returns the textual representation of the number.
		/// </summary>
		/// <param name="radix"> Specifies a radix for converting numeric values to strings. </param>
		/// <returns> The textual representation of the number. </returns>
		
		public static string ToString(ScriptEngine engine,double thisObj,int radix)
		{
			if(radix==0)
				radix=10;
			
			// Check the parameter is in range.
			if (radix < 2 || radix > 36)
				throw new JavaScriptException(engine, "RangeError", "The radix must be between 2 and 36, inclusive.");

			// NumberFormatter does the hard work.
			return NumberFormatter.ToString(thisObj, radix, NumberFormatter.Style.Regular);
		}

		/// <summary>
		/// Returns the primitive value of the specified object.
		/// </summary>
		/// <returns> The primitive value of the specified object. </returns>
		public static double ValueOf(double thisObj)
		{
			return thisObj;
		}
		
		/// <summary>
		/// Calculates the number of leading zero bits in the integer representation of this
		/// number.
		/// </summary>
		/// <returns> The number of leading zero bits in the integer representation of this number. </returns>
		public static int Clz(double thisObj)
		{
			uint x = (uint)thisObj;

			// Propagate leftmost 1-bit to the right 
			x = x | (x >> 1); 
			x = x | (x >> 2); 
			x = x | (x >> 4); 
			x = x | (x >> 8); 
			x = x | (x >> 16);

			return sizeof(int) * 8 - CountOneBits(x);
		}

		/// <summary>
		/// Counts the number of set bits in an integer.
		/// </summary>
		/// <param name="x"> The integer. </param>
		/// <returns> The number of set bits in the integer. </returns>
		private static int CountOneBits(uint x)
		{
			x -= ((x >> 1) & 0x55555555);
			x = (((x >> 2) & 0x33333333) + (x & 0x33333333));
			x = (((x >> 4) + x) & 0x0f0f0f0f);
			x += (x >> 8);
			x += (x >> 16);
			return (int)(x & 0x0000003f);
		}
		
		/// <summary>
		/// Called when the Number object is invoked like a function, e.g. var x = Number("5").
		/// Returns zero.
		/// </summary>
		public static double OnCall()
		{
			return 0;
		}

		/// <summary>
		/// Called when the Number object is invoked like a function, e.g. var x = Number("5").
		/// Converts the given argument into a number value (not a Number object).
		/// </summary>
		public static double OnCall(double value)
		{
			return value;
		}
		
		/// <summary>
		/// Creates a new Number instance and initializes it to zero.
		/// </summary>
		public static double OnConstruct(ScriptEngine engine)
		{
			return 0;
		}

		/// <summary>
		/// Creates a new Number instance and initializes it to the given value.
		/// </summary>
		/// <param name="value"> The value to initialize to. </param>
		public static double OnConstruct(ScriptEngine engine,double value)
		{
			// Note: The type system handles the conversion.
			return value;
		}
		
		//	 JAVASCRIPT FIELDS
		//_________________________________________________________________________________________

		/// <summary>
		/// The largest representable number, approximately 1.8e+308.
		/// </summary>
		public const double MAX_VALUE = double.MaxValue;

		/// <summary>
		/// The smallest positive representable number, approximately 5e-324.
		/// </summary>
		public const double MIN_VALUE = double.Epsilon;

		/// <summary>
		/// Special "not a number" value.
		/// </summary>
		public const double NaN = double.NaN;

		/// <summary>
		/// Special value representing negative infinity.
		/// </summary>
		public const double NEGATIVE_INFINITY = double.NegativeInfinity;

		/// <summary>
		/// Special value representing positive infinity.
		/// </summary>
		public const double POSITIVE_INFINITY = double.PositiveInfinity;

		/// <summary>
		/// The difference between 1 and the smallest value greater than 1 that is representable as
		/// a numeric value.
		/// </summary>
		public const double EPSILON = 2.2204460492503130808472633361816e-16;

		/// <summary>
		/// The maximum integer within the range of integers that can be represented exactly.
		/// Outside the safe range multiple integers are mapped to a single value.
		/// </summary>
		public const double MAX_SAFE_INTEGER = 9007199254740991;

		/// <summary>
		/// The minimum integer within the range of integers that can be represented exactly.
		/// Outside the safe range multiple integers are mapped to a single value.
		/// </summary>
		public const double MIN_SAFE_INTEGER = -9007199254740991;
		


		/// <summary>
		/// Determines whether the given number is finite.
		/// </summary>
		/// <param name="value"> The number to test. </param>
		/// <returns> <c>false</c> if the number is NaN or positive or negative infinity,
		/// <c>true</c> otherwise.  <c>false</c> if the value is not a number. </returns>
		
		public static bool IsFinite(object value)
		{
			if (value is int || value is uint)
				return true;
			if (value is double)
				return double.IsNaN((double)value) == false && double.IsInfinity((double)value) == false;
			return false;
		}

		/// <summary>
		/// Determines whether the given number is NaN.
		/// </summary>
		/// <param name="value"> The number to test. </param>
		/// <returns> <c>true</c> if the number is NaN, <c>false</c> otherwise. </returns>
		
		public static bool IsNaN(object value)
		{
			if (value is double)
				return double.IsNaN((double)value);
			return false;
		}

		/// <summary>
		/// Determines whether the given number is an integer.
		/// </summary>
		/// <param name="value"> The number to test. </param>
		/// <returns> <c>true</c> if the number is an integer, <c>false</c> otherwise. </returns>
	
		public static bool IsInteger(object value)
		{
			if (value is int || value is uint)
				return true;
			if (value is double)
			{
				double number = (double)value;
				if (double.IsInfinity(number))
					return false;
				return Math.Floor(number) == number;
			}
			return false;
		}

		/// <summary>
		/// Determines whether the given number is within the "safe" integer range.
		/// </summary>
		/// <param name="value"> The number to test. </param>
		/// <returns> <c>true</c> if the number is a safe integer, <c>false</c> otherwise. </returns>
		
		public static bool IsSafeInteger(object value)
		{
			if (value is int || value is uint)
				return true;
			if (value is double)
			{
				double number = (double)value;
				if (double.IsInfinity(number))
					return false;
				return (Math.Floor(number) == number) && number >= MIN_SAFE_INTEGER && number <= MAX_SAFE_INTEGER;
			}
			return false;
		}

		/// <summary>
		/// Parses the given string and returns the equivalent integer value. 
		/// </summary>
		/// <param name="engine"> The associated script engine. </param>
		/// <param name="input"> The string to parse. </param>
		/// <param name="radix"> The numeric base to use for parsing.  Pass zero to use base 10
		/// except when the input string starts with '0' in which case base 16 or base 8 are used
		/// instead (base 8 is only supported in compatibility mode). </param>
		/// <returns> The equivalent integer value of the given string. </returns>
		/// <remarks> Leading whitespace is ignored.  Parsing continues until the first invalid
		/// character, at which point parsing stops.  No error is returned in this case. </remarks>
	
		public static double ParseInt(ScriptEngine engine, string input, double radix)
		{
			return Window.ParseInt(engine, input, radix);
		}

		/// <summary>
		/// Parses the given string and returns the equivalent numeric value. 
		/// </summary>
		/// <param name="input"> The string to parse. </param>
		/// <returns> The equivalent numeric value of the given string. </returns>
		/// <remarks> Leading whitespace is ignored.  Parsing continues until the first invalid
		/// character, at which point parsing stops.  No error is returned in this case. </remarks>
		
		public static double ParseFloat(ScriptEngine engine,string input)
		{
			return Window.ParseFloat(input);
		}
		
	}
}
