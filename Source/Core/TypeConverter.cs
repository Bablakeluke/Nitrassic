﻿using System;
using System.Collections.Generic;
using System.Text;
using Nitrassic.Library;

namespace Nitrassic
{
	/// <summary>
	/// Provides a hint on how to do the conversion when converting to a primitive.
	/// </summary>
	public enum PrimitiveTypeHint
	{
		/// <summary>
		/// Use the default behaviour.
		/// </summary>
		None,

		/// <summary>
		/// Prefer converting to a number.
		/// </summary>
		Number,

		/// <summary>
		/// Prefer converting to a string.
		/// </summary>
		String,
	}

	/// <summary>
	/// Implements the JavaScript type conversion rules.
	/// </summary>
	public static class TypeConverter
	{
		/// <summary>
		/// Converts the given value to the given type.
		/// </summary>
		/// <param name="engine"> The script engine used to create new objects. </param>
		/// <param name="value"> The value to convert. </param>
		/// <typeparam name="T"> The type to convert the value to. </typeparam>
		/// <returns> The converted value. </returns>
		public static T ConvertTo<T>(ScriptEngine engine, object value)
		{
			return (T)ConvertTo(engine, value, typeof(T));
		}

		/// <summary>
		/// Converts the given value to the given type.
		/// </summary>
		/// <param name="engine"> The script engine used to create new objects. </param>
		/// <param name="value"> The value to convert. </param>
		/// <param name="type"> The type to convert the value to. </param>
		/// <returns> The converted value. </returns>
		public static object ConvertTo(ScriptEngine engine, object value, Type type)
		{
			if (type == typeof(bool))
				return ToBoolean(value);
			if (type == typeof(int))
				return ToInteger(value);
			if (type == typeof(double))
				return ToNumber(value);
			if (type == typeof(string))
				return ToString(value);
			return value;
		}

		/// <summary>
		/// Converts any JavaScript value to a primitive boolean value.
		/// </summary>
		/// <param name="value"> The value to convert. </param>
		/// <returns> A primitive boolean value. </returns>
		public static bool ToBoolean(object value)
		{
			if (value == null || value == Null.Value)
				return false;
			if (value == Undefined.Value)
				return false;
			
			// Get the type:
			Type valuesType=value.GetType();
			
			if (valuesType==typeof(bool))
				return (bool)value;
			if (valuesType==typeof(int))
				return ((int)value) != 0;
			if (valuesType==typeof(uint))
				return ((uint)value) != 0;
			if (valuesType==typeof(short))
				return ((short)value) != 0;
			if (valuesType==typeof(ushort))
				return ((ushort)value) != 0;
			if (valuesType==typeof(byte))
				return ((byte)value) != 0;
			if (valuesType==typeof(sbyte))
				return ((sbyte)value) != 0;
				
			if (valuesType==typeof(double)){
				double dbl=((double)value);
				return dbl != 0 && double.IsNaN(dbl) == false;
			}
			
			if (valuesType==typeof(float)){
				float flt=((float)value);
				return flt != 0 && float.IsNaN(flt) == false;
			}
			
			if (valuesType==typeof(string))
				return ((string)value).Length > 0;
			if (valuesType==typeof(ConcatenatedString))
				return ((ConcatenatedString)value).Length > 0;
			
			// Not null so it's some other object; this is fine:
			return true;
		}
		
		/// <summary>Gets the number of bytes of accuracy for a numeric type. 0 if it's not numeric.</summary>
		private static int Accuracy(Type a){
			
			if (a==typeof(long) || a==typeof(ulong))
				return 8;
				
			if (a==typeof(int) || a==typeof(uint))
				return 4;
				
			if (a==typeof(short) || a==typeof(ushort))
				return 2;
				
			if (a==typeof(sbyte) || a==typeof(byte))
				return 1;
			
			return 0;
			
		}
		
		/// <summary>Gets the most accurate integer type out of the given two.</summary>
		/// <returns>Null if one or both are not integer types.</returns>
		public static Type MostAccurateInteger(Type a,Type b){
			
			int accA=Accuracy(a);
			int accB=Accuracy(b);
			
			if(accA==0 || accB==0){
				return null;
			}
			
			if(accA>accB){
				return a;
			}
			
			return b;
			
		}
		
		/// <summary>
		/// Gets the prototype of an object unless it's an object instance.
		/// Essentially gets the thing you can search for functionality on.
		/// </summary>
		public static Prototype ToPrototype(object obj,ScriptEngine engine){
			
			if (obj==null || obj==Undefined.Value || obj == Null.Value)
				return null;
			
			// Get the type:
			Type type=obj.GetType();
			
			// Get the prototype or the object itself:
			
			if(type==typeof(Prototype)){
				return (Prototype)obj;
			}
			
			return engine.Prototypes[type];
			
		}
		
		/// <summary>
		/// Converts any JavaScript value to a primitive number value.
		/// </summary>
		/// <param name="value"> The value to convert. </param>
		/// <returns> A primitive number value. </returns>
		public static double ToNumber(object value)
		{
			
			if (value == null || value == Undefined.Value)
				return double.NaN;
			
			if (value == Null.Value)
				return +0;
			
			// Get the type:
			Type valuesType=value.GetType();
			
			if (valuesType==typeof(double))
				return (double)value;
			if (valuesType==typeof(int))
				return (double)(int)value;
			if (valuesType==typeof(uint))
				return (double)(uint)value;
			if (valuesType==typeof(byte))
				return (double)(byte)value;
			if (valuesType==typeof(sbyte))
				return (double)(sbyte)value;
			if (valuesType==typeof(short))
				return (double)(short)value;
			if (valuesType==typeof(ushort))
				return (double)(ushort)value;
			if (valuesType==typeof(float))
				return (double)(float)value;
			if (valuesType==typeof(bool))
				return (bool)value ? 1 : 0;
			if (valuesType==typeof(string))
				return NumberParser.CoerceToNumber((string)value);
			
			// If all else fails, use the string:
			double number=NumberParser.CoerceToNumber(value.ToString());
			
			if(number==double.NaN){
				throw new ArgumentException("Cannot convert object of type '"+valuesType+"' to a number.", "value");
			}
			
			return number;
		}
		
		/// <summary>
		/// Converts any JavaScript value to a primitive string value.
		/// </summary>
		/// <param name="value"> The value to convert. </param>
		/// <returns> A primitive string value. </returns>
		public static string ToString(object value)
		{
			if (value == null || value == Undefined.Value)
				return "undefined";
			if (value == Null.Value)
				return "null";
			if (value is bool)
				return (bool)value ? "true" : "false";
			return value.ToString();
		}

		/// <summary>
		/// Converts any JavaScript value to a concatenated string value.
		/// </summary>
		/// <param name="value"> The value to convert. </param>
		/// <returns> A concatenated string value. </returns>
		public static ConcatenatedString ToConcatenatedString(object value)
		{
			if (value is ConcatenatedString)
				return (ConcatenatedString)value;
			return new ConcatenatedString(ToString(value));
		}
		
		/// <summary>
		/// Converts any JavaScript value to a primitive value.
		/// </summary>
		/// <param name="value"> The value to convert. </param>
		/// <param name="preferredType"> Specifies whether toString() or valueOf() should be
		/// preferred when converting to a primitive. </param>
		/// <returns> A primitive (non-object) value. </returns>
		public static object ToPrimitive(object value, PrimitiveTypeHint preferredType)
		{
			if (value is ObjectInstance)
				return ((ObjectInstance)value).GetPrimitiveValue(preferredType);
			else
				return value;
		}

		/// <summary>
		/// Converts any JavaScript value to an integer.
		/// </summary>
		/// <param name="value"> The value to convert. </param>
		/// <returns> An integer value. </returns>
		public static int ToInteger(object value)
		{
			if (value == null || value is Undefined)
				return 0;
			double num = ToNumber(value);
			if (num > 2147483647.0)
				return 2147483647;
			#pragma warning disable 1718
			if (num != num)
				return 0;
			#pragma warning restore 1718
			return (int)num;
		}

		/// <summary>
		/// Converts any JavaScript value to a 32-bit integer.
		/// </summary>
		/// <param name="value"> The value to convert. </param>
		/// <returns> A 32-bit integer value. </returns>
		public static int ToInt32(object value)
		{
			if (value is int)
				return (int)value;
			return (int)(uint)ToNumber(value);
		}

		/// <summary>
		/// Converts any JavaScript value to an unsigned 32-bit integer.
		/// </summary>
		/// <param name="value"> The value to convert. </param>
		/// <returns> An unsigned 32-bit integer value. </returns>
		public static uint ToUint32(object value)
		{
			if (value is uint)
				return (uint)value;
			return (uint)ToNumber(value);
		}

		/// <summary>
		/// Converts any JavaScript value to a signed 16-bit integer.
		/// </summary>
		/// <param name="value"> The value to convert. </param>
		/// <returns> A signed 16-bit integer value. </returns>
		public static short ToInt16(object value)
		{
			return (short)(uint)ToNumber(value);
		}

		/// <summary>
		/// Converts any JavaScript value to an unsigned 16-bit integer.
		/// </summary>
		/// <param name="value"> The value to convert. </param>
		/// <returns> An unsigned 16-bit integer value. </returns>
		public static ushort ToUint16(object value)
		{
			return (ushort)(uint)ToNumber(value);
		}

		/// <summary>
		/// Converts any JavaScript value to a signed 8-bit integer.
		/// </summary>
		/// <param name="value"> The value to convert. </param>
		/// <returns> A signed 8-bit integer value. </returns>
		public static sbyte ToInt8(object value)
		{
			return (sbyte)(uint)ToNumber(value);
		}

		/// <summary>
		/// Converts any JavaScript value to an unsigned 8-bit integer.
		/// </summary>
		/// <param name="value"> The value to convert. </param>
		/// <returns> An unsigned 8-bit integer value. </returns>
		public static byte ToUint8(object value)
		{
			return (byte)(uint)ToNumber(value);
		}

		/// <summary>
		/// Utility method to convert an object array to a typed array.
		/// </summary>
		/// <typeparam name="T"> The type to convert to. </typeparam>
		/// <param name="engine"> The script engine used to create new objects. </param>
		/// <param name="args"> The array to convert. </param>
		/// <param name="offset"> The number of elements to skip at the beginning of the array. </param>
		/// <returns> A typed array. </returns>
		internal static T[] ConvertParameterArrayTo<T>(ScriptEngine engine, object[] args, int offset)
		{
			if (offset >= args.Length)
				return new T[0];
			var result = new T[args.Length - offset];
			for (int i = 0; i < result.Length; i ++)
			{
				result[i] = ConvertTo<T>(engine, args[offset + i]);
			}
			return result;
		}
	}

}
