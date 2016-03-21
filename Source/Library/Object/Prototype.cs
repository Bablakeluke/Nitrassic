using System;
using System.Collections.Generic;
using System.Reflection;
using Nitrassic.Compiler;

namespace Nitrassic.Library
{
	/// <summary>
	/// Provides functionality common to all JavaScript objects.
	/// </summary>
	public partial class Prototype
	{
		
		/// <summary>The type name.</summary>
		public string Name;
		/// <summary>If this is a constructor prototype, the global instance of the constructor object.</summary>
		public object CtrInstance;
		/// <summary>Host script engine.</summary>
		public ScriptEngine Engine;
		/// <summary>The inherited prototype.</summary>
		public Prototype BasePrototype;
		/// <summary>
		/// The type builder for this prototype.
		/// </summary>
		public System.Reflection.Emit.TypeBuilder Builder;
		/// <summary>The prototype for the constructor of this instance prototype.</summary>
		public Prototype ConstructorPrototype;
		
		/// <summary>
		/// Used to define the root prototype only.
		/// </summary>
		internal Prototype(ScriptEngine engine){
			Engine = engine;
			_Type=typeof(object);
		}
		
		internal Prototype(ScriptEngine engine,string name,Prototype baseProto,Type type)
		{
			Engine = engine;
			BasePrototype = baseProto;
			
			if(name==null)
			{
				name=type.Name;
			}
			
			Name=name;
			_Type=type;
			
		}
		
		internal Prototype(ScriptEngine engine,string name,Prototype baseProto)
		{
			
			Engine = engine;
			BasePrototype = baseProto;
			
			ReflectionEmitModuleInfo moduleInfo=engine.ReflectionEmitInfo;
			
			if(name==null)
			{
				name="__proto__"+moduleInfo.TypeCount;
				moduleInfo.TypeCount++;
			}
			
			Name=name;
			
			Type baseType;
			
			if(baseProto==null)
			{
				baseType=typeof(object);
			}else{
				baseType=baseProto.Type;
			}
			
			// Define the type now; note that methods are not declared on these (JS defined methods are always static):
			Builder = moduleInfo.ModuleBuilder.DefineType(name, 
				System.Reflection.TypeAttributes.Public | System.Reflection.TypeAttributes.Class, baseType
			);
			
		}
		
		/// <summary>The generated type.</summary>
		private Type _Type;
		
		/// <summary>The type.</summary>
		public Type Type
		{
			get
			{
				
				if(_Type==null)
				{
					return Builder;
				}
				
				return _Type;
			}
		}
		
		/// <summary>Adds the given method as this prototypes constructor.
		/// It's either a MethodBase or a MethodGroup.</summary>
		internal void AddConstructor(object method)
		{
			AddProperty("constructor",method,Nitrassic.Library.PropertyAttributes.NonEnumerable);
		}
		
		/// <summary>Generates the type now.</summary>
		public void Complete()
		{
			if(Builder==null)
			{
				return;
			}
			
			_Type=Builder.CreateType();
			Builder=null;
		}
		
		/// <summary>
		/// Instances something of this type.
		/// </summary>
		public object Instance()
		{
			if(Builder!=null){
				Complete();
			}
			return Activator.CreateInstance(_Type);
		}
		
		/// <summary>
		/// Derive a new prototype from this prototype.
		/// </summary>
		public Prototype Derive()
		{
			
			// Create the new one:
			Prototype proto=new Prototype(Engine,null,null);
			
			// Define an internal derived property:
			proto.AddProperty("JS__Internal__Derived",Type,PropertyAttributes.NonEnumerable);
			
			return proto;
			
		}
		
		/// <summary>
		/// Creates a new inheriting prototype from this prototype.
		/// </summary>
		public Prototype Inherit()
		{
			return new Prototype(Engine,null,this);
		}
		
		/// <summary>
		/// The constructor for this prototype.
		/// This is always either a MethodBase or MethodGroup object.
		/// </summary>
		internal object Constructor{
			get{
				
				// Get the constructor:
				PropertyVariable property=GetProperty("constructor");
				
				if(property==null)
				{
					if(Engine.Prototypes.ObjectPrototype == this)
					{
						// Object should always have a constructor but just in case!
						// We don't want this to go infinity on us.
						return null;
					}
					
					// Standard global constructor:
					return Engine.Prototypes.ObjectPrototype.Constructor;
				}
				
				return property.Value;
			}
		}
		
		public PropertyVariable this[string name]{
			get{
				return GetProperty(name);
			}
		}
		
		// Properties
		public Dictionary<string, PropertyVariable> Properties;
		
		/// <summary>
		/// Gets the number of properties defined in this object.
		/// </summary>
		[JSProperties(Hidden=true)]
		public int PropertyCount
		{
			get
			{
				if (Properties == null)
				{
					return 0;
				}
				return Properties.Count;
			}
		}
		
		/// <summary>
		/// Gets the zero-based index of the property with the given name and the attributes
		/// associated with the property.
		/// </summary>
		/// <param name="name"> The name of the property. </param>
		/// <returns> A structure containing the zero-based index of the property, or <c>-1</c> if a property with the
		/// given name does not exist. </returns>
		internal PropertyVariable GetProperty(string name)
		{
			if(Properties==null)
				return null;
			
			PropertyVariable propertyInfo;
			Properties.TryGetValue(name, out propertyInfo);
			return propertyInfo;
		}
		
		/// <summary>
		/// Calls the function with the given name.  The function must exist on this object or an
		/// exception will be thrown.
		/// </summary>
		/// <param name="functionName"> The name of the function to call. </param>
		/// <param name="parameters"> The parameters to pass to the function. </param>
		/// <returns> The result of calling the function. </returns>
		internal object CallMemberFunctionOn(object thisObj,string functionName, params object[] parameters)
		{
			PropertyVariable function = GetProperty(functionName);
			if (function == null)
				throw new JavaScriptException(this.Engine, "TypeError", "Object "+ToString()+" has no method '"+functionName+"'");
			
			MethodBase method = function.Value as MethodBase;
			
			if(method!=null)
			{
				// Invoke it:
				return method.Invoke(thisObj,parameters);
			}
			
			MethodGroup set = function.Value as MethodGroup;
			
			if(set!=null)
			{
				// Invoke it:
				set.Invoke(thisObj,parameters);
			}
			
			throw new JavaScriptException(this.Engine, "TypeError", "Property '"+functionName+"' of object "+ToString()+" is not a function");
			
		}
		
		/// <summary>
		/// Adds a property to this object.
		/// </summary>
		/// <param name="name"> The name of the property to add. </param>
		/// <param name="attributes"> The property attributes. </param>
		internal PropertyVariable AddProperty(string name, object value, Nitrassic.Library.PropertyAttributes attributes)
		{
			
			if(Properties==null)
			{
				Properties = new Dictionary<string, PropertyVariable>();
			}
			
			PropertyVariable sp=new PropertyVariable(this, name, value, attributes);
			Properties[name]=sp;
			
			return sp;
		}
		
		private static int Count(IEnumerable<PropertyNameAndValue> properties)
		{
			
			int c=0;
			foreach (var property in properties)
			 c++;
			
			return c;
			
		}
		
		//	 ATTRIBUTE-BASED PROTOTYPE POPULATION
		//_________________________________________________________________________________________
		
		/// <summary>
		/// Populate with everything from the given type.
		/// </summary>
		internal void PopulatePrototype(Type type,bool ctrs)
		{
			PopulateFunctions(type,ctrs);
			PopulateFields(type,0);
			
			if(Constructor==null)
			{
				AddConstructor(Engine.Prototypes.Object);
			}
		}
		
		/// <summary>
		/// Populate with only the static/ constructors.
		/// </summary>
		internal void PopulateConstructorPrototype(Type type)
		{
			PopulateStaticFunctions(type,true);
			PopulateFields(type,2);
		}
		
		/// <summary>
		/// Populate with the non-static members.
		/// </summary>
		internal void PopulateInstancePrototype(Type type)
		{
			PopulateNonStaticFunctions(type,false);
			PopulateFields(type,1);
		}
		
		/// <summary>
		/// Populates the object with functions by searching a .NET type for methods.
		/// Should be called only once at startup.  Also automatically
		/// populates properties too.
		/// </summary>
		/// <param name="type"> The type to search for methods. </param>
		/// <param name="ctrs"> Populate constructors too.</param>
		internal protected void PopulateFunctions(Type type,bool ctrs)
		{
			PopulateFunctions(type, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly,ctrs,0);
		}
		
		/// <summary>
		/// Used by the prototype for constructors.
		/// E.g. String.aStaticFunction is a function on the String constructor prototype.
		/// </summary>
		/// <param name="type"> The type to search for methods. </param>
		/// <param name="ctrs"> Populate constructors too.</param>
		internal protected void PopulateStaticFunctions(Type type,bool ctrs)
		{
			PopulateFunctions(type, BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly,ctrs,2);
		}
		
		/// <summary>
		/// Used by instances which have a constructor.
		/// Essentially culls out the functions which were claimed by the constructor.
		/// </summary>
		/// <param name="type"> The type to search for methods. </param>
		/// <param name="ctrs"> Populate constructors too.</param>
		internal protected void PopulateNonStaticFunctions(Type type,bool ctrs)
		{
			PopulateFunctions(type, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly,ctrs,1);
		}
		
		/// <summary>
		///  The value of the special .make property. Found on constructor objects only.
		/// </summary>
		public object OnConstruct
		{
			get{
				// Get the property:
				PropertyVariable property=GetProperty(".make");
				
				if(property==null)
				{
					return null;
				}
				
				return property.Value;
			}
		}
		
		/// <summary>
		///  The value of the special .call property. Found on constructor objects only.
		/// </summary>
		public object OnCall
		{
			get{
				// Get the property:
				PropertyVariable property=GetProperty(".call");
				
				if(property==null)
				{
					return null;
				}
				
				return property.Value;
			}
		}
		
		/// <summary>
		/// Adds the given method to the prototype.
		/// </summary>
		private void AddMethod(MethodBase method,Type type,int staticMode)
		{
			
			// Get the properties attribute if it's got one:
			var attribute = (JSProperties)Attribute.GetCustomAttribute(method, typeof(JSProperties));
			if (attribute != null && attribute.Hidden)
				return;

			// Determine the name of the method.
			string name;
			if (attribute!=null && attribute.Name != null)
				name = attribute.Name;
			else
				name = method.Name;
			
			if(name.StartsWith("get_") || name.StartsWith("set_"))
			{
				return;
			}
			
			if(method.IsStatic && staticMode!=0)
			{
				// If it's static and it does not have a 'thisObj' property
				// then it's a constructor level static function, IF the object has a constructor at all.
				ParameterInfo[] mParams=method.GetParameters();
				
				if(mParams.Length>0)
				{
					
					if(mParams[0].Name=="thisObj" || (mParams.Length>1 && mParams[1].Name=="thisObj"))
					{
						// Not actually static.
						if(staticMode==2)
						{
							return;
						}
					}else if(staticMode==1)
					{
						return;
					}
					
				}
			}
			
			bool enumerable=true;
			
			if(name=="OnConstruct" || name==".ctor")
			{
				name=".make";
				enumerable=false;
			}
			else if(name=="OnCall")
			{
				name=".call";
				enumerable=false;
			}
			
			// For methods, auto == lowercase.
			if(attribute==null)
			{
				
				name=char.ToLower(name[0])+name.Substring(1);
				
			}else
			{
				
				// For methods, auto == lowercase
				if(attribute.FirstCharacter==CaseMode.Upper)
				{
					name=char.ToUpper(name[0])+name.Substring(1);
				}else if(attribute.FirstCharacter==CaseMode.Lower || attribute.FirstCharacter==CaseMode.Auto)
				{
					name=char.ToLower(name[0])+name.Substring(1);
				}
				
			}
			
			// Check property attributes.
			Nitrassic.Library.PropertyAttributes descriptorAttributes = Nitrassic.Library.PropertyAttributes.Sealed;
			if (attribute!=null && attribute.IsEnumerable && enumerable)
				descriptorAttributes |= Nitrassic.Library.PropertyAttributes.Enumerable;
			if (attribute==null || attribute.IsConfigurable)
				descriptorAttributes |= Nitrassic.Library.PropertyAttributes.Configurable;
			if (attribute==null || attribute.IsWritable)
				descriptorAttributes |= Nitrassic.Library.PropertyAttributes.Writable;
				
			// Already defined this property?
			PropertyVariable property=GetProperty(name);
			
			if(property==null)
			{
				// Add as a single method:
				property=AddProperty(name,method,descriptorAttributes);
			}
			else
			{
				// Already a method set?
				MethodGroup group=property.Value as MethodGroup;
				
				if(group==null)
				{
					
					// Create a method group and add the current method to it:
					group=new MethodGroup();
					
					// Set the attributes:
					group.PropertyAttributes = descriptorAttributes;
					
					// Add the method:
					group.Add(property.Value as MethodBase);
					
					// Force the property to change type:
					property.ForceChange(group);
					
				}
				
				// Add this method to the group:
				group.Add(method);
				
				if (group.PropertyAttributes != descriptorAttributes)
					throw new InvalidOperationException("Inconsistant property attributes detected on "+method+".");
				
			}
			
		}
		
		/// <summary>
		/// Populates the object with functions by searching a .NET type for methods.
		/// Should be called only once at startup.  Also automatically
		/// populates properties too.
		/// </summary>
		/// <param name="type"> The type to search for methods. </param>
		/// <param name="bindingFlags"> The binding flags to use to search for properties and methods. </param>
		internal protected void PopulateFunctions(Type type, BindingFlags bindingFlags,bool constructors,int staticMode)
		{
			if (type == null)
				type = this.GetType();
			
			// Group the methods on the given type by name.
			var methods = type.GetMethods(bindingFlags);
			
			foreach (var method in methods)
			{
				AddMethod(method,type,staticMode);
			}
			
			if(constructors)
			{
				var set = type.GetConstructors();
				
				foreach (var ctr in set)
				{
					AddMethod(ctr,type,staticMode);
				}
			}
			
			PropertyInfo[] properties = type.GetProperties(bindingFlags);
			foreach (PropertyInfo prop in properties)
			{
				var attribute = Attribute.GetCustomAttribute(prop, typeof(JSProperties), false) as JSProperties;
				if (attribute != null && attribute.Hidden)
					continue;
			
				// The property name.
				string name;
				if (attribute!=null && attribute.Name != null)
					name = attribute.Name;
				else
					name = prop.Name;
				
				// For properties, auto == lowercase.
				if(attribute==null)
				{
					
					name=char.ToLower(name[0])+name.Substring(1);
					
				}else
				{
					
					// For properties, auto == lowercase
					if(attribute.FirstCharacter==CaseMode.Upper)
					{
						name=char.ToUpper(name[0])+name.Substring(1);
					}else if(attribute.FirstCharacter==CaseMode.Lower || attribute.FirstCharacter==CaseMode.Auto)
					{
						name=char.ToLower(name[0])+name.Substring(1);
					}
					
				}
				
				var getMethod = prop.GetGetMethod(true);
				
				if(getMethod.IsStatic)
				{
					// If it's static then it's a constructor level function (if there is a constructor).
					
					if(staticMode==1)
					{
						// We only want non-static ones.
						continue;
					}
				}else if(staticMode==2)
				{
					// We only want static ones.
					continue;
				}
				
				// Define the property.
				AddProperty(name, prop, PropertyAttributes.FullAccess);
			}
		}

		/// <summary>
		/// Populates the object with properties by searching a .NET type for fields.
		/// Should be called only once at startup.
		/// </summary>
		internal protected void PopulateFields()
		{
			PopulateFields(null,0);
		}
		
		/// <summary>
		/// Populates the object with properties by searching a .NET type for fields.
		/// Should be called only once at startup.
		/// </summary>
		/// <param name="type"> The type to search for fields. </param>
		internal protected void PopulateFields(Type type,int staticMode)
		{
			if (type == null)
				type = this.GetType();
			
			// Find all fields with [JsField]
			foreach (var field in type.GetFields())
			{
				var attribute = (JSProperties)Attribute.GetCustomAttribute(field, typeof(JSProperties));
				if (attribute != null && attribute.Hidden)
					continue;
				
				string name;
				if (attribute!=null && attribute.Name != null)
					name = attribute.Name;
				else
					name = field.Name;
				
				if(field.IsStatic)
				{
					// If it's static then it's a constructor level function (if there is a constructor).
					
					if(staticMode==1)
					{
						// We only want non-static ones.
						continue;
					}
				}
				else if(staticMode==2)
				{
					// We only want static ones.
					continue;
				}
				
				if(attribute!=null)
				{
					
					// For fields, auto == as is
					if(attribute.FirstCharacter==CaseMode.Upper)
					{
						name=char.ToUpper(name[0])+name.Substring(1);
					}else if(attribute.FirstCharacter==CaseMode.Lower)
					{
						name=char.ToLower(name[0])+name.Substring(1);
					}
					
				}
				
				if(field.IsLiteral && !field.IsInitOnly)
				{
					AddProperty(name,field.GetValue(null),PropertyAttributes.FullAccess);
				}else
				{
					AddProperty(name,field,PropertyAttributes.FullAccess);
				}
			}
		}
		
	}
}
