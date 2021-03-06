﻿using System;

namespace Nitrassic.Compiler
{
	
	/// <param name="two">True if the method should emit the value onto the stack twice. The second time
	/// is used as a return value from the expression.</param>
	internal delegate void SetValueMethod(bool two);
	
	/// <summary>
	/// Represents a reference - an expression that is valid on the left-hand-side of an assignment
	/// operation.
	/// </summary>
	internal interface IReferenceExpression
	{
		/// <summary>
		/// Gets the static type of the reference.
		/// </summary>
		Type GetResultType(OptimizationInfo optimizationInfo);

		/// <summary>
		/// Pushes the value of the reference onto the stack.
		/// </summary>
		/// <param name="generator"> The generator to output the CIL to. </param>
		/// <param name="optimizationInfo"> Information about any optimizations that should be performed. </param>
		/// <param name="throwIfUnresolvable"> <c>true</c> to throw a ReferenceError exception if
		/// the name is unresolvable; <c>false</c> to output <c>null</c> instead. </param>
		void GenerateGet(ILGenerator generator, OptimizationInfo optimizationInfo, bool throwIfUnresolvable);

		/// <summary>
		/// Stores the value on the top of the stack in the reference.
		/// </summary>
		/// <param name="generator"> The generator to output the CIL to. </param>
		/// <param name="optimizationInfo"> Information about any optimizations that should be performed. </param>
		/// <param name="valueType"> The primitive type of the value that is on the top of the stack. </param>
		/// <param name="throwIfUnresolvable"> <c>true</c> to throw a ReferenceError exception if
		/// the name is unresolvable; <c>false</c> to create a new property instead. </param>
		void GenerateSet(ILGenerator generator, OptimizationInfo optimizationInfo,bool rIU, Type valueType, SetValueMethod method, bool throwIfUnresolvable);

		/// <summary>
		/// Deletes the reference and pushes <c>true</c> if the delete succeeded, or <c>false</c>
		/// if the delete failed.
		/// </summary>
		/// <param name="generator"> The generator to output the CIL to. </param>
		/// <param name="optimizationInfo"> Information about any optimizations that should be performed. </param>
		void GenerateDelete(ILGenerator generator, OptimizationInfo optimizationInfo);
		
		/// <summary>The target is being set to something with the given type.
		/// This may cause the target variable to "collapse".</summary>
		void ApplyType(OptimizationInfo optimizationInfo,Type type);
		
		object GetConstantValue();
		
		void TryApplyConstant(OptimizationInfo optimizationInfo,bool isConst);
		
		object TryApplyConstant(OptimizationInfo optimizationInfo,Expression expr);
		
		
	}
}