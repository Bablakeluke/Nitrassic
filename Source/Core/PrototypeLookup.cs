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
		public Prototype Date;
		
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
		public Prototype Error;
		/// <summary>
		/// Gets the built-in RangeError constructor.
		/// </summary>
		public Prototype RangeError;
		/// <summary>
		/// Gets the built-in TypeError constructor.
		/// </summary>
		public Prototype TypeError;
		/// <summary>
		/// Gets the built-in TypeError constructor.
		/// </summary>
		public Prototype SyntaxError;
		/// <summary>
		/// Gets the built-in TypeError constructor.
		/// </summary>
		public Prototype URIError;
		/// <summary>
		/// Gets the built-in TypeError constructor.
		/// </summary>
		public Prototype EvalError;
		/// <summary>
		/// Gets the built-in TypeError constructor.
		/// </summary>
		public Prototype ReferenceError;
		
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
			
			// Create the prototype for the global scope:
			Engine.GlobalPrototype = CreateStatic();
			
			// Create the base of the prototype chain:
			BasePrototype=new Prototype(Engine);
			
			// Setup globals:
			// Engine.Global = new Window(Engine);
			
			// Setup the global cache:
			Engine.ReflectionEmitInfo.SetupGlobalCache();
			
			// Setup the object constructor:
			Object = SetConstructor(typeof(object),out ObjectPrototype).CtrInstance;
			
			// Setup the function constructor:
			Function=SetConstructor(typeof(FunctionProto),out FunctionPrototype).CtrInstance;
			
			// Initialize the prototypes for the base of the prototype chain.
			BasePrototype.AddConstructor(Object);
			
			BooleanPrototype=Get(typeof(Nitrassic.Library.Boolean));
			NumberPrototype=Get(typeof(Nitrassic.Library.Number));
			StringPrototype=Get(typeof(Nitrassic.Library.String));
			RegExpPrototype=Get(typeof(Nitrassic.Library.RegExp));
			ArrayPrototype=Get(typeof(Nitrassic.Library.Array));
			
			Array=ArrayPrototype.Constructor as MethodBase;
			Date=SetConstructor(typeof(Nitrassic.Library.Date));
			RegExp=RegExpPrototype.Constructor as MethodBase;
			
			// Create the error functions.
			Error = SetConstructor(typeof(Library.Error));
			RangeError = SetConstructor(typeof(RangeError));
			TypeError = SetConstructor(typeof(TypeError));
			SyntaxError = SetConstructor(typeof(SyntaxError));
			URIError = SetConstructor(typeof(URIError));
			EvalError = SetConstructor(typeof(EvalError));
			ReferenceError = SetConstructor(typeof(ReferenceError));
			
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
			SetConstructor(typeof(ArrayBuffer));
			
			Engine.SetGlobal("JSON",new JSON());
			Engine.SetGlobal("console",new console(Engine));
			Engine.SetGlobal("Math",new MathObject());
			
		}
		
		/// <summary>Creates a new prototype, suitable for adding extra properties to.</summary>
		public Prototype CreateStatic(){
			Prototype proto=new Prototype(Engine,"_global",null,true);
			Lookup[proto.Type]=proto;
			return proto;
		}
		
		/// <summary>Creates a new prototype, suitable for adding extra properties to.</summary>
		public Prototype Create(){
			Prototype proto=new Prototype(Engine,null,null,false);
			Lookup[proto.Type]=proto;
			return proto;
		}
		
		/// <summary>Sets the given type as a global constructor.</summary>
		private Prototype SetConstructor(Type type)
		{
			Prototype prototype;
			return SetConstructor(type,out prototype);
		}
		
		/// <summary>Sets the given type as a global constructor.</summary>
		private Prototype SetConstructor(Type type,out Prototype prototype)
		{
			// Get the prototype:
			prototype=Get(type);
			
			// Build the constructor prototype:
			Prototype ctrProto=prototype.ConstructorPrototype;
			
			if(ctrProto==null)
			{
				throw new Exception("The type '"+type.Name+"' is not constructable.");
			}
			
			object ctrInstance=ctrProto.CtrInstance;
			
			// Create the global:
			Engine.SetGlobal(prototype.Name,ctrInstance);
			
			// Return it:
			return ctrProto;
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
			
			// Get the type:
			Type type=CodeReference.GetFirstType(typeName);
			
			// Resolve the prototype:
			return Get(type,parentProto);
			
		}
		
		/// <summary>
		/// Gets or creates the prototype for the given type.
		/// </summary>
		public Prototype Get(Type type){
			return Get(type,null);
		}
		
		public void CompleteAll(){
			
			foreach(KeyValuePair<Type,Prototype> kvp in Lookup){
				
				kvp.Value.Complete();
				
			}
			
		}
		
		/// <summary>
		/// Gets or creates the prototype for the given type with an optional parent prototype.
		/// </summary>
		public Prototype Get(Type type,Prototype parentProto){
			
			if(type==null){
				return null;
			}
			
			string name=type.Name;
			
			JSProperties attribute;
			
			if(type is System.Reflection.Emit.TypeBuilder){
				
				attribute=null;
				
			}else{
				
				attribute = (JSProperties)Attribute.GetCustomAttribute(type,typeof(JSProperties),false);
				
				if (attribute != null && attribute.Name!=null)
				{
					name=attribute.Name;
				}
				
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
			Lookup[result.Type]=result;
			
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
				
				Prototype ctr=null;
				
				// Build the constructor prototype:
				ctr=new Prototype(Engine,name+"_ctor",null,false);
				ctr.PopulateConstructorPrototype(type);
				ctr.AddProperty("constructor",Function,Nitrassic.Library.PropertyAttributes.NonEnumerable);
				result.ConstructorPrototype=ctr;
				
				// ctr is a function instance, so set its base proto:
				ctr.BasePrototype=FunctionPrototype;
				
				// Generate an instance of the ctr (this also bakes the type):
				ctr.CtrInstance=ctr.Instance();
				
				// Add it to the lookup:
				Lookup[ctr.Type]=ctr;
				
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