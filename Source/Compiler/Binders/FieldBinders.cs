﻿using System;
using System.Collections.Generic;
using System.Reflection;
using Nitrassic;

namespace Nitrassic.Compiler
{

	/// <summary>
	/// Base class of field getter and setter binders.
	/// </summary>
	[Serializable]
	internal abstract class FieldBinder : Binder
	{
		protected FieldInfo field;

		/// <summary>
		/// Creates a new FieldGetterBinder instance.
		/// </summary>
		/// <param name="field"> The field. </param>
		protected FieldBinder(FieldInfo field)
		{
			this.field = field;
		}




		//	 PROPERTIES
		//_________________________________________________________________________________________

		/// <summary>
		/// Gets the name of the target methods.
		/// </summary>
		public override string Name
		{
			get { return this.field.Name; }
		}

		/// <summary>
		/// Gets the full name of the target methods, including the type name.
		/// </summary>
		public override string FullName
		{
			get { return string.Format("{0}.{1}", this.field.DeclaringType, this.field.Name); }
		}
	}




	/// <summary>
	/// Retrieves the value of a field.
	/// </summary>
	[Serializable]
	internal class FieldGetterBinder : FieldBinder
	{
		/// <summary>
		/// Creates a new FieldGetterBinder instance.
		/// </summary>
		/// <param name="field"> The field. </param>
		public FieldGetterBinder(FieldInfo field)
			: base(field)
		{
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
			// Check for the correct number of arguments.
			if (argumentCount != 0)
			{
				EmitHelpers.EmitThrow(generator, "TypeError", "Wrong number of arguments");
				EmitHelpers.EmitDefaultValue(generator, typeof(object));
				generator.Complete();
				return;
			}

			if (this.field.IsStatic == false)
			{
				EmitHelpers.LoadThis(generator);
				ClrBinder.EmitConversionToType(generator, this.field.DeclaringType, true);
			}
			generator.LoadField(this.field);
			generator.Complete();
		}

	}

	/// <summary>
	/// Sets the value of a field.
	/// </summary>
	[Serializable]
	internal class FieldSetterBinder : FieldBinder
	{
		/// <summary>
		/// Creates a new FieldSetterBinder instance.
		/// </summary>
		/// <param name="field"> The field. </param>
		public FieldSetterBinder(FieldInfo field)
			: base(field)
		{
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
			// Check for the correct number of arguments.
			if (argumentCount != 1)
			{
				EmitHelpers.EmitThrow(generator, "TypeError", "Wrong number of arguments");
				EmitHelpers.EmitDefaultValue(generator, typeof(object));
				generator.Complete();
				return;
			}
			if (this.field.IsStatic == false)
			{
				EmitHelpers.LoadThis(generator);
				ClrBinder.EmitConversionToType(generator, this.field.DeclaringType, true);
			}
			EmitHelpers.LoadArgumentsArray(generator);
			generator.LoadInt32(0);
			generator.LoadArrayElement(typeof(object));
			ClrBinder.EmitConversionToType(generator, this.field.FieldType, false);
			generator.StoreField(this.field);
			EmitHelpers.EmitUndefined(generator);
			generator.Complete();
		}

	}
}
