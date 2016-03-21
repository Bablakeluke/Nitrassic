﻿using System;
using System.Collections.Generic;
using Nitrassic.Library;

namespace Nitrassic.Compiler
{
	/// <summary>
	/// Represents the unit of compilation.
	/// </summary>
	internal abstract class MethodGenerator
	{
		/// <summary>
		/// Creates a new MethodGenerator instance.
		/// </summary>
		/// <param name="engine"> The script engine. </param>
		/// <param name="scope"> The initial scope. </param>
		/// <param name="source"> The source of javascript code. </param>
		/// <param name="options"> Options that influence the compiler. </param>
		protected MethodGenerator(ScriptEngine engine, Scope scope, ScriptSource source, CompilerOptions options)
		{
			if (engine == null)
				throw new ArgumentNullException("engine");
			if (scope == null)
				throw new ArgumentNullException("scope");
			if (source == null)
				throw new ArgumentNullException("source");
			if (options == null)
				throw new ArgumentNullException("options");
			this.Engine = engine;
			this.InitialScope = scope;
			this.Source = source;
			this.Options = options;
			this.StrictMode = this.Options.ForceStrictMode;
		}

		/// <summary>
		/// Gets a reference to the script engine.
		/// </summary>
		public ScriptEngine Engine;

		/// <summary>
		/// Gets a reference to any compiler options.
		/// </summary>
		public CompilerOptions Options;

		/// <summary>
		/// Gets the source of javascript code.
		/// </summary>
		public ScriptSource Source;

		/// <summary>
		/// Gets a value that indicates whether strict mode is enabled.
		/// </summary>
		public bool StrictMode;

		/// <summary>
		/// Gets the top-level scope associated with the context.
		/// </summary>
		public Scope InitialScope;
		
		/// <summary>
		/// The ID of the generated method in the method cache.
		/// </summary>
		internal long MethodID=-1;
		
		/// <summary>
		/// Gets the root node of the abstract syntax tree.  This will be <c>null</c> until Parse()
		/// is called.
		/// </summary>
		public Statement AbstractSyntaxTree;

		/// <summary>
		/// Gets or sets optimization information.
		/// </summary>
		public MethodOptimizationHints MethodOptimizationHints;

		/// <summary>
		/// Gets the generated IL.  This will be <c>null</c> until GenerateCode is
		/// called.
		/// </summary>
		// public ILGenerator ILGenerator;

		/// <summary>
		/// Gets a delegate to the emitted dynamic method, plus any dependencies.  This will be
		/// <c>null</c> until GenerateCode is called.
		/// </summary>
		public UserDefinedFunction GeneratedMethod;

		/// <summary>
		/// Gets a name for the generated method.
		/// </summary>
		/// <returns> A name for the generated method. </returns>
		protected abstract string GetMethodName();

		/// <summary>
		/// Gets a name for the function, as it appears in the stack trace.
		/// </summary>
		/// <returns> A name for the function, as it appears in the stack trace, or <c>null</c> if
		/// this generator is generating code in the global scope. </returns>
		protected virtual string GetStackName()
		{
			return null;
		}

		/// <summary>
		/// Gets an array of types - one for each parameter accepted by the method generated by
		/// this context.
		/// </summary>
		/// <returns> An array of parameter types. </returns>
		protected virtual Type[] GetParameterTypes()
		{
			return new Type[] {
				typeof(object),		 // The "this" object.
			};
		}

		/// <summary>
		/// Parses the source text into an abstract syntax tree.
		/// </summary>
		public abstract void Parse(OptimizationInfo optimizationInfo);

		/// <summary>
		/// Optimizes the abstract syntax tree.
		/// </summary>
		public void Optimize()
		{
		}

		/// <summary>
		/// Generates IL for the script.
		/// </summary>
		public void GenerateCode(IList<ArgVariable> arguments,OptimizationInfo optimizationInfo, string bodyText)
		{	
			// Generate the abstract syntax tree if it hasn't already been generated.
			if (this.AbstractSyntaxTree == null)
			{
				Parse(optimizationInfo);
				Optimize();
			}
			
			string name=GetMethodName();
			
			// Initialize global code-gen information.
			optimizationInfo.AbstractSyntaxTree = this.AbstractSyntaxTree;
			optimizationInfo.StrictMode = this.StrictMode;
			optimizationInfo.MethodOptimizationHints = this.MethodOptimizationHints;
			optimizationInfo.FunctionName = this.GetStackName();
			optimizationInfo.Source = this.Source;

			ILGenerator generator;
			
			// Debugging or low trust path.
			ReflectionEmitModuleInfo reflectionEmitInfo = this.Engine.ReflectionEmitInfo;
			
			var typeBuilder=reflectionEmitInfo.MainType;
			
			// Create a method.
			var methodBuilder = typeBuilder.DefineMethod(this.GetMethodName(),
				System.Reflection.MethodAttributes.HideBySig | System.Reflection.MethodAttributes.Static | System.Reflection.MethodAttributes.Public,
				typeof(object), GetParameterTypes());

			// Generate the IL for the method.
			generator = new ReflectionEmitILGenerator(Engine,methodBuilder.GetILGenerator());
		
			if (this.Engine.EnableILAnalysis == true)
			{
				// Replace the generator with one that logs.
				generator = new LoggingILGenerator(generator);
			}
			
			if (this.Source.Path != null && this.Options.EnableDebugging)
			{
				// Initialize the debugging information.
				optimizationInfo.DebugDocument = reflectionEmitInfo.ModuleBuilder.DefineDocument(this.Source.Path, COMHelpers.LanguageType, COMHelpers.LanguageVendor, COMHelpers.DocumentType);
				methodBuilder.DefineParameter(1, System.Reflection.ParameterAttributes.None, "scriptEngine");
				methodBuilder.DefineParameter(2, System.Reflection.ParameterAttributes.None, "scope");
				methodBuilder.DefineParameter(3, System.Reflection.ParameterAttributes.None, "thisValue");
			}
			
			// optimizationInfo.MarkSequencePoint(generator, new SourceCodeSpan(1, 1, 1, 1));
			GenerateCode(generator, optimizationInfo);
			generator.Complete();
			
			/*
			
				// Create a new dynamic method.
				System.Reflection.Emit.DynamicMethod dynamicMethod;

				// Low privilege path.
				dynamicMethod = new System.Reflection.Emit.DynamicMethod(
					name,										// Name of the generated method.
					typeof(object),										 // Return type of the generated method.
					GetParameterTypes());								   // Parameter types of the generated method.
				generator = new ReflectionEmitILGenerator(Engine,dynamicMethod.GetILGenerator());
				
				if (this.Engine.EnableILAnalysis == true)
				{
					// Replace the generator with one that logs.
					generator = new LoggingILGenerator(generator);
				}

				// Initialization code will appear to come from line 1.
				optimizationInfo.MarkSequencePoint(generator, new SourceCodeSpan(1, 1, 1, 1));

				// Generate the IL.
				GenerateCode(generator, optimizationInfo);
				generator.Complete();
				
				// Create a delegate from the method.
				GeneratedMethod=new UserDefinedFunction(
					Engine.Prototypes.FunctionPrototype, name, arguments, bodyText, 
					dynamicMethod.CreateDelegate(GetDelegate()), optimizationInfo.NestedFunctions, StrictMode
				);
				
			*/
			
			#warning this goes in the null slot
			// Delegate.CreateDelegate(GetDelegate(), methodBuilder)
			
			GeneratedMethod=new UserDefinedFunction(
				Engine.Prototypes.FunctionPrototype, name, arguments, bodyText, 
				methodBuilder, optimizationInfo.NestedFunctions, StrictMode
			);
			
			if (this.Engine.EnableILAnalysis == true)
			{
				// Store the disassembled IL so it can be retrieved for analysis purposes.
				this.GeneratedMethod.DisassembledIL = generator.ToString();
			}
			
			if(MethodID!=-1)
			{
				// Store the generated method in the cache.
				MethodLookup.SaveAs(MethodID,GeneratedMethod);
			}
			
		}

		/// <summary>
		/// Generates IL for the script.
		/// </summary>
		/// <param name="generator"> The generator to output the CIL to. </param>
		/// <param name="optimizationInfo"> Information about any optimizations that should be performed. </param>
		protected abstract void GenerateCode(ILGenerator generator, OptimizationInfo optimizationInfo);
		
		/// <summary>
		/// Retrieves a delegate for the generated method.
		/// </summary>
		/// <returns> The delegate type that matches the method parameters. </returns>
		protected virtual Type GetDelegate()
		{
			return typeof(Func<object, object>);
		}
	}

}