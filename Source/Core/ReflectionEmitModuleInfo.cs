using System;
using System.Collections.Generic;
using Nitrassic.Library;
using System.Reflection;
using System.Reflection.Emit;
using System.Diagnostics;


namespace Nitrassic
{
	#if !WINDOWS_PHONE
	internal class ReflectionEmitModuleInfo
	{
		
		/// <summary>
		/// The type which holds all currently compiling methods.
		/// </summary>
		internal TypeBuilder MainType;
		
		/// <summary>
		/// When the script engine starts, it generates a static class to hold general info.
		/// This is the static reference to the script engine itself.
		/// </summary>
		internal FieldInfo GlobalEngine;
		
		/// <summary>The global object as a static instance.</summary>
		internal FieldInfo GlobalObject;
		
		/// <summary>The prototypes set as a static instance.</summary>
		internal FieldInfo GlobalPrototypes;
		
		/// <summary>
		/// The global type which holds general static information such as a reference to the script engine.
		/// </summary>
		internal Type GlobalType;
		
		/// <summary>
		/// A reference to the current global variable ID.
		/// </summary>
		public int GlobalID;
		public ScriptEngine Engine;
		public System.Reflection.Emit.AssemblyBuilder AssemblyBuilder;
		public System.Reflection.Emit.ModuleBuilder ModuleBuilder;
		public int TypeCount;
		
		internal ReflectionEmitModuleInfo(ScriptEngine engine){
			
			Engine=engine;
			
			// Create a dynamic assembly and module.
			AssemblyBuilder = System.Threading.Thread.GetDomain().DefineDynamicAssembly(
				new AssemblyName("Nitro Dynamic Assembly"), AssemblyBuilderAccess.Run);
			
			// Create a dynamic module.
			ModuleBuilder = AssemblyBuilder.DefineDynamicModule("Module", ScriptEngine.EnableDebugging);
			
		}
		
		/// <summary>Create a global.</summary>
		internal FieldInfo CreateGlobal(Type type){
			
			if(MainType==null){
				CreateMainType();
			}
			
			GlobalID++;
			
			return Define(MainType,"__global_"+GlobalID,type);
			
		}
		
		internal void CreateMainType(){
			
			// Create a new type to hold our method.
			MainType = ModuleBuilder.DefineType("JavaScriptClass" + TypeCount.ToString(), 
				System.Reflection.TypeAttributes.Public | System.Reflection.TypeAttributes.Class
			);
			
			TypeCount++;
			
		}
		
		public Type Close()
		{
			Type result=MainType.CreateType();
			MainType=null;
			return result;
		}
		
		internal void SetupGlobalCache()
		{
			if(GlobalType!=null)
			{
				return;
			}
			
			// Create a new type to hold our method.
			var typeBuilder = ModuleBuilder.DefineType("GlobalCache", TypeAttributes.Public | TypeAttributes.Class);
			
			Define(typeBuilder,"Engine",typeof(ScriptEngine));
			Define(typeBuilder,"Globals",typeof(Window));
			Define(typeBuilder,"Prototypes",typeof(PrototypeLookup));
			
			// Build it now:
			GlobalType = typeBuilder.CreateType();
			
			// Get the fields:
			GlobalEngine=Get("Engine");
			GlobalObject=Get("Globals");
			GlobalPrototypes=Get("Prototypes");
			
			// Apply the values:
			GlobalEngine.SetValue(null,Engine);
			GlobalObject.SetValue(null,Engine.Global);
			GlobalPrototypes.SetValue(null,Engine.Prototypes);
			
		}
		
		/// <summary>
		/// Defines a static field on the given type builder.
		/// </summary>
		private FieldInfo Define(TypeBuilder builder,string field,Type fieldType){
			return builder.DefineField(field,fieldType,FieldAttributes.Static | FieldAttributes.Public);
		}
		
		/// <summary>
		/// Gets the field info for the given field on the global cache object.
		/// </summary>
		private FieldInfo Get(string name){
			return GlobalType.GetField(name);
		}
		
	}
#endif //!WINDOWS_PHONE
}