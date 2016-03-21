using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Nitrassic.Library
{
	/// <summary>
	/// A float32 array.
	/// </summary>
	public class Float32Array : TypedArray
	{
		
		/// <summary>An internal buffer.</summary>
		private float[] buffer;
		
		// new Float32Array(length);
		// new Float32Array(typedArray);
		// new Float32Array(object);
		// new Float32Array(buffer[, byteOffset[, length]]);
		
		public Float32Array(int length):base(TypedArrayStyle.Float32Array, length){
			
			// Create the fast buffer:
			buffer=new float[Length];
			
		}
		
		public Float32Array(TypedArray array):base(TypedArrayStyle.Float32Array, array)
		{
			
			// Create the quick buffer:
			buffer=new float[Length];
			
			// Now add the array:
			Add(array);
			
		}
		
		public Float32Array(object iterableObj):base(TypedArrayStyle.Float32Array, iterableObj)
		{
			
			// Create the quick buffer:
			buffer=new float[Length];
			
			// Now add it:
			Add(iterableObj);
			
		}
		
		public Float32Array(ArrayBuffer buff,int byteOffset,int length):base(TypedArrayStyle.Float32Array, length==0?buff.ByteLength:length)
		{
			ByteOffset=byteOffset;
			_Buffer=buff;
		}
		
		protected override void FillBuffer()
		{
			int length=Length;
			int byteOffset=ByteOffset;
			
			for(int i=0;i<length;i++)
			{
				var value=buffer[i];
				byte[] bytes=BitConverter.GetBytes(value);
				System.Array.Copy(bytes,0,_Buffer.buffer,byteOffset,4);
				byteOffset+=4;
			}
			
			// Remove the fast buffer:
			buffer=null;
		}
		
		/// <summary>Gets the value at the given index.</summary>
		protected override object Get(int index)
		{
			if(buffer==null)
			{
				// Use the _Buffer instead:
				return BitConverter.ToSingle(_Buffer.buffer,(index * 4) + ByteOffset);
			}
			return buffer[index];
		}
		
		/// <summary>
		/// Puts an unknown object into this array.
		/// Note that the value is always expected to be a value type.
		/// </summary>
		protected override void Set(int index,object value)
		{
			if(buffer==null)
			{
				// Use the _Buffer instead (this is unfortunate!):
				byte[] bytes=BitConverter.GetBytes((float)value);
				System.Array.Copy(bytes,0,_Buffer.buffer,(index * 4) + ByteOffset,4);
				return;
			}
			// Get it as an int and put it in:
			buffer[index]=(float)value;
		}
		
		/// <summary>
		/// Gets or sets the given entry in the array.
		/// </summary>
		public float this[int index]{
			get{
				if(buffer==null)
				{
					// Use the _Buffer instead:
					return BitConverter.ToSingle(_Buffer.buffer,(index * 4) + ByteOffset);
				}
				return buffer[index];
			}
			set{
				if(buffer==null)
				{
					// Use the _Buffer instead (this is unfortunate!):
					byte[] bytes=BitConverter.GetBytes(value);
					System.Array.Copy(bytes,0,_Buffer.buffer,(index * 4) + ByteOffset,4);
					return;
				}
				buffer[index]=value;
			}
		}
		
		/// <summary>Creates a Float32Array from the given iterable object.</summary>
		public static Float32Array From(object value)
		{
			return new Float32Array(value);
		}
		
	}
	
}