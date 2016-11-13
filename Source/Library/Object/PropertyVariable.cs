using System;
using System.Collections.Generic;
using Nitrassic.Compiler;
using System.Reflection;


namespace Nitrassic.Library
{

	/// <summary>
	/// Represents the information stored about a property in the class schema.
	/// </summary>
	public class PropertyVariable : Variable
	{
		/// <summary>
		/// Creates a new SchemaProperty instance.
		/// </summary>
		/// <param name="index"> The index of the property in the
		/// <see cref="ObjectInstance.InlinePropertyValues"/> array. </param>
		/// <param name="attributes"> The property attributes.  These attributes describe how the
		/// property can be modified. </param>
		public PropertyVariable(Prototype proto,string name, object value, PropertyAttributes attributes)
			: base(name)
		{
			Prototype = proto;
			Attributes = attributes;
			
			if(value!=null){
				ConstantValue = value;
				_Type = value.GetType();
				Resolve(_Type);
			}
			
		}
		
		public PropertyVariable(Prototype proto,string name, PropertyAttributes attributes, Type valueType)
			: base(name)
		{
			Prototype = proto;
			Attributes = attributes;
			
			_Type = valueType;
			Resolve(valueType);
			
		}
		
		/// <summary>
		/// The type of the index value to use. E.g. the type of 'index' in this[index].
		/// Typically int32.
		/// </summary>
		public Type FirstIndexType{
			get{
				
				// Get the parameters:
				ParameterInfo[] parameters=Property.GetMethod.GetParameters();
				
				// It's the first one after our special 'engine' and 'thisObj' params (if they're set):
				int offset=0;
				
				if(parameters[0].ParameterType==typeof(ScriptEngine)){
					offset++;
				}
				
				if(parameters[offset].Name=="thisObj"){
					offset++;
				}
				
				return parameters[offset].ParameterType;
				
			}
		}
		
		/// <summary>
		/// The prototype this belongs to.
		/// </summary>
		public Prototype Prototype;
		
		/// <summary>
		/// The default value as a method, if it is one. Null otherwise.
		/// </summary>
		public MethodBase Method
		{
			get{
				return ConstantValue as MethodBase;
			}
		}
		
		/// <summary>
		/// The property if it is one.
		/// </summary>
		private VirtualProperty Property;
		
		/// <summary>
		/// The field if it is one.
		/// </summary>
		private FieldInfo Field;
		
		/// <summary>
		/// The type of this property.
		/// </summary>
		private Type _Type;
		
		/// <summary>
		/// The value type of this property.
		/// </summary>
		internal override Type Type
		{
			get{
				return _Type;
			}
			set{
				if(value==_Type){
					return;
				}
				
				if(Property!=null){
					// Can't change a fixed property type.
					return;
				}
				
				if(_Type==null || _Type==typeof(Nitrassic.Undefined)){
					_Type=value;
				}else{
					// The variable has collapsed.
					_Type=typeof(object);
					
					// Which functions use it? Inform them of the collapse.
					RequireRecompile();
					
				}
				
				Resolve(value);
				
			}
		}
		
		/// <summary>True if the accessor variable needs to be emitted too.</summary>
		public bool HasAccessor{
			get{
				if(Property==null && Field==null)
				{
					Resolve(null);
				}
				
				// Note that if neither is set, this is a constant (includes method references).
				return ((Property!=null && Property.RequiresThis) || (Field!=null && !Field.IsStatic));
			}
		}
		
		/// <summary>True if it has an 'engine' parameter. Applies with get/set methods only.</summary>
		public bool HasEngine{
			get{
				if(Property==null && Field==null)
				{
					Resolve(null);
				}
				
				return (Property!=null && Property.RequiresEngine);
			}
		}
		
		/// <summary>Is this a static property?</summary>
		private bool IsStatic{
			get{
				if(Property==null && Field==null)
				{
					Resolve(null);
				}
				
				// Either the property or field is static:
				return ((Property!=null && Property.GetMethod.IsStatic) || (Field!=null && Field.IsStatic));
			}
		}
		
		/// <summary>
		///	Determines if this is a field or method.
		/// </summary>
		private void Resolve(Type type){
			
			if(_Type==null){
				_Type=type;
			}
			
			// Property=null;
			// Field=null;
			
			if(typeof(PropertyInfo).IsAssignableFrom(_Type))
			{
				
				// It's a property!
				PropertyInfo pI=(PropertyInfo)ConstantValue;
				
				Property=new VirtualProperty( pI );
				_Type=pI.PropertyType;
			
			}else if(typeof(VirtualProperty).IsAssignableFrom(_Type))
			{
				
				// It's a property!
				Property=(VirtualProperty)ConstantValue;
				_Type=Property.PropertyType;
				
			}
			else if(typeof(FieldInfo).IsAssignableFrom(_Type))
			{
				
				// It's a field!
				Field=(FieldInfo)ConstantValue;
				_Type=Field.FieldType;
			}
			else if(typeof(MethodBase).IsAssignableFrom(_Type) || _Type==typeof(MethodGroup))
			{
				// It's a method - do nothing here; the prototype will store the reference for us.
			}
			else if(Prototype.Builder!=null && _Type!=null)
			{
				
				FieldAttributes attribs=FieldAttributes.Public;
				
				// Is it static?
				if(Prototype.IsStatic){
					attribs |= FieldAttributes.Static;
				}
				
				// Declare a new field to store it.
				Field=Prototype.Builder.DefineField(Name,_Type,attribs);
				
			}
			
		}
		
		/// <summary>
		/// Sets the value of this property on the given object.
		/// Note that the object must be an instance of 'this' prototype.
		/// </summary>
		public void SetValue(object thisObj,object newValue){
			
			if(Property==null && Field==null)
			{
				Resolve(newValue.GetType());
			}
			
			if(Property!=null)
			{
				
				// Call the set method:
				Property.SetMethod.Invoke(thisObj,new object[]{newValue});
				return;
				
			}
			
			if(Field!=null)
			{
				// Set the field (used by globals only):
				
				if(Field.GetType()!=typeof(FieldInfo)){
					// This occurs when we've got a fieldbuilder.
					// In order to set it, we must re-resolve the field by obtaining it from the built type.
					ReloadField(thisObj.GetType());
				}
				
				Field.SetValue(thisObj,newValue);
				return;
			}
			
		}
		
		/// <summary>
		/// Gets the value of this property from the given object.
		/// Note that the object must be an instance of 'this' prototype.
		/// </summary>
		public object GetValue(object thisObj)
		{
			
			if(Property==null && Field==null)
			{
				Resolve(null);
			}
			
			if(Property!=null)
			{
				
				// Call the get method:
				return Property.GetMethod.Invoke(thisObj,null);
				
			}
			
			if(Field!=null)
			{
				// Load the field (used by globals only):
				
				if(Field.GetType()!=typeof(FieldInfo)){
					// This occurs when we've got a fieldbuilder.
					// In order to set it, we must re-resolve the field by obtaining it from the built type.
					ReloadField(thisObj.GetType());
				}
				
				return Field.GetValue(thisObj);
			}
			
			return null;
			
		}
		
		/// <summary>Reloads the Field from being a fieldBuilder to a full FieldInfo object.</summary>
		private void ReloadField(Type type){
			
			// Grab the fieldInfo by name:
			Field=type.GetField(Field.Name);
			
		}
		
		/// <summary>Called when users of this property must recompile.</summary>
		private void RequireRecompile(){
		
		}
		
		/// <summary>Sets a new method. This triggers call sites to compile as jump tables.</summary>
		public void OverwriteMethod(MethodBase newMethod)
		{
			
			// Is it already a jump table?
			MethodGroup group=ConstantValue as MethodGroup;
			
			if(group==null)
			{
				// Create a group:
				group=new MethodGroup();
				
				// Mark it as a jump table:
				group.JumpTable=true;
				
				// Add the current value:
				group.Add(Method);
				
				// Change the value:
				ConstantValue=group;
			}
			
			// Add the method:
			group.Add(newMethod);
			
			// Which functions use it? Inform them of the collapse.
			RequireRecompile();
			
		}
		
		/// <summary>Force the variable to change type.
		/// Note that this is only ever used during prototype construction.
		/// Any other use will likely have unintended results.</summary>
		public void ForceChange(object value)
		{
			Field=null;
			ConstantValue=value;
			_Type=value.GetType();
		}
		
		internal override Type Get(ILGenerator generator)
		{
			
			if(Property==null && Field==null)
			{
				Resolve(null);
			}
			
			if(Property!=null)
			{
				
				// Call the get method:
				generator.Call(Property.GetMethod);
				return _Type;
				
			}
			
			if(Field!=null)
			{
				// Load the field:
				generator.LoadField(Field);
			}
			else if(IsConstant)
			{
				// Emit the default constant value:
				EmitHelpers.EmitValue(generator,ConstantValue);
			}
			else
			{
				// Emit undefined:
				EmitHelpers.EmitUndefined(generator);
			}
			
			return _Type;
			
		}
		
		internal override void Set(ILGenerator generator, OptimizationInfo optimizationInfo,bool rIU, Type valueType, SetValueMethod value)
		{
			
			// Resolve if needed:
			if(Field==null && Property==null){
				Resolve(valueType);
			}else{
				
				// Update type, potentially forcing the variable to change type
				// (unless it's a fixed property, in which case the value being set changes type instead):
				if(Property==null){
					
					// Fixed property - var changes type:
					Type=valueType;
					
				}
				
			}
			
			ILLocalVariable localVar=null;
			
			// Is the return value of this set in use?
			// E.g. var x=obj.x=14;
			if(rIU){
				
				// It's in use and 'obj.x' is not static.
				
				// Output the value twice:
				value(true);
				
				// If it's not static then we now have [obj][value][value] on the stack.
				// Calling the set method would fail (as it'll operate on the duplicated value).
				// So, we have to pop one off and re-add it after.
				
				if(!IsStatic){
					
					// Note that if it's static, no 'obj' reference goes onto the stack anyway.
					
					// At this point, we have our target object followed by two copies of the value.
					// For the following Call/StoreField to work correctly, we must store the 2nd copy into a local.
					localVar=generator.DeclareVariable(valueType);
					
					// Store into the local:
					generator.StoreVariable(localVar);
					
				}
				
			}else{
				
				// Output the value just once:
				value(false);
				
			}
			
			if(_Type!=valueType){
				
				// Convert if needed:
				EmitConversion.Convert(generator,valueType, _Type,optimizationInfo);
			}
			
			if(Property!=null)
			{
				
				// Call the set method:
				generator.Call(Property.SetMethod);
				
			}
			else
			{
				
				// Load the field:
				generator.StoreField(Field);
				
			}
			
			if(localVar!=null){
				
				// Reload the 2nd copy of the value:
				generator.LoadVariable(localVar);
				
			}
			
		}
		
		/// <summary>
		/// Gets a boolean value indicating whether the property value will be included during an
		/// enumeration.
		/// </summary>
		public bool IsEnumerable
		{
			get { return (this.Attributes & PropertyAttributes.Enumerable) != 0; }
		}

		/// <summary>
		/// Gets a boolean value indicating whether the property can be deleted.
		/// </summary>
		public bool IsConfigurable
		{
			get { return (this.Attributes & PropertyAttributes.Configurable) != 0; }
		}
		
		/// <summary>
		/// Gets a value that indicates whether the value is computed using accessor functions.
		/// </summary>
		public bool IsAccessor
		{
			get { return (this.Attributes & PropertyAttributes.IsAccessorProperty) != 0; }
		}

	}

}
