using System;

namespace Nitrassic.Compiler
{
	/// <summary>
	/// Outputs IL that converts between types.
	/// </summary>
	internal static class EmitConversion
	{
		/// <summary>
		/// Pops the value on the stack, converts it to the given type, then pushes the result
		/// onto the stack.
		/// </summary>
		/// <param name="generator"> The IL generator. </param>
		/// <param name="fromType"> The type to convert from. </param>
		/// <param name="toType"> The type to convert to. </param>
		public static void Convert(ILGenerator generator, Type fromType, Type toType)
		{
			Convert(generator, fromType, toType, null, null);
		}

		/// <summary>
		/// Pops the value on the stack, converts it to the given type, then pushes the result
		/// onto the stack.
		/// </summary>
		/// <param name="generator"> The IL generator. </param>
		/// <param name="fromType"> The type to convert from. </param>
		/// <param name="toType"> The type to convert to. </param>
		/// <param name="optimizationInfo"> Information about the line number, function and path. </param>
		public static void Convert(ILGenerator generator, Type fromType, Type toType, OptimizationInfo optimizationInfo)
		{
			Convert(generator, fromType, toType, optimizationInfo.Source.Path, optimizationInfo.FunctionName);
		}
		
		/// <summary>
		/// Pops the value on the stack, converts it to the given type, then pushes the result
		/// onto the stack.
		/// </summary>
		/// <param name="generator"> The IL generator. </param>
		/// <param name="fromType"> The type to convert from. </param>
		/// <param name="toType"> The type to convert to. </param>
		/// <param name="path"> The path of the javascript source file that is currently executing. </param>
		/// <param name="function"> The name of the currently executing function. </param>
		public static void Convert(ILGenerator generator, Type fromType, Type toType, string path, string function)
		{
			// Check that a conversion is actually necessary.
			if (fromType == toType)
				return;
			
			if(toType == typeof(object))
			{
				ToAny(generator, fromType);
			}
			else if(toType == typeof(Nitrassic.Undefined))
			{
				generator.Pop();
				EmitHelpers.EmitUndefined(generator);
			}
			else if(toType == typeof(Nitrassic.Null))
			{
				generator.Pop();
				EmitHelpers.EmitNull(generator);
			}
			else if(toType == typeof(bool))
			{
				ToBool(generator, fromType);
			
			}else if (toType==typeof(long)){
				generator.ConvertToInt64();
			}else if(toType==typeof(ulong)){
				generator.ConvertToUnsignedInt64();
			}else if(toType==typeof(int)){
				generator.ConvertToInt32();
			}else if(toType==typeof(uint)){
				generator.ConvertToUnsignedInt32();
			}else if(toType==typeof(short)){
				generator.ConvertToInt16();
			}else if(toType==typeof(ushort)){
				generator.ConvertToUnsignedInt16();
			}else if(toType==typeof(sbyte)){
				generator.ConvertToInt8();
			}else if(toType==typeof(byte)){
				generator.ConvertToUnsignedInt8();
			}
			else if(toType == typeof(double))
			{
				ToNumber(generator, fromType);
			}else if(toType == typeof(string))
			{
				ToString(generator, fromType);
			}
			else if(toType == typeof(ConcatenatedString))
			{
				ToConcatenatedString(generator, fromType);
			}
			else
			{
				throw new NotImplementedException(string.Format("Unsupported primitive type: {0}", toType));
			}
			
		}

		/// <summary>
		/// Pops the value on the stack, converts it to a boolean, then pushes the boolean result
		/// onto the stack.
		/// </summary>
		/// <param name="generator"> The IL generator. </param>
		/// <param name="fromType"> The type to convert from. </param>
		public static void ToBool(ILGenerator generator, Type fromType)
		{
			// Check that a conversion is actually necessary.
			if (fromType == typeof(bool))
				return;

			if(fromType == typeof(Nitrassic.Undefined) || fromType == typeof(Nitrassic.Null)){
				// Converting from undefined or null produces false.
				generator.Pop();
				generator.LoadInt32(0);
			}else if(fromType == typeof(int) || fromType == typeof(uint)){
				// Converting from an integer produces true if the integer is non-zero.
				generator.LoadInt32(0);
				generator.CompareGreaterThanUnsigned();
			}else if(fromType == typeof(double)){
				// Converting from a number produces true if the number is non-zero and not NaN.
				var temp = generator.CreateTemporaryVariable(fromType);
				generator.StoreVariable(temp);

				// input != 0
				generator.LoadVariable(temp);
				generator.LoadDouble(0.0);
				generator.CompareEqual();
				generator.LoadInt32(0);
				generator.CompareEqual();

				// input == input
				generator.LoadVariable(temp);
				generator.Duplicate();
				generator.CompareEqual();

				// &&
				generator.CompareEqual();

				// The temporary variable is no longer needed.
				generator.ReleaseTemporaryVariable(temp);
				
			}else if(fromType == typeof(string)){
				// Converting from a string produces true if the string is not empty.
				generator.Call(ReflectionHelpers.String_Length);
				generator.LoadInt32(0);
				generator.CompareGreaterThan();
				
			}else if(fromType == typeof(ConcatenatedString)){
				// Converting from a string produces true if the string is not empty.
				generator.Call(ReflectionHelpers.ConcatenatedString_Length);
				generator.LoadInt32(0);
				generator.CompareGreaterThan();
				
			}else{
				Console.WriteLine("CVTR "+fromType);
				// Otherwise, fall back to calling TypeConverter.ToBoolean()
				generator.Call(ReflectionHelpers.TypeConverter_ToBoolean);
			}
			
		}

		/// <summary>
		/// Pops the value on the stack, converts it to an integer, then pushes the integer result
		/// onto the stack.
		/// </summary>
		/// <param name="generator"> The IL generator. </param>
		/// <param name="fromType"> The type to convert from. </param>
		public static void ToInteger(ILGenerator generator, Type fromType)
		{
			// Check that a conversion is actually necessary.
			if (fromType == typeof(int) || fromType == typeof(uint) || fromType == typeof(bool))
				return;

			if(fromType == typeof(Nitrassic.Undefined) || fromType == typeof(Nitrassic.Null)){
				// Converting from undefined or null produces 0.
				generator.Pop();
				generator.LoadInt32(0);
				
			}else if(fromType == typeof(double)){
				// Converting from a number produces the following:
				// Any number between -2147483648 and +2147483647 -> itself
				// Any number smaller than -2147483648 -> -2147483648
				// Any number larger than +2147483647 -> +2147483647
				// NaN -> 0

				// bool isPositiveInfinity = input > 2147483647.0
				var isPositiveInfinity = generator.CreateTemporaryVariable(typeof(bool));
				generator.Duplicate();
				generator.LoadDouble(2147483647.0);
				generator.CompareGreaterThan();
				generator.StoreVariable(isPositiveInfinity);

				// bool notNaN = input == input
				var notNaN = generator.CreateTemporaryVariable(typeof(bool));
				generator.Duplicate();
				generator.Duplicate();
				generator.CompareEqual();
				generator.StoreVariable(notNaN);

				// input = (int)input
				// Infinity -> -2147483648
				// -Infinity -> -2147483648
				// NaN -> -2147483648
				generator.ConvertToInt32();

				// input = input & -((int)notNaN)
				generator.LoadVariable(notNaN);
				generator.Negate();
				generator.BitwiseAnd();

				// input = input - (int)isPositiveInfinity
				generator.LoadVariable(isPositiveInfinity);
				generator.Subtract();

				// The temporary variables are no longer needed.
				generator.ReleaseTemporaryVariable(notNaN);
				generator.ReleaseTemporaryVariable(isPositiveInfinity);
				
			}else if(fromType == typeof(string) || fromType == typeof(ConcatenatedString) ||
				fromType == typeof(object) || fromType == typeof(Library.ObjectInstance)){
				
				// Otherwise, fall back to calling TypeConverter.ToInteger()
				generator.Call(ReflectionHelpers.TypeConverter_ToInteger);
				
			}else{
			
				throw new NotImplementedException(string.Format("Unsupported primitive type: {0}", fromType));
			
			}
			
		}

		/// <summary>
		/// Pops the value on the stack, converts it to an integer, then pushes the integer result
		/// onto the stack.  Large numbers wrap (i.e. 4294967296 -> 0).
		/// </summary>
		/// <param name="generator"> The IL generator. </param>
		/// <param name="fromType"> The type to convert from. </param>
		public static void ToInt32(ILGenerator generator, Type fromType)
		{
			// Check that a conversion is actually necessary.
			if (fromType == typeof(int) || fromType == typeof(uint) || fromType == typeof(bool))
				return;

			if(fromType == typeof(Nitrassic.Undefined) || fromType == typeof(Nitrassic.Null)){
				// Converting from undefined or null produces 0.
				generator.Pop();
				generator.LoadInt32(0);
				
			}else if(fromType == typeof(double)){
				// Converting from a number produces the number mod 4294967296.  NaN produces 0.
				generator.ConvertToUnsignedInt32();
				
			}else if(fromType == typeof(string) || fromType == typeof(ConcatenatedString) ||
					fromType == typeof(object) || fromType == typeof(Library.ObjectInstance)){
				
				// Otherwise, fall back to calling TypeConverter.ToInt32()
				generator.Call(ReflectionHelpers.TypeConverter_ToInt32);
			
			}else{
			
				throw new NotImplementedException(string.Format("Unsupported primitive type: {0}", fromType));
			
			}
			
		}

		/// <summary>
		/// Pops the value on the stack, converts it to an unsigned integer, then pushes the
		/// integer result onto the stack.  Large numbers wrap (i.e. 4294967296 -> 0).
		/// </summary>
		/// <param name="generator"> The IL generator. </param>
		/// <param name="fromType"> The type to convert from. </param>
		public static void ToUInt32(ILGenerator generator, Type fromType)
		{
			ToInt32(generator, fromType);
		}
		
		/// <summary>Converts an integer numeric type to another numeric type.
		/// Used to increase the accuracy, e.g. a byte to an int32.
		/// </summary>
		public static void ToNumber(ILGenerator generator, Type fromType, Type toType)
		{
			
			if(fromType==toType){
				// Nothing to do.
				return;
			}
			
			if (toType==typeof(long)){
				generator.ConvertToInt64();
			}else if(toType==typeof(ulong)){
				generator.ConvertToUnsignedInt64();
			}else if(toType==typeof(int)){
				generator.ConvertToInt32();
			}else if(toType==typeof(uint)){
				generator.ConvertToUnsignedInt32();
			}else if(toType==typeof(short)){
				generator.ConvertToInt16();
			}else if(toType==typeof(ushort)){
				generator.ConvertToUnsignedInt16();
			}else if(toType==typeof(sbyte)){
				generator.ConvertToInt8();
			}else if(toType==typeof(byte)){
				generator.ConvertToUnsignedInt8();
			}
			
		}
		
		/// <summary>
		/// Pops the value on the stack, converts it to a double, then pushes the double result
		/// onto the stack.
		/// </summary>
		/// <param name="generator"> The IL generator. </param>
		/// <param name="fromType"> The type to convert from. </param>
		public static void ToNumber(ILGenerator generator, Type fromType)
		{
			// Check that a conversion is actually necessary.
			if (fromType == typeof(double))
				return;

			if(fromType == typeof(Nitrassic.Undefined))
			{
				// Converting from undefined produces NaN.
				generator.Pop();
				generator.LoadDouble(double.NaN);
				
			}
			else if(fromType == typeof(Nitrassic.Null))
			{
				// Converting from null produces 0.
				generator.Pop();
				generator.LoadDouble(0.0);
			}else if(fromType == typeof(Library.Date))
			{
				// Converting from a date produces the time in ms.
				generator.Call(ReflectionHelpers.Date_ToNumber);
			}
			else if(fromType == typeof(bool))
			{
				// Converting from a boolean produces 0 if the boolean is false, or 1 if the boolean is true.
				generator.ConvertToDouble();
				
			}
			else if(fromType == typeof(int) || fromType==typeof(short) || fromType==typeof(sbyte) || fromType==typeof(long))
			{
				// Converting from int32 produces the same number.
				generator.ConvertToDouble();
				
			}
			else if(fromType == typeof(uint) || fromType==typeof(ushort) || fromType==typeof(byte) || fromType==typeof(ulong))
			{
				// Converting from a number produces the following:
				generator.ConvertUnsignedToDouble();
				
			}
			else if(fromType == typeof(string) || fromType == typeof(ConcatenatedString) || 
					fromType == typeof(object) || fromType == typeof(Library.ObjectInstance))
			{
				
				// Otherwise, fall back to calling TypeConverter.ToNumber()
				generator.Call(ReflectionHelpers.TypeConverter_ToNumber);
			
			}
			else
			{
				throw new NotImplementedException(string.Format("Unsupported primitive type: {0}", fromType));
			}
			
		}

		/// <summary>
		/// Pops the value on the stack, converts it to a string, then pushes the result onto the
		/// stack.
		/// </summary>
		/// <param name="generator"> The IL generator. </param>
		/// <param name="fromType"> The type to convert from. </param>
		public static void ToString(ILGenerator generator, Type fromType)
		{
			// Check that a conversion is actually necessary.
			if (fromType == typeof(string))
				return;

			if(fromType == typeof(Nitrassic.Undefined))
			{
				// Converting from undefined produces "undefined".
				generator.Pop();
				generator.LoadString("undefined");
				
			}
			else if(fromType == typeof(Nitrassic.Null))
			{
				// Converting from null produces "null".
				generator.Pop();
				generator.LoadString("null");
				
			}
			else if(fromType == typeof(bool))
			{
				// Converting from a boolean produces "false" if the boolean is false, or "true" if the boolean is true.
				var elseClause = generator.CreateLabel();
				var endOfIf = generator.CreateLabel();
				generator.BranchIfFalse(elseClause);
				generator.LoadString("true");
				generator.Branch(endOfIf);
				generator.DefineLabelPosition(elseClause);
				generator.LoadString("false");
				generator.DefineLabelPosition(endOfIf);
				
			}else if(fromType == typeof(ConcatenatedString)){
				generator.Call(ReflectionHelpers.ConcatenatedString_ToString);
				
			}else{
					
				// Otherwise, fall back to calling TypeConverter.ToString()
				if (PrimitiveTypeUtilities.IsValueType(fromType)){
					generator.Box(fromType);
				}
				
				generator.Call(ReflectionHelpers.TypeConverter_ToString);
				
			}
			
		}

		/// <summary>
		/// Pops the value on the stack, converts it to a concatenated string, then pushes the result
		/// onto the stack.
		/// </summary>
		/// <param name="generator"> The IL generator. </param>
		/// <param name="fromType"> The type to convert from. </param>
		public static void ToConcatenatedString(ILGenerator generator, Type fromType)
		{
			// Check that a conversion is actually necessary.
			if (fromType == typeof(ConcatenatedString))
				return;

			if(fromType == typeof(Nitrassic.Undefined) || fromType == typeof(Nitrassic.Null) ||
				fromType == typeof(bool) || fromType == typeof(string)){
				
				// Convert as per ToString, then create a new ConcatenatedString instance.
				ToString(generator, fromType);
				generator.NewObject(ReflectionHelpers.ConcatenatedString_Constructor_String);
				
			}else if(fromType == typeof(int) || fromType == typeof(uint) || fromType == typeof(double) ||
					fromType == typeof(object) || fromType == typeof(Library.ObjectInstance)){
					
				// Otherwise, fall back to calling TypeConverter.ToConcatenatedString()
				if (PrimitiveTypeUtilities.IsValueType(fromType))
					generator.Box(fromType);
				generator.Call(ReflectionHelpers.TypeConverter_ToConcatenatedString);
			
			}else{
			
				throw new NotImplementedException(string.Format("Unsupported primitive type: {0}", fromType));
			
			}
			
		}
		
		/// <summary>
		/// Pops the value on the stack, converts it to a javascript object, then pushes the result
		/// onto the stack.
		/// </summary>
		/// <param name="generator"> The IL generator. </param>
		/// <param name="fromType"> The type to convert from. </param>
		/// <param name="optimizationInfo"> Information about the line number, function and path. </param>
		public static void ToPrototype(ILGenerator generator, Type fromType, OptimizationInfo optimizationInfo)
		{
			ToPrototype(generator, fromType, optimizationInfo.Source.Path, optimizationInfo.FunctionName);
		}

		/// <summary>
		/// Pops the value on the stack, converts it to a javascript object, then pushes the result
		/// onto the stack.
		/// </summary>
		/// <param name="generator"> The IL generator. </param>
		/// <param name="fromType"> The type to convert from. </param>
		/// <param name="path"> The path of the javascript source file that is currently executing. </param>
		/// <param name="function"> The name of the currently executing function. </param>
		public static void ToPrototype(ILGenerator generator, Type fromType, string path, string function)
		{
			if(fromType == typeof(Nitrassic.Undefined))
			{
				// Converting from undefined always throws an exception.
				EmitHelpers.EmitThrow(generator, "TypeError", "Undefined cannot be converted to an object", path, function,1);
				
			}
			else if(fromType == typeof(Nitrassic.Null))
			{
				// Converting from null always throws an exception.
				EmitHelpers.EmitThrow(generator, "TypeError", "Null cannot be converted to an object", path, function,1);
				
			}
			else
			{
				// Otherwise, fall back to calling TypeConverter.ToPrototype()
				ToAny(generator, fromType);
				EmitHelpers.LoadEngine(generator);
				generator.Call(ReflectionHelpers.TypeConverter_ToPrototype);
			}
		}
	
		/// <summary>
		/// Pops the value on the stack, converts it to a primitive value, then pushes the result
		/// onto the stack.
		/// </summary>
		/// <param name="generator"> The IL generator. </param>
		/// <param name="fromType"> The type to convert from. </param>
		/// <param name="preferredType"> Specifies whether toString() or valueOf() should be
		/// preferred when converting to a primitive. </param>
		public static void ToPrimitive(ILGenerator generator, Type fromType, PrimitiveTypeHint preferredType)
		{
			
			if(fromType == typeof(Nitrassic.Undefined) || fromType == typeof(Nitrassic.Null) ||
				fromType == typeof(bool) || fromType == typeof(string) || fromType == typeof(ConcatenatedString) ||
				fromType == typeof(int) || fromType == typeof(uint) || fromType == typeof(double))
			{
				
				return;
			
			}
			else if(fromType == typeof(object) || fromType == typeof(Library.ObjectInstance))
			{
				// Otherwise, fall back to calling TypeConverter.ToPrimitive()
				if (PrimitiveTypeUtilities.IsValueType(fromType))
					generator.Box(fromType);
				generator.LoadInt32((int)preferredType);
				generator.Call(ReflectionHelpers.TypeConverter_ToPrimitive);
			
			}
			else
			{
			
				throw new NotImplementedException("Unsupported primitive type: "+fromType);
			
			}
			
		}
		
		/// <summary>
		/// Pops the value on the stack, converts it to an object, then pushes the result onto the
		/// stack.
		/// </summary>
		/// <param name="generator"> The IL generator. </param>
		/// <param name="fromType"> The type to convert from. </param>
		public static void ToAny(ILGenerator generator, Type fromType)
		{
			
			if (PrimitiveTypeUtilities.IsValueType(fromType)){
				generator.Box(fromType);
			}
			
		}
	}

}
