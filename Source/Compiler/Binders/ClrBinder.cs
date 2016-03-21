using System;
using System.Collections.Generic;
using System.Reflection;
using Nitrassic;

namespace Nitrassic.Compiler
{

	/// <summary>
	/// Binds to a method group using pretty standard .NET rules.  The main difference from the
	/// JSBinder is that the number of arguments must be correct.  Additionally, it is possible to
	/// bind to overloaded methods with the same number of arguments.
	/// </summary>
	[Serializable]
	internal class ClrBinder : MethodBinder
	{
		private IEnumerable<BinderMethod> targetMethods;

		/// <summary>
		/// Creates a new ClrBinder instance.
		/// </summary>
		/// <param name="targetMethod"> A method to bind to. </param>
		public ClrBinder(MethodBase targetMethod)
			: this(new BinderMethod[] { new BinderMethod(targetMethod) })
		{
			this.targetMethods = new BinderMethod[] { new BinderMethod(targetMethod) };
		}

		/// <summary>
		/// Creates a new ClrBinder instance.
		/// </summary>
		/// <param name="targetMethods"> An enumerable list of methods to bind to. </param>
		public ClrBinder(IEnumerable<MethodBase> targetMethods)
			: this(Convert(targetMethods))
		{
		}

		private static IEnumerable<BinderMethod> Convert(IEnumerable<MethodBase> s)
		{
			
			List<BinderMethod> bm=new List<BinderMethod>();
			
			foreach(MethodBase m in s)
			{
				bm.Add(new BinderMethod(m));
			}
			
			return (IEnumerable<BinderMethod>)bm;
			
		}
		
		/// <summary>
		/// Creates a new ClrBinder instance.
		/// </summary>
		/// <param name="targetMethods"> An enumerable list of methods to bind to. </param>
		public ClrBinder(IEnumerable<BinderMethod> targetMethods)
			: base(targetMethods)
		{
			this.targetMethods = targetMethods;
		}

		/// <summary>
		/// Generates a method that does type conversion and calls the bound method.
		/// </summary>
		/// <param name="generator"> The ILGenerator used to output the body of the method. </param>
		/// <param name="argumentCount"> The number of arguments that will be passed to the delegate. </param>
		/// <returns> A delegate that does type conversion and calls the method represented by this
		/// object. </returns>
		protected override void GenerateStub(ILGenerator generator, int argumentCount)
		{
			// Determine the methods that have the correct number of arguments.
			var candidateMethods = new List<BinderMethod>();
			foreach (var candidateMethod in this.targetMethods)
			{
				if (candidateMethod.IsArgumentCountCompatible(argumentCount) == true)
					candidateMethods.Add(candidateMethod);
			}

			// Zero candidates means no overload had the correct number of arguments.
			if (candidateMethods.Count == 0)
			{
				EmitHelpers.EmitThrow(generator, "TypeError", string.Format("No overload for method '{0}' takes {1} arguments", this.Name, argumentCount));
				EmitHelpers.EmitDefaultValue(generator, typeof(object));
				generator.Complete();
				return;
			}

			// Select the method to call at run time.
			generator.LoadInt32(candidateMethods.Count);
			generator.NewArray(typeof(RuntimeMethodHandle));
			for (int i = 0; i < candidateMethods.Count; i ++)
			{
				generator.Duplicate();
				generator.LoadInt32(i);
				generator.LoadToken(candidateMethods[i]);
				generator.StoreArrayElement(typeof(RuntimeMethodHandle));
			}
			
			EmitHelpers.LoadEngine(generator);
			EmitHelpers.LoadThis(generator);
			EmitHelpers.LoadArgumentsArray(generator);
			generator.Call(ReflectionHelpers.BinderUtilities_ResolveOverloads);

			var endOfMethod = generator.CreateLabel();
			for (int i = 0; i < candidateMethods.Count; i++)
			{
				// Check if this is the selected method.
				ILLabel endOfIf = null;
				if (i < candidateMethods.Count - 1)
				{
					generator.Duplicate();
					generator.LoadInt32(i);
					endOfIf = generator.CreateLabel();
					generator.BranchIfNotEqual(endOfIf);
				}
				generator.Pop();

				var targetMethod = candidateMethods[i];

				// Convert the arguments.
				foreach (var argument in targetMethod.GenerateArguments(generator, argumentCount))
				{
					// Load the input parameter value.
					switch (argument.Source)
					{
						case BinderArgumentSource.ScriptEngine:
							EmitHelpers.LoadEngine(generator);
							break;
						case BinderArgumentSource.ThisValue:
							EmitHelpers.LoadThis(generator);
							break;
						case BinderArgumentSource.InputParameter:
							EmitHelpers.LoadArgumentsArray(generator);
							generator.LoadInt32(argument.InputParameterIndex);
							generator.LoadArrayElement(typeof(object));
							break;
					}

					// Convert to the target type.
					EmitConversionToType(generator, argument.Type, argument.Source == BinderArgumentSource.ThisValue);
				}

				// Call the target method.
				targetMethod.GenerateCall(generator);

				// Convert the return value.
				if (targetMethod.ReturnType == typeof(void))
					EmitHelpers.EmitUndefined(generator);
				
				// Branch to the end of the method if this was the selected method.
				if (endOfIf != null)
				{
					generator.Branch(endOfMethod);
					generator.DefineLabelPosition(endOfIf);
				}
			}

			generator.DefineLabelPosition(endOfMethod);
			generator.Complete();
		}

		/// <summary>
		/// Pops the value on the stack, converts it from an object to the given type, then pushes
		/// the result onto the stack.
		/// </summary>
		/// <param name="generator"> The IL generator. </param>
		/// <param name="toType"> The type to convert to. </param>
		/// <param name="convertToAddress"> <c>true</c> if the value is intended for use as an
		/// instance pointer; <c>false</c> otherwise. </param>
		internal static void EmitConversionToType(ILGenerator generator, Type toType, bool convertToAddress)
		{
			
			switch (Type.GetTypeCode(toType))
			{
				case TypeCode.Boolean:
					EmitConversion.ToBool(generator, typeof(object));
					break;
				case TypeCode.Byte:
					EmitConversion.ToInt32(generator, typeof(object));
					break;
				case TypeCode.Char:
					EmitConversion.ToString(generator, typeof(object));
					generator.Duplicate();
					generator.Call(ReflectionHelpers.String_Length);
					generator.LoadInt32(1);
					var endOfCharCheck = generator.CreateLabel();
					generator.BranchIfEqual(endOfCharCheck);
					EmitHelpers.EmitThrow(generator, "TypeError", "Cannot convert string to char - the string must be exactly one character long");
					generator.DefineLabelPosition(endOfCharCheck);
					generator.LoadInt32(0);
					generator.Call(ReflectionHelpers.String_GetChars);
					break;
				case TypeCode.DBNull:
					throw new NotSupportedException("DBNull is not a supported parameter type.");
				case TypeCode.Decimal:
					EmitConversion.ToNumber(generator, typeof(object));
					generator.NewObject(ReflectionHelpers.Decimal_Constructor_Double);
					break;
				case TypeCode.Double:
					EmitConversion.ToNumber(generator, typeof(object));
					break;
				case TypeCode.Empty:
					throw new NotSupportedException("Empty is not a supported return type.");
				case TypeCode.Int16:
					EmitConversion.ToInt32(generator, typeof(object));
					break;
				case TypeCode.Int32:
					EmitConversion.ToInt32(generator, typeof(object));
					break;
				case TypeCode.Int64:
					EmitConversion.ToNumber(generator, typeof(object));
					generator.ConvertToInt64();
					break;

				case TypeCode.DateTime:
				case TypeCode.Object:
					
					// Value types must be unboxed.
					if (toType.IsValueType)
					{
						if (convertToAddress == true)
							// Unbox.
							generator.Unbox(toType);
						else
							// Unbox and copy to the stack.
							generator.UnboxAny(toType);

						//// Calling methods on value required the address of the value type, not the value type itself.
						//if (argument.Source == BinderArgumentSource.ThisValue && argument.Type.IsValueType == true)
						//{
						//	var temp = generator.CreateTemporaryVariable(argument.Type);
						//	generator.StoreVariable(temp);
						//	generator.LoadAddressOfVariable(temp);
						//	generator.ReleaseTemporaryVariable(temp);
						//}
					}


					break;

				case TypeCode.SByte:
					EmitConversion.ToInt32(generator, typeof(object));
					break;
				case TypeCode.Single:
					EmitConversion.ToNumber(generator, typeof(object));
					break;
				case TypeCode.String:
					EmitConversion.ToString(generator, typeof(object));
					break;
				case TypeCode.UInt16:
					EmitConversion.ToInt32(generator, typeof(object));
					break;
				case TypeCode.UInt32:
					EmitConversion.ToUInt32(generator, typeof(object));
					break;
				case TypeCode.UInt64:
					EmitConversion.ToNumber(generator, typeof(object));
					generator.ConvertToUnsignedInt64();
					break;
			}
			
		}
		
	}
}
