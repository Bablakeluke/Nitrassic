using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Nitrassic.Library
{
	/// <summary>
	/// An int32 array.
	/// </summary>
	public class Int32Array : TypedArray
	{
		
		/// <summary>An internal buffer.</summary>
		private int[] buffer;
		
		// new Int32Array(length);
		// new Int32Array(typedArray);
		// new Int32Array(object);
		// new Int32Array(buffer[, byteOffset[, length]]);
		
		public Int32Array(int length):base(TypedArrayStyle.Int32Array, length){
			
			// Create the fast buffer:
			buffer=new int[Length];
			
		}
		
		public Int32Array(TypedArray array):base(TypedArrayStyle.Int32Array, array)
		{
			
			// Create the quick buffer:
			buffer=new int[Length];
			
			// Now add the array:
			Add(array);
			
		}
		
		public Int32Array(object iterableObj):base(TypedArrayStyle.Int32Array, iterableObj)
		{
			
			// Create the quick buffer:
			buffer=new int[Length];
			
			// Now add it:
			Add(iterableObj);
			
		}
		
		public Int32Array(ArrayBuffer buff,int byteOffset,int length):base(TypedArrayStyle.Int32Array, length==0?buff.ByteLength:length)
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
				LittleConverter.GetBytes(value,_Buffer.buffer,byteOffset);
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
				return LittleConverter.ToInt32(_Buffer.buffer,(index * 4) + ByteOffset);
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
				// Use the _Buffer instead:
				LittleConverter.GetBytes((int)value,_Buffer.buffer,(index * 4) + ByteOffset);
				return;
			}
			// Get it as an int and put it in:
			buffer[index]=(int)value;
		}
		
		/// <summary>
		/// Gets or sets the given entry in the array.
		/// </summary>
		public int this[int index]{
			get{
				if(buffer==null)
				{
					// Use the _Buffer instead:
					return LittleConverter.ToInt32(_Buffer.buffer,(index * 4) + ByteOffset);
				}
				return buffer[index];
			}
			set{
				if(buffer==null)
				{
					// Use the _Buffer instead:
					LittleConverter.GetBytes(value,_Buffer.buffer,(index * 4) + ByteOffset);
					return;
				}
				buffer[index]=value;
			}
		}
		
		/// <summary>Creates a Int32Array from the given iterable object.</summary>
		public static Int32Array From(object value)
		{
			return new Int32Array(value);
		}
		
	}
	
}