using System;
using System.Collections.Generic;
using System.Reflection;
using Nitrassic.Compiler;


namespace Nitrassic.Library
{
	/// <summary>
	/// Represents a JavaScript function.
	/// </summary>
	[Serializable]
	[JSProperties(Name="Function")]
	public abstract partial class FunctionInstance : ObjectInstance
	{
		// Used to speed up access to the prototype property.
		private int cachedInstancePrototypeIndex;
		private int cachedInstancePrototypeSchema=-1;


		//	 INITIALIZATION
		//_________________________________________________________________________________________

		/// <summary>
		/// Creates a new instance of a built-in function object, with the default Function
		/// prototype.
		/// </summary>
		/// <param name="engine"> The associated script engine. </param>
		protected FunctionInstance(ScriptEngine engine)
			: base(engine, engine.Prototypes.FunctionPrototype)
		{
		}

		/// <summary>
		/// Creates a new instance of a built-in function object.
		/// </summary>
		/// <param name="prototype"> The next object in the prototype chain. </param>
		protected FunctionInstance(Prototype prototype)
			: base(prototype)
		{
		}

		/// <summary>
		/// Creates a new instance of a built-in function object.
		/// </summary>
		/// <param name="engine"> The associated script engine. </param>
		/// <param name="prototype"> The next object in the prototype chain.  Can be <c>null</c>. </param>
		protected FunctionInstance(ScriptEngine engine, Prototype prototype)
			: base(engine, prototype)
		{
		}

		/// <summary>
		/// Initializes the prototype properties.
		/// </summary>
		/// <param name="constructor"> A reference to the constructor that owns the prototype. </param>
		internal void InitializePrototypeProperties(MethodBase constructor)
		{
			// Initialize the prototype properties.
			AddProperty("constructor", constructor, PropertyAttributes.NonEnumerable,false);
			AddProperty("name", "Empty", PropertyAttributes.Configurable,false);
			AddProperty("length", 0, PropertyAttributes.Configurable,false);
		}



		//	 .NET ACCESSOR PROPERTIES
		//_________________________________________________________________________________________
		
		/// <summary>
		/// Gets the prototype of objects constructed using this function.  Equivalent to
		/// the Function.prototype property.
		/// </summary>
		public Prototype InstancePrototype
		{
			get
			{
				// See 13.2.2
				
				// Retrieve the value of the prototype property.
				//var prototype = this["prototype"] as Prototype;
				Prototype prototype;
				if (this.cachedInstancePrototypeSchema == this.InlineCacheKey)
					prototype = this.InlinePropertyValues[this.cachedInstancePrototypeIndex] as Prototype;
				else
					prototype = this.InlineGetPropertyValue("prototype", out this.cachedInstancePrototypeIndex, out this.cachedInstancePrototypeSchema) as Prototype;
				
				// If the prototype property is not set to an object, use the Object prototype property instead.
				#warning disabled a compare from this function instance and the object ctr
				if (prototype == null) // && this != this.Engine.Prototypes.Object.Constructor)
					return this.Engine.Prototypes.ObjectPrototype;
				
				return prototype;
			}
		}

		/// <summary>
		/// Gets the name of the function.
		/// </summary>
		public string Name
		{
			get { return TypeConverter.ToString(this["name"]); }
		}

		/// <summary>
		/// Gets the display name of the function.  This is equal to the displayName property, if
		/// it exists, or the name property otherwise.
		/// </summary>
		public string DisplayName
		{
			get
			{
				if (this.HasProperty("displayName"))
					return TypeConverter.ToString(this["displayName"]);
				var name = TypeConverter.ToString(this["name"]);
				if (name == string.Empty)
					return "[Anonymous]";
				return name;
			}
		}

		/// <summary>
		/// Gets the number of arguments expected by the function.
		/// </summary>
		public int Length
		{
			get { return TypeConverter.ToInteger(this["length"]); }
			protected set { this.FastSetProperty("length", value); }
		}



		//	 JAVASCRIPT INTERNAL FUNCTIONS
		//_________________________________________________________________________________________

		/// <summary>
		/// Determines whether the given object inherits from this function.  More precisely, it
		/// checks whether the prototype chain of the object contains the prototype property of
		/// this function.  Used by the "instanceof" operator.
		/// </summary>
		/// <param name="instance"> The instance to check. </param>
		/// <returns> <c>true</c> if the object inherits from this function; <c>false</c>
		/// otherwise. </returns>
		public virtual bool HasInstance(object instance)
		{
			if ((instance is ObjectInstance) == false)
				return false;
			object functionPrototype = this["prototype"];
			if ((functionPrototype is ObjectInstance) == false)
				throw new JavaScriptException(this.Engine, "TypeError", "Function has non-object prototype in instanceof check");
			var instancePrototype = ((ObjectInstance)instance).Prototype;
			while (instancePrototype != null)
			{
				if (instancePrototype == functionPrototype)
					return true;
				instancePrototype = instancePrototype.Prototype;
			}
			return false;
		}

		/// <summary>
		/// Calls this function, passing in the given "this" value and zero or more arguments.
		/// </summary>
		/// <param name="thisObj"> The value of the "this" keyword within the function. </param>
		/// <param name="argumentValues"> An array of argument values. </param>
		/// <returns> The value that was returned from the function. </returns>
		public abstract object CallLateBound(object thisObj, params object[] argumentValues);
		
		/// <summary>
		/// Creates an object, using this function as the constructor.
		/// </summary>
		/// <param name="argumentValues"> An array of argument values. </param>
		/// <returns> The object that was created. </returns>
		public virtual ObjectInstance ConstructLateBound(params object[] argumentValues)
		{
			// Create a new object and set the prototype to the instance prototype of the function.
			var newObject = ObjectInstance.CreateRawObject(this.InstancePrototype);

			// Run the function, with the new object as the "this" keyword.
			var result = CallLateBound(newObject, argumentValues);

			// Return the result of the function if it is an object.
			if (result is ObjectInstance)
				return (ObjectInstance)result;

			// Otherwise, return the new object.
			return newObject;
		}



		//	 JAVASCRIPT FUNCTIONS
		//_________________________________________________________________________________________

		/// <summary>
		/// Calls the function, passing in parameters from the given array.
		/// </summary>
		/// <param name="thisObj"> The value of <c>this</c> in the context of the function. </param>
		/// <param name="arguments"> The arguments passed to the function, as an array. </param>
		/// <returns> The result from the function call. </returns>
		public object Apply(object thisObj, object arguments)
		{
			// Convert the arguments parameter into an array.
			object[] argumentsArray;
			if (arguments == null || arguments == Undefined.Value || arguments == Null.Value)
				argumentsArray = new object[0];
			else
			{
				if ((arguments is ObjectInstance) == false)
					throw new JavaScriptException(this.Engine, "TypeError", "The second parameter of apply() must be an array or an array-like object.");
				ObjectInstance argumentsObject = (ObjectInstance)arguments;
				object arrayLengthObj = argumentsObject["length"];
				if (arrayLengthObj == null || arrayLengthObj == Undefined.Value || arrayLengthObj == Null.Value)
					throw new JavaScriptException(this.Engine, "TypeError", "The second parameter of apply() must be an array or an array-like object.");
				uint arrayLength = TypeConverter.ToUint32(arrayLengthObj);
				if (arrayLength != TypeConverter.ToNumber(arrayLengthObj))
					throw new JavaScriptException(this.Engine, "TypeError", "The second parameter of apply() must be an array or an array-like object.");
				argumentsArray = new object[arrayLength];
				for (uint i = 0; i < arrayLength; i++)
					argumentsArray[i] = argumentsObject[i];
			}

			return this.CallLateBound(thisObj, argumentsArray);
		}

		/// <summary>
		/// Calls the function.
		/// </summary>
		/// <param name="thisObj"> The value of <c>this</c> in the context of the function. </param>
		/// <param name="arguments"> Any number of arguments that will be passed to the function. </param>
		/// <returns> The result from the function call. </returns>
		public object Call(object thisObj, params object[] arguments)
		{
			return this.CallLateBound(thisObj, arguments);
		}
		
		#warning bind
		/*
		/// <summary>
		/// Creates a new function that, when called, calls this function with the given "this"
		/// value and, optionally, one or more more arguments.
		/// </summary>
		/// <param name="boundThis"> The fixed value of "this". </param>
		/// <param name="boundArguments"> Any number of fixed arguments values. </param>
		/// <returns> A new function. </returns>
		public FunctionInstance Bind(object boundThis, params object[] boundArguments)
		{
			return new BoundFunction(this, boundThis, boundArguments);
		}
		*/
		
		/// <summary>
		/// Returns a string representing this object.
		/// </summary>
		/// <returns> A string representing this object. </returns>
		public override string ToString()
		{
			return "function "+Name+"() {{ [native code] }}";
		}
		
		//	 CONSTRUCTORS
		//_________________________________________________________________________________________
		
		/// <summary>
		/// Creates a new function instance.
		/// </summary>
		/// <param name="argumentsAndBody"> The argument names plus the function body. </param>
		/// <returns> A new function instance. </returns>
		public static FunctionInstance OnConstruct(ScriptEngine engine,params string[] argumentsAndBody)
		{
			// Passing no arguments results in an empty function.
			if (argumentsAndBody.Length == 0)
				return UserDefinedFunction.Create(engine, "anonymous", new ArgVariable[0], string.Empty);

			// Split any comma-delimited names.
			List<ArgVariable> args = new List<ArgVariable>();
			for (int i = 0; i < argumentsAndBody.Length - 1; i++)
			{
				var splitNames = argumentsAndBody[i].Split(',');
				
				for (int j = 0; j < splitNames.Length; j++)
				{
					// Trim any whitespace from the start and end of the argument name.
					string argumentName = Nitrassic.Library.String.Trim(splitNames[j]);
					if (argumentName == string.Empty)
						throw new JavaScriptException(engine, "SyntaxError", "Unexpected ',' in argument");

					// Check the name is valid and resolve any escape sequences.
					argumentName = Compiler.Lexer.ResolveIdentifier(engine, argumentName);
					if (argumentName == null)
						throw new JavaScriptException(engine, "SyntaxError", "Expected identifier");
					
					args.Add(new ArgVariable(argumentName));
					
				}
				
			}

			// Create a new function.
			return UserDefinedFunction.Create(engine, "anonymous", args, argumentsAndBody[argumentsAndBody.Length - 1]);
		}
	}
}
