﻿using System;
using System.Collections.Generic;
using Nitrassic.Library;
using System.Reflection;


namespace Nitrassic.Compiler
{
	/// <summary>
	/// A lookup for user defined function info.
	/// </summary>
	public static class MethodLookup
	{
		
		private static Dictionary<long, FunctionMethodGenerator> generatedMethodCache;
		private static object cacheLock = new object();
		private static long generatedMethodID;
		private const int compactGeneratedCacheCount = 100;

		/// <summary>
		/// Retrieves the code for a generated method, given the ID.  For internal use only.
		/// </summary>
		/// <param name="id"> The ID of the generated method. </param>
		/// <returns> A <c>FunctionMethodGenerator</c> instance. </returns>
		public static FunctionMethodGenerator LoadGenerator(long id)
		{
			lock (cacheLock)
			{
				if (generatedMethodCache == null)
					throw new InvalidOperationException("Internal error: no generated method cache available.");
				FunctionMethodGenerator generatedMethod;
				if (generatedMethodCache.TryGetValue(id, out generatedMethod) == false)
					throw new InvalidOperationException(string.Format("Internal error: generated method {0} was garbage collected.", id));
				
				return generatedMethod;
			}
		}
		
		public static long GetNextID()
		{
			lock (cacheLock)
			{
				
				// Create a cache (if it hasn't already been created).
				if (generatedMethodCache == null)
					generatedMethodCache = new Dictionary<long, FunctionMethodGenerator>();

				long id=generatedMethodID;
				
				// Increment the ID for next time.
				generatedMethodID++;
				
				/*
				// Every X calls to this method, compact the cache by removing any weak references that
				// point to objects that have been collected.
				if (generatedMethodID % compactGeneratedCacheCount == 0)
				{
					// Remove any weak references that have expired.
					var expiredIDs = new List<long>();
					foreach (var pair in generatedMethodCache)
						if (pair.Value.Target == null)
							expiredIDs.Add(pair.Key);
					foreach (int expiredID in expiredIDs)
						generatedMethodCache.Remove(expiredID);
				}
				*/
				
				// Return the ID that was allocated.
				return id;
			
			}
			
		}
		
		/// <summary>
		/// A cache for the Search method.
		/// </summary>
		internal static Dictionary<MethodInfo,UserDefinedFunction> FastSearchCache;
		
		/// <summary>
		/// Counts how often search is called.
		/// </summary>
		// private static int SearchCount;
		
		/// <summary>
		/// A (slow!) search for the given MethodInfo. Used to find callee. Avoid unless expected to call repeatedly.
		/// </summary>
		public static UserDefinedFunction Search(MethodInfo mtd)
		{
			
			UserDefinedFunction udf;
			
			if(FastSearchCache!=null)
			{
				
				if(FastSearchCache.TryGetValue(mtd,out udf))
				{
					return udf;
				}
				
			}
			
			// SearchCount++;
			
			foreach(KeyValuePair<long,FunctionMethodGenerator> kvp in generatedMethodCache)
			{
				
				FunctionMethodGenerator fmg=kvp.Value;
				
				if(fmg==null)
				{
					continue;
				}
				
				// Check if result uses the given method info.
				udf=fmg.Find(mtd);
				
				if(udf!=null)
				{
					// Found it!
					
					if(FastSearchCache==null)
					{
						FastSearchCache=new Dictionary<MethodInfo,UserDefinedFunction>();
					}
					
					// Add to cache:
					FastSearchCache[mtd]=udf;
					
					return udf;
				}
				
			}
			
			return null;
			
		}
		
		/// <summary>
		/// Saves the given generated method and returns an ID.  For internal use only.
		/// </summary>
		/// <param name="generatedMethod"> The generated method to save. </param>
		/// <returns> The ID that was associated with the generated method. </returns>
		public static void SaveAs(long id,FunctionMethodGenerator generatedMethod)
		{
			if (generatedMethod == null)
				throw new ArgumentNullException("generatedMethod");
			
			// Add to cache
			generatedMethodCache.Add(id, generatedMethod);
			
		}
	}
}
