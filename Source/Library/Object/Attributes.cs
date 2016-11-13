using System;

namespace Nitrassic.Library
{
	/// <summary>
	/// The base class of the javascript function attributes.
	/// </summary>
	// [AttributeUsage(AllowMultiple = false)]
	public class JSProperties : Attribute
	{
		/// <summary>
		/// Get or sets the name of the function, as exposed to javascript.
		/// </summary>
		public string Name;
		
		/// <summary>
		/// True if this should not be exposed to JS.
		/// </summary>
		public bool Hidden;
		
		/// <summary>
		/// True if constructors should be hidden. I.e. it acts like a static class.
		/// </summary>
		public bool NoConstructors;
		
		public bool IsEnumerable;
		
		public bool IsConfigurable=true;
		
		public bool IsWritable=true;
		
		/// <summary>
		/// Used to automatically lowercase the first character of a method/field name.
		/// </summary>
		public CaseMode FirstCharacter=CaseMode.Auto;
		
		/// <summary>
		/// Used to automatically lowercase the first character of a types fields.
		/// </summary>
		public CaseMode FirstFieldCharacter=CaseMode.Auto;
		
	}
	
	/// <summary>Defines the case of a character.</summary>
	public enum CaseMode
	{
		/// <summary>From the stdlib, method names are lowercased and fields are 'as is' (unchanged).
		/// Anything not in the stdlib (Nitrassic.Library namespace) is 'as is' (unchanged).</summary>
		Auto,
		/// <summary>Always uppercase it.</summary>
		Upper,
		/// <summary>Always lowercase it.</summary>
		Lower,
		/// <summary>Leave the text as it is.</summary>
		Unchanged
	}
	
}
