using System;
using System.Collections.Generic;
using System.Reflection;
using Nitrassic.Library;

namespace Nitrassic.Compiler
{

	/// <summary>
	/// Represents a generic delegate that all method calls pass through.  For internal use only.
	/// </summary>
	/// <param name="engine"> The associated script engine. </param>
	/// <param name="thisObj"> The value of the <c>this</c> keyword. </param>
	/// <param name="arguments"> The arguments that were passed to the function. </param>
	/// <returns> The result of calling the method. </returns>
	public delegate object BinderDelegate(ScriptEngine engine,object thisObj, params object[] arguments);
	
	/// <summary>
	/// Selects a method from a list of candidates and performs type conversion from actual
	/// argument type to formal argument type.
	/// </summary>
	[Serializable]
	internal abstract class Binder
	{
		[NonSerialized]
		private FunctionDelegate[] delegateCache;
		private const int MaximumCachedParameterCount = 8;




		//	 PROPERTIES
		//_________________________________________________________________________________________

		/// <summary>
		/// Gets the name of the target methods.
		/// </summary>
		public abstract string Name
		{
			get;
		}

		/// <summary>
		/// Gets the full name of the target methods, including the type name.
		/// </summary>
		public abstract string FullName
		{
			get;
		}

		/// <summary>
		/// Gets the maximum number of arguments of any of the target methods.  Used to set the
		/// length property on the function.
		/// </summary>
		public virtual int FunctionLength
		{
			get { return 0; }
		}




		//	 METHODS
		//_________________________________________________________________________________________

		/// <summary>
		/// Calls the bound method.
		/// </summary>
		/// <param name="engine"> The associated script engine. </param>
		/// <param name="thisObj"> The value of the <c>this</c> keyword. </param>
		/// <param name="arguments"> The arguments to pass to the function. </param>
		/// <returns> The result of calling the method. </returns>
		public object Call(ScriptEngine engine, object thisObj, params object[] arguments)
		{
			FunctionDelegate binderDelegate = CreateDelegate(arguments.Length,engine);
			return binderDelegate(thisObj, arguments);
		}

		/// <summary>
		/// Creates a delegate that does type conversion and calls the bound method.
		/// </summary>
		/// <param name="argumentCount"> The number of arguments that will be passed to the delegate. </param>
		/// <returns> A delegate that does type conversion and calls the method represented by this
		/// object. </returns>
		/// <remarks> This method caches the result so calling CreateDelegate a second time with
		/// the same parameter count will be markedly quicker. </remarks>
		public FunctionDelegate CreateDelegate(int argumentCount,ScriptEngine engine)
		{
			// If there are too many arguments, don't cache the delegate.
			if (argumentCount > MaximumCachedParameterCount)
				return CreateDelegateCore(argumentCount,engine);

			// Save the delegate that is created into a cache so it doesn't have to be created again.
			if (this.delegateCache == null)
				this.delegateCache = new FunctionDelegate[MaximumCachedParameterCount + 1];
			var binderDelegate = this.delegateCache[argumentCount];
			if (binderDelegate == null)
			{
				// Create a binding method.
				binderDelegate = CreateDelegateCore(argumentCount,engine);

				// Store it in the cache.
				this.delegateCache[argumentCount] = binderDelegate;
			}
			return binderDelegate;
		}

		/// <summary>
		/// Creates a delegate that does type conversion and calls the bound method.
		/// </summary>
		/// <param name="argumentCount"> The number of arguments that will be passed to the delegate. </param>
		/// <returns> A delegate that does type conversion and calls the method represented by this
		/// object. </returns>
		/// <remarks> No caching of the result occurs. </remarks>
		private FunctionDelegate CreateDelegateCore(int argumentCount,ScriptEngine engine)
		{
			// Create a new dynamic method.
			System.Reflection.Emit.DynamicMethod dm;
			ILGenerator generator;

				// Partial trust / silverlight.
				dm = new System.Reflection.Emit.DynamicMethod(
					string.Format("binder_for_{0}", this.FullName),												 // Name of the generated method.
					typeof(object),																				 // Return type of the generated method.
					new Type[] { typeof(object), typeof(object[]) });						 // Parameter types of the generated method.
				generator = new ReflectionEmitILGenerator(engine,dm.GetILGenerator());
			
			// Generate the body of the method.
			GenerateStub(generator, argumentCount);
			
			// Convert the DynamicMethod to a delegate.
			return (FunctionDelegate)dm.CreateDelegate(typeof(FunctionDelegate));
		}

		/// <summary>
		/// Generates a method that does type conversion and calls the bound method.
		/// </summary>
		/// <param name="generator"> The ILGenerator used to output the body of the method. </param>
		/// <param name="argumentCount"> The number of arguments that will be passed to the delegate. </param>
		/// <returns> A delegate that does type conversion and calls the method represented by this
		/// object. </returns>
		protected abstract void GenerateStub(ILGenerator generator, int argumentCount);




		//	 OBJECT OVERRIDES
		//_________________________________________________________________________________________

		/// <summary>
		/// Returns a textual representation of this object.
		/// </summary>
		/// <returns> A textual representation of this object. </returns>
		public override string ToString()
		{
			return this.FullName;
		}
	}
}
