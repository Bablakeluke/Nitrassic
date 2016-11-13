using System;
using System.Collections.Generic;
using System.Reflection;
using Nitrassic.Compiler;
using Nitrassic.Library;

namespace Nitrassic
{

	/// <summary>
	/// Contains static methods to ease reflection operations.
	/// Not used at runtime (used as a source by TricksGen).
	/// </summary>
	internal static class ReflectionHelpers
	{
		internal static MethodInfo TypeConverter_ToString;
		internal static MethodInfo TypeConverter_ToConcatenatedString;
		internal static MethodInfo TypeConverter_ToNumber;
		internal static MethodInfo TypeConverter_ToBoolean;
		internal static MethodInfo TypeConverter_ToPrototype;
		internal static MethodInfo TypeConverter_ToInteger;
		internal static MethodInfo TypeConverter_ToInt32;
		internal static MethodInfo TypeConverter_ToUint32;
		internal static MethodInfo TypeConverter_ToPrimitive;
		internal static MethodInfo MethodBase_GetMethod;

		internal static MethodInfo TypeComparer_Equals;
		internal static MethodInfo TypeComparer_StrictEquals;
		internal static MethodInfo TypeComparer_LessThan;
		internal static MethodInfo TypeComparer_LessThanOrEqual;
		internal static MethodInfo TypeComparer_GreaterThan;
		internal static MethodInfo TypeComparer_GreaterThanOrEqual;

		internal static MethodInfo TypeUtilities_TypeOf;
		internal static MethodInfo TypeUtilities_EnumeratePropertyNames;
		internal static MethodInfo TypeUtilities_EnumeratePropertyValues;
		internal static MethodInfo TypeUtilities_Add;
		internal static MethodInfo TypeUtilities_IsPrimitiveOrObject;
		internal static MethodInfo TypeUtilities_VerifyThisObject;

		internal static MethodInfo FunctionInstance_HasInstance;
		internal static MethodInfo FunctionInstance_InstancePrototype;
		
		internal static FieldInfo PrototypeLookup_RegExp;
		internal static FieldInfo PrototypeLookup_Object;
		internal static MethodInfo PrototypeLookup_Get;
		internal static MethodInfo Prototype_GetProperty;
		internal static MethodInfo Object_GetPropertyValue;
		internal static MethodInfo Object_SetPropertyValue;
		internal static MethodInfo Object_HasProperty;
		internal static MethodInfo Global_Eval;
		
		internal static ConstructorInfo String_Constructor_Char_Int;
		internal static MethodInfo String_Concat;
		internal static MethodInfo String_Length;
		internal static MethodInfo String_CompareOrdinal;
		internal static MethodInfo String_Format;
		internal static MethodInfo String_GetChars;

		internal static ConstructorInfo ConcatenatedString_Constructor_String;
		internal static ConstructorInfo ConcatenatedString_Constructor_String_String;
		internal static MethodInfo ConcatenatedString_Length;
		internal static MethodInfo ConcatenatedString_Concatenate_Object;
		internal static MethodInfo ConcatenatedString_Concatenate_String;
		internal static MethodInfo ConcatenatedString_Concatenate_ConcatenatedString;
		internal static MethodInfo ConcatenatedString_Append_Object;
		internal static MethodInfo ConcatenatedString_Append_String;
		internal static MethodInfo ConcatenatedString_Append_ConcatenatedString;
		internal static MethodInfo ConcatenatedString_ToString;

		internal static MethodInfo IEnumerable_GetEnumerator;
		internal static MethodInfo IEnumerator_MoveNext;
		internal static MethodInfo IEnumerator_Current;

		internal static MethodInfo Debugger_Break;
		internal static MethodInfo JavaScriptException_ErrorObject;
		internal static MethodInfo Object_Construct;
		
		internal static MethodInfo RegExp_Construct;
		internal static MethodInfo Array_New;
		internal static MethodInfo Date_ToNumber;
		internal static MethodInfo Delegate_CreateDelegate;
		internal static MethodInfo Type_GetTypeFromHandle;
		internal static MethodInfo MethodBase_GetMethodFromHandle;
		internal static MethodInfo MethodLookup_Load;
		internal static MethodInfo Decimal_ToDouble;
		internal static MethodInfo Convert_ToInt32_Double;
		
		internal static ConstructorInfo JavaScriptException_Constructor_Error;
		internal static ConstructorInfo JavaScriptException_Constructor_Object;
		internal static ConstructorInfo FunctionDelegate_Constructor;
		internal static ConstructorInfo Arguments_Constructor;
		internal static ConstructorInfo PropertyDescriptor_Constructor2;
		internal static ConstructorInfo PropertyDescriptor_Constructor3;
		internal static ConstructorInfo Decimal_Constructor_Double;
		internal static ConstructorInfo HoistFunctionReference_Constructor;
		
		internal static FieldInfo Undefined_Value;
		internal static FieldInfo Null_Value;

		internal static ConstructorInfo LongJumpException_Constructor;
		internal static FieldInfo LongJumpException_RouteID;

		/// <summary>
		/// Initializes static members of this class.
		/// </summary>
		static ReflectionHelpers()
		{
			// Retrieve the various MethodInfos used for type conversion.
			TypeConverter_ToString = GetStaticMethod(typeof(TypeConverter), "ToString", typeof(object));
			TypeConverter_ToConcatenatedString = GetStaticMethod(typeof(TypeConverter), "ToConcatenatedString", typeof(object));
			TypeConverter_ToNumber = GetStaticMethod(typeof(TypeConverter), "ToNumber", typeof(object));
			TypeConverter_ToBoolean = GetStaticMethod(typeof(TypeConverter), "ToBoolean", typeof(object));
			TypeConverter_ToPrototype = GetStaticMethod(typeof(TypeConverter), "ToPrototype", typeof(object), typeof(ScriptEngine));
			TypeConverter_ToInteger = GetStaticMethod(typeof(TypeConverter), "ToInteger", typeof(object));
			TypeConverter_ToInt32 = GetStaticMethod(typeof(TypeConverter), "ToInt32", typeof(object));
			TypeConverter_ToUint32 = GetStaticMethod(typeof(TypeConverter), "ToUint32", typeof(object));
			TypeConverter_ToPrimitive = GetStaticMethod(typeof(TypeConverter), "ToPrimitive", typeof(object), typeof(PrimitiveTypeHint));
			
			Date_ToNumber = GetInstanceMethod(typeof(Date), "ToNumber");
			
			TypeComparer_Equals = GetStaticMethod(typeof(TypeComparer), "Equals", typeof(object), typeof(object));
			TypeComparer_StrictEquals = GetStaticMethod(typeof(TypeComparer), "StrictEquals", typeof(object), typeof(object));
			TypeComparer_LessThan = GetStaticMethod(typeof(TypeComparer), "LessThan", typeof(object), typeof(object));
			TypeComparer_LessThanOrEqual = GetStaticMethod(typeof(TypeComparer), "LessThanOrEqual", typeof(object), typeof(object));
			TypeComparer_GreaterThan = GetStaticMethod(typeof(TypeComparer), "GreaterThan", typeof(object), typeof(object));
			TypeComparer_GreaterThanOrEqual = GetStaticMethod(typeof(TypeComparer), "GreaterThanOrEqual", typeof(object), typeof(object));
			
			MethodBase_GetMethod = GetStaticMethod(typeof(MethodBase),"GetMethodFromHandle", typeof(RuntimeMethodHandle));
			
			TypeUtilities_TypeOf = GetStaticMethod(typeof(TypeUtilities), "TypeOf", typeof(object));
			TypeUtilities_EnumeratePropertyNames = GetStaticMethod(typeof(TypeUtilities), "EnumeratePropertyNames", typeof(ScriptEngine), typeof(object));
			TypeUtilities_EnumeratePropertyValues = GetStaticMethod(typeof(TypeUtilities), "EnumeratePropertyValues", typeof(ScriptEngine), typeof(object));
			TypeUtilities_Add = GetStaticMethod(typeof(TypeUtilities), "Add", typeof(object), typeof(object));
			TypeUtilities_IsPrimitiveOrObject = GetStaticMethod(typeof(TypeUtilities), "IsPrimitiveOrObject", typeof(object));
			TypeUtilities_VerifyThisObject = GetStaticMethod(typeof(TypeUtilities), "VerifyThisObject", typeof(ScriptEngine), typeof(object), typeof(string));
			
			FunctionInstance_HasInstance = GetInstanceMethod(typeof(FunctionInstance), "HasInstance", typeof(ScriptEngine),typeof(object));
			FunctionInstance_InstancePrototype = GetInstanceMethod(typeof(FunctionInstance), "GetInstancePrototype",typeof(ScriptEngine));
			
			PrototypeLookup_RegExp = GetField(typeof(PrototypeLookup), "RegExp");
			PrototypeLookup_Object = GetField(typeof(PrototypeLookup), "Object");
			PrototypeLookup_Get = GetInstanceMethod(typeof(PrototypeLookup), "Get", typeof(System.Type));
			
			Object_HasProperty=GetStaticMethod(typeof(Prototype),"HasProperty",typeof(ScriptEngine),typeof(object),typeof(string));
			Object_GetPropertyValue=GetStaticMethod(typeof(Prototype),"GetPropertyValue",typeof(ScriptEngine),typeof(object),typeof(string));
			Object_SetPropertyValue=GetStaticMethod(typeof(Prototype),"SetPropertyValue",typeof(ScriptEngine),typeof(object),typeof(string),typeof(object));
			
			Prototype_GetProperty=GetInstanceMethod(typeof(Prototype),"GetProperty",typeof(string));
			
			Global_Eval = GetStaticMethod(typeof(Window), "Eval", typeof(ScriptEngine), typeof(object));

			String_Constructor_Char_Int = GetConstructor(typeof(string), typeof(char), typeof(int));
			String_Concat = GetStaticMethod(typeof(string), "Concat", typeof(string), typeof(string));
			String_Length = GetInstanceMethod(typeof(string), "get_Length");
			String_CompareOrdinal = GetStaticMethod(typeof(string), "CompareOrdinal", typeof(string), typeof(string));
			String_Format = GetStaticMethod(typeof(string), "Format", typeof(string), typeof(object[]));
			String_GetChars = GetInstanceMethod(typeof(string), "get_Chars", typeof(int));
			
			HoistFunctionReference_Constructor = GetConstructor(typeof(HoistFunctionReference),typeof(ScriptEngine),typeof(long),typeof(object[]));
			ConcatenatedString_Constructor_String = GetConstructor(typeof(ConcatenatedString), typeof(string));
			ConcatenatedString_Constructor_String_String = GetConstructor(typeof(ConcatenatedString), typeof(string), typeof(string));
			ConcatenatedString_Length = GetInstanceMethod(typeof(ConcatenatedString), "get_Length");
			ConcatenatedString_Concatenate_Object = GetInstanceMethod(typeof(ConcatenatedString), "Concatenate", typeof(object));
			ConcatenatedString_Concatenate_String = GetInstanceMethod(typeof(ConcatenatedString), "Concatenate", typeof(string));
			ConcatenatedString_Concatenate_ConcatenatedString = GetInstanceMethod(typeof(ConcatenatedString), "Concatenate", typeof(ConcatenatedString));
			ConcatenatedString_Append_Object = GetInstanceMethod(typeof(ConcatenatedString), "Append", typeof(object));
			ConcatenatedString_Append_String = GetInstanceMethod(typeof(ConcatenatedString), "Append", typeof(string));
			ConcatenatedString_Append_ConcatenatedString = GetInstanceMethod(typeof(ConcatenatedString), "Append", typeof(ConcatenatedString));
			ConcatenatedString_ToString = GetInstanceMethod(typeof(ConcatenatedString), "ToString");

			JavaScriptException_Constructor_Error = GetConstructor(typeof(JavaScriptException), typeof(ScriptEngine), typeof(string), typeof(string), typeof(int), typeof(string), typeof(string));
			JavaScriptException_Constructor_Object = GetConstructor(typeof(JavaScriptException), typeof(ScriptEngine), typeof(object), typeof(int), typeof(string), typeof(string));
			IEnumerable_GetEnumerator = GetInstanceMethod(typeof(IEnumerable<string>), "GetEnumerator");
			IEnumerator_MoveNext = GetInstanceMethod(typeof(System.Collections.IEnumerator), "MoveNext");
			IEnumerator_Current = GetInstanceMethod(typeof(IEnumerator<string>), "get_Current");
			Debugger_Break = GetStaticMethod(typeof(System.Diagnostics.Debugger), "Break");
			JavaScriptException_ErrorObject = GetInstanceMethod(typeof(JavaScriptException), "get_ErrorObject");
			
			RegExp_Construct = GetStaticMethod(typeof(Nitrassic.Library.RegExp), "OnConstruct", typeof(ScriptEngine), typeof(object), typeof(string));
			Array_New = GetStaticMethod(typeof(Nitrassic.Library.Array), "New", typeof(ScriptEngine),typeof(object[]));
			Object_Construct = GetStaticMethod(typeof(ObjectInstance), "OnConstruct",typeof(ScriptEngine));
			Delegate_CreateDelegate = GetStaticMethod(typeof(Delegate), "CreateDelegate", typeof(Type), typeof(MethodInfo));
			Type_GetTypeFromHandle = GetStaticMethod(typeof(Type), "GetTypeFromHandle", typeof(RuntimeTypeHandle));
			MethodBase_GetMethodFromHandle = GetStaticMethod(typeof(MethodBase), "GetMethodFromHandle", typeof(RuntimeMethodHandle));
			FunctionDelegate_Constructor = GetConstructor(typeof(Library.FunctionDelegate), typeof(object), typeof(IntPtr));
			Arguments_Constructor = GetConstructor(typeof(ArgumentsInstance), typeof(object[]));
			
			PropertyDescriptor_Constructor2 = GetConstructor(typeof(PropertyDescriptor), typeof(object), typeof(Library.PropertyAttributes));
			PropertyDescriptor_Constructor3 = GetConstructor(typeof(PropertyDescriptor), typeof(FunctionInstance), typeof(FunctionInstance), typeof(Library.PropertyAttributes));
			Decimal_Constructor_Double = GetConstructor(typeof(decimal), typeof(double));

			MethodLookup_Load = GetStaticMethod(typeof(MethodLookup), "LoadGenerator", typeof(long));
			Decimal_ToDouble = GetStaticMethod(typeof(decimal), "ToDouble", typeof(decimal));
			Convert_ToInt32_Double = GetStaticMethod(typeof(Convert), "ToInt32", typeof(double));
			
			Undefined_Value = GetField(typeof(Undefined), "Value");
			Null_Value = GetField(typeof(Null), "Value");

			LongJumpException_Constructor = GetConstructor(typeof(LongJumpException), typeof(int));
			LongJumpException_RouteID = GetField(typeof(LongJumpException), "RouteID");

#if DEBUG
			// When using Reflection Emit, all calls into Nitrassic.dll are cross-assembly and thus
			// must be public.
			var text = new System.Text.StringBuilder();
			foreach (var reflectionField in GetMembers())
			{
				var methodBase = reflectionField.MemberInfo as MethodBase;
				if (methodBase != null && (methodBase.Attributes & MethodAttributes.Public) != MethodAttributes.Public)
				{
					text.Append(methodBase.DeclaringType.ToString());
					text.Append("/");
					text.AppendLine(methodBase.ToString());
				}
				var field = reflectionField.MemberInfo as FieldInfo;
				if (field != null && (field.Attributes & FieldAttributes.Public) != FieldAttributes.Public)
					text.AppendLine(field.ToString());
				if ((reflectionField.MemberInfo.DeclaringType.Attributes & TypeAttributes.Public) != TypeAttributes.Public)
					text.AppendLine(reflectionField.MemberInfo.DeclaringType.ToString());
			}
			if (text.Length > 0)
				throw new InvalidOperationException("The following members need to be public: " + Environment.NewLine + text.ToString());
			
#endif
		}

		internal struct ReflectionField
		{
			public string FieldName;
			public MemberInfo MemberInfo;
		}

		/// <summary>
		/// Gets an enumerable list of all the MemberInfos that are statically known to be used by this DLL.
		/// </summary>
		/// <returns> An enumerable list of all the MemberInfos that are used by this DLL. </returns>
		internal static IEnumerable<ReflectionField> GetMembers()
		{
			foreach (FieldInfo field in typeof(ReflectionHelpers).GetFields(BindingFlags.NonPublic | BindingFlags.Static))
			{
				if (field.FieldType != typeof(MethodInfo) && field.FieldType != typeof(ConstructorInfo) && field.FieldType != typeof(FieldInfo))
					continue;
				yield return new ReflectionField() { FieldName = field.Name, MemberInfo = (MemberInfo)field.GetValue(null) };
			}
		}

		/// <summary>
		/// Gets the FieldInfo for a field.  Throws an exception if the search fails.
		/// </summary>
		/// <param name="type"> The type to search. </param>
		/// <param name="name"> The name of the field. </param>
		/// <returns> The FieldInfo for a field. </returns>
		public static FieldInfo GetField(Type type, string name)
		{
			FieldInfo result = type.GetField(name);
			if (result == null)
				throw new InvalidOperationException(string.Format("The field '{1}' does not exist on type '{0}'.", type, name));
			return result;
		}
		
		/// <summary>
		/// Gets the ConstructorInfo for a constructor.  Throws an exception if the search fails.
		/// </summary>
		/// <param name="type"> The type to search. </param>
		/// <param name="parameterTypes"> The types of the parameters accepted by the constructor. </param>
		/// <returns> The ConstructorInfo for the constructor. </returns>
		public static ConstructorInfo GetConstructor(Type type, params Type[] parameterTypes)
		{
			var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
			ConstructorInfo result = type.GetConstructor(flags, null, parameterTypes, null);
			if (result == null)
				throw new InvalidOperationException(string.Format("The constructor {0}({1}) does not exist.", type.FullName, StringHelpers.Join<Type>(", ", parameterTypes)));
			return result;
		}

		/// <summary>
		/// Gets the MethodInfo for an instance method.  Throws an exception if the search fails.
		/// </summary>
		/// <param name="type"> The type to search. </param>
		/// <param name="name"> The name of the method to search for. </param>
		/// <param name="parameterTypes"> The types of the parameters accepted by the method. </param>
		/// <returns> The MethodInfo for the method. </returns>
		public static MethodInfo GetInstanceMethod(Type type, string name, params Type[] parameterTypes)
		{
			var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.ExactBinding;
			MethodInfo result = type.GetMethod(name, flags, null, parameterTypes, null);
			if (result == null)
				throw new InvalidOperationException(string.Format("The instance method {0}.{1}({2}) does not exist.", type.FullName, name, StringHelpers.Join<Type>(", ", parameterTypes)));
			return result;
		}

		/// <summary>
		/// Gets the MethodInfo for a static method.  Throws an exception if the search fails.
		/// </summary>
		/// <param name="type"> The type to search. </param>
		/// <param name="name"> The name of the method to search for. </param>
		/// <param name="parameterTypes"> The types of the parameters accepted by the method. </param>
		/// <returns> The MethodInfo for the method. </returns>
		public static MethodInfo GetStaticMethod(Type type, string name, params Type[] parameterTypes)
		{
			var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.ExactBinding;
			MethodInfo result = type.GetMethod(name, flags, null, parameterTypes, null);
			if (result == null)
				throw new InvalidOperationException(string.Format("The static method {0}.{1}({2}) does not exist.", type.FullName, name, StringHelpers.Join<Type>(", ", parameterTypes)));
			return result;
		}

		/// <summary>
		/// Gets the MethodInfo for a generic instance method.  Throws an exception if the search fails.
		/// </summary>
		/// <param name="type"> The type to search. </param>
		/// <param name="name"> The name of the method to search for. </param>
		/// <returns> The MethodInfo for the method. </returns>
		private static MethodInfo GetGenericInstanceMethod(Type type, string name)
		{
			var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
			MethodInfo result = type.GetMethod(name, flags);
			if (result == null)
				throw new InvalidOperationException(string.Format("The instance method {0}.{1}(...) does not exist.", type.FullName, name));
			return result;
		}

	}

}