using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;


public static class Arguments{
	
	/// <summary>The args for the program. Note that the keys are lowercase.</summary>
	public static Dictionary<string,List<string>> Set;
	
	
	/// <summary>Loads the given arguments.</summary>
	public static void Load(string[] args){
		
		// Create args:
		Set=new Dictionary<string,List<string>>();
		
		// For each one..
		for(int i=0;i<args.Length;i++){
			
			// Grab the arg:
			string argument=args[i];
			
			// Split it by colon:
			string[] parts=argument.Split(new char[]{':'},2,StringSplitOptions.None);
			
			if(parts.Length==1){
				
				// Add to args:
				Add(parts[0],"");
					
			}else{
			
				// Add to args:
				Add(parts[0],parts[1]);
				
			}
			
		}
		
	}
	
	/// <summary>Adds the given argument to the set.</summary>
	private static void Add(string key,string value){
		
		// LC key:
		key=key.ToLower();
		
		// Get:
		List<string> values;
		
		if(!Set.TryGetValue(key,out values)){
			
			values=new List<string>();
			Set[key]=values;
			
		}
		
		// Add the value:
		values.Add(value);
		
	}
	
	/// <summary>Gets all the arguments which have no :value.</summary>
	public static List<string> GetSingle(){
		
		List<string> results=new List<string>();
		
		// For each arg..
		foreach(KeyValuePair<string,List<string>> kvp in Set){
			
			// One arg value?
			if(kvp.Value.Count==1){
				
				// Blank?
				if(kvp.Value[0]==""){
					
					// Add result:
					results.Add(kvp.Key);
					
				}
				
			}
			
		}
		
		return results;
		
	}
	
	/// <summary>Gets the latest value for the given argument. Given default if not found.</summary>
	public static string Get(string name,string def){
		
		// Exists?
		string result=Get(name);
		
		if(result==null){
			return def;
		}
		
		return result;
		
	}
		
	/// <summary>Gets the latest value for the given argument. Null if doesn't exist.</summary>
	public static string Get(string name){
		
		List<string> results;
		
		if(Set.TryGetValue(name,out results)){
			
			// Always at least 1 entry.
			return results[results.Count-1];
			
		}
		
		return null;
		
	}
	
	/// <summary>Gets the given argument as a number.</summary>
	public static int GetNumber(string name,int defaultValue){
		
		string val=Get(name);
		
		if(val==null){
			return defaultValue;
		}
		
		return int.Parse(val.Trim());
		
	}
	
	/// <summary>Gets all the values for the given argument. Null if doesn't exist.</summary>
	public static List<string> GetAll(string name){
		List<string> results;
		Set.TryGetValue(name,out results);
		return results;
	}
	
	/// <summary>Is the given arg set?</summary>
	public static bool IsSet(string name){
		
		return Set.ContainsKey(name);
		
	}
	
	/// <summary>Is the given arg set?</summary>
	public static bool Has(string name){
		
		return Set.ContainsKey(name);
		
	}
	
	/*
	/// <summary>Gets the latest value for the given argument. Null if doesn't exist.</summary>
	public static string this[string arg]{
		get{
			return Get(arg);
		}
	}
	*/
	
}