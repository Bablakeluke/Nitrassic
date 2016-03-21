using System;
using System.Collections;
using System.Collections.Generic;
using Nitrassic.Compiler;
using Nitrassic.Library;
using System.Reflection;


namespace Nitrassic{
	
	/// <summary>
	/// A lookup from system type to prototype object.
	/// </summary>
	public class PrototypeLookup{
		
		/// <summary>The standard globally shared set.</summary>
		public static PrototypeLookup All;
		
		internal Prototype BasePrototype;
		
		internal Prototype BooleanPrototype;
		internal Prototype NumberPrototype;
		internal Prototype StringPrototype;
		internal Prototype RegExpPrototype;
		internal Prototype ObjectPrototype;
		internal Prototype ArrayPrototype;
		internal Prototype FunctionPrototype;
		
		/// <summary>
		/// Gets the built-in Object constructor.
		/// </summary>
		public object Object;
		
		/// <summary>
		/// Gets the built-in Array constructor.
		/// </summary>
		public MethodBase Array;
		
		/// <summary>
		/// Gets the built-in Date constructor.
		/// </summary>
		public MethodBase Date;
		
		/// <summary>
		/// Gets the built-in Function constructor.
		/// </summary>
		public object Function;
		
		/// <summary>
		/// Gets the built-in RegExp constructor.
		/// </summary>
		public MethodBase RegExp;
		
		// The built-in error objects.
		
		/// <summary>
		/// Gets the built-in Error constructor.
		/// </summary>
		public MethodBase Error;
		/// <summary>
		/// Gets the built-in RangeError constructor.
		/// </summary>
		public MethodBase RangeError;
		/// <summary>
		/// Gets the built-in TypeError constructor.
		/// </summary>
		public MethodBase TypeError;
		/// <summary>
		/// Gets the built-in TypeError constructor.
		/// </summary>
		public MethodBase SyntaxError;
		/// <summary>
		/// Gets the built-in TypeError constructor.
		/// </summary>
		public MethodBase URIError;
		/// <summary>
		/// Gets the built-in TypeError constructor.
		/// </summary>
		public MethodBase EvalError;
		/// <summary>
		/// Gets the built-in TypeError constructor.
		/// </summary>
		public MethodBase ReferenceError;
		
		/// <summary>
		/// The parent script engine.
		/// </summary>
		public ScriptEngine Engine;
		
		/// <summary>
		/// The lookup itself.
		/// </summary>
		public Dictionary<Type,Prototype> Lookup=new Dictionary<Type,Prototype>();
		
		
		public PrototypeLookup(ScriptEngine engine){
			Engine=engine;
		}
			
		internal void Setup(){
			// Create the base of the prototype chain:
			BasePrototype=new Prototype(Engine);
			
			// Setup globals:
			Engine.Global = new Window(Engine);
			
			// Setup the global cache:
			Engine.ReflectionEmitInfo.SetupGlobalCache();
			
			// Setup the object constructor:
			Object = SetConstructor(typeof(object),out ObjectPrototype);
			
			// Setup the function constructor:
			Function=SetConstructor(typeof(FunctionProto),out FunctionPrototype);
			
			// Initialize the prototypes for the base of the prototype chain.
			BasePrototype.AddConstructor(Object);
			
			BooleanPrototype=Get(typeof(Nitrassic.Library.Boolean));
			NumberPrototype=Get(typeof(Nitrassic.Library.Number));
			StringPrototype=Get(typeof(Nitrassic.Library.String));
			RegExpPrototype=Get(typeof(Nitrassic.Library.RegExp));
			ArrayPrototype=Get(typeof(Nitrassic.Library.Array));
			
			Array=ArrayPrototype.Constructor as MethodBase;
			Date=SetConstructor(typeof(Nitrassic.Library.Date)) as MethodBase;
			RegExp=RegExpPrototype.Constructor as MethodBase;
			
			// Create the error functions.
			Error = SetConstructor(typeof(Library.Error)) as MethodBase;
			RangeError = SetConstructor(typeof(RangeError)) as MethodBase;
			TypeError = SetConstructor(typeof(TypeError)) as MethodBase;
			SyntaxError = SetConstructor(typeof(SyntaxError)) as MethodBase;
			URIError = SetConstructor(typeof(URIError)) as MethodBase;
			EvalError = SetConstructor(typeof(EvalError)) as MethodBase;
			ReferenceError = SetConstructor(typeof(ReferenceError)) as MethodBase;
			
			// Create the typed array functions.
			Get(typeof(ArrayBuffer));
			Get(typeof(DataView));
			
			SetConstructor(typeof(Int8Array));
			SetConstructor(typeof(Uint8Array));
			SetConstructor(typeof(Uint8ClampedArray));
			SetConstructor(typeof(Int16Array));
			SetConstructor(typeof(Uint16Array));
			SetConstructor(typeof(Int32Array));
			SetConstructor(typeof(Uint32Array));
			SetConstructor(typeof(Float32Array));
			SetConstructor(typeof(Float64Array));
			
			Engine.SetGlobal("JSON",new JSON());
			Engine.SetGlobal("console",new console(Engine));
			Engine.SetGlobal("Math",new MathObject());
			
		}
		
		/// <summary>Sets the given type as a global constructor.</summary>
		private object SetConstructor(Type type)
		{
			Prototype prototype;
			return SetConstructor(type,out prototype);
		}
		
		/// <summary>Sets the given type as a global constructor.</summary>
		private object SetConstructor(Type type,out Prototype prototype)
		{
			// Get the prototype:
			prototype=Get(type);
			
			// Build the constructor prototype:
			Prototype ctrProto=prototype.ConstructorPrototype;
			
			if(ctrProto==null)
			{
				throw new Exception("The type '"+type.Name+"' is not constructable.");
			}
			
			// Set the global:
			Engine.SetGlobal(prototype.Name,ctrProto.CtrInstance);
			
			// Return the instance object:
			return ctrProto.CtrInstance;
		}
		
		/// <summary>
		/// The number of prototypes in this lookup.
		/// </summary>
		public int Count{
			get{
				return Lookup.Count;
			}
		}
		
		/// <summary>Gets the prototype instance for the given type. Assumes it exists.</summary>
		public Prototype this[Type type]{
			get{
				return Lookup[type];
			}
		}
		
		/// <summary>
		/// Gets or creates the prototype for the given type.
		/// </summary>
		public Prototype Get(string typeName){
			return Get(typeName,null);
		}
		
		/// <summary>
		/// Gets or creates the prototype for the given type.
		/// </summary>
		public Prototype Get(string typeName,Prototype parentProto){
			#warning Resolve the given type name into a system type.
			// (Using existing Nitro framework code)
			Type type=null;
			return Get(type,parentProto);
		}
		
		/// <summary>
		/// Gets or creates the prototype for the given type.
		/// </summary>
		public Prototype Get(Type type){
			return Get(type,null);
		}
		
		/// <summary>
		/// Gets or creates the prototype for the given type with an optional parent prototype.
		/// </summary>
		public Prototype Get(Type type,Prototype parentProto){
			
			string name=type.Name;
			var attribute = (JSProperties)Attribute.GetCustomAttribute(type,typeof(JSProperties),false);
			
			if (attribute != null && attribute.Name!=null)
			{
				name=attribute.Name;
			}
			
			Prototype result;
			if(Lookup.TryGetValue(type,out result)){
				return result;
			}
			
			// Get the parent proto (obj if none):
			if(parentProto==null){
				
				Type baseType=type.BaseType;
				
				if(baseType==typeof(object) || baseType==null){
					
					// Get the very bottom of the proto stack:
					parentProto=Engine.Prototypes.BasePrototype;
					
				}else{
				
					parentProto=Get(type.BaseType);
				
				}
			}
			
			// Create and add it:
			result=new Prototype(Engine,name,parentProto,type);
			Lookup[type]=result;
			
			// Does it declare PrototypeFor?
			FieldInfo protoFor=type.GetField("PrototypeFor");
			
			if(protoFor!=null)
			{
				// Yes - get the type it's a prototype for:
				object value=protoFor.GetValue(null);
				
				// It can actually be a set, so check if it's just one first:
				Type protoForType=value as Type;
				
				if(protoForType!=null)
				{
					// It's just the one.
					Lookup[protoForType]=result;
				}
				else
				{
					// It's a set - add each one.
					// For example, Function acts as the prototype for both MethodBase and MethodGroup.
					Type[] set=value as Type[];
					
					for(int i=0;i<set.Length;i++)
					{
						Lookup[set[i]]=result;
					}
				}
			}
			
			// Can this prototype be instanced?
			// - Not a static class
			// - Doesn't have "NoConstructors" set
			bool instanceable=(!type.IsAbstract && !(attribute!=null && attribute.NoConstructors));
			
			if(instanceable)
			{
				
				if(type==typeof(object)){
					name="_base_object";
				}
				
				// Build the constructor prototype:
				Prototype ctr=new Prototype(Engine,name+"_.ctor",null);
				ctr.PopulateConstructorPrototype(type);
				ctr.AddProperty("constructor",Function,Nitrassic.Library.PropertyAttributes.NonEnumerable);
				result.ConstructorPrototype=ctr;
				
				// ctr is a function instance, so set its base proto:
				ctr.BasePrototype=FunctionPrototype;
				
				// Create the type and add it to the lookup:
				ctr.Complete();
				Lookup[ctr.Type]=ctr;
				
				// Generate an instance of the ctr:
				ctr.CtrInstance=ctr.Instance();
				
				// Setup the instance proto:
				result.PopulateInstancePrototype(type);
				
				// Add the constructor property to the instance:
				result.AddProperty("constructor",ctr.CtrInstance,Nitrassic.Library.PropertyAttributes.NonEnumerable);
				
			}else{
				
				// Not instanceable - has no constructors or a constructor proto.
				// Init the single prototype now:
				result.PopulatePrototype(type,false);
				
			}
			
			return result;
		}
		
	}
	
}