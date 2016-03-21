using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Nitrassic.Library
{
	/// <summary>
	/// A Uint8 array. Essentially just a byte[].
	/// </summary>
	public class Uint8Array : TypedArray
	{
		
		/// <summary>A reference to the internal buffer.</summary>
		private byte[] buffer;
		
		// new Int8Array(length);
		// new Int8Array(typedArray);
		// new Int8Array(object);
		// new Int8Array(buffer[, byteOffset[, length]]);
		
		public Uint8Array(int length):base(TypedArrayStyle.Uint8Array, length){
			
			// Always uses a buffer as they are a byte[] anyway:
			_Buffer=new ArrayBuffer(ByteLength);
			
			// Quick ref to the buffers buffer:
			buffer=_Buffer.buffer;
			
		}
		
		public Uint8Array(TypedArray array):base(TypedArrayStyle.Uint8Array, array)
		{
			
			// Create a new buffer:
			_Buffer=new ArrayBuffer(ByteLength);
			
			// Get a quick ref to the buffers buffer:
			buffer=_Buffer.buffer;
			
			// Now add the array:
			Add(array);
			
		}
		
		public Uint8Array(object iterableObj):base(TypedArrayStyle.Uint8Array, iterableObj)
		{
			
			// Create a new buffer:
			_Buffer=new ArrayBuffer(ByteLength);
			
			// Get a quick ref to the buffers buffer:
			buffer=_Buffer.buffer;
			
			// Now add it:
			Add(iterableObj);
			
		}
		
		public Uint8Array(ArrayBuffer buff,int byteOffset,int length):base(TypedArrayStyle.Uint8Array, length==0?buff.ByteLength:length)
		{
			ByteOffset=byteOffset;
			_Buffer=buff;
			buffer=_Buffer.buffer;
		}
		
		/// <summary>Gets the value at the given index.</summary>
		protected override object Get(int index)
		{
			return buffer[index + ByteOffset];
		}
		
		/// <summary>
		/// Puts an unknown object into this array.
		/// Note that the value is always expected to be a value type.
		/// </summary>
		protected override void Set(int index,object value)
		{
			// Get it as a byte and put it in:
			buffer[index + ByteOffset]=(byte)value;
		}
		
		/// <summary>
		/// Gets or sets the given entry in the array.
		/// </summary>
		public byte this[int index]{
			get{
				return buffer[index + ByteOffset];
			}
			set{
				buffer[index + ByteOffset]=value;
			}
		}
		
		/// <summary>Creates a Uint8Array from the given iterable object.</summary>
		public static Uint8Array From(object value)
		{
			return new Uint8Array(value);
		}
		
	}
	
}