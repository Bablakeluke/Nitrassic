using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Nitrassic.Library
{
	/// <summary>
	/// Represents an instance of the JavaScript string object.
	/// </summary>
	public static class String
	{
		
		[JSProperties(Hidden=true)]
		public static readonly Type PrototypeFor=typeof(string);
		
		//	 .NET ACCESSOR PROPERTIES
		//_________________________________________________________________________________________
		
		/// <summary>
		/// Gets the number of characters in the string.
		/// </summary>
		public static int get_Length(string thisObj){
			return thisObj.Length;
		}
		
		//	 OVERRIDES
		//_________________________________________________________________________________________

		/// <summary>
		/// Gets a descriptor for the property with the given array index.
		/// </summary>
		/// <param name="index"> The array index of the property. </param>
		/// <returns> A property descriptor containing the property value and attributes. </returns>
		/// <remarks> The prototype chain is not searched. </remarks>
		public static string get_Item(string thisObj,uint index)
		{
			if (index < thisObj.Length)
			{
				return thisObj[(int)index].ToString();
			}
			
			return null;
		}

		/// <summary>
		/// Gets an enumerable list of every property name and value associated with thisObj.
		/// </summary>
		public static IEnumerable<PropertyNameAndValue> get_Properties(string thisObj)
		{
			
			// Enumerate array indices.
			for (int i = 0; i < thisObj.Length; i++)
				yield return new PropertyNameAndValue(i.ToString(), thisObj[i].ToString(), PropertyAttributes.Enumerable);
			
		}
		
		//	 JAVASCRIPT FUNCTIONS
		//_________________________________________________________________________________________

		/// <summary>
		/// Returns the character at the specified index.
		/// </summary>
		/// <param name="thisObj"> The string that is being operated on. </param>
		/// <param name="index"> The character position (starts at 0). </param>
		/// <returns></returns>
		public static string CharAt(string thisObj, int index)
		{
			if (index < 0 || index >= thisObj.Length)
				return string.Empty;
			return thisObj[index].ToString();
		}

		/// <summary>
		/// Returns a number indicating the 16-bit UTF-16 character code at the given index.
		/// </summary>
		/// <param name="thisObj"> The string that is being operated on. </param>
		/// <param name="index"> The character position (starts at 0). </param>
		/// <returns></returns>
		public static double CharCodeAt(string thisObj, int index)
		{
			if (index < 0 || index >= thisObj.Length)
				return double.NaN;
			return (double)(int)thisObj[index];
		}

		/// <summary>
		/// Returns a number indicating the Unicode code point of the character at the given index.
		/// </summary>
		/// <param name="thisObj"> The string that is being operated on. </param>
		/// <param name="index"> The character position (starts at 0). </param>
		/// <returns></returns>
		public static double CodePointAt(string thisObj, int index)
		{
			if (index < 0 || index >= thisObj.Length)
				return double.NaN;
			int firstCodePoint = (int) thisObj[index];
			if (firstCodePoint < 0xD800 || firstCodePoint > 0xDBFF || index + 1 == thisObj.Length)
				return firstCodePoint;
			int secondCodePoint = (int) thisObj[index + 1];
			if (secondCodePoint < 0xDC00 || secondCodePoint > 0xDFFF)
				return firstCodePoint;
			return (double)((firstCodePoint - 0xD800) * 1024 + (secondCodePoint - 0xDC00) + 0x10000);
		}

		/// <summary>
		/// Combines the text of two or more strings and returns a new string.
		/// </summary>
		/// <param name="engine"> The current script environment. </param>
		/// <param name="thisObj"> The string that is being operated on. </param>
		/// <param name="strings"> The strings to concatenate with this string. </param>
		/// <returns> The result of combining this string with the given strings. </returns>
		public static ConcatenatedString Concat(ScriptEngine engine, object thisObj, params object[] strings)
		{
			if (thisObj is ConcatenatedString)
			{
				// Append the strings together.
				ConcatenatedString result = (ConcatenatedString)thisObj;
				if (strings.Length == 0)
					return result;
				result = result.Concatenate(strings[0]);
				for (int i = 1; i < strings.Length; i ++)
					result.Append(strings[i]);
				return result;
			}
			else
			{
				// Convert "this" to a string.
				TypeUtilities.VerifyThisObject(engine, thisObj, "concat");
				var thisObj2 = TypeConverter.ToString(thisObj);

				// Append the strings together.
				var result = new ConcatenatedString(thisObj2);
				foreach (object str in strings)
					result.Append(str);
				return result;
			}
		}

		
		/// <summary>
		/// Returns <c>true</c> if the calling String object contains the given string.
		/// </summary>
		/// <param name="engine"> The script engine. </param>
		/// <param name="thisObj"> The string that is being operated on. </param>
		/// <param name="substring"> The substring to search for. </param>
		/// <param name="startIndex"> The character position within the string to start searching. </param>
		/// <returns> <c>true</c> if the substring was found; <c>false</c> otherwise. </returns>
		public static bool Includes(ScriptEngine engine, string thisObj, object substring, int startIndex)
		{
			if (TypeUtilities.IsRegularExpression(substring))
				throw new JavaScriptException(engine, "TypeError", "Substring argument must not be a regular expression.");
			return IndexOf(thisObj, TypeConverter.ToString(substring), startIndex) >= 0;
		}

		
		/// <summary>
		/// Returns the index within the calling String object of the first occurrence of the specified value, or -1 if not found.
		/// </summary>
		/// <param name="thisObj"> The string that is being operated on. </param>
		/// <param name="substring"> The substring to search for. </param>
		/// <param name="startIndex"> The character position to start searching from.  Defaults to 0. </param>
		/// <returns> The character position of the start of the substring, if it was found, or -1 if it wasn't. </returns>
		public static int IndexOf(string thisObj, string substring, int startIndex)
		{
			startIndex = Math.Min(Math.Max(startIndex, 0), thisObj.Length);
			return thisObj.IndexOf(substring, startIndex, StringComparison.Ordinal);
		}

		

		/// <summary>
		/// Returns the index within the calling String object of the specified value, searching
		/// backwards from the end of the string.
		/// </summary>
		/// <param name="thisObj"> The string that is being operated on. </param>
		/// <param name="substring"> The substring to search for. </param>
		/// <param name="startIndex"> The index of the character to start searching. </param>
		/// <returns> The index of the substring, or <c>-1</c> if not found. </returns>
		public static int LastIndexOf(string thisObj, string substring, double startIndex)
		{
			// Limit startIndex to the length of the string.  This must be done first otherwise
			// when startIndex = MaxValue it wraps around to negative.
			int startIndex2 = startIndex==0 ? int.MaxValue : TypeConverter.ToInteger(startIndex);
			startIndex2 = Math.Min(startIndex2, thisObj.Length - 1);
			startIndex2 = Math.Min(startIndex2 + substring.Length - 1, thisObj.Length - 1);
			if (startIndex2 < 0)
			{
				if (thisObj == string.Empty && substring == string.Empty)
					return 0;
				return -1;
			}
			return thisObj.LastIndexOf(substring, startIndex2, StringComparison.Ordinal);
		}

		/// <summary>
		/// Returns a number indicating whether a reference string comes before or after or is the
		/// same as the given string in sort order.
		/// </summary>
		/// <param name="thisObj"> The string that is being operated on. </param>
		/// <param name="str"> The string to compare with. </param>
		/// <returns> -1, 0 or 1 depending on whether the given string comes before or after or is
		/// the same as the given string in sort order. </returns>
		public static int LocaleCompare(string thisObj, string str)
		{
			return string.Compare(thisObj, str);
		}

		/// <summary>
		/// Finds the first match of the given substring within this string.
		/// </summary>
		/// <param name="engine"> The current script environment. </param>
		/// <param name="thisObj"> The string that is being operated on. </param>
		/// <param name="substrOrRegExp"> The substring or regular expression to search for. </param>
		/// <returns> An array containing the matched strings. </returns>
		public static object Match(ScriptEngine engine, string thisObj, object substrOrRegExp)
		{
			if (substrOrRegExp is RegExp)
				// substrOrRegExp is a regular expression.
				return ((RegExp)substrOrRegExp).Match(thisObj);

			if (TypeUtilities.IsUndefined(substrOrRegExp))
				// substrOrRegExp is undefined.
				return (engine.Prototypes.RegExp.Invoke(null,new object[]{"",null}) as RegExp).Match(thisObj);

			// substrOrRegExp is a string (or convertible to a string).
			return (engine.Prototypes.RegExp.Invoke(null,new object[]{TypeConverter.ToString(substrOrRegExp),null}) as RegExp).Match(thisObj);
		}
		
		/// <summary>
		/// Returns a new string whose binary representation is in a particular Unicode normalization form.
		/// </summary>
		/// <param name="engine"> The current ScriptEngine instance. </param>
		/// <param name="thisObj"> The string that is being operated on. </param>
		/// <param name="form"> A Unicode normalization form. </param>
		/// <returns> A new string whose binary representation is in a particular Unicode normalization form. </returns>
		public static string Normalize(ScriptEngine engine, string thisObj, string form)
		{
			switch (form)
			{
				default:
				case "NFC":
					return thisObj.Normalize(NormalizationForm.FormC);
				case "NFD":
					return thisObj.Normalize(NormalizationForm.FormD);
				case "NFKC":
					return thisObj.Normalize(NormalizationForm.FormKC);
				case "NFKD":
					return thisObj.Normalize(NormalizationForm.FormKD);
			}
			throw new JavaScriptException(engine, "RangeError", "The normalization form should be one of NFC, NFD, NFKC, NFKD.");
		}

		/// <summary>
		/// Wraps the string in double quotes (").  Any existing double quotes in the string are
		/// escaped using the backslash character.
		/// </summary>
		/// <param name="thisObj"> The string to wrap. </param>
		/// <returns> The input string wrapped with double quotes and with existing double quotes
		/// escaped. </returns>
		public static string Quote(string thisObj)
		{
			var result = new System.Text.StringBuilder(thisObj.Length + 2);
			result.Append('"');
			for (int i = 0; i < thisObj.Length; i++)
			{
				char c = thisObj[i];
				if (c == '"')
					result.Append('\\');
				result.Append(c);
			}
			result.Append('"');
			return result.ToString();
		}

		/// <summary>
		/// Substitutes the given string or regular expression with the given text or the result
		/// of a replacement function.
		/// </summary>
		/// <param name="thisObj"> The string that is being operated on. </param>
		/// <param name="substrOrRegExp"> The substring to replace -or- a regular expression that
		/// matches the text to replace. </param>
		/// <param name="replaceTextOrFunction"> The text to substitute -or- a function that
		/// returns the text to substitute. </param>
		/// <returns> A copy of this string with text replaced. </returns>
		public static string Replace(string thisObj, object substrOrRegExp, object replaceTextOrFunction)
		{
			// The built-in function binding system is not powerful enough to bind the replace
			// function properly, so we bind to the correct function manually.
			if (substrOrRegExp is RegExp)
			{
				if (replaceTextOrFunction is FunctionInstance)
					return Replace(thisObj, (RegExp)substrOrRegExp, (FunctionInstance)replaceTextOrFunction);
				else
					return Replace(thisObj, (RegExp)substrOrRegExp, TypeConverter.ToString(replaceTextOrFunction));
			}
			else
			{
				if (replaceTextOrFunction is FunctionInstance)
					return Replace(thisObj, TypeConverter.ToString(substrOrRegExp), (FunctionInstance)replaceTextOrFunction);
				else
					return Replace(thisObj, TypeConverter.ToString(substrOrRegExp), TypeConverter.ToString(replaceTextOrFunction));
			}
		}

		/// <summary>
		/// Returns a copy of this string with text replaced.
		/// </summary>
		/// <param name="thisObj"> The string that is being operated on. </param>
		/// <param name="substr"> The text to search for. </param>
		/// <param name="replaceText"> A string containing the text to replace for every successful
		/// match. </param>
		/// <returns> A copy of this string with text replaced. </returns>
		internal static string Replace(string thisObj, string substr, string replaceText)
		{
			// Find the first occurrance of substr.
			int start = thisObj.IndexOf(substr, StringComparison.Ordinal);
			if (start == -1)
				return thisObj;
			int end = start + substr.Length;

			// Replace only the first match.
			var result = new System.Text.StringBuilder(thisObj.Length + (replaceText.Length - substr.Length));
			result.Append(thisObj, 0, start);
			result.Append(replaceText);
			result.Append(thisObj, end, thisObj.Length - end);
			return result.ToString();
		}

		/// <summary>
		/// Returns a copy of this string with text replaced using a replacement function.
		/// </summary>
		/// <param name="thisObj"> The string that is being operated on. </param>
		/// <param name="substr"> The text to search for. </param>
		/// <param name="replaceFunction"> A function that is called to produce the text to replace
		/// for every successful match. </param>
		/// <returns> A copy of this string with text replaced. </returns>
		internal static string Replace(string thisObj, string substr, FunctionInstance replaceFunction)
		{
			// Find the first occurrance of substr.
			int start = thisObj.IndexOf(substr, StringComparison.Ordinal);
			if (start == -1)
				return thisObj;
			int end = start + substr.Length;

			// Get the replacement text from the provided function.
			var replaceText = TypeConverter.ToString(replaceFunction.CallLateBound(null, substr, start, thisObj));

			// Replace only the first match.
			var result = new System.Text.StringBuilder(thisObj.Length + (replaceText.Length - substr.Length));
			result.Append(thisObj, 0, start);
			result.Append(replaceText);
			result.Append(thisObj, end, thisObj.Length - end);
			return result.ToString();
		}

		/// <summary>
		/// Returns a copy of this string with text replaced using a regular expression.
		/// </summary>
		/// <param name="thisObj"> The string that is being operated on. </param>
		/// <param name="regExp"> The regular expression to search for. </param>
		/// <param name="replaceText"> A string containing the text to replace for every successful match. </param>
		/// <returns> A copy of this string with text replaced using a regular expression. </returns>
		internal static string Replace(string thisObj, RegExp regExp, string replaceText)
		{
			return regExp.Replace(thisObj, replaceText);
		}

		/// <summary>
		/// Returns a copy of this string with text replaced using a regular expression and a
		/// replacement function.
		/// </summary>
		/// <param name="thisObj"> The string that is being operated on. </param>
		/// <param name="regExp"> The regular expression to search for. </param>
		/// <param name="replaceFunction"> A function that is called to produce the text to replace
		/// for every successful match. </param>
		/// <returns> A copy of this string with text replaced using a regular expression. </returns>
		internal static string Replace(string thisObj, RegExp regExp, FunctionInstance replaceFunction)
		{
			return regExp.Replace(thisObj, replaceFunction);
		}

		/// <summary>
		/// Returns the position of the first substring match.
		/// </summary>
		/// <param name="thisObj"> The string that is being operated on. </param>
		/// <param name="substrOrRegExp"> The string or regular expression to search for. </param>
		/// <returns> The character position of the first match, or -1 if no match was found. </returns>
		public static int Search(string thisObj, object substrOrRegExp)
		{
			if (substrOrRegExp is RegExp)
				// substrOrRegExp is a regular expression.
				return ((RegExp)substrOrRegExp).Search(thisObj);
			
			if (TypeUtilities.IsUndefined(substrOrRegExp))
				// substrOrRegExp is undefined.
				return 0;

			// substrOrRegExp is a string (or convertible to a string).
			return thisObj.IndexOf(TypeConverter.ToString(substrOrRegExp), StringComparison.Ordinal);
		}

		/// <summary>
		/// Extracts a section of the string and returns a new string.
		/// </summary>
		/// <param name="thisObj"> The string that is being operated on. </param>
		/// <param name="start"> The character position to start extracting. </param>
		/// <param name="end"> The character position to stop extacting. </param>
		/// <returns> A section of the string. </returns>
		public static string Slice(string thisObj, int start, int end)
		{
			if(end==0)
				end=int.MaxValue;
			
			// Negative offsets are measured from the end of the string.
			if (start < 0)
				start += thisObj.Length;
			if (end < 0)
				end += thisObj.Length;

			// Constrain the parameters to within the limits of the string.
			start = Math.Min(Math.Max(start, 0), thisObj.Length);
			end = Math.Min(Math.Max(end, 0), thisObj.Length);
			if (end <= start)
				return string.Empty;

			return thisObj.Substring(start, end - start);
		}

		/// <summary>
		/// Splits this string into an array of strings by separating the string into substrings.
		/// </summary>
		/// <param name="engine"> The current script environment. </param>
		/// <param name="thisObj"> The string that is being operated on. </param>
		/// <param name="separator"> A string or regular expression that indicates where to split the string. </param>
		/// <param name="limit"> The maximum number of array items to return.  Defaults to unlimited. </param>
		/// <returns> An array containing the split strings. </returns>
		public static Nitrassic.Library.Array Split(ScriptEngine engine, string thisObj, object separator, double limit)
		{
			if(limit==0)
				limit=uint.MaxValue;
			
			// Limit defaults to unlimited.  Note the ToUint32() conversion.
			uint limit2 = uint.MaxValue;
			if (TypeUtilities.IsUndefined(limit) == false)
				limit2 = TypeConverter.ToUint32(limit);

			// Call separate methods, depending on whether the separator is a regular expression.
			if (separator is RegExp)
				return Split(thisObj, (RegExp)separator, limit2);
			else
				return Split(engine, thisObj, TypeConverter.ToString(separator), limit2);
		}

		/// <summary>
		/// Splits this string into an array of strings by separating the string into substrings.
		/// </summary>
		/// <param name="thisObj"> The string that is being operated on. </param>
		/// <param name="regExp"> A regular expression that indicates where to split the string. </param>
		/// <param name="limit"> The maximum number of array items to return.  Defaults to unlimited. </param>
		/// <returns> An array containing the split strings. </returns>
		internal static Nitrassic.Library.Array Split(string thisObj, RegExp regExp, uint limit)
		{
			if(limit==0)
				limit=uint.MaxValue;
			
			return regExp.Split(thisObj, limit);
		}

		/// <summary>
		/// Splits this string into an array of strings by separating the string into substrings.
		/// </summary>
		/// <param name="engine"> The current script environment. </param>
		/// <param name="thisObj"> The string that is being operated on. </param>
		/// <param name="separator"> A string that indicates where to split the string. </param>
		/// <param name="limit"> The maximum number of array items to return.  Defaults to unlimited. </param>
		/// <returns> An array containing the split strings. </returns>
		internal static Nitrassic.Library.Array Split(ScriptEngine engine, string thisObj, string separator, uint limit)
		{
			if(limit==0)
				limit=uint.MaxValue;
			
			if (string.IsNullOrEmpty(separator))
			{
				// If the separator is empty, split the string into individual characters.
				var result = Nitrassic.Library.Array.New(engine);
				for (int i = 0; i < thisObj.Length; i ++)
					result[i] = thisObj[i].ToString();
				return result;
			}
			var splitStrings = thisObj.Split(new string[] { separator }, StringSplitOptions.None);
			if (limit < splitStrings.Length)
			{
				var splitStrings2 = new string[limit];
				System.Array.Copy(splitStrings, splitStrings2, (int)limit);
				splitStrings = splitStrings2;
			}
			return Nitrassic.Library.Array.New(engine,splitStrings);
		}

		/// <summary>
		/// Returns the characters in a string beginning at the specified location through the specified number of characters.
		/// </summary>
		/// <param name="thisObj"> The string that is being operated on. </param>
		/// <param name="start"> The character position to start extracting. </param>
		/// <param name="length"> The number of characters to extract. </param>
		/// <returns> A substring of this string. </returns>
		public static string Substr(string thisObj, int start, int length)
		{
			if(length==0)
				length = int.MaxValue;
			
			// If start is less than zero, it is measured from the end of the string.
			if (start < 0)
				start = Math.Max(start + thisObj.Length, 0);

			// Compute the actual length.
			length = Math.Max(Math.Min(length, thisObj.Length - start), 0);
			if (length <= 0)
				return string.Empty;

			// Extract the substring.
			return thisObj.Substring(start, length);
		}

		/// <summary>
		/// Returns the characters in a string between two indexes into the string.
		/// </summary>
		/// <param name="thisObj"> The string that is being operated on. </param>
		/// <param name="start"> The character position to start extracting. </param>
		/// <param name="end"> The character position to stop extracting. </param>
		/// <returns> A substring of this string. </returns>
		public static string Substring(string thisObj, int start, int end)
		{
			if(end==0)
				end=int.MaxValue;
			
			return Slice(thisObj, Math.Max(Math.Min(start, end), 0), Math.Max(Math.Max(start, end), 0));
		}

		/// <summary>
		/// Converts the characters within this string to lowercase while respecting the current
		/// locale.
		/// </summary>
		/// <returns> A copy of this string with the characters converted to lowercase. </returns>
		public static string ToLocaleLowerCase(string thisObj)
		{
			return thisObj.ToLower();
		}

		/// <summary>
		/// Converts the characters within this string to uppercase while respecting the current
		/// locale.
		/// </summary>
		/// <returns> A copy of this string with the characters converted to uppercase. </returns>
		public static string ToLocaleUpperCase(string thisObj)
		{
			return thisObj.ToUpper();
		}

		/// <summary>
		/// Returns the calling string value converted to lowercase.
		/// </summary>
		/// <returns> A copy of this string with the characters converted to lowercase. </returns>
		public static string ToLowerCase(string thisObj)
		{
			return thisObj.ToLowerInvariant();
		}

		/// <summary>
		/// Returns a string representing the current object.
		/// </summary>
		/// <returns> A string representing the current object. </returns>
		public static string ToString(string thisObj)
		{
			return thisObj;
		}

		/// <summary>
		/// Returns the calling string value converted to uppercase.
		/// </summary>
		/// <returns> A copy of this string with the characters converted to uppercase. </returns>
		public static string ToUpperCase(string thisObj)
		{
			return thisObj.ToUpperInvariant();
		}

		private static char[] trimCharacters = new char[] {
			// Whitespace
			'\x09', '\x0B', '\x0C', '\x20', '\xA0', '\xFEFF',

			// Unicode space separator
			'\u1680', '\u180E', '\u2000', '\u2001',
			'\u2002', '\u2003', '\u2004', '\u2005',
			'\u2006', '\u2007', '\u2008', '\u2009',
			'\u200A', '\u202F', '\u205F', '\u3000', 

			// Line terminators
			'\x0A', '\x0D', '\u2028', '\u2029',
		};

		/// <summary>
		/// Trims whitespace from the beginning and end of the string.
		/// </summary>
		/// <returns></returns>
		public static string Trim(string thisObj)
		{
			return thisObj.Trim(trimCharacters);
		}

		/// <summary>
		/// Trims whitespace from the beginning of the string.
		/// </summary>
		/// <returns></returns>
		public static string TrimLeft(string thisObj)
		{
			return thisObj.TrimStart(trimCharacters);
		}

		/// <summary>
		/// Trims whitespace from the beginning of the string.
		/// </summary>
		/// <returns></returns>
		public static string TrimRight(string thisObj)
		{
			return thisObj.TrimEnd(trimCharacters);
		}

		/// <summary>
		/// Returns the underlying primitive value of the current object.
		/// </summary>
		/// <returns> The underlying primitive value of the current object. </returns>
		public static string ValueOf(string thisObj)
		{
			return thisObj;
		}
		
		//	 ECMASCRIPT 6 FUNCTIONS
		//_________________________________________________________________________________________

		/// <summary>
		/// Determines whether a string begins with the characters of another string.
		/// </summary>
		/// <param name="engine"> The script engine. </param>
		/// <param name="thisObj"> The string that is being operated on. </param>
		/// <param name="searchStringObj"> The characters to be searched for at the start of this string. </param>
		/// <param name="position"> The position at which to begin searching.  Defaults to zero. </param>
		/// <returns> <c>true</c> if this string starts with the given string, <c>false</c> otherwise. </returns>
		public static bool StartsWith(ScriptEngine engine, string thisObj, object searchStringObj, int position)
		{
			if (TypeUtilities.IsRegularExpression(searchStringObj))
				throw new JavaScriptException(engine, "TypeError", "Substring argument must not be a regular expression.");
			string searchString = TypeConverter.ToString(searchStringObj);
			if (position == 0)
				return thisObj.StartsWith(searchString);
			position = Math.Min(Math.Max(0, position), thisObj.Length);
			if (position + searchString.Length > thisObj.Length)
				return false;
			return thisObj.Substring(position, searchString.Length) == searchString;
		}

		/// <summary>
		/// Determines whether a string ends with the characters of another string.
		/// </summary>
		/// <param name="engine"> The script engine. </param>
		/// <param name="thisObj"> The string that is being operated on. </param>
		/// <param name="searchStringObj"> The characters to be searched for at the end of this string. </param>
		/// <param name="position"> Search within the string as if the string were only this long.
		/// Defaults to the string's actual length. </param>
		/// <returns> <c>true</c> if this string ends with the given string, <c>false</c> otherwise. </returns>
		public static bool EndsWith(ScriptEngine engine, string thisObj, object searchStringObj, int position)
		{
			if(position==0)
				position=int.MaxValue;
			
			if (TypeUtilities.IsRegularExpression(searchStringObj))
				throw new JavaScriptException(engine, "TypeError", "Substring argument must not be a regular expression.");
			string searchString = TypeConverter.ToString(searchStringObj);
			if (position == int.MaxValue)
				return thisObj.EndsWith(searchString);
			position = Math.Min(Math.Max(0, position), thisObj.Length);
			if (searchString.Length > position)
				return false;
			return thisObj.Substring(position - searchString.Length, searchString.Length) == searchString;
		}

		/// <summary>
		/// Determines whether a string contains the characters of another string.
		/// </summary>
		/// <param name="thisObj"> The string that is being operated on. </param>
		/// <param name="searchString"> The characters to be searched for. </param>
		/// <param name="position"> The position at which to begin searching.  Defaults to zero. </param>
		/// <returns> <c>true</c> if this string contains the given string, <c>false</c> otherwise. </returns>
		public static bool Contains(string thisObj, string searchString, int position)
		{
			position = Math.Min(Math.Max(0, position), thisObj.Length);
			return thisObj.IndexOf(searchString, position) >= 0;
		}

		/// <summary>
		/// Repeats this string a number of times and returns the result.
		/// </summary>
		/// <param name="engine"> The current ScriptEngine instance. </param>
		/// <param name="thisObj"> The string that is being operated on. </param>
		/// <param name="count"> The number of times to repeat the string.  Must be zero or higher. </param>
		/// <returns> A repeated string. </returns>
		public static string Repeat(ScriptEngine engine, string thisObj, int count)
		{
			if (count < 0 || count == int.MaxValue)
				throw new JavaScriptException(engine, "RangeError", "The count parameter is out of range.");
			var result = new StringBuilder();
			for (int i = 0; i < count; i ++)
				result.Append(thisObj);
			return result.ToString();
		}



		//	 JAVASCRIPT FUNCTIONS (HTML WRAPPER FUNCTIONS)
		//_________________________________________________________________________________________

		/// <summary>
		/// Wraps the string with an anchor tag.
		/// </summary>
		/// <param name="thisObj"> The string that is being operated on. </param>
		/// <param name="name"> The name of the anchor. </param>
		/// <returns> </returns>
		public static string Anchor(string thisObj, string name)
		{
			return string.Format(@"<a name=""{1}"">{0}</a>", thisObj, name);
		}

		/// <summary>
		/// Wraps the string with a big tag.
		/// </summary>
		/// <param name="thisObj"> The string that is being operated on. </param>
		/// <returns></returns>
		public static string Big(string thisObj)
		{
			return string.Format("<big>{0}</big>", thisObj);
		}

		/// <summary>
		/// Wraps the string with a blink tag.
		/// </summary>
		/// <param name="thisObj"> The string that is being operated on. </param>
		/// <returns></returns>
		public static string Blink(string thisObj)
		{
			return string.Format("<blink>{0}</blink>", thisObj);
		}

		/// <summary>
		/// Wraps the string with a bold (b) tag.
		/// </summary>
		/// <param name="thisObj"> The string that is being operated on. </param>
		/// <returns></returns>
		public static string Bold(string thisObj)
		{
			return string.Format("<b>{0}</b>", thisObj);
		}

		/// <summary>
		/// Wraps the string with a tt tag.
		/// </summary>
		/// <returns></returns>
		public static string Fixed(string thisObj)
		{
			return string.Format("<tt>{0}</tt>", thisObj);
		}

		/// <summary>
		/// Wraps the string with a font tag that specifies the given color.
		/// </summary>
		/// <param name="thisObj"> The string that is being operated on. </param>
		/// <param name="colorValue"> The color value or name. </param>
		/// <returns></returns>
		public static string FontColor(string thisObj, string colorValue)
		{
			return string.Format(@"<font color=""{1}"">{0}</font>", thisObj, colorValue);
		}

		/// <summary>
		/// Wraps the string with a font tag that specifies the given font size.
		/// </summary>
		/// <param name="thisObj"> The string that is being operated on. </param>
		/// <param name="size"> The font size, specified as an integer. </param>
		/// <returns></returns>
		public static string FontSize(string thisObj, string size)
		{
			return string.Format(@"<font size=""{1}"">{0}</font>", thisObj, size);
		}

		/// <summary>
		/// Wraps the string with a italics (i) tag.
		/// </summary>
		/// <param name="thisObj"> The string that is being operated on. </param>
		/// <returns></returns>
		public static string Italics(string thisObj)
		{
			return string.Format("<i>{0}</i>", thisObj);
		}

		/// <summary>
		/// Wraps the string with a hyperlink.
		/// </summary>
		/// <param name="thisObj"> The string that is being operated on. </param>
		/// <param name="href"> The hyperlink URL. </param>
		/// <returns></returns>
		public static string Link(string thisObj, string href)
		{
			return string.Format(@"<a href=""{1}"">{0}</a>", thisObj, href);
		}

		/// <summary>
		/// Wraps the string in a <c>small</c> tag.
		/// </summary>
		/// <param name="thisObj"> The string that is being operated on. </param>
		/// <returns></returns>
		public static string Small(string thisObj)
		{
			return string.Format("<small>{0}</small>", thisObj);
		}

		/// <summary>
		/// Wraps the string in a <c>strike</c> tag.
		/// </summary>
		/// <param name="thisObj"> The string that is being operated on. </param>
		/// <returns></returns>
		public static string Strike(string thisObj)
		{
			return string.Format("<strike>{0}</strike>", thisObj);
		}

		/// <summary>
		/// Wraps the string in a <c>sub</c> tag.
		/// </summary>
		/// <param name="thisObj"> The string that is being operated on. </param>
		/// <returns></returns>
		public static string Sub(string thisObj)
		{
			return string.Format("<sub>{0}</sub>", thisObj);
		}

		/// <summary>
		/// Wraps the string in a <c>sup</c> tag.
		/// </summary>
		/// <param name="thisObj"> The string that is being operated on. </param>
		/// <returns></returns>
		public static string Sup(string thisObj)
		{
			return string.Format("<sup>{0}</sup>", thisObj);
		}
		
		/// <summary>
		/// Called when the String object is invoked like a function, e.g. var x = String().
		/// Returns an empty string.
		/// </summary>
		public static string OnCall()
		{
			return string.Empty;
		}

		/// <summary>
		/// Called when the String object is invoked like a function, e.g. var x = String(NaN).
		/// Converts the given argument into a string value (not a String object).
		/// </summary>
		public static string OnCall(string value)
		{
			// Note: the parameter conversion machinery handles the required conversion.
			return value;
		}

		/// <summary>
		/// Creates a new String instance and initializes it to the empty string.
		/// </summary>
		public static string OnConstruct(ScriptEngine engine)
		{
			return "";
		}

		/// <summary>
		/// Creates a new String instance and initializes it to the given value.
		/// </summary>
		/// <param name="value"> The value to initialize to. </param>
		public static string OnConstruct(ScriptEngine engine,string value)
		{
			// Note: the parameter conversion machinery handles the required conversion.
			return value;
		}
		


		//	 JAVASCRIPT FUNCTIONS
		//_________________________________________________________________________________________

		/// <summary>
		/// Returns a string created by using the specified sequence of Unicode values.
		/// </summary>
		/// <param name="charCodes"> An array of 16-bit character codes. </param>
		/// <returns></returns>
		
		public static string FromCharCode(params double[] charCodes)
		{
			// Note: charCodes must be an array of doubles, because the default marshalling
			// rule to int uses ToInteger() and there are no marshalling rules for short, ushort
			// or uint.  ToInteger() doesn't preserve the wrapping behaviour we need.
			var result = new System.Text.StringBuilder(charCodes.Length);
			foreach (double charCode in charCodes)
				result.Append((char)TypeConverter.ToUint16(charCode));
			return result.ToString();
		}

		/// <summary>
		/// Returns a string created by using the specified sequence of Unicode codepoints.
		/// </summary>
		/// <param name="scriptEngine"> The script engine. </param>
		/// <param name="codePoints"> An array of unicode code points. </param>
		/// <returns></returns>
	
		public static string FromCodePoint(ScriptEngine scriptEngine, params double[] codePoints)
		{
			// Note: charCodes must be an array of doubles, because the default marshalling
			// rule to int uses ToInteger() and ToInteger() does not throw a RangeError if the
			// input value is not an integer.
			var result = new System.Text.StringBuilder(codePoints.Length);
			foreach (double codePointDouble in codePoints)
			{
				int codePoint = (int) codePointDouble;
				if (codePoint < 0 || codePoint > 0x10FFFF || (double)codePoint != codePointDouble)
					throw new JavaScriptException(scriptEngine, "RangeError", string.Format("Invalid code point {0}", codePointDouble));
				if (codePoint <= 65535)
					result.Append((char)codePoint);
				else
				{
					result.Append((char)((codePoint - 65536)/1024 + 0xD800));
					result.Append((char)((codePoint - 65536) % 1024 + 0xDC00));
				}
			}
			return result.ToString();
		}
		
	}
}
