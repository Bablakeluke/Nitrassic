using System;
using System.Collections.Generic;
using System.Reflection;
using Nitrassic.Compiler;

namespace Nitrassic.Library
{
	/// <summary>
	/// Provides functionality common to all objects.
	/// </summary>
	[JSProperties(Name="Object")]
	public partial class ObjectInstance
	{
		
		[JSProperties(Hidden=true)]
		public static readonly Type PrototypeFor=typeof(object);
		
		// Host script engine.
		private ScriptEngine engine;
		
		// Internal prototype chain.
		private ObjectInstance prototype;
		
		// Stores the property values for this object.
		public object[] propertyValues = new object[4];
		
		/// <summary>
		/// Gets or sets a value that indicates whether the object can have new properties added
		/// to it.
		/// </summary>
		internal bool IsExtensible=true;

		//	 INITIALIZATION
		//_________________________________________________________________________________________
		
		/// <summary>
		/// Use this if you want to create an object which is entirely dynamic in the JS zone.
		/// </summary>
		internal ObjectInstance(ScriptEngine engine)
		{
			this.engine = engine;
		}
		
		internal ObjectInstance(){}
		
		public void SetPrototype(ObjectInstance prototype){
			this.prototype = prototype;
			this.engine = prototype.Engine;
		}
		
		/// <summary>
		/// Called by derived classes to create a new object instance.
		/// </summary>
		/// <param name="engine"> The script engine associated with this object. </param>
		/// <param name="prototype"> The next object in the prototype chain.  Can be <c>null</c>. </param>
		protected ObjectInstance(ScriptEngine engine, ObjectInstance prototype)
		{
			if (engine == null)
				throw new ArgumentNullException("engine");
			this.engine = engine;
			this.prototype = prototype;
		}
		
		/// <summary>
		/// Creates an Object with no prototype to serve as the base prototype of all objects.
		/// </summary>
		/// <param name="engine"> The script engine associated with this object. </param>
		/// <returns> An Object with no prototype. </returns>
		internal static ObjectInstance CreateRootObject(ScriptEngine engine)
		{
			return new ObjectInstance(engine, null);
		}
		
		/// <summary>
		/// Creates an Object instance (use ObjectInstance.OnConstruct rather than this).
		/// </summary>
		/// <param name="prototype"> The next object in the prototype chain. </param>
		/// <returns> An Object instance. </returns>
		internal static ObjectInstance CreateRawObject(ObjectInstance prototype)
		{
			return new ObjectInstance(prototype.Engine,prototype);
		}
		
		/// <summary>
		/// The constructor for this prototype.
		/// </summary>
		internal MethodBase Constructor{
			get{
				return this["constructor"] as MethodBase;
			}
		}
		
		//	 .NET ACCESSOR PROPERTIES
		//_________________________________________________________________________________________

		/// <summary>
		/// Gets a reference to the script engine associated with this object.
		/// </summary>
		[JSProperties(Hidden=true)]
		public ScriptEngine Engine
		{
			get { return engine; }
		}
		
		/// <summary>
		/// The prototype being used by this object.
		/// </summary>
		public ObjectInstance __proto__{
			get{
				return this.prototype;
			}
		}
		
		/// <summary>
		/// Gets the next object in the prototype chain.  There is no corresponding property in
		/// javascript (it is is *not* the same as the prototype property), instead use
		/// Object.getPrototypeOf().
		/// </summary>
		[JSProperties(Hidden=true)]
		public ObjectInstance Prototype
		{
			get { return this.prototype; }
		}
		
		/// <summary>
		/// Gets or sets the value of a named property.
		/// </summary>
		/// <param name="propertyName"> The name of the property to get or set. </param>
		/// <returns> The property value, or <c>null</c> if the property doesn't exist. </returns>
		[JSProperties(Hidden=true)]
		public object this[string propertyName]
		{
			get { return GetPropertyValue(propertyName); }
			set { SetPropertyValue(propertyName, value, false); }
		}

		/// <summary>
		/// Gets or sets the value of an array-indexed property.
		/// </summary>
		/// <param name="index"> The index of the property to retrieve. </param>
		/// <returns> The property value, or <c>null</c> if the property doesn't exist. </returns>
		[JSProperties(Hidden=true)]
		public object this[uint index]
		{
			get { return GetPropertyValue(index); }
			set { SetPropertyValue(index, value, false); }
		}

		/// <summary>
		/// Gets or sets the value of an array-indexed property.
		/// </summary>
		/// <param name="index"> The index of the property to retrieve. </param>
		/// <returns> The property value, or <c>null</c> if the property doesn't exist. </returns>
		[JSProperties(Hidden=true)]
		public object this[int index]
		{
			get
			{
				if (index < 0)
					throw new ArgumentOutOfRangeException("index");
				return GetPropertyValue((uint)index);
			}
			set
			{
				if (index < 0)
					throw new ArgumentOutOfRangeException("index");
				SetPropertyValue((uint)index, value, false);
			}
		}
		
		/// <summary>
		/// Gets an enumerable list of every property name and value associated with this object.
		/// Does not include properties in the prototype chain.
		/// </summary>
		[JSProperties(Hidden=true)]
		public virtual IEnumerable<PropertyNameAndValue> Properties
		{
			get
			{
				// Enumerate named properties.
				foreach (var pair in this.properties){
					yield return new PropertyNameAndValue(pair.Key, InlinePropertyValues[pair.Value.Index], pair.Value.Attributes);
				}
				
			}
		}



		//	 INLINE CACHING
		//_________________________________________________________________________________________

		/// <summary>
		/// Gets a number to use as a cache key.  Adding, deleting, or modifying properties will
		/// cause the value of this property to change.
		/// </summary>
		[JSProperties(Hidden=true)]
		public int InlineCacheKey;

		/// <summary>
		/// Gets the values stored against this object, one for each property.  The index to use
		/// can be retrieved from <see cref="InlineGetPropertyValue"/>,
		/// <see cref="InlineSetPropertyValue"/> or <see cref="InlineSetPropertyValueIfExists"/>.
		/// </summary>
		[JSProperties(Hidden=true)]
		public object[] InlinePropertyValues
		{
			get { return this.propertyValues; }
		}
		
		/// <summary>
		/// Gets the value of the given property plus the information needed to speed up access to
		/// the property in future.
		/// </summary>
		/// <param name="name"> The name of the property. </param>
		/// <param name="cachedIndex"> Set to a zero-based index that can be used to get or set the
		/// property value in future (provided the cache key doesn't change). </param>
		/// <param name="cacheKey"> Set to a value that can be compared with the CacheKey property
		/// to determine if the cached index needs to be refreshed.  Can be set to <c>null</c> to
		/// prohibit caching. </param>
		/// <returns> The value of the property, or <c>null</c> if the property doesn't exist. </returns>
		[JSProperties(Hidden=true)]
		public object InlineGetPropertyValue(string name, out int cachedIndex, out int cacheKey)
		{
			var propertyInfo = GetPropertyIndexAndAttributes(name);
			if (propertyInfo.Exists == true)
			{
				// The property exists; it can be cached as long as it is not an accessor property.
				if ((propertyInfo.Attributes & (PropertyAttributes.IsAccessorProperty)) != 0)
				{
					// Getters and the length property cannot be cached.
					cachedIndex = -1;
					cacheKey = -1;

					// Call the getter if there is one.
					if (propertyInfo.IsAccessor == true)
						return ((PropertyAccessorValue)this.propertyValues[propertyInfo.Index]).GetValue(this);
					
				}

				// The property can be cached.
				cachedIndex = propertyInfo.Index;
				cacheKey = InlineCacheKey;
				return this.propertyValues[cachedIndex];
			}
			else
			{
				// The property is in the prototype or is non-existent.
				cachedIndex = -1;
				cacheKey = -1;
				if (this.Prototype == null)
					return GetMissingPropertyValue(name);
				return this.Prototype.GetNamedPropertyValue(name, this);
			}
		}

		/// <summary>
		/// Sets the value of the given property plus retrieves the information needed to speed up
		/// access to the property in future.  If a property with the given name does not exist, or
		/// exists in the prototype chain (and is not a setter) then a new property is created.
		/// </summary>
		/// <param name="name"> The name of the property to set. </param>
		/// <param name="value"> The desired value of the property. </param>
		/// <param name="throwOnError"> <c>true</c> to throw an exception if the property could not
		/// be set (i.e. if the property is read-only or if the object is not extensible and a new
		/// property needs to be created). </param>
		/// <param name="cachedIndex"> Set to a zero-based index that can be used to get or set the
		/// property value in future (provided the cache key doesn't change). </param>
		/// <param name="cacheKey"> Set to a value that can be compared with the CacheKey property
		/// to determine if the cached index needs to be refreshed.  Can be set to <c>null</c> to
		/// prohibit caching. </param>
		[JSProperties(Hidden=true)]
		public void InlineSetPropertyValue(string name, object value, bool throwOnError, out int cachedIndex, out int cacheKey)
		{
			var propertyInfo = GetPropertyIndexAndAttributes(name);
			if (propertyInfo.Exists == true)
			{
				// The property exists; it can be cached as long as it is not read-only or an accessor property.
				if ((propertyInfo.Attributes & (PropertyAttributes.Writable | PropertyAttributes.IsAccessorProperty)) != PropertyAttributes.Writable)
				{
					cachedIndex = -1;
					cacheKey = -1;
					SetPropertyValue(name, value, throwOnError);
				}
				else
				{
					// The property can be cached.
					cachedIndex = propertyInfo.Index;
					cacheKey = this.InlineCacheKey;
					this.InlinePropertyValues[cachedIndex] = value ?? Undefined.Value;
				}
			}
			else
			{
				// The property is in the prototype or is non-existent.
				cachedIndex = -1;
				cacheKey = -1;
				SetPropertyValue(name, value, throwOnError);
			}
		}

		/// <summary>
		/// Sets the value of the given property plus retrieves the information needed to speed up
		/// access to the property in future.  If a property with the given name exists, but only
		/// in the prototype chain, then a new property is created (unless the property is a
		/// setter, in which case the setter is called and no property is created).  If the
		/// property does not exist at all, then no property is created and the method returns
		/// <c>false</c>.
		/// </summary>
		/// <param name="name"> The name of the property to set. </param>
		/// <param name="value"> The desired value of the property.  This must be a javascript
		/// primitive (double, string, etc) or a class derived from <see cref="ObjectInstance"/>. </param>
		/// <param name="throwOnError"> <c>true</c> to throw an exception if the property could not
		/// be set (i.e. if the property is read-only or if the object is not extensible and a new
		/// property needs to be created). </param>
		/// <param name="cachedIndex"> Set to a zero-based index that can be used to get or set the
		/// property value in future (provided the cache key doesn't change). </param>
		/// <param name="cacheKey"> Set to a value that can be compared with the CacheKey property
		/// to determine if the cached index needs to be refreshed.  Can be set to <c>null</c> to
		/// prohibit caching. </param>
		/// <returns> <c>true</c> if the property value exists; <c>false</c> otherwise. </returns>
		[JSProperties(Hidden=true)]
		public bool InlineSetPropertyValueIfExists(string name, object value, bool throwOnError, out int cachedIndex, out int cacheKey)
		{
			var propertyInfo = GetPropertyIndexAndAttributes(name);
			if (propertyInfo.Exists == true)
			{
				// The property exists; it can be cached as long as it is not read-only or an accessor property.
				if ((propertyInfo.Attributes & (PropertyAttributes.Writable | PropertyAttributes.IsAccessorProperty)) != PropertyAttributes.Writable)
				{
					cachedIndex = -1;
					cacheKey = -1;
					return SetPropertyValueIfExists(name, value, throwOnError);
				}
				else
				{
					// The property can be cached.
					cachedIndex = propertyInfo.Index;
					cacheKey = this.InlineCacheKey;
					this.InlinePropertyValues[cachedIndex] = value ?? Undefined.Value;
					return true;
				}

			}
			else
			{
				// The property is in the prototype or is non-existent.
				cachedIndex = -1;
				cacheKey = -1;
				return SetPropertyValueIfExists(name, value, throwOnError);
			}
		}



		//	 PROPERTY MANAGEMENT
		//_________________________________________________________________________________________

		///// <summary>
		///// Gets the property descriptor for the property with the given name.  The prototype
		///// chain is not searched.
		///// </summary>
		///// <param name="propertyName"> The name of the property. </param>
		///// <returns> A property descriptor containing the property value and attributes.  The
		///// result will be <c>PropertyDescriptor.Undefined</c> if the property doesn't exist. </returns>
		//internal virtual PropertyDescriptor GetOwnProperty(string propertyName)
		//{
		//	PropertyAttributes attributes;
		//	int index = GetPropertyIndexAndAttributes(propertyName, out attributes);
		//	if (index == -1)
		//		return PropertyDescriptor.Undefined;
		//	return new PropertyDescriptor(this.propertyValues[index], attributes);
		//}

		///// <summary>
		///// Gets the property descriptor for the property with the given array index.  The
		///// prototype chain is not searched.
		///// </summary>
		///// <param name="index"> The array index of the property. </param>
		///// <returns> A property descriptor containing the property value and attributes.  The
		///// result will be <c>PropertyDescriptor.Undefined</c> if the property doesn't exist. </returns>
		//internal virtual PropertyDescriptor GetOwnProperty(uint index)
		//{
		//	return GetOwnProperty(index.ToString());
		//}

		///// <summary>
		///// Gets the property descriptor for the property with the given name.
		///// </summary>
		///// <param name="propertyName"> The name of the property. </param>
		///// <returns> A property descriptor containing the property value and attributes.  The
		///// value will be <c>PropertyDescriptor.Undefined</c> if the property doesn't exist. </returns>
		//internal PropertyDescriptor GetProperty(string propertyName)
		//{

		//}

		/// <summary>
		/// Determines if a property with the given name exists.
		/// </summary>
		/// <param name="propertyName"> The name of the property to check. </param>
		/// <returns> <c>true</c> if the property exists on this object or in the prototype chain;
		/// <c>false</c> otherwise. </returns>
		[JSProperties(Hidden=true)]
		public bool HasProperty(string propertyName)
		{
			return this.GetPropertyValue(propertyName) != null;
		}

		/// <summary>
		/// Gets the value of the property with the given array index.
		/// </summary>
		/// <param name="index"> The array index of the property. </param>
		/// <returns> The value of the property, or <c>null</c> if the property doesn't exist. </returns>
		/// <remarks> The prototype chain is searched if the property does not exist directly on
		/// this object. </remarks>
		[JSProperties(Hidden=true)]
		public object GetPropertyValue(uint index)
		{
			return GetPropertyValue(index, this);
		}

		/// <summary>
		/// Gets the value of the property with the given array index.
		/// </summary>
		/// <param name="index"> The array index of the property. </param>
		/// <param name="thisValue"> The value of the "this" keyword inside a getter. </param>
		/// <returns> The value of the property, or <c>null</c> if the property doesn't exist. </returns>
		/// <remarks> The prototype chain is searched if the property does not exist directly on
		/// this object. </remarks>
		private object GetPropertyValue(uint index, ObjectInstance thisValue)
		{
			// Get the descriptor for the property.
			var property = this.GetOwnPropertyDescriptor(index);
			if (property.Exists == true)
			{
				// The property was found!  Call the getter if there is one.
				object value = property.Value;
				var accessor = value as PropertyAccessorValue;
				if (accessor != null)
					return accessor.GetValue(thisValue);
				return value;
			}

			// The property might exist in the prototype.
			if (this.prototype == null)
				return thisValue.GetMissingPropertyValue(index.ToString());
			return this.prototype.GetPropertyValue(index, thisValue);
		}

		/// <summary>
		/// Gets the value of the property with the given name.
		/// </summary>
		/// <param name="propertyName"> The name of the property. </param>
		/// <returns> The value of the property, or <c>null</c> if the property doesn't exist. </returns>
		/// <remarks> The prototype chain is searched if the property does not exist directly on
		/// this object. </remarks>
		[JSProperties(Hidden=true)]
		public object GetPropertyValue(string propertyName)
		{
			// Check if the property is an indexed property.
			uint arrayIndex = Nitrassic.Library.Array.ParseArrayIndex(propertyName);
			if (arrayIndex != uint.MaxValue)
				return GetPropertyValue(arrayIndex);

			// Otherwise, the property is a name.
			return GetNamedPropertyValue(propertyName, this);
		}
		
		// Properties
		private Dictionary<string, SchemaProperty> properties;
		
		/// <summary>
		/// Gets the index into the Values array of the next added property.
		/// </summary>
		[JSProperties(Hidden=true)]
		public int NextValueIndex;
		
		/// <summary>
		/// Gets the number of properties defined in this object.
		/// </summary>
		[JSProperties(Hidden=true)]
		public int PropertyCount{
			get{
				if (properties == null){
					return 0;
				}
				return properties.Count;
			}
		}
		
		/// <summary>
		/// Gets the zero-based index of the property with the given name.
		/// </summary>
		/// <param name="name"> The name of the property. </param>
		/// <returns> The zero-based index of the property, or <c>-1</c> if a property with the
		/// given name does not exist. </returns>
		[JSProperties(Hidden=true)]
		public int GetPropertyIndex(string name)
		{
			return GetPropertyIndexAndAttributes(name).Index;
		}

		/// <summary>
		/// Gets the zero-based index of the property with the given name and the attributes
		/// associated with the property.
		/// </summary>
		/// <param name="name"> The name of the property. </param>
		/// <returns> A structure containing the zero-based index of the property, or <c>-1</c> if a property with the
		/// given name does not exist. </returns>
		[JSProperties(Hidden=true)]
		internal SchemaProperty GetPropertyIndexAndAttributes(string name)
		{
			if(properties==null)
				return SchemaProperty.Undefined;
			
			SchemaProperty propertyInfo;
			if (properties.TryGetValue(name, out propertyInfo))
				return propertyInfo;
			
			return SchemaProperty.Undefined;
		}
		
		/// <summary>
		/// Gets the value of the property with the given name.  The name cannot be an array index.
		/// </summary>
		/// <param name="propertyName"> The name of the property.  The name cannot be an array index. </param>
		/// <param name="thisValue"> The value of the "this" keyword inside a getter. </param>
		/// <returns> The value of the property, or <c>null</c> if the property doesn't exist. </returns>
		/// <remarks> The prototype chain is searched if the property does not exist directly on
		/// this object. </remarks>
		private object GetNamedPropertyValue(string propertyName, ObjectInstance thisValue)
		{
			ObjectInstance prototypeObject = this;
			do
			{
				// Retrieve information about the property.
				var property = prototypeObject.GetPropertyIndexAndAttributes(propertyName);
				if (property.Exists == true)
				{
					// The property was found!
					object value = prototypeObject.propertyValues[property.Index];
					if ((property.Attributes & (PropertyAttributes.IsAccessorProperty)) == 0)
						return value;

					// Call the getter if there is one.
					if (property.IsAccessor == true)
						return ((PropertyAccessorValue)value).GetValue(thisValue);
					
				}

				// Traverse the prototype chain.
				prototypeObject = prototypeObject.prototype;
			} while (prototypeObject != null);

			// The property doesn't exist.
			return thisValue.GetMissingPropertyValue(propertyName);
		}

		/// <summary>
		/// Retrieves the value of a property which doesn't exist on the object.  This method can
		/// be overridden to effectively construct properties on the fly.  The default behavior is
		/// to return <c>undefined</c>.
		/// </summary>
		/// <param name="propertyName"> The name of the missing property. </param>
		/// <returns> The value of the missing property. </returns>
		/// <remarks> When overriding, call the base class implementation only if you want to
		/// revert to the default behavior. </remarks>
		protected virtual object GetMissingPropertyValue(string propertyName)
		{
			if (this.prototype == null)
				return null;
			return this.prototype.GetMissingPropertyValue(propertyName);
		}

		/// <summary>
		/// Gets a descriptor for the property with the given array index.
		/// </summary>
		/// <param name="index"> The array index of the property. </param>
		/// <returns> A property descriptor containing the property value and attributes. </returns>
		/// <remarks> The prototype chain is not searched. </remarks>
		[JSProperties(Hidden=true)]
		public virtual PropertyDescriptor GetOwnPropertyDescriptor(uint index)
		{
			var property = GetPropertyIndexAndAttributes(index.ToString());
			if (property.Exists == true)
				return new PropertyDescriptor(this.propertyValues[property.Index], property.Attributes);
			return PropertyDescriptor.Undefined;
		}

		/// <summary>
		/// Gets a descriptor for the property with the given name.
		/// </summary>
		/// <param name="propertyName"> The name of the property. </param>
		/// <returns> A property descriptor containing the property value and attributes. </returns>
		/// <remarks> The prototype chain is not searched. </remarks>
		[JSProperties(Hidden=true)]
		public PropertyDescriptor GetOwnPropertyDescriptor(string propertyName)
		{
			// Check if the property is an indexed property.
			uint arrayIndex = Nitrassic.Library.Array.ParseArrayIndex(propertyName);
			if (arrayIndex != uint.MaxValue)
				return GetOwnPropertyDescriptor(arrayIndex);

			// Retrieve information about the property.
			var property = GetPropertyIndexAndAttributes(propertyName);
			if (property.Exists == true)
			{
				return new PropertyDescriptor(this.propertyValues[property.Index], property.Attributes);
			}

			// The property doesn't exist.
			return PropertyDescriptor.Undefined;
		}

		/// <summary>
		/// Sets the value of the property with the given array index.  If a property with the
		/// given index does not exist, or exists in the prototype chain (and is not a setter) then
		/// a new property is created.
		/// </summary>
		/// <param name="index"> The array index of the property to set. </param>
		/// <param name="value"> The value to set the property to.  This must be a javascript
		/// primitive (double, string, etc) or a class derived from <see cref="ObjectInstance"/>. </param>
		/// <param name="throwOnError"> <c>true</c> to throw an exception if the property could not
		/// be set.  This can happen if the property is read-only or if the object is sealed. </param>
		[JSProperties(Hidden=true)]
		public virtual void SetPropertyValue(uint index, object value, bool throwOnError)
		{
			string indexStr = index.ToString();
			bool exists = SetPropertyValueIfExists(indexStr, value, throwOnError);
			if (exists == false)
			{
				// The property doesn't exist - add it.
				AddProperty(indexStr, value, PropertyAttributes.FullAccess, throwOnError);
			}
		}

		/// <summary>
		/// Sets the value of the property with the given name.  If a property with the given name
		/// does not exist, or exists in the prototype chain (and is not a setter) then a new
		/// property is created.
		/// </summary>
		/// <param name="propertyName"> The name of the property to set. </param>
		/// <param name="value"> The value to set the property to.  This must be a javascript
		/// primitive (double, string, etc) or a class derived from <see cref="ObjectInstance"/>. </param>
		/// <param name="throwOnError"> <c>true</c> to throw an exception if the property could not
		/// be set (i.e. if the property is read-only or if the object is not extensible and a new
		/// property needs to be created). </param>
		[JSProperties(Hidden=true)]
		public void SetPropertyValue(string propertyName, object value, bool throwOnError)
		{
			// Check if the property is an indexed property.
			uint arrayIndex = Nitrassic.Library.Array.ParseArrayIndex(propertyName);
			if (arrayIndex != uint.MaxValue)
			{
				SetPropertyValue(arrayIndex, value, throwOnError);
				return;
			}

			bool exists = SetPropertyValueIfExists(propertyName, value, throwOnError);
			if (exists == false)
			{
				// The property doesn't exist - add it.
				AddProperty(propertyName, value, PropertyAttributes.FullAccess, throwOnError);
			}
		}

		/// <summary>
		/// Sets the value of the given property.  If a property with the given name exists, but
		/// only in the prototype chain, then a new property is created (unless the property is a
		/// setter, in which case the setter is called and no property is created).  If the
		/// property does not exist at all, then no property is created and the method returns
		/// <c>false</c>.  This method is used to set the value of a variable reference within a
		/// with statement.
		/// </summary>
		/// <param name="propertyName"> The name of the property to set. Cannot be an array index. </param>
		/// <param name="value"> The desired value of the property. This must be a javascript
		/// primitive (double, string, etc) or a class derived from <see cref="ObjectInstance"/>. </param>
		/// <param name="throwOnError"> <c>true</c> to throw an exception if the property could not
		/// be set (i.e. if the property is read-only or if the object is not extensible and a new
		/// property needs to be created). </param>
		/// <returns> <c>true</c> if the property value exists; <c>false</c> otherwise. </returns>
		[JSProperties(Hidden=true)]
		public bool SetPropertyValueIfExists(string propertyName, object value, bool throwOnError)
		{
			// Do not store nulls - null represents a non-existant value.
			value = value ?? Undefined.Value;

			// Retrieve information about the property.
			var property = GetPropertyIndexAndAttributes(propertyName);
			if (property.Exists == true)
			{
				// Check if the property is read-only.
				if (property.IsWritable == false)
				{
					// The property is read-only.
					if (throwOnError == true)
						throw new JavaScriptException(this.Engine, "TypeError", string.Format("The property '{0}' is read-only.", propertyName));
					return true;
				}

				if ((property.Attributes & (PropertyAttributes.IsAccessorProperty)) == 0)
				{
					// The property contains a simple value.  Set the property value.
					this.propertyValues[property.Index] = value;
				}
				else if (property.IsAccessor == true)
				{
					// The property contains an accessor function.  Set the property value by calling the accessor.
					((PropertyAccessorValue)this.propertyValues[property.Index]).SetValue(this, value);
				}
				
				return true;
			}

			// Search the prototype chain for a accessor function.  If one is found, it will
			// prevent the creation of a new property.
			bool propertyExistsInPrototype = false;
			ObjectInstance prototypeObject = this.prototype;
			while (prototypeObject != null)
			{
				property = prototypeObject.GetPropertyIndexAndAttributes(propertyName);
				if (property.Exists == true)
				{
					if (property.IsAccessor == true)
					{
						// The property contains an accessor function.  Set the property value by calling the accessor.
						((PropertyAccessorValue)prototypeObject.propertyValues[property.Index]).SetValue(this, value);
						return true;
					}
					propertyExistsInPrototype = true;
					break;
				}
				prototypeObject = prototypeObject.prototype;
			}

			// If the property exists in the prototype, create a new property.
			if (propertyExistsInPrototype == true)
			{
				AddProperty(propertyName, value, PropertyAttributes.FullAccess, throwOnError);
				return true;
			}

			// The property does not exist.
			return false;
		}
		
		/// <summary>
		/// Deletes the property with the given array index.
		/// </summary>
		/// <param name="index"> The array index of the property to delete. </param>
		/// <param name="throwOnError"> <c>true</c> to throw an exception if the property could not
		/// be set because the property was marked as non-configurable.  </param>
		/// <returns> <c>true</c> if the property was successfully deleted, or if the property did
		/// not exist; <c>false</c> if the property was marked as non-configurable and
		/// <paramref name="throwOnError"/> was <c>false</c>. </returns>
		[JSProperties(Hidden=true)]
		public virtual bool Delete(uint index, bool throwOnError)
		{
			string indexStr = index.ToString();

			// Retrieve the attributes for the property.
			var propertyInfo = GetPropertyIndexAndAttributes(indexStr);
			if (propertyInfo.Exists == false)
				return true;	// Property doesn't exist - delete succeeded!

			// Delete the property.
			if (properties==null || !properties.Remove(indexStr)){
				throw new InvalidOperationException("The property '"+indexStr+"' does not exist.");
			}
			
			InlineCacheKey++;
			return true;
		}
		
		/// <summary>
		/// Deletes the property with the given name.
		/// </summary>
		/// <param name="propertyName"> The name of the property to delete. </param>
		/// <param name="throwOnError"> <c>true</c> to throw an exception if the property could not
		/// be set because the property was marked as non-configurable.  </param>
		/// <returns> <c>true</c> if the property was successfully deleted, or if the property did
		/// not exist; <c>false</c> if the property was marked as non-configurable and
		/// <paramref name="throwOnError"/> was <c>false</c>. </returns>
		[JSProperties(Hidden=true)]
		public bool Delete(string propertyName, bool throwOnError)
		{
			// Check if the property is an indexed property.
			uint arrayIndex = Nitrassic.Library.Array.ParseArrayIndex(propertyName);
			if (arrayIndex != uint.MaxValue)
				return Delete(arrayIndex, throwOnError);

			// Retrieve the attributes for the property.
			var propertyInfo = GetPropertyIndexAndAttributes(propertyName);
			if (propertyInfo.Exists == false)
				return true;	// Property doesn't exist - delete succeeded!

			// Check if the property can be deleted.
			if (propertyInfo.IsConfigurable == false)
			{
				if (throwOnError == true)
					throw new JavaScriptException(this.Engine, "TypeError", string.Format("The property '{0}' cannot be deleted.", propertyName));
				return false;
			}

			// Delete the property.
			if (properties==null || !properties.Remove(propertyName)){
				throw new InvalidOperationException("The property '"+propertyName+"' does not exist.");
			}
			
			InlineCacheKey++;
			return true;
		}

		/// <summary>
		/// Defines or redefines the value and attributes of a property.  The prototype chain is
		/// not searched so if the property exists but only in the prototype chain a new property
		/// will be created.
		/// </summary>
		/// <param name="propertyName"> The name of the property to modify. </param>
		/// <param name="descriptor"> The property value and attributes. </param>
		/// <param name="throwOnError"> <c>true</c> to throw an exception if the property could not
		/// be set.  This can happen if the property is not configurable or the object is sealed. </param>
		/// <returns> <c>true</c> if the property was successfully modified; <c>false</c> otherwise. </returns>
		[JSProperties(Hidden=true)]
		public virtual SchemaProperty DefineProperty(string propertyName, PropertyDescriptor descriptor, bool throwOnError)
		{
			// Retrieve info on the property.
			var current = GetPropertyIndexAndAttributes(propertyName);

			if (current.Exists == false)
			{
				// Create a new property.
				return AddProperty(propertyName, descriptor.Value, descriptor.Attributes, throwOnError);
			}

			// If the current property is not configurable, then the only change that is allowed is
			// a change from one simple value to another (i.e. accessors are not allowed) and only
			// if the writable attribute is set.
			if (current.IsConfigurable == false)
			{
				// Get the current value of the property.
				object currentValue = this.propertyValues[current.Index];
				object getter = null, setter = null;
				if (currentValue is PropertyAccessorValue)
				{
					getter = ((PropertyAccessorValue)currentValue).Getter;
					setter = ((PropertyAccessorValue)currentValue).Setter;
				}

				// Check if the modification is allowed.
				if (descriptor.Attributes != current.Attributes ||
					(descriptor.IsAccessor == true && (getter != descriptor.Getter || setter != descriptor.Setter)) ||
					(descriptor.IsAccessor == false && current.IsWritable == false && TypeComparer.SameValue(currentValue, descriptor.Value) == false))
				{
					if (throwOnError == true)
						throw new JavaScriptException(this.Engine, "TypeError", string.Format("The property '{0}' is non-configurable.", propertyName));
					return SchemaProperty.Undefined;
				}
			}

			// Set the property value and attributes (doesn't change InlineCacheKey).
			current.Attributes = descriptor.Attributes;
			this.propertyValues[current.Index] = descriptor.Value;
			
			return current;
		}

		/// <summary>
		/// Adds a property to this object.  The property must not already exist.
		/// </summary>
		/// <param name="propertyName"> The name of the property to add. </param>
		/// <param name="value"> The desired value of the property.  This can be a
		/// <see cref="PropertyAccessorValue"/>. </param>
		/// <param name="attributes"> Attributes describing how the property may be modified. </param>
		/// <param name="throwOnError"> <c>true</c> to throw an exception if the property could not
		/// be added (i.e. if the object is not extensible). </param>
		/// <returns> <c>true</c> if the property was successfully added; <c>false</c> otherwise. </returns>
		[JSProperties(Hidden=true)]
		public SchemaProperty AddProperty(string propertyName, object value, PropertyAttributes attributes, bool throwOnError)
		{
			// Make sure adding a property is allowed.
			if (this.IsExtensible == false)
			{
				if (throwOnError == true)
					throw new JavaScriptException(Engine, "TypeError", string.Format("The property '{0}' cannot be created as the object is not extensible.", propertyName));
				return SchemaProperty.Undefined;
			}

			// To avoid running out of memory, restrict the number of properties.
			if (PropertyCount == 16384)
				throw new JavaScriptException(Engine, "Error", "Maximum number of named properties reached.");

			// Do not store nulls - null represents a non-existant value.
			value = value ?? Undefined.Value;

			// Add a new property to this object.
			SchemaProperty result=AddSchemaProperty(propertyName, attributes);
			InlineCacheKey++;
			
			// Check if the value array needs to be resized.
			int propertyIndex = NextValueIndex - 1;
			if (propertyIndex >= this.InlinePropertyValues.Length){
				System.Array.Resize(ref this.propertyValues, this.InlinePropertyValues.Length * 2);
			}
			
			// Set the value of the property.
			this.propertyValues[propertyIndex] = value;

			// Success.
			return result;
		}
		
		[JSProperties(Hidden=true)]
		public void FastSetProperty(string propertyName, object value, PropertyAttributes attributes)
		{
			FastSetProperty(propertyName,value,attributes,false);
		}
		
		[JSProperties(Hidden=true)]
		public void FastSetProperty(string propertyName, object value)
		{
			FastSetProperty(propertyName,value,PropertyAttributes.Sealed,false);
		}
		
		[JSProperties(Hidden=true)]
		internal SchemaProperty AddSchemaProperty(string name)
		{
			return AddSchemaProperty(name,PropertyAttributes.FullAccess);
		}
		
		/// <summary>
		/// Adds a property to this object.
		/// </summary>
		/// <param name="name"> The name of the property to add. </param>
		/// <param name="attributes"> The property attributes. </param>
		[JSProperties(Hidden=true)]
		internal SchemaProperty AddSchemaProperty(string name, PropertyAttributes attributes)
		{
			
			if(properties==null){
				properties = new Dictionary<string, SchemaProperty>();
			}
			
			SchemaProperty sp=new SchemaProperty(NextValueIndex, attributes);
			properties[name]=sp;
			NextValueIndex++;
			
			return sp;
		}
		
		/// <summary>
		/// Sets a property value and attributes, or adds a new property if it doesn't already
		/// exist.  Any existing attributes are ignored (and not modified).
		/// </summary>
		/// <param name="propertyName"> The name of the property. </param>
		/// <param name="value"> The intended value of the property. </param>
		/// <param name="attributes"> Attributes that indicate whether the property is writable,
		/// configurable and enumerable. </param>
		/// <param name="overwriteAttributes"> Indicates whether to overwrite any existing attributes. </param>
		[JSProperties(Hidden=true)]
		public void FastSetProperty(string propertyName, object value, PropertyAttributes attributes, bool overwriteAttributes)
		{
			SchemaProperty current = GetPropertyIndexAndAttributes(propertyName);
			if (current.Index < 0)
			{
				// The property doesn't exist - add a new property.
				AddProperty(propertyName, value, attributes, false);
				return;
			}
			if (overwriteAttributes){
				// Doesn't change InlineCacheKey
				current.Attributes=attributes;
			}
			
			this.propertyValues[current.Index] = value;
		}
		
		private int Count(IEnumerable<PropertyNameAndValue> properties){
			
			int c=0;
			foreach (var property in properties)
			 c++;
			
			return c;
			
		}
		/// <summary>
		/// Sets up multiple properties at once.
		/// </summary>
		/// <param name="propSet"> The list of properties to set. </param>
		[JSProperties(Hidden=true)]
		public void FastSetProperties(IEnumerable<PropertyNameAndValue> propSet)
		{
			if (NextValueIndex != 0)
				throw new InvalidOperationException("This method can only be called on a virgin object (one with no properties).");

			int c=Count(propSet);
			
			if (this.propertyValues.Length < c){
				this.propertyValues = new object[c];
			}
			
			properties = new Dictionary<string, SchemaProperty>(c);
			int nextValueIndex = 0;
			
			foreach (var property in propSet)
			{
				this.propertyValues[nextValueIndex] = property.Value;
				properties.Add(property.Name, new SchemaProperty(nextValueIndex++, property.Attributes));
			}
			
			NextValueIndex=nextValueIndex;
		}


		//	 OTHERS
		//_________________________________________________________________________________________

		/// <summary>
		/// Returns a primitive value that represents the current object.  Used by the addition and
		/// equality operators.
		/// </summary>
		/// <param name="typeHint"> Indicates the preferred type of the result. </param>
		/// <returns> A primitive value that represents the current object. </returns>
		internal virtual object GetPrimitiveValue(PrimitiveTypeHint typeHint)
		{
			if (typeHint == PrimitiveTypeHint.None || typeHint == PrimitiveTypeHint.Number)
			{
				
				// Try calling valueOf().
				object valueOfResult;
				if (TryCallMemberFunction(out valueOfResult, "valueOf") == true)
				{
					// Return value must be primitive.
					if (valueOfResult is double || TypeUtilities.IsPrimitive(valueOfResult) == true)
						return valueOfResult;
				}

				// Try calling toString().
				object toStringResult;
				if (TryCallMemberFunction(out toStringResult, "toString") == true)
				{
					// Return value must be primitive.
					if (toStringResult is string || TypeUtilities.IsPrimitive(toStringResult) == true)
						return toStringResult;
				}

			}
			else
			{

				// Try calling toString().
				object toStringResult;
				if (TryCallMemberFunction(out toStringResult, "toString") == true)
				{
					// Return value must be primitive.
					if (toStringResult is string || TypeUtilities.IsPrimitive(toStringResult) == true)
						return toStringResult;
				}

				// Try calling valueOf().
				object valueOfResult;
				if (TryCallMemberFunction(out valueOfResult, "valueOf") == true)
				{
					// Return value must be primitive.
					if (valueOfResult is double || TypeUtilities.IsPrimitive(valueOfResult) == true)
						return valueOfResult;
				}

			}

			throw new JavaScriptException(this.Engine, "TypeError", "Attempted conversion of the object to a primitive value failed.  Check the toString() and valueOf() functions.");
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
			var function = GetPropertyValue(functionName);
			if (function == null)
				throw new JavaScriptException(this.Engine, "TypeError", string.Format("Object {0} has no method '{1}'", this.ToString(), functionName));
			if ((function is FunctionInstance) == false)
				throw new JavaScriptException(this.Engine, "TypeError", string.Format("Property '{1}' of object {0} is not a function", this.ToString(), functionName));
			return ((FunctionInstance)function).CallLateBound(Engine,thisObj, parameters);
		}
		
		/// <summary>
		/// Calls the function with the given name.  The function must exist on this object or an
		/// exception will be thrown.
		/// </summary>
		/// <param name="functionName"> The name of the function to call. </param>
		/// <param name="parameters"> The parameters to pass to the function. </param>
		/// <returns> The result of calling the function. </returns>
		internal object CallMemberFunction(string functionName, params object[] parameters)
		{
			var function = GetPropertyValue(functionName);
			if (function == null)
				throw new JavaScriptException(this.Engine, "TypeError", string.Format("Object {0} has no method '{1}'", this.ToString(), functionName));
			if ((function is FunctionInstance) == false)
				throw new JavaScriptException(this.Engine, "TypeError", string.Format("Property '{1}' of object {0} is not a function", this.ToString(), functionName));
			return ((FunctionInstance)function).CallLateBound(Engine,this, parameters);
		}
		
		/// <summary>
		/// Calls the function with the given name.
		/// </summary>
		/// <param name="result"> The result of calling the function. </param>
		/// <param name="functionName"> The name of the function to call. </param>
		/// <param name="parameters"> The parameters to pass to the function. </param>
		/// <returns> <c>true</c> if the function was called successfully; <c>false</c> otherwise. </returns>
		internal bool TryCallMemberFunction(out object result, string functionName, params object[] parameters)
		{
			var function = GetPropertyValue(functionName);
			if ((function is FunctionInstance) == false)
			{
				result = null;
				return false;
			}
			result = ((FunctionInstance)function).CallLateBound(Engine,this, parameters);
			return true;
		}

		/// <summary>
		/// Returns a string representing this object.
		/// </summary>
		/// <returns> A string representing this object. </returns>
		[JSProperties(Hidden=true)]
		public override string ToString()
		{
			try
			{
				// Does a toString method/property exist?
				if (HasProperty("toString") == true)
				{
					// Call the toString() method.
					var result = CallMemberFunction("toString");

					// Make sure we don't recursively loop forever.
					if (result == this)
						return "<error>";

					// Convert to a string.
					return TypeConverter.ToString(result);
				}
				else
				{
					// Otherwise, return the type name.
					var constructor = this["constructor"];
					if (constructor is FunctionInstance)
						return string.Format("[{0}]", ((FunctionInstance) constructor).Name);
					return "<unknown>";
				}
			}
			catch (JavaScriptException)
			{
				return "<error>";
			}
		}



		//	 JAVASCRIPT FUNCTIONS
		//_________________________________________________________________________________________

		/// <summary>
		/// Determines if a property with the given name exists on this object.
		/// </summary>
		/// <param name="engine"> The associated script engine. </param>
		/// <param name="thisObj"> The object that is being operated on. </param>
		/// <param name="propertyName"> The name of the property. </param>
		/// <returns> <c>true</c> if a property with the given name exists on this object,
		/// <c>false</c> otherwise. </returns>
		/// <remarks> Objects in the prototype chain are not considered. </remarks>
		public static bool HasOwnProperty(ScriptEngine engine, object thisObj, string propertyName)
		{
			TypeUtilities.VerifyThisObject(engine, thisObj, "hasOwnProperty");
			return engine.Prototypes.Get(thisObj.GetType())[propertyName]!=null;
		}

		/// <summary>
		/// Determines if this object is in the prototype chain of the given object.
		/// </summary>
		/// <param name="engine"> The associated script engine. </param>
		/// <param name="thisObj"> The object that is being operated on. </param>
		/// <param name="obj"> The object to check. </param>
		/// <returns> <c>true</c> if this object is in the prototype chain of the given object;
		/// <c>false</c> otherwise. </returns>
		public static bool IsPrototypeOf(ScriptEngine engine, object thisObj, object obj)
		{
			if ((obj is ObjectInstance) == false)
				return false;
			TypeUtilities.VerifyThisObject(engine, thisObj, "isPrototypeOf");
			var obj2 = obj as ObjectInstance;
			while (true)
			{
				obj2 = obj2.Prototype;
				if (obj2 == null)
					return false;
				if (obj2 == thisObj)
					return true;
			}
		}

		/// <summary>
		/// Determines if a property with the given name exists on this object and is enumerable.
		/// </summary>
		/// <param name="engine"> The associated script engine. </param>
		/// <param name="thisObj"> The object that is being operated on. </param>
		/// <param name="propertyName"> The name of the property. </param>
		/// <returns> <c>true</c> if a property with the given name exists on this object and is
		/// enumerable, <c>false</c> otherwise. </returns>
		/// <remarks> Objects in the prototype chain are not considered. </remarks>
		public static bool PropertyIsEnumerable(ScriptEngine engine, object thisObj, string propertyName)
		{
			TypeUtilities.VerifyThisObject(engine, thisObj, "propertyIsEnumerable");
			var property = engine.Prototypes.Get(thisObj.GetType()).GetProperty(propertyName);
			return property!=null && property.IsEnumerable;
		}

		/// <summary>
		/// Returns a locale-dependant string representing the current object.
		/// </summary>
		/// <returns> Returns a locale-dependant string representing the current object. </returns>
		public string ToLocaleString()
		{
			return TypeConverter.ToString(CallMemberFunction("toString"));
		}

		/// <summary>
		/// Returns a primitive value associated with the object.
		/// </summary>
		/// <returns> A primitive value associated with the object. </returns>
		public ObjectInstance ValueOf()
		{
			return this;
		}

		/// <summary>
		/// Returns a string representing the current object.
		/// </summary>
		/// <param name="engine"> The current script environment. </param>
		/// <param name="thisObj"> The value of the "this" keyword. </param>
		/// <returns> A string representing the current object. </returns>
		public static string ToString(ScriptEngine engine, object thisObj)
		{
			if (thisObj == null || thisObj == Undefined.Value)
				return "[object Undefined]";
			if (thisObj == Null.Value)
				return "[object Null]";
			return "[object "+engine.Prototypes.Get(thisObj.GetType()).Name+"]";
		}
		
		/// <summary>
		/// Creates a new Object instance.
		/// </summary>
		public static ObjectInstance OnConstruct(ScriptEngine engine)
		{
			#warning doesn't apply a suitable runtime friendly prototype
			return new ObjectInstance(engine,null);// engine.Prototypes.ObjectPrototype);
		}
		
		//	 JAVASCRIPT FUNCTIONS
		//_________________________________________________________________________________________
		
		/// <summary>
		/// Retrieves the next object in the prototype chain for the given object.
		/// </summary>
		/// <param name="obj"> The object to retrieve the prototype from. </param>
		/// <returns> The next object in the prototype chain for the given object, or <c>null</c>
		/// if the object has no prototype chain. </returns>
		
		public static object GetPrototypeOf(ObjectInstance obj)
		{
			var result = obj.Prototype;
			if (result == null)
				return Null.Value;
			return result;
		}

		/// <summary>
		/// Gets an object that contains details of the property with the given name.
		/// </summary>
		/// <param name="obj"> The object to retrieve property details for. </param>
		/// <param name="propertyName"> The name of the property to retrieve details for. </param>
		/// <returns> An object containing some of the following properties: configurable,
		/// writable, enumerable, value, get and set. </returns>
		
		public static ObjectInstance GetOwnPropertyDescriptor(ObjectInstance obj, string propertyName)
		{
			var descriptor = obj.GetOwnPropertyDescriptor(propertyName);
			if (descriptor.Exists == false)
				return null;
			return descriptor.ToObject(obj.Engine);
		}

		/// <summary>
		/// Creates an array containing the names of all the properties on the object (even the
		/// non-enumerable ones).
		/// </summary>
		/// <param name="obj"> The object to retrieve the property names for. </param>
		/// <returns> An array containing the names of all the properties on the object. </returns>
		
		public static Nitrassic.Library.Array GetOwnPropertyNames(ObjectInstance obj)
		{
			var result = Nitrassic.Library.Array.New(obj.Engine);
			foreach (var property in ((ObjectInstance)obj).Properties)
				result.Push(obj.Engine,property.Name);
			return result;
		}

		/// <summary>
		/// Creates an object with the given prototype and, optionally, a set of properties.
		/// </summary>
		/// <param name="engine">  </param>
		/// <param name="prototype"> A reference to the next object in the prototype chain for the
		/// created object. </param>
		/// <param name="properties"> An object containing one or more property descriptors. </param>
		/// <returns> A new object instance. </returns>
		
		public static ObjectInstance Create(ScriptEngine engine, object prototype, ObjectInstance properties)
		{
			if ((prototype is ObjectInstance) == false && prototype != Null.Value)
				throw new JavaScriptException(engine, "TypeError", "object prototype must be an object or null");
			ObjectInstance result;
			if (prototype == Null.Value)
				result = ObjectInstance.CreateRootObject(engine);
			else
				result = ObjectInstance.CreateRawObject(prototype as ObjectInstance);
			if (properties != null)
				DefineProperties(result, properties);
			return result;
		}

		/// <summary>
		/// Assigns enumerable properties of one or more source objects onto a destination object.
		/// </summary>
		/// <param name="engine"> The script engine. </param>
		/// <param name="target"> The destination object to copy properties to. </param>
		/// <param name="sources"> One or more source objects to copy properties from. </param>
		/// <returns> A new object instance. </returns>
		
		/*
		public static ObjectInstance Assign(ScriptEngine engine, ObjectInstance target, params object[] sources)
		{
			foreach (object rawSource in sources)
			{
				// Ignore undefined or null sources.
				if (rawSource == null || rawSource == Undefined.Value || rawSource == Null.Value)
					continue;
				Prototype source = engine.Prototypes.Get(rawSource.GetType());

				// Copy the enumerable properties from the source object.
				foreach (KeyValuePair<string,PropertyVariable> property in source.Properties)
					if (property.Value.IsEnumerable == true)
						target.SetPropertyValue(property.Key, property.Value.GetValue(rawSource), true);
			}
			return target;
		}
		*/
		
		/// <summary>
		/// Modifies the value and attributes of a property.
		/// </summary>
		/// <param name="obj"> The object to define the property on. </param>
		/// <param name="propertyName"> The name of the property to modify. </param>
		/// <param name="attributes"> A property descriptor containing some of the following
		/// properties: configurable, writable, enumerable, value, get and set. </param>
		/// <returns> The object with the property. </returns>
		
		public static ObjectInstance DefineProperty(ObjectInstance obj, string propertyName, object attributes)
		{
			var defaults = obj.GetOwnPropertyDescriptor(propertyName);
			if (!(attributes is ObjectInstance))
				throw new JavaScriptException(obj.Engine, "TypeError", "Invalid descriptor for property '{propertyName}'.");
			var descriptor = PropertyDescriptor.FromObject((ObjectInstance)attributes, defaults);
			obj.DefineProperty(propertyName, descriptor, true);
			return obj;
		}

		/// <summary>
		/// Modifies multiple properties on an object.
		/// </summary>
		/// <param name="obj"> The object to define the properties on. </param>
		/// <param name="properties"> An object containing one or more property descriptors. </param>
		/// <returns> The object with the properties. </returns>
		
		public static ObjectInstance DefineProperties(object obj, ObjectInstance properties)
		{
			if (!(obj is ObjectInstance))
				throw new JavaScriptException(properties.Engine, "TypeError", "Object.defineProperties called on non-object.");
			var obj2 = (ObjectInstance)obj;
			foreach (var property in properties.Properties)
				if (property.IsEnumerable == true)
					DefineProperty(obj2, property.Name, property.Value);
			return obj2;
		}

		/// <summary>
		/// Prevents the addition or deletion of any properties on the given object.
		/// </summary>
		/// <param name="obj"> The object to modify. </param>
		/// <returns> The object that was affected. </returns>
		
		public static ObjectInstance Seal(ObjectInstance obj)
		{
			var properties = new List<PropertyNameAndValue>();
			foreach (var property in obj.Properties)
				properties.Add(property);
			foreach (var property in properties)
			{
				obj.FastSetProperty(property.Name, property.Value,
					property.Attributes & ~PropertyAttributes.Configurable, true);
			}
			obj.IsExtensible = false;
			return obj;
		}

		/// <summary>
		/// Prevents the addition, deletion or modification of any properties on the given object.
		/// </summary>
		/// <param name="obj"> The object to modify. </param>
		/// <returns> The object that was affected. </returns>
		
		public static ObjectInstance Freeze(ObjectInstance obj)
		{
			var properties = new List<PropertyNameAndValue>();
			foreach (var property in obj.Properties)
				properties.Add(property);
			foreach (var property in properties)
			{
				obj.FastSetProperty(property.Name, property.Value,
					property.Attributes & ~(PropertyAttributes.NonEnumerable), true);
			}
			obj.IsExtensible = false;
			return obj;
		}

		/// <summary>
		/// Prevents the addition of any properties on the given object.
		/// </summary>
		/// <param name="obj"> The object to modify. </param>
		/// <returns> The object that was affected. </returns>
		
		public static ObjectInstance PreventExtensions(ObjectInstance obj)
		{
			obj.IsExtensible = false;
			return obj;
		}

		/// <summary>
		/// Determines if addition or deletion of any properties on the object is allowed.
		/// </summary>
		/// <param name="obj"> The object to check. </param>
		/// <returns> <c>true</c> if properties can be added or at least one property can be
		/// deleted; <c>false</c> otherwise. </returns>
		
		public static bool IsSealed(ObjectInstance obj)
		{
			foreach (var property in obj.Properties)
				if (property.IsConfigurable == true)
					return false;
			return obj.IsExtensible == false;
		}

		/// <summary>
		/// Determines if addition, deletion or modification of any properties on the object is
		/// allowed.
		/// </summary>
		/// <param name="obj"> The object to check. </param>
		/// <returns> <c>true</c> if properties can be added or at least one property can be
		/// deleted or modified; <c>false</c> otherwise. </returns>
		
		public static bool IsFrozen(ObjectInstance obj)
		{
			foreach (var property in obj.Properties)
				if (property.IsConfigurable == true || property.IsWritable == true)
					return false;
			return obj.IsExtensible == false;
		}

		/// <summary>
		/// Determines if addition of properties on the object is allowed.
		/// </summary>
		/// <param name="obj"> The object to check. </param>
		/// <returns> <c>true</c> if properties can be added to the object; <c>false</c> otherwise. </returns>
		
		[JSProperties(Name="isExtensible")]
		public static bool IsExtensibleG(ObjectInstance obj)
		{
			return obj.IsExtensible;
		}
		
		/// <summary>
		/// Creates an array containing the names of all the enumerable properties on the object.
		/// </summary>
		/// <param name="obj"> The object to retrieve the property names for. </param>
		/// <returns> An array containing the names of all the enumerable properties on the object. </returns>
		
		public static Nitrassic.Library.Array Keys(ObjectInstance obj)
		{
			var result = Nitrassic.Library.Array.New(obj.Engine);
			foreach (var property in obj.Properties)
				if (property.IsEnumerable == true)
					result.Push(obj.Engine,property.Name);
			return result;
		}

		/// <summary>
		/// Determines whether two values are the same value.  Note that this method considers NaN
		/// to be equal with itself and negative zero is considered different from positive zero.
		/// </summary>
		/// <param name="value1"> The first value to compare. </param>
		/// <param name="value2"> The second value to compare. </param>
		/// <returns> <c>true</c> if the values are the same.  </returns>
		
		public static bool Is(object value1, object value2)
		{
			return TypeComparer.SameValue(value1, value2);
		}
		
	}
}
