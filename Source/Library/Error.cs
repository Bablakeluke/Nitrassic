using System;
using System.Collections.Generic;
using System.Text;

namespace Nitrassic.Library
{
	
	[Serializable]
	public class RangeError : Error{
	
		public RangeError(string message):base(message){}
		
	}
	
	[Serializable]
	public class TypeError : Error{
	
		public TypeError(string message):base(message){}
		
	}
	
	[Serializable]
	public class SyntaxError : Error{
	
		public SyntaxError(string message):base(message){}
		
	}
	
	[Serializable]
	public class URIError : Error{
	
		public URIError(string message):base(message){}
		
	}
	
	[Serializable]
	public class EvalError : Error{
	
		public EvalError(string message):base(message){}
		
	}
	
	[Serializable]
	public class ReferenceError : Error{
		
		public ReferenceError(string message):base(message){}
		
	}
	
	/// <summary>
	/// Represents the base class of all the javascript errors.
	/// </summary>
	[Serializable]
	public partial class Error
	{

		//	 INITIALIZATION
		//_________________________________________________________________________________________
		
		/// <summary>
		/// Creates a new Error instance with the given name, message and optionally a stack trace.
		/// </summary>
		/// <param name="prototype"> The next object in the prototype chain. </param>
		/// <param name="name"> The initial value of the name property.  Pass <c>null</c> to avoid
		/// creating this property. </param>
		/// <param name="message"> The initial value of the message property.  Pass <c>null</c> to
		/// avoid creating this property. </param>
		internal Error(string message)
		{
			Name=GetType().Name;
			Message=message;
		}



		//	 .NET ACCESSOR PROPERTIES
		//_________________________________________________________________________________________
		
		/// <summary>
		/// Gets the name for the type of error.
		/// </summary>
		public string Name;
		
		/// <summary>
		/// Gets a human-readable description of the error.
		/// </summary>
		public string Message;

		/// <summary>
		/// Gets the stack trace.  Note that this is populated when the object is thrown, NOT when
		/// it is initialized.
		/// </summary>
		public string Stack;
		
		/// <summary>
		/// Sets the stack trace information.
		/// </summary>
		/// <param name="path"> The path of the javascript source file that is currently executing. </param>
		/// <param name="function"> The name of the currently executing function. </param>
		/// <param name="line"> The line number of the statement that is currently executing. </param>
		internal void SetStackTrace(ScriptEngine engine,int depth)
		{
			Stack=engine.FormatStackTrace(depth);
		}



		//	 JAVASCRIPT FUNCTIONS
		//_________________________________________________________________________________________

		/// <summary>
		/// Returns a string representing the current object.
		/// </summary>
		/// <returns> A string representing the current object. </returns>
		public override string ToString()
		{
			if (string.IsNullOrEmpty(this.Message))
				return this.Name;
			else if (string.IsNullOrEmpty(this.Name))
				return this.Message;
			else
				return string.Format("{0}: {1}", this.Name, this.Message);
		}
		
		/// <summary>
		/// Creates a new derived error instance with the given message.
		/// </summary>
		/// <param name="message"> A description of the error. </param>
		public static Error OnConstruct(ScriptEngine engine,string message)
		{
			if(message==null)
				message="";
			
			return new Error(message);
		}
		
		/// <summary>
		/// Creates a new derived error instance.
		/// </summary>
		/// <param name="message"> A description of the error. </param>
		public static Error OnCall(ScriptEngine engine){
			Error e=new Error("");
			e.SetStackTrace(engine,2);
			return e;
		}
		
		/// <summary>
		/// Creates a new derived error instance.
		/// </summary>
		/// <param name="message"> A description of the error. </param>
		public static Error OnCall(ScriptEngine engine,string message){
			Error e=new Error(message);
			e.SetStackTrace(engine,2);
			return e;
		}
		
	}
}
