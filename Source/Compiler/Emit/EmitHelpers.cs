using System;

namespace Nitrassic.Compiler
{
	/// <summary>
	/// Outputs IL for misc tasks.
	/// </summary>
	internal static class EmitHelpers
	{
		/// <summary>
		/// Emits undefined.
		/// </summary>
		/// <param name="generator"> The IL generator. </param>
		public static void EmitUndefined(ILGenerator generator)
		{
			generator.LoadField(ReflectionHelpers.Undefined_Value);
		}

		/// <summary>
		/// Emits null.
		/// </summary>
		/// <param name="generator"> The IL generator. </param>
		public static void EmitNull(ILGenerator generator)
		{
			generator.LoadField(ReflectionHelpers.Null_Value);
		}

		/// <summary>
		/// Emits a default value of the given type.
		/// </summary>
		/// <param name="generator"> The IL generator. </param>
		/// <param name="type"> The type of value to generate. </param>
		public static void EmitDefaultValue(ILGenerator generator, Type type)
		{
			var temp = generator.CreateTemporaryVariable(type);
			generator.LoadAddressOfVariable(temp);
			generator.InitObject(temp.Type);
			generator.LoadVariable(temp);
			generator.ReleaseTemporaryVariable(temp);
		}
		
		/// <summary>
		/// Emits a JavaScriptException.
		/// </summary>
		/// <param name="generator"> The IL generator. </param>
		/// <param name="name"> The type of error to generate. </param>
		/// <param name="message"> The error message. </param>
		public static void EmitThrow(ILGenerator generator, string name, string message)
		{
			EmitThrow(generator, name, message, null, null, 0);
		}

		/// <summary>
		/// Emits a JavaScriptException.
		/// </summary>
		/// <param name="generator"> The IL generator. </param>
		/// <param name="name"> The type of error to generate. </param>
		/// <param name="message"> The error message. </param>
		/// <param name="optimizationInfo"> Information about the line number, function and path. </param>
		public static void EmitThrow(ILGenerator generator, string name, string message, OptimizationInfo optimizationInfo)
		{
			EmitThrow(generator, name, message, optimizationInfo.Source.Path, optimizationInfo.FunctionName, 1);
		}

		/// <summary>
		/// Emits a JavaScriptException.
		/// </summary>
		/// <param name="generator"> The IL generator. </param>
		/// <param name="name"> The type of error to generate. </param>
		/// <param name="message"> The error message. </param>
		/// <param name="path"> The path of the javascript source file that is currently executing. </param>
		/// <param name="function"> The name of the currently executing function. </param>
		/// <param name="line"> The line number of the statement that is currently executing. </param>
		public static void EmitThrow(ILGenerator generator, string name, string message, string path, string function, int line)
		{
			EmitHelpers.LoadEngine(generator);
			generator.LoadString(name);
			generator.LoadString(message);
			generator.LoadInt32(line);
			generator.LoadStringOrNull(path);
			generator.LoadStringOrNull(function);
			generator.NewObject(ReflectionHelpers.JavaScriptException_Constructor_Error);
			generator.Throw();
		}

		/// <summary>
		/// Emits the given value.  Only possible for certain types.
		/// </summary>
		/// <param name="generator"> The IL generator. </param>
		/// <param name="value"> The value to emit. </param>
		public static void EmitValue(ILGenerator generator, object value)
		{
			if (value == null)
				generator.LoadNull();
			else if ( value is Nitrassic.Undefined )
			{
				EmitHelpers.EmitUndefined(generator);
			}
			else if ( value is System.Reflection.MethodBase )
			{
				generator.LoadToken(value as System.Reflection.MethodBase);
				generator.Call(ReflectionHelpers.MethodBase_GetMethod);
			}
			else if ( value is MethodGroup)
			{
				throw new NotImplementedException("Can't emit methodGroups here");
			}
			else if( value is FunctionMethodGenerator )
			{
				// body
				generator.LoadInt64((value as FunctionMethodGenerator).MethodID);
				generator.Call(ReflectionHelpers.MethodLookup_Load);
			}
			else
			{
				switch (Type.GetTypeCode(value.GetType()))
				{
					case TypeCode.Boolean:
						generator.LoadBoolean((bool)value);
						break;
					case TypeCode.Byte:
						generator.LoadInt32((byte)value);
						break;
					case TypeCode.Char:
						generator.LoadInt32((char)value);
						break;
					case TypeCode.Double:
						generator.LoadDouble((double)value);
						break;
					case TypeCode.Int16:
						generator.LoadInt32((short)value);
						break;
					case TypeCode.Int32:
						generator.LoadInt32((int)value);
						break;
					case TypeCode.Int64:
						generator.LoadInt64((long)value);
						break;
					case TypeCode.SByte:
						generator.LoadInt32((sbyte)value);
						break;
					case TypeCode.Single:
						generator.LoadDouble((float)value);
						break;
					case TypeCode.String:
						generator.LoadString((string)value);
						break;
					case TypeCode.UInt16:
						generator.LoadInt32((ushort)value);
						break;
					case TypeCode.UInt32:
						generator.LoadInt32((uint)value);
						break;
					case TypeCode.UInt64:
						generator.LoadInt64((ulong)value);
						break;
					case TypeCode.Object:
					case TypeCode.Empty:
					case TypeCode.DateTime:
					case TypeCode.DBNull:
					case TypeCode.Decimal:
						throw new NotImplementedException(string.Format("Cannot emit the value '{0}' (a "+value.GetType()+")", value));
				}
			}
		}



		//	 LOAD METHOD PARAMETERS
		//_________________________________________________________________________________________

		/// <summary>
		/// Pushes a reference to the script engine onto the stack.
		/// </summary>
		/// <param name="generator"> The IL generator. </param>
		public static void LoadEngine(ILGenerator generator)
		{
			
			// Get the static ref to the engine:
			generator.LoadField(generator.Engine.ReflectionEmitInfo.GlobalEngine);
			
		}
		
		/// <summary>
		/// Pushes a particular proto to the stack.
		/// </summary>
		public static void LoadPrototype(ILGenerator generator,Type forType){
			
			// Load proto set:
			LoadPrototypes(generator);
			
			// Put the type on the stack:
			generator.LoadToken(forType);
			
			// Call get now:
			generator.Call(ReflectionHelpers.PrototypeLookup_Get);
			
		}
		
		/// <summary>
		/// Pushes a reference to the prototype set onto the stack.
		/// </summary>
		/// <param name="generator"> The IL generator. </param>
		public static void LoadPrototypes(ILGenerator generator)
		{
			
			// Get the static ref to the engine:
			generator.LoadField(generator.Engine.ReflectionEmitInfo.GlobalPrototypes);
			
		}
		
		public static void GlobalObject(ILGenerator generator){
			
			// Get the static ref to the global object:
			generator.LoadField(generator.Engine.ReflectionEmitInfo.GlobalObject);
			
		}
		
		/// <summary>
		/// Pushes the value of the <c>this</c> keyword onto the stack.
		/// </summary>
		/// <param name="generator"> The IL generator. </param>
		public static void LoadThis(ILGenerator generator)
		{
			generator.LoadArgument(0);
		}

		/// <summary>
		/// Stores the reference on top of the stack as the new value of the <c>this</c> keyword.
		/// </summary>
		/// <param name="generator"> The IL generator. </param>
		public static void StoreThis(ILGenerator generator)
		{
			generator.StoreArgument(0);
		}
		
	}

}
