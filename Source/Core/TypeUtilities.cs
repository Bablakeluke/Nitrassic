﻿using System;
using System.Collections.Generic;
using System.Text;
using Nitrassic.Library;

namespace Nitrassic
{

	/// <summary>
	/// Contains type-related functionality that isn't conversion or comparison.
	/// </summary>
	public static class TypeUtilities
	{
		/// <summary>
		/// Gets the type name for the given object.  Used by the typeof operator.
		/// </summary>
		/// <param name="obj"> The object to get the type name for. </param>
		/// <returns> The type name for the given object. </returns>
		public static string TypeOf(object obj)
		{
			if (obj == null || obj == Undefined.Value)
				return "undefined";
			if (obj == Null.Value)
				return "object";
			if (obj is bool)
				return "boolean";
			if (obj is double || obj is int || obj is uint)
				return "number";
			if (obj is string || obj is ConcatenatedString)
				return "string";
			if (obj is FunctionInstance)
				return "function";
			if (obj is ObjectInstance)
				return "object";
			throw new InvalidOperationException("Unsupported object type.");
		}

		/// <summary>
		/// Returns <c>true</c> if the given value is undefined.
		/// </summary>
		/// <param name="obj"> The object to check. </param>
		/// <returns> <c>true</c> if the given value is undefined; <c>false</c> otherwise. </returns>
		internal static bool IsUndefined(object obj)
		{
			return obj == null || obj == Undefined.Value;
		}

		/// <summary>
		/// Returns <c>true</c> if the given value is a supported numeric type.
		/// </summary>
		/// <param name="obj"> The object to check. </param>
		/// <returns> <c>true</c> if the given value is a supported numeric type; <c>false</c>
		/// otherwise. </returns>
		internal static bool IsNumeric(object obj)
		{
			return obj is double || obj is int || obj is uint;
		}

		/// <summary>
		/// Returns <c>true</c> if the given value is a supported string type.
		/// </summary>
		/// <param name="obj"> The object to check. </param>
		/// <returns> <c>true</c> if the given value is a supported string type; <c>false</c>
		/// otherwise. </returns>
		internal static bool IsString(object obj)
		{
			return obj is string || obj is ConcatenatedString;
		}

		/// <summary>
		/// Converts the given value into a standard .NET type, suitable for returning from an API.
		/// </summary>
		/// <param name="obj"> The value to normalize. </param>
		/// <returns> The value as a standard .NET type. </returns>
		internal static object NormalizeValue(object obj)
		{
			if (obj == null)
				return Undefined.Value;
			else if (obj is double)
			{
				var numericResult = (double)obj;
				if ((double)((int)numericResult) == numericResult)
					return (int)numericResult;
			}
			else if (obj is uint)
			{
				var uintValue = (uint)obj;
				if ((int)uintValue >= 0)
					return (int)uintValue;
				return (double)uintValue;
			}
			else if (obj is ConcatenatedString)
				obj = ((ConcatenatedString)obj).ToString();
			return obj;
		}

		/// <summary>
		/// Enumerates the names of the enumerable properties on the given object, including
		/// properties defined on the object's prototype.  Used by the for-in statement.
		/// </summary>
		/// <param name="engine"> The script engine used to convert the given value to an object. </param>
		/// <param name="obj"> The object to enumerate. </param>
		/// <returns> An enumerator that iteratively returns property names. </returns>
		public static IEnumerable<string> EnumeratePropertyNames(ScriptEngine engine, object obj)
		{
			
			// Get the proto:
			Prototype obj2=TypeConverter.ToPrototype(obj,engine);
			
			if (obj2==null)
				yield break;
			
			foreach (KeyValuePair<string, PropertyVariable> property in obj2.Properties)
			{
				if(property.Value.IsEnumerable)
				{
					yield return property.Key;
				}
			}
			
		}

		/// <summary>
		/// Adds two objects together, as if by the javascript addition operator.
		/// </summary>
		/// <param name="left"> The left hand side operand. </param>
		/// <param name="right"> The right hand side operand. </param>
		/// <returns> Either a number or a concatenated string. </returns>
		public static object Add(object left, object right)
		{
			var leftPrimitive = TypeConverter.ToPrimitive(left, PrimitiveTypeHint.None);
			var rightPrimitive = TypeConverter.ToPrimitive(right, PrimitiveTypeHint.None);

			if (leftPrimitive is ConcatenatedString)
			{
				return ((ConcatenatedString)leftPrimitive).Concatenate(rightPrimitive);
			}
			else if (leftPrimitive is string || rightPrimitive is string || rightPrimitive is ConcatenatedString)
			{
				return new ConcatenatedString(TypeConverter.ToString(leftPrimitive), TypeConverter.ToString(rightPrimitive));
			}

			return TypeConverter.ToNumber(leftPrimitive) + TypeConverter.ToNumber(rightPrimitive);
		}

		/// <summary>
		/// Determines if the given value is a supported JavaScript primitive.
		/// </summary>
		/// <param name="value"> The value to test. </param>
		/// <returns> <c>true</c> if the given value is a supported JavaScript primitive;
		/// <c>false</c> otherwise. </returns>
		public static bool IsPrimitive(object value)
		{
			if (value == null)
				return true;
			var type = value.GetType();
			return type == typeof(bool) ||
				type == typeof(int) || type == typeof(uint) || type == typeof(double) ||
				type == typeof(string) || type == typeof(ConcatenatedString) ||
				type == typeof(Null) || type == typeof(Undefined);
		}

		/// <summary>
		/// Determines if the given value is a supported JavaScript primitive or derives from
		/// ObjectInstance.
		/// </summary>
		/// <param name="value"> The value to test. </param>
		/// <returns> <c>true</c> if the given value is a supported JavaScript primitive or derives
		/// from ObjectInstance; <c>false</c> otherwise. </returns>
		public static bool IsPrimitiveOrObject(object value)
		{
			if (value == null)
				return true;
			var type = value.GetType();
			return type == typeof(bool) ||
				type == typeof(int) || type == typeof(uint) || type == typeof(double) ||
				type == typeof(string) || type == typeof(ConcatenatedString) ||
				type == typeof(Null) || type == typeof(Undefined) ||
				typeof(ObjectInstance).IsAssignableFrom(type);
		}

		/// <summary>
		/// Throws a TypeError when the given value is <c>null</c> or <c>undefined.</c>
		/// </summary>
		/// <param name="engine"> The associated script engine. </param>
		/// <param name="value"> The value to check. </param>
		/// <param name="functionName"> The name of the function which is doing the check. </param>
		public static void VerifyThisObject(ScriptEngine engine, object value, string functionName)
		{
			if (value == null || value == Undefined.Value)
				throw new JavaScriptException(engine, "TypeError", string.Format("The function '{0}' does not allow the value of 'this' to be undefined", functionName));
			if (value == Null.Value)
				throw new JavaScriptException(engine, "TypeError", string.Format("The function '{0}' does not allow the value of 'this' to be null", functionName));
		}

		/// <summary>
		/// Utility method for create a slice of an array.
		/// </summary>
		/// <param name="args"> The array to slice. </param>
		/// <param name="offset"> The offset to begin the resulting array. </param>
		/// <returns> An array containing the input array elements, excluding the first
		/// <paramref name="offset"/> entries. </returns>
		internal static object[] SliceArray(object[] args, int offset)
		{
			if (offset == 0)
				return args;
			if (offset >= args.Length)
				return new object[0];
			var result = new object[args.Length - offset];
			System.Array.Copy(args, offset, result, 0, args.Length - offset);
			return result;
		}

		/// <summary>
		/// Determines if the given value is a regular expression.
		/// </summary>
		/// <param name="value"> The value to test. </param>
		/// <returns> <c>true</c> if the given value is a regular expression; <c>false</c> otherwise. </returns>
		public static bool IsRegularExpression(object value)
		{
			// TODO: ECMAScript 6 defines IsRegExp in terms of a predefined symbol @@match.
			return value is RegExp;
		}
	}

}
