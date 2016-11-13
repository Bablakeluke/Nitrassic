using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Nitrassic.Library
{
	/// <summary>
	/// A uint16 array.
	/// </summary>
	public class Uint16Array : TypedArray
	{
		
		/// <summary>An internal buffer.</summary>
		private ushort[] buffer;
		
		// new Uint16Array(length);
		// new Uint16Array(typedArray);
		// new Uint16Array(object);
		// new Uint16Array(buffer[, byteOffset[, length]]);
		
		public Uint16Array(int length):base(TypedArrayStyle.Uint16Array, length){
			
			// Create the fast buffer:
			buffer=new ushort[Length];
			
		}
		
		public Uint16Array(double length):base(TypedArrayStyle.Uint16Array, (int)length){
			
			// Create the fast buffer:
			buffer=new ushort[Length];
			
		}
		
		public Uint16Array(TypedArray array):base(TypedArrayStyle.Uint16Array, array)
		{
			
			// Create the quick buffer:
			buffer=new ushort[Length];
			
			// Now add the array:
			Add(array);
			
		}
		
		public Uint16Array(object iterableObj):base(TypedArrayStyle.Uint16Array, iterableObj)
		{
			
			// Create the quick buffer:
			buffer=new ushort[Length];
			
			// Now add it:
			Add(iterableObj);
			
		}
		
		public Uint16Array(ArrayBuffer buff):this(buff,0,0){}
		
		public Uint16Array(ArrayBuffer buff,int byteOffset,int length):base(TypedArrayStyle.Uint16Array, length==0?buff.ByteLength:length)
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
				return LittleConverter.ToUInt16(_Buffer.buffer,(index * 2) + ByteOffset);
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
				LittleConverter.GetBytes((ushort)value,_Buffer.buffer,(index * 2) + ByteOffset);
				return;
			}
			// Get it as a ushort and put it in:
			buffer[index]=(ushort)value;
		}
		
		/// <summary>
		/// Gets or sets the given entry in the array.
		/// </summary>
		public ushort this[int index]{
			get{
				if(buffer==null)
				{
					// Use the _Buffer instead:
					return LittleConverter.ToUInt16(_Buffer.buffer,(index * 2) + ByteOffset);
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
		
		/// <summary>Creates a Uint16Array from the given iterable object.</summary>
		public static Uint16Array From(object value)
		{
			return new Uint16Array(value);
		}
		
	}
	
}