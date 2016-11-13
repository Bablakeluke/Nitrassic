using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Nitrassic.Library
{
	/// <summary>
	/// 
	/// </summary>
	public class TypedArray
	{
		
		/// <summary>
		/// The number of bytes per element in the array. Read-only.
		/// </summary>
		public int BYTES_PER_ELEMENT;
		
		/// <summary>
		/// The length of the array. Read-only.
		/// </summary>
		public int Length;
		
		/// <summary>
		/// The number of bytes that form this array. Read-only.
		/// </summary>
		public int ByteLength;
		
		/// <summary>
		/// The offset to the start of buffer.
		/// </summary>
		public int ByteOffset;
		
		/// <summary>
		/// The underlying buffer.
		/// Note that this may be created on demand.
		/// </summary>
		protected ArrayBuffer _Buffer;
		
		/// <summary>
		/// The underlying buffer.
		/// Note that this may be created on demand.
		/// </summary>
		public ArrayBuffer Buffer{
			get{
				if(_Buffer==null)
				{
					// Create the buffer now:
					_Buffer=new ArrayBuffer(ByteLength);
				}
				
				return _Buffer;
			}
		}
		
		/// <summary>
		/// Transfers the contents of this array instance into the given buffer.
		/// This occurs when a 'native' ushort[] etc are in use and a byte[] is required.
		/// </summary>
		protected virtual void FillBuffer()
		{
		}
		
		/// <summary>
		/// The type of typed array that this is.
		/// </summary>
		private TypedArrayStyle Style;

		//	 INITIALIZATION
		//_________________________________________________________________________________________
		
		internal TypedArray(TypedArrayStyle style,object iterableObj):this(style,GetLength(iterableObj))
		{
		}
		
		internal TypedArray(TypedArrayStyle style,TypedArray array):this(style,(int)array.Length)
		{
		}
		
		/// <summary>Gets the length of the given iterable object.</summary>
		private static int GetLength(object iterableObj)
		{
			Array arr=(iterableObj as Array);
			
			if(arr!=null)
			{
				return Nitrassic.Library.Array.get_Length(null,arr);
			}
			
			throw new NotImplementedException("TypedArrays can only be constructed from arrays at the moment (tried to use "+iterableObj+").");
		}
		
		/// <summary>Adds all values in the given array into this array.</summary>
		protected void Add(object iterableObj)
		{
			Array arr=(iterableObj as Array);
			
			if(arr==null)
			{
				throw new NotImplementedException("TypedArrays can only be constructed from arrays at the moment (tried to use "+iterableObj+").");
			}
			
			int index=0;
			
			foreach(var value in arr.ElementValues)
			{
				
				Set(index,value);
				index++;
				
			}
			
		}
		
		/// <summary>Adds all values in the given array into this array.</summary>
		protected void Add(TypedArray array)
		{
			
			int index=0;
			
			foreach(var value in array.ElementValues)
			{
				
				Set(index,value);
				index++;
				
			}
			
		}
		
		/// <summary>Gets the value at the given index.</summary>
		protected virtual object Get(int index)
		{
			throw new NotImplementedException();
		}
		
		/// <summary>Puts the given value into this array at the given index.</summary>
		protected virtual void Set(int index,object value)
		{
			throw new NotImplementedException();
		}
		
		/// <summary>The set of values in this array.</summary>
		public IEnumerable<object> ElementValues
		{
			get
			{
				int length=Length;
				
				for (int i = 0; i < length; i++)
				{
					yield return Get(i);
				}
			}
		}
		
		/// <summary>
		/// Creates an empty typed array instance for use as a prototype.
		/// </summary>
		/// <param name="constructor"> A reference to the constructor that owns the prototype. </param>
		internal TypedArray(TypedArrayStyle style,int length)
		{
			Style=style;
			int bytesPerElement;
			
			switch (style)
			{
				case TypedArrayStyle.Int8Array:
				case TypedArrayStyle.Uint8Array:
				case TypedArrayStyle.Uint8ClampedArray:
					bytesPerElement = 1;
					break;
				case TypedArrayStyle.Int16Array:
				case TypedArrayStyle.Uint16Array:
					bytesPerElement = 2;
					break;
				case TypedArrayStyle.Int32Array:
				case TypedArrayStyle.Uint32Array:
				case TypedArrayStyle.Float32Array:
					bytesPerElement = 4;
					break;
				case TypedArrayStyle.Float64Array:
					bytesPerElement = 8;
					break;
				default:
					throw new NotSupportedException("Unsupported TypedArray style '"+style+"'.");
			}
			
			BYTES_PER_ELEMENT=bytesPerElement;
			Length=length;
			ByteLength=length * BYTES_PER_ELEMENT;
		
		}
		
		//	 OVERRIDES
		//_________________________________________________________________________________________

		///// <summary>
		///// Gets a descriptor for the property with the given array index.
		///// </summary>
		///// <param name="index"> The array index of the property. </param>
		///// <returns> A property descriptor containing the property value and attributes. </returns>
		///// <remarks> The prototype chain is not searched. </remarks>
		//public override PropertyDescriptor GetOwnPropertyDescriptor(uint index)
		//{
		//	if (index < this.value.Length)
		//	{
		//		var result = this.value[(int)index].ToString();
		//		return new PropertyDescriptor(result, PropertyAttributes.Enumerable);
		//	}

		//	// Delegate to the base class.
		//	return base.GetOwnPropertyDescriptor(index);
		//}
	
		/// <summary>
		/// Gets an enumerable list of every property name and value associated with this object.
		/// </summary>
		[JSProperties(Hidden=true)]
		public IEnumerator<object> PropertyValues{
			get{
				// Enumerate array values.
				for (int i = 0; i < ByteLength; i+=BYTES_PER_ELEMENT)
					yield return Get(i);
			}
		}
		
		/// <summary>
		/// Gets an enumerable list of every property name and value associated with this object.
		/// </summary>
		[JSProperties(Hidden=true)]
		public IEnumerator<string> Properties{
			get{
				// Enumerate array indices.
				for (int i = 0; i < Length; i++)
					yield return i.ToString();
			}
		}
		
		//	 JAVASCRIPT FUNCTIONS
		//_________________________________________________________________________________________

		/// <summary>
		/// Fills all the elements of a typed array from a start index to an end index with a
		/// static value.
		/// </summary>
		/// <param name="value"> The value to fill the typed array with. </param>
		/// <param name="start"> Optional. Start index. Defaults to 0. </param>
		/// <param name="end"> Optional. End index (exclusive). Defaults to the length of the array. </param>
		/// <returns></returns>
		
		public string Fill(int value, int start, int end)
		{
			
			if(end==0)
			{
				end=Length;
			}
			
			throw new NotImplementedException();
		}
		
		/// <summary>
		/// Called when the typed array object is invoked like a function, e.g. Int8Array().
		/// Throws an error.
		/// </summary>
		public static TypedArray OnCall(ScriptEngine engine)
		{
			throw new JavaScriptException(engine, "TypeError", "Constructor TypedArray requires 'new'");
		}
		
		//	 JAVASCRIPT FUNCTIONS
		//_________________________________________________________________________________________

		/// <summary>
		/// Creates a new typed array from an array-like or iterable object.
		/// </summary>
		/// <param name="source"> An array-like or iterable object to convert to a typed array. </param>
		/// <param name="mapFn"> Optional. Map function to call on every element of the typed array. </param>
		/// <param name="thisArg"> Optional. Value to use as this when executing mapFn. </param>
		/// <returns></returns>
		
		public static TypedArray From(object source, FunctionInstance mapFn, object thisArg)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Creates a new typed array with a variable number of arguments.
		/// </summary>
		/// <param name="elements"> Elements of which to create the typed array. </param>
		/// <returns></returns>
		
		public static TypedArray Of(params object[] elements)
		{
			throw new NotImplementedException();
		}

	}
}
