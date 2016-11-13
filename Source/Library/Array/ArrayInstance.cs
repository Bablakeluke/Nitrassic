using System;
using System.Collections.Generic;

namespace Nitrassic.Library
{
	
	internal delegate double OnCompare(object a,object b);
	
	/// <summary>
	/// Represents an instance of the JavaScript Array object.
	/// </summary>
	public partial class Array
	{
		// The array, if it is dense.
		private object[] dense;
		
		private ObjectInstance lookup=new ObjectInstance();
		
		// The dense array might have holes within the range 0..length-1.
		private bool denseMayContainHoles = false;

		// The array, if it is sparse.
		private SparseArray sparse;

		// The length of the array.
		private uint length;



		//	 INITIALIZATION
		//_________________________________________________________________________________________
		
		/// <summary>
		/// Creates a new array with the given length and capacity.
		/// </summary>
		/// <param name="prototype"> The next object in the prototype chain. </param>
		/// <param name="length"> The initial value of the length property. </param>
		/// <param name="capacity"> The number of elements to allocate. </param>
		internal Array(uint length, uint capacity)
		{
			if (length > capacity)
				throw new ArgumentOutOfRangeException("length", "length must be less than or equal to capacity.");
			if (length <= 1000)
			{
				this.dense = new object[(int)capacity];
				this.denseMayContainHoles = length > 0;
			}
			else
			{
				this.sparse = new SparseArray();
			}

			// Initialize the length property.
			this.length = length;
		}

		/// <summary>
		/// Creates a new array and initializes it with the given array.
		/// </summary>
		/// <param name="prototype"> The next object in the prototype chain. </param>
		/// <param name="elements"> The initial values in the array. </param>
		internal Array(object[] elements)
		{
			if (elements == null)
				throw new ArgumentNullException("elements");
			this.dense = elements;
			
			this.denseMayContainHoles = System.Array.IndexOf(elements, null) >= 0;

			// Initialize the length property:
			this.length = (uint)elements.Length;
		}

		/// <summary>
		/// Creates a new array and initializes it with the given sparse array.
		/// </summary>
		/// <param name="prototype"> The next object in the prototype chain. </param>
		/// <param name="sparseArray"> The sparse array to use as the backing store. </param>
		/// <param name="length"> The initial value of the length property. </param>
		private Array(SparseArray sparseArray, uint length)
		{
			if (sparseArray == null)
				throw new ArgumentNullException("sparseArray");
			this.sparse = sparseArray;

			// Initialize the length property.
			this.length = length;
		}



		//	 .NET ACCESSOR PROPERTIES
		//_________________________________________________________________________________________
		
		/// <summary>
		/// Gets or sets the number of elements in the array.  Equivalent to the javascript
		/// Array.prototype.length property.
		/// </summary>
		public static int get_Length(ScriptEngine engine,Array thisObj) // Note that they must *both* either have or not have engine!
		{
			return (int)thisObj.length;
		}
		
		public static void set_Length(ScriptEngine engine,Array thisObj,int value)
		{
		
			if(value<0){
				throw new JavaScriptException(engine,"RangeError","Invalid array length.");
			}
			
			uint previousLength = thisObj.length;
			thisObj.length = (uint)value;
			
			if (thisObj.dense != null)
			{
				if (thisObj.length < thisObj.dense.Length / 2 && thisObj.dense.Length > 100)
				{
					// Shrink the array.
					thisObj.ResizeDenseArray(thisObj.length + 10, thisObj.length);
				}
				else if (thisObj.length > thisObj.dense.Length + 10)
				{
					// Switch to a sparse array.
					thisObj.sparse = SparseArray.FromDenseArray(thisObj.dense, (int)previousLength);
					thisObj.dense = null;
				}
				else if (thisObj.length > thisObj.dense.Length)
				{
					// Enlarge the array.
					thisObj.ResizeDenseArray(thisObj.length + 10, previousLength);
					thisObj.denseMayContainHoles = true;
				}
				else if (thisObj.length > previousLength)
				{
					// Increasing the length property creates an array with holes.
					thisObj.denseMayContainHoles = true;

					// Remove all the elements with indices >= length.
					System.Array.Clear(thisObj.dense, (int)previousLength, (int)(thisObj.length - previousLength));
				}
			}
			else
			{
				// Remove all the elements with indices >= length.
				if (thisObj.length < previousLength)
					thisObj.sparse.DeleteRange(thisObj.length, previousLength - thisObj.length);
			}
		
		}


		/// <summary>
		/// Gets an enumerable list of the defined element values stored in this array.
		/// </summary>
		[JSProperties(Hidden=true)]
		public IEnumerable<object> ElementValues
		{
			get
			{
				// Enumerate the dense values.
				if (this.dense != null)
				{
					for (uint i = 0; i < this.dense.Length; i++)
					{
						object elementValue = this.dense[i];
						if (elementValue != null)
							yield return elementValue;
					}
				}
				else
				{
					// Enumerate the sparse values.
					foreach (var range in this.sparse.Ranges)
					{
						for (int i = 0; i < range.Length; i++)
						{
							object elementValue = range.Array[i];
							if (elementValue != null)
								yield return elementValue;
						}
					}
				}
			}
		}



		//	 INTERNAL HELPER METHODS
		//_________________________________________________________________________________________

		/// <summary>
		/// Attempts to parse a string into a valid array index.
		/// </summary>
		/// <param name="propertyName"> The property name to parse. </param>
		/// <returns> The array index value, or <c>uint.MaxValue</c> if the property name does not reference
		/// an array index. </returns>
		internal static uint ParseArrayIndex(string propertyName)
		{
			if (propertyName.Length == 0)
				return uint.MaxValue;
			int digit = propertyName[0] - '0';
			if (digit < 0 || digit > 9)
				return uint.MaxValue;
			uint result = (uint)digit;
			for (int i = 1; i < propertyName.Length; i++)
			{
				digit = propertyName[i] - '0';
				if (digit < 0 || digit > 9)
					return uint.MaxValue;
				if (result > 429496728)
				{
					// Largest number 429496728 * 10 + 9 = 4294967289
					// Largest number 429496729 * 10 + 9 = 4294967299
					// Largest valid array index is 4294967294
					if (result != 429496729 || digit > 4)
						return uint.MaxValue;
				}
				result = result * 10 + (uint)digit;
			}
			return result;
		}



		//	 OVERRIDES
		//_________________________________________________________________________________________

		/// <summary>
		/// Gets a descriptor for the property with the given array index.
		/// </summary>
		/// <param name="index"> The array index of the property. </param>
		/// <returns> A property descriptor containing the property value and attributes. </returns>
		/// <remarks> The prototype chain is not searched. </remarks>
		[JSProperties(Hidden=true)]
		public PropertyDescriptor GetOwnPropertyDescriptor(uint index)
		{
			if (this.dense != null)
			{
				// The array is dense and therefore has at least "length" elements.
				if (index < this.length)
					return new PropertyDescriptor(this.dense[index], PropertyAttributes.FullAccess);
				return PropertyDescriptor.Undefined;
			}

			// The array is sparse and therefore has "holes".
			return new PropertyDescriptor(this.sparse[index], PropertyAttributes.FullAccess);
		}
		
		/// <summary>
		/// Gets or sets the value of an array-indexed property.
		/// </summary>
		/// <param name="index"> The index of the property to retrieve. </param>
		/// <returns> The property value, or <c>null</c> if the property doesn't exist. </returns>
		public object this[string index]
		{
			get
			{
				uint intIndex=ParseArrayIndex(index);
				
				if(intIndex==uint.MaxValue){
					
					// It's textual - read from lookup.
					return lookup[index];
					
				}
				
				return this[intIndex];
				
			}
			set
			{
				uint intIndex=ParseArrayIndex(index);
				
				if(intIndex==uint.MaxValue){
					
					// It's textual.
					lookup[index]=value;
					return;
					
				}
				
				this[intIndex]=value;
				
			}
		}
		
		/// <summary>
		/// Gets or sets the value of an array-indexed property.
		/// </summary>
		/// <param name="index"> The index of the property to retrieve. </param>
		/// <returns> The property value, or <c>null</c> if the property doesn't exist. </returns>
		public object this[int index]
		{
			get
			{
				if (index < 0)
					throw new ArgumentOutOfRangeException("index");
				return this[(uint)index];
			}
			set
			{
				if (index < 0)
					throw new ArgumentOutOfRangeException("index");
				this[(uint)index]=value;
			}
		}
		
		/// <summary>
		/// Sets the value of the property with the given array index.
		/// </summary>
		public object this[uint index]{
			get{
				
				if(this.dense!=null)
				{
					return dense[index];
				}
				
				return sparse[index];
				
			}
			set{
				
				value = value ?? Undefined.Value;
				
				if (this.dense != null)
				{
					if (index < this.length)
					{
						// The index is inside the existing bounds of the array.
						this.dense[index] = value;
					}
					else if (index < this.dense.Length)
					{
						// The index is outside the bounds of the array but inside the allocated buffer.
						this.dense[index] = value;
						this.denseMayContainHoles = this.denseMayContainHoles || index > this.length;
						this.length = index + 1;
					}
					else
					{
						// The index is out of range - either enlarge the array or switch to sparse.
						if (index < this.dense.Length + 10)
						{
							// Enlarge the dense array.
							ResizeDenseArray((uint)(this.dense.Length * 2 + 10), this.length);

							// Set the value.
							this.dense[index] = value;
							this.denseMayContainHoles = this.denseMayContainHoles || index > this.length;
						}
						else
						{
							// Switch to a sparse array.
							this.sparse = SparseArray.FromDenseArray(this.dense, (int)this.length);
							this.dense = null;
							this.sparse[index] = value;
						}

						// Update the length.
						this.length = index + 1;
					}
				}
				else
				{
					// Set the value and update the length.
					this.sparse[index] = value;
					this.length = Math.Max(this.length, index + 1);
				}
			}
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
		public bool Delete(uint index, bool throwOnError)
		{
			if (this.dense != null)
			{
				this.dense[index] = null;
				this.denseMayContainHoles = true;
			}
			else
				this.sparse.Delete(index);
			return true;
		}
		
		/// <summary>
		/// Gets an enumerable list of every property name and value associated with this object.
		/// </summary>
		[JSProperties(Hidden=true)]
		public IEnumerable<object> PropertyValues
		{
			get
			{
				if (this.dense != null)
				{
					// Enumerate dense array indices.
					for (uint i = 0; i < this.dense.Length; i++)
					{
						object arrayElementValue = this.dense[i];
						if (arrayElementValue != null)
							yield return arrayElementValue;
					}
				}
				else
				{
					// Enumerate sparse array indices.
					foreach (var range in this.sparse.Ranges)
						for (uint i = range.StartIndex; i < range.StartIndex + range.Array.Length; i++)
						{
							object arrayElementValue = this.sparse[i];
							if (arrayElementValue != null)
								yield return arrayElementValue;
						}
				}
				
			}
		}
		
		/// <summary>
		/// Gets an enumerable list of every property name and value associated with this object.
		/// </summary>
		[JSProperties(Hidden=true)]
		public IEnumerable<string> Properties
		{
			get
			{
				if (this.dense != null)
				{
					// Enumerate dense array indices.
					for (uint i = 0; i < this.dense.Length; i++)
					{
						object arrayElementValue = this.dense[i];
						if (arrayElementValue != null)
							yield return i.ToString();
					}
				}
				else
				{
					// Enumerate sparse array indices.
					foreach (var range in this.sparse.Ranges)
						for (uint i = range.StartIndex; i < range.StartIndex + range.Array.Length; i++)
						{
							object arrayElementValue = this.sparse[i];
							if (arrayElementValue != null)
								yield return i.ToString();
						}
				}
				
				/*
				// Delegate to the base implementation.
				foreach (var nameAndValue in HostProperties)
					yield return nameAndValue;
				*/
			}
		}
		
		/*
		[JSProperties(Hidden=true)]
		public IEnumerator<string> HostProperties
		{
			get
			{
				return base.Properties;
			}
		}
		*/
		
		//	 JAVASCRIPT FUNCTIONS
		//_________________________________________________________________________________________

		/// <summary>
		/// Returns a new array consisting of the values of this array plus any number of
		/// additional items.  Values are appended from left to right.
		/// </summary>
		/// <param name="thisObj"> The array that is being operated on. </param>
		/// <param name="items"> Any number of items to append. </param>
		/// <returns> A new array consisting of the values of this array plus any number of
		/// additional items. </returns>
		public static Nitrassic.Library.Array Concat(ScriptEngine engine,Array thisObj, params object[] items)
		{
			// Create a new items array with the thisObj at the beginning.
			var temp = new object[items.Length + 1];
			temp[0] = thisObj;
			System.Array.Copy(items, 0, temp, 1, items.Length);
			items = temp;

			// Determine if the resulting array should be dense or sparse and calculate the length
			// at the same time.
			bool dense = true;
			uint length = (uint)items.Length;
			foreach (object item in items)
				if (item is Nitrassic.Library.Array)
				{
					length += ((Nitrassic.Library.Array)item).length - 1;
					if (((Nitrassic.Library.Array)item).dense == null)
						dense = false;
				}

			// This method only supports arrays of length up to 2^31-1, rather than 2^32-1.
			if (length > int.MaxValue)
				throw new JavaScriptException(engine, "RangeError", "The resulting array is too long");

			if (dense == true)
			{
				// Create a dense array.
				var result = new object[length];

				int index = 0;
				foreach (object item in items)
				{
					if (item is Nitrassic.Library.Array)
					{
						// Add the items in the array to the end of the resulting array.
						var array = (Nitrassic.Library.Array)item;
						System.Array.Copy(array.dense, 0, result, index, (int)array.length);
						if (array.denseMayContainHoles == true && array.lookup != null)
						{
							// Populate holes from the prototype.
							for (uint i = 0; i < array.length; i++)
								if (array.dense[i] == null)
									result[index + i] = array.lookup.GetPropertyValue(i);
						}
						index += (int)array.length;
					}
					else
					{
						// Add the item to the end of the resulting array.
						result[index ++] = item;
					}
				}

				// Return the new dense array.
				return new Nitrassic.Library.Array(result);
			}
			else
			{
				// Create a new sparse array.
				var result = new SparseArray();

				int index = 0;
				foreach (object item in items)
				{
					if (item is Nitrassic.Library.Array)
					{
						// Add the items in the array to the end of the resulting array.
						var array = (Nitrassic.Library.Array)item;
						if (array.dense != null)
						{
							result.CopyTo(array.dense, (uint)index, (int)array.length);
							if (array.lookup != null)
							{
								// Populate holes from the lookup.
								for (uint i = 0; i < array.length; i++)
									if (array.dense[i] == null)
										result[(uint)index + i] = array.lookup.GetPropertyValue(i);
							}
						}
						else
						{
							result.CopyTo(array.sparse, (uint)index);
							if (array.lookup != null)
							{
								// Populate holes from the lookup.
								for (uint i = 0; i < array.length; i++)
									if (array.sparse[i] == null)
										result[(uint)index + i] = array.lookup.GetPropertyValue(i);
							}
						}
						index += (int)array.length;
					}
					else
					{
						// Add the item to the end of the resulting array.
						result[(uint)index] = item;
						index++;
					}
				}

				// Return the new sparse array.
				return new Nitrassic.Library.Array(result, length);
			}
		}

		/// <summary>
		/// Concatenates all the elements of the array, using the specified separator between each
		/// element.  If no separator is provided, a comma is used for this purpose.
		/// </summary>
		/// <param name="thisObj"> The array that is being operated on. </param>
		/// <param name="separator"> The string to use as a separator. </param>
		/// <returns> A string that consists of the element values separated by the separator string. </returns>
		public static string Join(ScriptEngine engine,Array thisObj, string separator)
		{
			
			if(separator==null)
				separator=",";
			
			// Get the length of the array.
			uint arrayLength = GetLength(thisObj);

			// This method only supports strings of length up to 2^31-1.
			if (arrayLength > int.MaxValue / 2)
				throw new JavaScriptException(engine, "RangeError", "The array is too long");

			var result = new System.Text.StringBuilder((int)arrayLength * 2);
			try
			{

				for (uint i = 0; i < arrayLength; i++)
				{
					if (i > 0)
						result.Append(separator);
					object element = thisObj[i];
					if (element != null && element != Undefined.Value && element != Null.Value)
						result.Append(TypeConverter.ToString(element));
				}

			}
			catch (ArgumentOutOfRangeException)
			{
				throw new JavaScriptException(engine, "RangeError", "The array is too long");
			}
			return result.ToString();
		}

		/// <summary>
		/// Removes the last element from the array and returns it.
		/// </summary>
		/// <param name="thisObj"> The array to operate on. </param>
		/// <returns> The last element from the array. </returns>
		public static object Pop(Array thisObj)
		{
			// If the "this" object is an array, use the fast version of this method.
			// if (thisObj is Nitrassic.Library.Array)
			return thisObj.Pop();
			
			/*
			// Get the length of the array.
			uint arrayLength = GetLength(thisObj);

			// Return undefined if the array is empty.
			if (arrayLength == 0)
			{
				SetLength(thisObj, 0);
				return Undefined.Value;
			}

			// Decrement the array length.
			arrayLength -= 1;

			// Get the last element of the array.
			var lastElement = thisObj[arrayLength];

			// Remove the last element from the array.
			thisObj.Delete(arrayLength, true);

			// Update the length.
			SetLength(thisObj, arrayLength);

			// Return the last element.
			return lastElement;
			*/
			
		}

		/// <summary>
		/// Removes the last element from the array and returns it.
		/// </summary>
		/// <returns> The last element from the array. </returns>
		public object Pop()
		{
			// Return undefined if the array is empty.
			if (this.length == 0)
				return Undefined.Value;

			// Decrement the array length.
			this.length -= 1;

			if (this.dense != null)
			{
				// Get the last value.
				var result = this.dense[this.length];
				
				// Delete it from the array.
				this.dense[this.length] = null;

				// Check if the array should be shrunk.
				if (this.length < this.dense.Length / 2 && this.length > 10)
					ResizeDenseArray((uint)(this.dense.Length / 2 + 10), this.length);

				// Return the last value.
				return result;
			}
			else
			{
				// Get the last value.
				var result = this.sparse[this.length];
				
				// Delete it from the array.
				this.sparse.Delete(this.length);

				// Return the last value.
				return result;
			}
		}

		/// <summary>
		/// Appends one or more elements to the end of the array.
		/// </summary>
		/// <param name="thisObj"> The array that is being operated on. </param>
		/// <param name="items"> The items to append to the array. </param>
		public static double Push(ScriptEngine engine,Array thisObj, params object[] items)
		{
			// If the "this" object is an array, use the fast version of this method.
			return thisObj.Push(engine,items[0]);
			
			/*
			// Get the length of the array.
			uint arrayLength = GetLength(thisObj);

			if (arrayLength > uint.MaxValue - items.Length)
			{
				// Even though attempting to push more items than can fit in the array raises an
				// error, the items are still pushed correctly (but the length is stuck at the
				// maximum).
				double arrayLength2 = arrayLength;
				for (int i = 0; i < items.Length; i++)
				{
					// Append the new item to the array.
					thisObj[(arrayLength2++)]=items[i];
				}
				SetLength(thisObj, uint.MaxValue);
				throw new JavaScriptException(engine, "RangeError", "Invalid array length");
			}

			// For each item to append.
			for (int i = 0; i < items.Length; i++)
			{
				// Append the new item to the array.
				thisObj[arrayLength ++]=items[i];
			}

			// Update the length property.
			SetLength(thisObj, arrayLength);

			// Return the new length.
			return (double)arrayLength;
			*/
		}

		/// <summary>
		/// Appends one element to the end of the array.
		/// </summary>
		/// <param name="item"> The item to append to the array. </param>
		internal int Push(ScriptEngine engine,object item)
		{
			
			if (this.length == uint.MaxValue)
			{
				// Even though attempting to push more items than can fit in the array raises an
				// error, the items are still pushed correctly (but the length is stuck at the
				// maximum).
				this[this.length]=item;
				throw new JavaScriptException(engine, "RangeError", "Invalid array length");
			}

			if (this.dense != null)
			{
				// Check if we need to enlarge the array.
				if (this.length == this.dense.Length)
					ResizeDenseArray(this.length * 2 + 10, this.length);

				// Append the new item to the array.
				this.dense[this.length++] = item;
			}
			else
			{
				// Append the new item to the array.
				this.sparse[this.length++] = item;
			}

			// Return the new length.
			return (int)this.length;
		}

		/// <summary>
		/// Reverses the order of the elements in the array.
		/// </summary>
		/// <param name="thisObj"> The array that is being operated on. </param>
		/// <returns> The array that is being operated on. </returns>
		public static Array Reverse(Array thisObj)
		{
			// Get the length of the array.
			uint arrayLength = GetLength(thisObj);

			// Reverse each element.
			for (uint lowIndex = 0; lowIndex < arrayLength / 2; lowIndex++)
			{
				uint highIndex = arrayLength - lowIndex - 1;

				// Swap the two values.
				object low = thisObj[lowIndex];
				object high = thisObj[highIndex];
				if (high != null)
					thisObj[lowIndex] = high;
				else
					thisObj.Delete(lowIndex, true);
				if (low != null)
					thisObj[highIndex] = low;
				else
					thisObj.Delete(highIndex, true);
			}

			return thisObj;
		}

		/// <summary>
		/// The first element in the array is removed from the array and returned.  All the other
		/// elements are shifted down in the array.
		/// </summary>
		/// <param name="thisObj"> The array that is being operated on. </param>
		/// <returns> The first element in the array. </returns>
		public static object Shift(ScriptEngine engine,Array thisObj)
		{
			// Get the length of the array.
			uint arrayLength = GetLength(thisObj);

			// Return undefined if the array is empty.
			if (arrayLength == 0)
			{
				SetLength(engine,thisObj, 0);
				return Undefined.Value;
			}

			// Get the first element.
			var firstElement = thisObj[0];

			// Shift all the other elements down one place.
			for (uint i = 1; i < arrayLength; i++)
			{
				object elementValue = thisObj[i];
				if (elementValue != null)
					thisObj[i - 1] = elementValue;
				else
					thisObj.Delete(i - 1, true);
			}

			// Delete the last element.
			thisObj.Delete(arrayLength - 1, true);

			// Update the length property.
			SetLength(engine,thisObj, arrayLength - 1);

			// Return the first element.
			return firstElement;
		}

		/// <summary>
		/// Returns a section of an array.
		/// </summary>
		/// <param name="thisObj"> The array that is being operated on. </param>
		/// <param name="start"> The index of the first element in the section.  If this value is
		/// negative it is treated as an offset from the end of the array. </param>
		/// <param name="end"> The index of the element just past the last element in the section.
		/// If this value is negative it is treated as an offset from the end of the array.  If
		/// <paramref name="end"/> is less than or equal to <paramref name="start"/> then an empty
		/// array is returned. </param>
		/// <returns> A section of an array. </returns>
		public static Nitrassic.Library.Array Slice(ScriptEngine engine,Array thisObj, int start, int end)
		{
			
			if(end==0)
				end=int.MaxValue;
			
			// Get the length of the array.
			uint arrayLength = GetLength(thisObj);

			// This method only supports arrays of length up to 2^31-1.
			if (arrayLength > int.MaxValue)
				throw new JavaScriptException(engine, "RangeError", "The array is too long");

			// Fix the arguments so they are positive and within the bounds of the array.
			if (start < 0)
				start += (int)arrayLength;
			if (end < 0)
				end += (int)arrayLength;
			if (end <= start)
				return Nitrassic.Library.Array.New(engine,new object[0]);
			start = Math.Min(Math.Max(start, 0), (int)arrayLength);
			end = Math.Min(Math.Max(end, 0), (int)arrayLength);

			// Populate a new array.
			object[] result = new object[end - start];
			for (int i = 0; i < result.Length; i++)
			{
				result[i] = thisObj[(uint)(start + i)];
			}
			return Nitrassic.Library.Array.New(engine,result);
		}

		/// <summary>
		/// Sorts the array.
		/// </summary>
		/// <param name="thisObj"> The array that is being operated on. </param>
		/// <param name="comparisonFunction"> A function which determines the order of the
		/// elements.  This function should return a number less than zero if the first argument is
		/// less than the second argument, zero if the arguments are equal or a number greater than
		/// zero if the first argument is greater than Defaults to an ascending ASCII ordering. </param>
		/// <returns> The array that was sorted. </returns>
		public static Array Sort(ScriptEngine engine,Array thisObj, FunctionInstance comparisonFunction)
		{
			// Get the length of the array.
			uint arrayLength = GetLength(thisObj);

			// An array of size 1 or less is already sorted.
			if (arrayLength <= 1)
				return thisObj;

			// Create a comparer delegate.
			OnCompare comparer;
			if (comparisonFunction == null)
				comparer = delegate(object a,object b)
				{
					if (a == null && b == null)
						return 0f;
					if (a == null)
						return 1f;
					if (b == null)
						return -1f;
					if (a == Undefined.Value && b == Undefined.Value)
						return 0f;
					if (a == Undefined.Value)
						return 1f;
					if (b == Undefined.Value)
						return -1f;
					return string.Compare(TypeConverter.ToString(a), TypeConverter.ToString(b), StringComparison.Ordinal);
				};
			else
				comparer =delegate (object a,object b)
				{
					if (a == null && b == null)
						return 0f;
					if (a == null)
						return 1f;
					if (b == null)
						return -1f;
					if (a == Undefined.Value && b == Undefined.Value)
						return 0f;
					if (a == Undefined.Value)
						return 1f;
					if (b == Undefined.Value)
						return -1f;
					return TypeConverter.ToNumber(comparisonFunction.CallLateBound(engine,null, a, b));
				}; 

			try
			{
				// Sort the array.
				QuickSort(thisObj, comparer, 0, arrayLength - 1);
			}
			catch (IndexOutOfRangeException)
			{
				throw new JavaScriptException(engine, "TypeError", "Invalid comparison function");
			}

			return thisObj;
		}

		/// <summary>
		/// Deletes a range of elements from the array and optionally inserts new elements.
		/// </summary>
		/// <param name="thisObj"> The array that is being operated on. </param>
		/// <param name="start"> The index to start deleting from. </param>
		/// <param name="deleteCount"> The number of elements to delete. </param>
		/// <param name="items"> The items to insert. </param>
		/// <returns> An array containing the deleted elements, if any. </returns>
		public static Nitrassic.Library.Array Splice(ScriptEngine engine,Array thisObj, int start, int deleteCount, params object[] items)
		{
			// Get the length of the array.
			uint arrayLength = GetLength(thisObj);

			// This method only supports arrays of length up to 2^31-1.
			if (arrayLength > int.MaxValue)
				throw new JavaScriptException(engine, "RangeError", "The array is too long");

			// Fix the arguments so they are positive and within the bounds of the array.
			if (start < 0)
				start = Math.Max((int)arrayLength + start, 0);
			else
				start = Math.Min(start, (int)arrayLength);
			deleteCount = Math.Min(Math.Max(deleteCount, 0), (int)arrayLength - start);

			// Get the deleted items.
			object[] deletedItems = new object[deleteCount];
			for (int i = 0; i < deleteCount; i++)
				deletedItems[i] = thisObj[(uint)(start + i)];

			// Move the trailing elements.
			int offset = items.Length - deleteCount;
			int newLength = (int)arrayLength + offset;
			if (deleteCount > items.Length)
			{
				for (int i = start + items.Length; i < newLength; i++)
					thisObj[(uint)i] = thisObj[(uint)(i - offset)];
				
				// Delete the trailing elements.
				for (int i = newLength; i < arrayLength; i++)
					thisObj.Delete((uint)i, true);
			}
			else
			{
				for (int i = newLength - 1; i >= start + items.Length; i--)
					thisObj[(uint)i] = thisObj[(uint)(i - offset)];
			}
			SetLength(engine,thisObj, (uint)newLength);

			// Insert the new elements.
			for (int i = 0; i < items.Length; i++)
				thisObj[(uint)(start + i)] = items[i];

			// Return the deleted items.
			return Nitrassic.Library.Array.New(engine,deletedItems);
		}

		/// <summary>
		/// Prepends the given items to the start of the array.
		/// </summary>
		/// <param name="thisObj"> The array that is being operated on. </param>
		/// <param name="items"> The items to prepend. </param>
		/// <returns> The new length of the array. </returns>
		public static uint Unshift(ScriptEngine engine,Array thisObj, params object[] items)
		{
			// If the "this" object is an array and the array is dense, use the fast version of this method.
			var array = thisObj; // as Nitrassic.Library.Array;
			if (array != null && array.dense != null)
			{
				// Dense arrays are supported up to 2^32-1.
				if (array.length + items.Length > int.MaxValue)
					throw new JavaScriptException(engine, "RangeError", "Invalid array length");

				if (array.denseMayContainHoles == true && array.lookup != null)
				{
					// Find all the holes and populate them from the lookup.
					for (uint i = 0; i < array.length; i++)
						if (array.dense[i] == null)
							array.dense[i] = array.lookup.GetPropertyValue(i);
				}

				// Allocate some more space if required.
				if (array.length + items.Length > array.dense.Length)
					array.ResizeDenseArray((uint)Math.Max(array.dense.Length * 2 + 10, array.length + items.Length * 10), array.length);

				// Shift all the items up.
				System.Array.Copy(array.dense, 0, array.dense, items.Length, (int)array.length);

				// Prepend the new items.
				for (int i = 0; i < items.Length; i++)
					array.dense[i] = items[i];

				// Update the length property.
				array.length += (uint)items.Length;

				// Return the new length of the array.
				return array.length;
			}

			// Get the length of the array.
			uint arrayLength = GetLength(thisObj);

			// This method supports arrays of length up to 2^32-1.
			if (uint.MaxValue - arrayLength < items.Length)
				throw new JavaScriptException(engine, "RangeError", "Invalid array length");

			// Update the length property.
			SetLength(engine,thisObj, arrayLength + (uint)items.Length);

			// Shift all the items up.
			for (int i = (int)arrayLength - 1; i >= 0; i--)
				thisObj[(uint)(i + items.Length)] = thisObj[(uint)i];

			// Prepend the new items.
			for (uint i = 0; i < items.Length; i++)
				thisObj[i]=items[i];
			
			// Return the new length of the array.
			return arrayLength + (uint)items.Length;
		}

		/// <summary>
		/// Returns a locale-specific string representing this object.
		/// </summary>
		/// <param name="thisObj"> The array that is being operated on. </param>
		/// <returns> A locale-specific string representing this object. </returns>
		public static string ToLocaleString(ScriptEngine engine,Array thisObj)
		{
			// Get the length of the array.
			var arrayLength = GetLength(thisObj);

			// This method only supports arrays of length up to 2^30-1.
			if (arrayLength > int.MaxValue / 2)
				throw new JavaScriptException(engine, "RangeError", "The array is too long");

			var result = new System.Text.StringBuilder((int)arrayLength * 2);

			// Get the culture-specific list separator.
			var listSeparator = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ListSeparator;

			try
			{

				for (uint i = 0; i < arrayLength; i++)
				{
					// Append the localized list separator.
					if (i > 0)
						result.Append(listSeparator);

					// Retrieve the array element.
					var element = thisObj[i];

					// Convert the element to a string and append it to the result.
					Prototype obj2=TypeConverter.ToPrototype(element,engine);
					
					if (obj2!=null){
						
						result.Append(obj2.CallMemberFunctionOn(element,"toLocaleString"));
						
					}
					
				}

			}
			catch (ArgumentOutOfRangeException)
			{
				throw new JavaScriptException(engine, "RangeError", "The array is too long");
			}

			return result.ToString();
		}

		/// <summary>
		/// Returns a string representing this object.
		/// </summary>
		/// <param name="thisObj"> The array that is being operated on. </param>
		/// <returns> A string representing this object. </returns>
		public static string ToString(ScriptEngine engine,Array thisObj)
		{
			// Try calling thisObj.join().
			return Join(engine,thisObj,",");
		}



		//	 JAVASCRIPT FUNCTIONS (NEW IN ES5)
		//_________________________________________________________________________________________

		/// <summary>
		/// Returns the index of the given search element in the array, starting from
		/// <paramref name="fromIndex"/>.
		/// </summary>
		/// <param name="thisObj"> The array that is being operated on. </param>
		/// <param name="searchElement"> The value to search for. </param>
		/// <param name="fromIndex"> The array index to start searching. </param>
		/// <returns> The index of the given search element in the array, or <c>-1</c> if the
		/// element wasn't found. </returns>
		public static int IndexOf(ScriptEngine engine,Array thisObj, object searchElement, int fromIndex)
		{
			// Get the length of the array.
			uint arrayLength = GetLength(thisObj);

			// This method only supports arrays of length up to 2^31-1.
			if (arrayLength > int.MaxValue)
				throw new JavaScriptException(engine, "RangeError", "The array is too long");

			// If fromIndex is less than zero, it is an offset from the end of the array.
			if (fromIndex < 0)
				fromIndex += (int)arrayLength;

			for (int i = Math.Max(fromIndex, 0); i < arrayLength; i++)
			{
				// Get the value of the array element.
				object elementValue = thisObj[(uint)i];

				// Compare the given search element with the array element.
				if (elementValue != null && TypeComparer.StrictEquals(searchElement, elementValue) == true)
					return i;
			}

			// The search element wasn't found.
			return -1;
		}

		/// <summary>
		/// Returns the index of the given search element in the array, searching backwards from
		/// the end of the array.
		/// </summary>
		/// <param name="thisObj"> The array that is being operated on. </param>
		/// <param name="searchElement"> The value to search for. </param>
		/// <returns> The index of the given search element in the array, or <c>-1</c> if the
		/// element wasn't found. </returns>
		public static int LastIndexOf(ScriptEngine engine,Array thisObj, object searchElement)
		{
			return LastIndexOf(engine, thisObj, searchElement, int.MaxValue);
		}

		/// <summary>
		/// Returns the index of the given search element in the array, searching backwards from
		/// <paramref name="fromIndex"/>.
		/// </summary>
		/// <param name="thisObj"> The array that is being operated on. </param>
		/// <param name="searchElement"> The value to search for. </param>
		/// <param name="fromIndex"> The array index to start searching. </param>
		/// <returns> The index of the given search element in the array, or <c>-1</c> if the
		/// element wasn't found. </returns>
		public static int LastIndexOf(ScriptEngine engine, Array thisObj, object searchElement, int fromIndex)
		{
			// Get the length of the array.
			uint arrayLength = GetLength(thisObj);

			// This method only supports arrays of length up to 2^31-1.
			if (arrayLength > int.MaxValue)
				throw new JavaScriptException(engine, "RangeError", "The array is too long");

			// If fromIndex is less than zero, it is an offset from the end of the array.
			if (fromIndex < 0)
				fromIndex += (int)arrayLength;

			for (int i = Math.Min((int)arrayLength - 1, fromIndex); i >= 0; i--)
			{
				// Get the value of the array element.
				object elementValue = thisObj[(uint)i];

				// Compare the given search element with the array element.
				if (elementValue != null && TypeComparer.StrictEquals(searchElement, elementValue) == true)
					return i;
			}

			// The search element wasn't found.
			return -1;
		}

		/// <summary>
		/// Determines if every element of the array matches criteria defined by the given user-
		/// defined function.
		/// </summary>
		/// <param name="thisObj"> The array that is being operated on. </param>
		/// <param name="callbackFunction"> A user-defined function that is called for each element in the
		/// array.  This function is called with three arguments: the value of the element, the
		/// index of the element, and the array that is being operated on.  The function should
		/// return <c>true</c> or <c>false</c>. </param>
		/// <param name="context"> The value of <c>this</c> in the context of the callback function. </param>
		/// <returns> <c>true</c> if every element of the array matches criteria defined by the
		/// given user-defined function; <c>false</c> otherwise. </returns>
		public static bool Every(ScriptEngine engine, Array thisObj, FunctionInstance callbackFunction, ObjectInstance context)
		{
			// callbackFunction must be a valid function.
			if (callbackFunction == null)
				throw new JavaScriptException(engine, "TypeError", "Invalid callback function");

			// Get the length of the array.
			uint arrayLength = GetLength(thisObj);

			// This method only supports arrays of length up to 2^31-1.
			if (arrayLength > int.MaxValue)
				throw new JavaScriptException(engine, "RangeError", "The array is too long");

			for (int i = 0; i < arrayLength; i ++)
			{
				// Get the value of the array element.
				object elementValue = thisObj[(uint)i];

				// Only call the callback function for array elements that exist in the array.
				if (elementValue != null)
				{
					// Call the callback function.
					if (TypeConverter.ToBoolean(callbackFunction.CallLateBound(engine,context, elementValue, i, thisObj)) == false)
						return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Determines if at least one element of the array matches criteria defined by the given
		/// user-defined function.
		/// </summary>
		/// <param name="thisObj"> The array that is being operated on. </param>
		/// <param name="callbackFunction"> A user-defined function that is called for each element in the
		/// array.  This function is called with three arguments: the value of the element, the
		/// index of the element, and the array that is being operated on.  The function should
		/// return <c>true</c> or <c>false</c>. </param>
		/// <param name="context"> The value of <c>this</c> in the context of the callback function. </param>
		/// <returns> <c>true</c> if at least one element of the array matches criteria defined by
		/// the given user-defined function; <c>false</c> otherwise. </returns>
		public static bool Some(ScriptEngine engine,Array thisObj, FunctionInstance callbackFunction, ObjectInstance context)
		{
			// callbackFunction must be a valid function.
			if (callbackFunction == null)
				throw new JavaScriptException(engine, "TypeError", "Invalid callback function");

			// Get the length of the array.
			uint arrayLength = GetLength(thisObj);

			// This method only supports arrays of length up to 2^31-1.
			if (arrayLength > int.MaxValue)
				throw new JavaScriptException(engine, "RangeError", "The array is too long");

			for (int i = 0; i < arrayLength; i++)
			{
				// Get the value of the array element.
				object elementValue = thisObj[(uint)i];

				// Only call the callback function for array elements that exist in the array.
				if (elementValue != null)
				{
					// Call the callback function.
					if (TypeConverter.ToBoolean(callbackFunction.CallLateBound(engine,context, elementValue, i, thisObj)) == true)
						return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Calls the given user-defined function once per element in the array.
		/// </summary>
		/// <param name="thisObj"> The array that is being operated on. </param>
		/// <param name="callbackFunction"> A user-defined function that is called for each element in the
		/// array.  This function is called with three arguments: the value of the element, the
		/// index of the element, and the array that is being operated on. </param>
		/// <param name="context"> The value of <c>this</c> in the context of the callback function. </param>
		public static void ForEach(ScriptEngine engine,Array thisObj, FunctionInstance callbackFunction, ObjectInstance context)
		{
			// callbackFunction must be a valid function.
			if (callbackFunction == null)
				throw new JavaScriptException(engine, "TypeError", "Invalid callback function");

			// Get the length of the array.
			uint arrayLength = GetLength(thisObj);

			// This method only supports arrays of length up to 2^31-1.
			if (arrayLength > int.MaxValue)
				throw new JavaScriptException(engine, "RangeError", "The array is too long");

			for (int i = 0; i < arrayLength; i++)
			{
				// Get the value of the array element.
				object elementValue = thisObj[(uint)i];

				// Only call the callback function for array elements that exist in the array.
				if (elementValue != null)
				{
					// Call the callback function.
					callbackFunction.CallLateBound(engine,context, elementValue, i, thisObj);
				}
			}
		}

		/// <summary>
		/// Creates a new array with the results of calling the given function on every element in
		/// this array.
		/// </summary>
		/// <param name="thisObj"> The array that is being operated on. </param>
		/// <param name="callbackFunction"> A user-defined function that is called for each element
		/// in the array.  This function is called with three arguments: the value of the element,
		/// the index of the element, and the array that is being operated on.  The value that is
		/// returned from this function is stored in the resulting array. </param>
		/// <param name="context"> The value of <c>this</c> in the context of the callback function. </param>
		/// <returns> A new array with the results of calling the given function on every element
		/// in the array. </returns>
		public static Nitrassic.Library.Array Map(ScriptEngine engine,Array thisObj, FunctionInstance callbackFunction, ObjectInstance context)
		{
			// callbackFunction must be a valid function.
			if (callbackFunction == null)
				throw new JavaScriptException(engine, "TypeError", "Invalid callback function");

			// Get the length of the array.
			uint arrayLength = GetLength(thisObj);

			// This method only supports arrays of length up to 2^31-1.
			if (arrayLength > int.MaxValue)
				throw new JavaScriptException(engine, "RangeError", "The array is too long");

			// Create a new array to hold the new values.
			// The length of the output array is always equal to the length of the input array.
			var resultArray = new Nitrassic.Library.Array(arrayLength, arrayLength);

			for (int i = 0; i < arrayLength; i++)
			{
				// Get the value of the array element.
				object elementValue = thisObj[(uint)i];

				// Only call the callback function for array elements that exist in the array.
				if (elementValue != null)
				{
					// Call the callback function.
					object result = callbackFunction.CallLateBound(engine,context, elementValue, i, thisObj);

					// Store the result.
					resultArray[(uint)i] = result;
				}
			}

			return resultArray;
		}

		/// <summary>
		/// Returns the first element in the given array that passes the test implemented by the
		/// given function.
		/// </summary>
		/// <param name="thisObj"> The array that is being operated on. </param>
		/// <param name="callbackFunction"> A user-defined function that is called for each element in the
		/// array.  This function is called with three arguments: the value of the element, the
		/// index of the element, and the array that is being operated on.  The function should
		/// return <c>true</c> or <c>false</c>. </param>
		/// <param name="context"> The value of <c>this</c> in the context of the callback function. </param>
		/// <returns> The first element that results in the callback returning <c>true</c>. </returns>
		public static object Find(ScriptEngine engine,Array thisObj, FunctionInstance callbackFunction, ObjectInstance context)
		{
			// callbackFunction must be a valid function.
			if (callbackFunction == null)
				throw new JavaScriptException(engine, "TypeError", "Invalid callback function");

			// Get the length of the array.
			uint arrayLength = GetLength(thisObj);

			// This method only supports arrays of length up to 2^31-1.
			if (arrayLength > int.MaxValue)
				throw new JavaScriptException(engine, "RangeError", "The array is too long");

			for (int i = 0; i < arrayLength; i++)
			{
				// Get the value of the array element.
				object elementValue = thisObj[(uint)i];

				// Only call the callback function for array elements that exist in the array.
				if (elementValue != null)
				{
					// Call the callback function.
					bool result = TypeConverter.ToBoolean(callbackFunction.CallLateBound(engine,context, elementValue, i, thisObj));

					// Return if the result was true.
					if (result == true)
						return elementValue;
				}
			}

			// No matches, return undefined.
			return Undefined.Value;
		}

		/// <summary>
		/// Creates a new array with the elements from this array that pass the test implemented by
		/// the given function.
		/// </summary>
		/// <param name="thisObj"> The array that is being operated on. </param>
		/// <param name="callbackFunction"> A user-defined function that is called for each element in the
		/// array.  This function is called with three arguments: the value of the element, the
		/// index of the element, and the array that is being operated on.  The function should
		/// return <c>true</c> or <c>false</c>. </param>
		/// <param name="context"> The value of <c>this</c> in the context of the callback function. </param>
		/// <returns> A copy of this array but with only those elements which produce <c>true</c>
		/// when passed to the provided function. </returns>
		public static Nitrassic.Library.Array Filter(ScriptEngine engine,Array thisObj, FunctionInstance callbackFunction, ObjectInstance context)
		{
			// callbackFunction must be a valid function.
			if (callbackFunction == null)
				throw new JavaScriptException(engine, "TypeError", "Invalid callback function");

			// Get the length of the array.
			uint arrayLength = GetLength(thisObj);

			// This method only supports arrays of length up to 2^31-1.
			if (arrayLength > int.MaxValue)
				throw new JavaScriptException(engine, "RangeError", "The array is too long");

			// Create a new array to hold the new values.
			var result = Nitrassic.Library.Array.New(engine);

			for (int i = 0; i < arrayLength; i++)
			{
				// Get the value of the array element.
				object elementValue = thisObj[(uint)i];

				// Only call the callback function for array elements that exist in the array.
				if (elementValue != null)
				{
					// Call the callback function.
					bool includeInArray = TypeConverter.ToBoolean(callbackFunction.CallLateBound(engine,context, elementValue, i, thisObj));

					// Store the result if the callback function returned true.
					if (includeInArray == true)
						result.Push(engine,elementValue);
				}
			}
			return result;
		}

		/// <summary>
		/// Accumulates a single value by calling a user-defined function for each element.
		/// </summary>
		/// <param name="thisObj"> The array that is being operated on. </param>
		/// <param name="callbackFunction"> A user-defined function that is called for each element
		/// in the array.  This function is called with four arguments: the current accumulated
		/// value, the value of the element, the index of the element, and the array that is being
		/// operated on.  The return value for this function is the new accumulated value and is
		/// passed to the next invocation of the function. </param>
		/// <param name="initialValue"> The initial accumulated value. </param>
		/// <returns> The accumulated value returned from the last invocation of the callback
		/// function. </returns>
		public static object Reduce(ScriptEngine engine,Array thisObj, FunctionInstance callbackFunction, object initialValue)
		{
			// callbackFunction must be a valid function.
			if (callbackFunction == null)
				throw new JavaScriptException(engine, "TypeError", "Invalid callback function");

			// Get the length of the array.
			uint arrayLength = GetLength(thisObj);

			// This method only supports arrays of length up to 2^31-1.
			if (arrayLength > int.MaxValue)
				throw new JavaScriptException(engine, "RangeError", "The array is too long");

			// If an initial value is not provided, the initial value is the first (defined) element.
			int i = 0;
			object accumulatedValue = initialValue;
			if (accumulatedValue == null)
			{
				// Scan for a defined element.
				for (; i < arrayLength; i++)
				{
					if (thisObj[(uint)i] != null)
					{
						accumulatedValue = thisObj[(uint)(i++)];
						break;
					}
				}
				if (accumulatedValue == null)
					throw new JavaScriptException(engine, "TypeError", "Reduce of empty array with no initial value");
			}

			// Scan from low to high.
			for (; i < arrayLength; i++)
			{
				// Get the value of the array element.
				object elementValue = thisObj[(uint)i];

				// Only call the callback function for array elements that exist in the array.
				if (elementValue != null)
				{
					// Call the callback function.
					accumulatedValue = callbackFunction.CallLateBound(engine,Undefined.Value, accumulatedValue, elementValue, i, thisObj);
				}
			}

			return accumulatedValue;
		}

		/// <summary>
		/// Accumulates a single value by calling a user-defined function for each element
		/// (starting with the last element in the array).
		/// </summary>
		/// <param name="thisObj"> The array that is being operated on. </param>
		/// <param name="callbackFunction"> A user-defined function that is called for each element
		/// in the array.  This function is called with four arguments: the current accumulated
		/// value, the value of the element, the index of the element, and the array that is being
		/// operated on.  The return value for this function is the new accumulated value and is
		/// passed to the next invocation of the function. </param>
		/// <param name="initialValue"> The initial accumulated value. </param>
		/// <returns> The accumulated value returned from the last invocation of the callback
		/// function. </returns>
		public static object ReduceRight(ScriptEngine engine,Array thisObj, FunctionInstance callbackFunction, object initialValue)
		{
			// callbackFunction must be a valid function.
			if (callbackFunction == null)
				throw new JavaScriptException(engine, "TypeError", "Invalid callback function");

			// Get the length of the array.
			uint arrayLength = GetLength(thisObj);

			// This method only supports arrays of length up to 2^31-1.
			if (arrayLength > int.MaxValue)
				throw new JavaScriptException(engine, "RangeError", "The array is too long");

			// If an initial value is not provided, the initial value is the last (defined) element.
			int i = (int)arrayLength - 1;
			object accumulatedValue = initialValue;
			if (accumulatedValue == null)
			{
				// Scan for a defined element.
				for (; i >= 0; i--)
				{
					if (thisObj[(uint)i] != null)
					{
						accumulatedValue = thisObj[(uint)(i--)];
						break;
					}
				}
				if (accumulatedValue == null)
					throw new JavaScriptException(engine, "TypeError", "Reduce of empty array with no initial value");
			}

			// Scan from high to to low.
			for (; i >= 0; i--)
			{
				// Get the value of the array element.
				object elementValue = thisObj[(uint)i];

				// Only call the callback function for array elements that exist in the array.
				if (elementValue != null)
				{
					// Call the callback function.
					accumulatedValue = callbackFunction.CallLateBound(engine,Undefined.Value, accumulatedValue, elementValue, i, thisObj);
				}
			}

			return accumulatedValue;
		}



		//	 PRIVATE IMPLEMENTATION METHODS
		//_________________________________________________________________________________________

		/// <summary>
		/// Gets the number of items in the array.
		/// </summary>
		/// <param name="thisObj"> The array that is being operated on. </param>
		/// <returns> The number of items in the array. </returns>
		private static uint GetLength(Array thisObj)
		{
			return thisObj.length;
			// return TypeConverter.ToUint32(thisObj["length"]);
		}

		/// <summary>
		/// Sets the number of items in the array.
		/// </summary>
		/// <param name="thisObj"> The array that is being operated on. </param>
		/// <param name="value"> The new value of the length property. </param>
		private static void SetLength(ScriptEngine engine,Array thisObj, uint value)
		{
			set_Length(engine,thisObj,(int)value);
		}

		/// <summary>
		/// Enlarges the size of the dense array.
		/// </summary>
		/// <param name="newCapacity"> The new capacity of the array. </param>
		/// <param name="length"> The valid number of items in the array. </param>
		private void ResizeDenseArray(uint newCapacity, uint length)
		{
			if (newCapacity < length)
				throw new InvalidOperationException("Cannot resize smaller than the length property.");
			var resizedArray = new object[(int)newCapacity];
			System.Array.Copy(this.dense, resizedArray, (int)length);
			this.dense = resizedArray;
		}

		/// <summary>
		/// Sorts a array using the quicksort algorithm.
		/// </summary>
		/// <param name="array"> The array to sort. </param>
		/// <param name="comparer"> A comparison function. </param>
		/// <param name="start"> The first index in the range. </param>
		/// <param name="end"> The last index in the range. </param>
		private static void QuickSort(Array array, OnCompare comparer, uint start, uint end)
		{
			if (end - start < 30)
			{
				// Insertion sort is faster than quick sort for small arrays.
				InsertionSort(array, comparer, start, end);
				return;
			}

			// Choose a random pivot.
			uint pivotIndex = start + (uint)(MathObject.Random() * (end - start));

			// Get the pivot value.
			object pivotValue = array[pivotIndex];

			// Send the pivot to the back.
			Swap(array, pivotIndex, end);

			// Sweep all the low values to the front of the array and the high values to the back
			// of the array.  This version of quicksort never gets into an infinite loop even if
			// the comparer function is not consistent.
			uint newPivotIndex = start;
			for (uint i = start; i < end; i++)
			{
				if (comparer(array[i], pivotValue) <= 0.0)
				{
					Swap(array, i, newPivotIndex);
					newPivotIndex++;
				}
			}

			// Swap the pivot back to where it belongs.
			Swap(array, end, newPivotIndex);

			// Quick sort the array to the left of the pivot.
			if (newPivotIndex > start)
				QuickSort(array, comparer, start, newPivotIndex - 1);

			// Quick sort the array to the right of the pivot.
			if (newPivotIndex < end)
				QuickSort(array, comparer, newPivotIndex + 1, end);
		}
		
		/// <summary>
		/// Sorts a array using the insertion sort algorithm.
		/// </summary>
		/// <param name="array"> The array to sort. </param>
		/// <param name="comparer"> A comparison function. </param>
		/// <param name="start"> The first index in the range. </param>
		/// <param name="end"> The last index in the range. </param>
		private static void InsertionSort(Array array, OnCompare comparer, uint start, uint end)
		{
			for (uint i = start + 1; i <= end; i++)
			{
				object value = array[i];
				uint j;
				for (j = i - 1; j > start && comparer(array[j], value) > 0; j--)
					array[j + 1] = array[j];

				// Normally the for loop above would continue until j < start but since we are
				// using uint it doesn't work when start == 0.  Therefore the for loop stops one
				// short of start then the extra loop iteration runs below.
				if (j == start && comparer(array[j], value) > 0.0)
				{
					array[j + 1] = array[j];
					j--;
				}

				array[j + 1] = value;
			}
		}

		/// <summary>
		/// Swaps the elements at two locations in the array.
		/// </summary>
		/// <param name="array"> The array object. </param>
		/// <param name="index1"> The location of the first element. </param>
		/// <param name="index2"> The location of the second element. </param>
		private static void Swap(Array array, uint index1, uint index2)
		{
			object temp = array[index1];
			array[index1] = array[index2];
			array[index2] = temp;
		}
		
		
		/// <summary>
		/// Creates a new Array instance.
		/// </summary>
		public static Nitrassic.Library.Array New(ScriptEngine engine)
		{
			return new Nitrassic.Library.Array(0, 10);
		}

		/// <summary>
		/// Creates a new Array instance.
		/// </summary>
		/// <param name="elements"> The initial elements of the new array. </param>
		public static Nitrassic.Library.Array New(ScriptEngine engine,object[] elements)
		{
			// Copy the array if it is not an object array (for example, if it is a string[]).
			if (elements.GetType() != typeof(object[]))
			{
				var temp = new object[elements.Length];
				System.Array.Copy(elements, temp, elements.Length);
				return new Nitrassic.Library.Array(elements);
			}

			return new Nitrassic.Library.Array(elements);
		}



		//	 JAVASCRIPT INTERNAL FUNCTIONS
		//_________________________________________________________________________________________
	
		/// <summary>
		/// Creates a new Array instance and initializes the contents of the array.
		/// Called when the new expression is used on this object, e.g. var x = new Array(length).
		/// </summary>
		/// <param name="elements"> The initial elements of the new array. </param>
		public static Nitrassic.Library.Array OnConstruct(ScriptEngine engine,params object[] elements)
		{
			if (elements.Length == 1)
			{
				if (TypeUtilities.IsNumeric(elements[0]))
				{
					double specifiedLength = TypeConverter.ToNumber(elements[0]);
					uint actualLength = TypeConverter.ToUint32(elements[0]);
					if (specifiedLength != (double)actualLength)
						throw new JavaScriptException(engine, "RangeError", "Invalid array length");
					return new Nitrassic.Library.Array(actualLength, actualLength);
				}
			}

			// Transform any nulls into undefined.
			for (int i = 0; i < elements.Length; i++)
				if (elements[i] == null)
					elements[i] = Undefined.Value;
		
			return New(engine,elements);
		}
		
		/// <summary>
		/// Tests if the given value is an Array instance.
		/// </summary>
		/// <param name="value"> The value to test. </param>
		/// <returns> <c>true</c> if the given value is an Array instance, <c>false</c> otherwise. </returns>
		public static bool IsArray(object value)
		{
			return value is Nitrassic.Library.Array;
		}
		
	}
}
