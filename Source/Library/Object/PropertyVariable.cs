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
			Value = value;
			Prototype = proto;
			Attributes = attributes;
			
			if(value!=null){
				_Type = value.GetType();
				Resolve();
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
				return Value as MethodBase;
			}
		}
		
		/// <summary>
		/// The property if it is one.
		/// </summary>
		private PropertyInfo Property;
		
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
			
				if(_Type==null){
					_Type=value;
				}else{
					// The variable has collapsed.
					_Type=typeof(object);
					
					// Which functions use it? Inform them of the collapse.
					RequireRecompile();
					
				}
				
				Resolve();
				
			}
		}
		
		/// <summary>True if the accessor variable needs to be emitted too.</summary>
		public bool HasAccessor{
			get{
				if(Property==null && Field==null)
				{
					Resolve();
				}
				
				// Note that if neither is set, this is a constant (includes method references).
				return ((Property!=null && !Property.GetGetMethod().IsStatic) || (Field!=null && !Field.IsStatic));
			}
		}
		
		/// <summary>
		///	Determines if this is a field or method.
		/// </summary>
		private void Resolve(){
			
			Property=null;
			Field=null;
			
			if(typeof(PropertyInfo).IsAssignableFrom(_Type))
			{
				
				// It's a property!
				Property=(PropertyInfo)Value;
				_Type=Property.PropertyType;
				
			}
			else if(typeof(FieldInfo).IsAssignableFrom(_Type))
			{
				
				// It's a field!
				Field=(FieldInfo)Value;
				_Type=Field.FieldType;
			}
			else if(typeof(MethodBase).IsAssignableFrom(_Type) || _Type==typeof(MethodGroup))
			{
				// It's a method - do nothing here; the prototype will store the reference for us.
			}
			else if(Prototype.Builder!=null)
			{
				
				// Declare a new field to store it.
				Field=Prototype.Builder.DefineField(Name,_Type,FieldAttributes.Public);
				
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
				Resolve();
			}
			
			if(Property!=null)
			{
				
				// Call the get method:
				return Property.GetGetMethod().Invoke(thisObj,null);
				
			}
			
			if(Field!=null)
			{
				// Load the field:
				return Field.GetValue(thisObj);
			}
			
			return Value;
			
		}
		
		/// <summary>Called when users of this property must recompile.</summary>
		private void RequireRecompile(){
		
		}
		
		/// <summary>Sets a new method. This triggers call sites to compile as jump tables.</summary>
		public void OverwriteMethod(MethodBase newMethod)
		{
			
			// Is it already a jump table?
			MethodGroup group=Value as MethodGroup;
			
			if(group==null)
			{
				// Create a group:
				group=new MethodGroup();
				
				// Mark it as a jump table:
				group.JumpTable=true;
				
				// Add the current value:
				group.Add(Method);
				
				// Change the value:
				Value=group;
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
			Value=value;
			_Type=value.GetType();
		}
		
		/// <summary>
		/// The default value of this property.
		/// </summary>
		public object Value;
		
		internal override Type Get(ILGenerator generator)
		{
			
			if(Property==null && Field==null)
			{
				Resolve();
			}
			
			if(Property!=null)
			{
				
				// Call the get method:
				generator.Call(Property.GetGetMethod());
				
				return _Type;
				
			}
			
			if(Field!=null)
			{
				// Load the field:
				generator.LoadField(Field);
			}
			else
			{
				// Emit the default constant value:
				EmitHelpers.EmitValue(generator,Value);
			}
			
			return _Type;
			
		}
		
		internal override void Set(ILGenerator generator, OptimizationInfo optimizationInfo, Type valueType, SetValueMethod value)
		{
			
			// Update type (which always triggers a resolve):
			Type=valueType;
			
			value();
			
			if(Property!=null)
			{
				
				// Call the get method:
				generator.Call(Property.GetSetMethod());
				
			}
			else
			{
				
				// Load the field:
				generator.StoreField(Field);
				
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

		/// <summary>
		/// Gets a value that indicates whether the property is the magic length property.
		/// </summary>
		public bool IsLength
		{
			get { return (this.Attributes & PropertyAttributes.IsLengthProperty) != 0; }
		}
	}

}
