using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Nitrassic.Library
{
	/// <summary>
	/// A uint32 array.
	/// </summary>
	public class Uint32Array : TypedArray
	{
		
		/// <summary>An internal buffer.</summary>
		private uint[] buffer;
		
		// new Uint32Array(length);
		// new Uint32Array(typedArray);
		// new Uint32Array(object);
		// new Uint32Array(buffer[, byteOffset[, length]]);
		
		public Uint32Array(int length):base(TypedArrayStyle.Uint32Array, length){
			
			// Create the fast buffer:
			buffer=new uint[Length];
			
		}
		
		public Uint32Array(TypedArray array):base(TypedArrayStyle.Uint32Array, array)
		{
			
			// Create the quick buffer:
			buffer=new uint[Length];
			
			// Now add the array:
			Add(array);
			
		}
		
		public Uint32Array(object iterableObj):base(TypedArrayStyle.Uint32Array, iterableObj)
		{
			
			// Create the quick buffer:
			buffer=new uint[Length];
			
			// Now add it:
			Add(iterableObj);
			
		}
		
		public Uint32Array(ArrayBuffer buff,int byteOffset,int length):base(TypedArrayStyle.Uint32Array, length==0?buff.ByteLength:length)
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
				return LittleConverter.ToUInt32(_Buffer.buffer,(index * 4) + ByteOffset);
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
				LittleConverter.GetBytes((uint)value,_Buffer.buffer,(index * 4) + ByteOffset);
				return;
			}
			// Get it as a uint and put it in:
			buffer[index]=(uint)value;
		}
		
		/// <summary>
		/// Gets or sets the given entry in the array.
		/// </summary>
		public uint this[int index]{
			get{
				if(buffer==null)
				{
					// Use the _Buffer instead:
					return LittleConverter.ToUInt32(_Buffer.buffer,(index * 4) + ByteOffset);
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
		
		/// <summary>Creates a Uint32Array from the given iterable object.</summary>
		public static Uint32Array From(object value)
		{
			return new Uint32Array(value);
		}
		
	}
	
}