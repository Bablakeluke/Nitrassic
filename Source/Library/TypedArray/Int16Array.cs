using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Nitrassic.Library
{
	/// <summary>
	/// An int16 array.
	/// </summary>
	public class Int16Array : TypedArray
	{
		
		/// <summary>An internal buffer.</summary>
		private short[] buffer;
		
		// new Int16Array(length);
		// new Int16Array(typedArray);
		// new Int16Array(object);
		// new Int16Array(buffer[, byteOffset[, length]]);
		
		public Int16Array(int length):base(TypedArrayStyle.Int16Array, length){
			
			// Create the fast buffer:
			buffer=new short[Length];
			
		}
		
		public Int16Array(double length):base(TypedArrayStyle.Int16Array, (int)length){
			
			// Create the fast buffer:
			buffer=new short[Length];
			
		}
		
		public Int16Array(TypedArray array):base(TypedArrayStyle.Int16Array, array)
		{
			
			// Create the quick buffer:
			buffer=new short[Length];
			
			// Now add the array:
			Add(array);
			
		}
		
		public Int16Array(object iterableObj):base(TypedArrayStyle.Int16Array, iterableObj)
		{
			
			// Create the quick buffer:
			buffer=new short[Length];
			
			// Now add it:
			Add(iterableObj);
			
		}
		
		public Int16Array(ArrayBuffer buff):this(buff,0,0){}
		
		public Int16Array(ArrayBuffer buff,int byteOffset,int length):base(TypedArrayStyle.Int16Array, length==0?buff.ByteLength:length)
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
				byteOffset+=2;
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
				return LittleConverter.ToInt16(_Buffer.buffer,(index * 2) + ByteOffset);
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
				LittleConverter.GetBytes((short)value,_Buffer.buffer,(index * 2) + ByteOffset);
				return;
			}
			// Get it as a short and put it in:
			buffer[index]=(short)value;
		}
		
		/// <summary>
		/// Gets or sets the given entry in the array.
		/// </summary>
		public short this[int index]{
			get{
				if(buffer==null)
				{
					// Use the _Buffer instead:
					return LittleConverter.ToInt16(_Buffer.buffer,(index * 2) + ByteOffset);
				}
				return buffer[index];
			}
			set{
				if(buffer==null)
				{
					// Use the _Buffer instead:
					LittleConverter.GetBytes(value,_Buffer.buffer,(index * 2) + ByteOffset);
					return;
				}
				buffer[index]=value;
			}
		}
		
		/// <summary>Creates a Int16Array from the given iterable object.</summary>
		public static Int16Array From(object value)
		{
			return new Int16Array(value);
		}
		
	}
	
}