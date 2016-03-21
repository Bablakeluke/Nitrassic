using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;

namespace Nitrassic
{
	/// <summary>
	/// Represents a wrapper for javascript error objects.
	/// </summary>
	[Serializable]
	public class JavaScriptException : Exception
	{

		//	 INITIALIZATION
		//_________________________________________________________________________________________

		/// <summary>
		/// Creates a new JavaScriptException instance based on the given object.
		/// </summary>
		/// <param name="errorObject"> The javascript object that was thrown. </param>
		/// <param name="lineNumber"> The line number in the source file the error occurred on. </param>
		/// <param name="sourcePath"> The path or URL of the source file.  Can be <c>null</c>. </param>
		public JavaScriptException(object errorObject, int lineNumber, string sourcePath)
			: base(TypeConverter.ToString(errorObject))
		{
			this.ErrorObject = errorObject;
			this.LineNumber = lineNumber;
			this.SourcePath = sourcePath;
			this.PopulateStackTrace(0);
		}

		/// <summary>
		/// Creates a new JavaScriptException instance based on the given object.
		/// </summary>
		/// <param name="errorObject"> The javascript object that was thrown. </param>
		/// <param name="lineNumber"> The line number in the source file the error occurred on. </param>
		/// <param name="sourcePath"> The path or URL of the source file.  Can be <c>null</c>. </param>
		/// <param name="functionName"> The name of the function.  Can be <c>null</c>. </param>
		public JavaScriptException(object errorObject, int lineNumber, string sourcePath, string functionName)
			: base(TypeConverter.ToString(errorObject))
		{
			this.ErrorObject = errorObject;
			this.LineNumber = lineNumber;
			this.SourcePath = sourcePath;
			this.FunctionName = functionName;
			this.PopulateStackTrace(0);
		}

		/// <summary>
		/// Creates a new JavaScriptException instance.
		/// </summary>
		/// <param name="engine"> The script engine used to create the error object. </param>
		/// <param name="name"> The name of the error, e.g "RangeError". </param>
		/// <param name="message"> A description of the error. </param>
		public JavaScriptException(ScriptEngine engine, string name, string message)
			: this(engine, name, message, 0)
			{}

		/// <summary>
		/// Creates a new JavaScriptException instance.
		/// </summary>
		/// <param name="engine"> The script engine used to create the error object. </param>
		/// <param name="name"> The name of the error, e.g "RangeError". </param>
		/// <param name="message"> A description of the error. </param>
		public JavaScriptException(ScriptEngine engine, string name, string message, int depth)
			: base(string.Format("{0}: {1}", name, message))
		{
			this.ErrorObject = CreateError(engine, name, message);
			this.PopulateStackTrace(depth);
		}

		/// <summary>
		/// Creates a new JavaScriptException instance.
		/// </summary>
		/// <param name="engine"> The current script environment. </param>
		/// <param name="name"> The name of the error, e.g "RangeError". </param>
		/// <param name="message"> A description of the error. </param>
		/// <param name="innerException"> The exception that is the cause of the current exception,
		/// or <c>null</c> if no inner exception is specified. </param>
		public JavaScriptException(ScriptEngine engine, string name, string message, Exception innerException)
			: base(string.Format("{0}: {1}", name, message), innerException)
		{
			this.ErrorObject = CreateError(engine, name, message);
			this.PopulateStackTrace(0);
		}

		/// <summary>
		/// Creates a new JavaScriptException instance.
		/// </summary>
		/// <param name="engine"> The current script environment. </param>
		/// <param name="name"> The name of the error, e.g "RangeError". </param>
		/// <param name="message"> A description of the error. </param>
		/// <param name="lineNumber"> The line number in the source file the error occurred on. </param>
		/// <param name="sourcePath"> The path or URL of the source file.  Can be <c>null</c>. </param>
		public JavaScriptException(ScriptEngine engine, string name, string message, int lineNumber, string sourcePath)
			: base(string.Format("{0}: {1}", name, message))
		{
			this.ErrorObject = CreateError(engine, name, message);
			this.LineNumber = lineNumber;
			this.SourcePath = sourcePath;
			this.PopulateStackTrace(0);
		}

		/// <summary>
		/// Creates a new JavaScriptException instance.
		/// </summary>
		/// <param name="engine"> The current script environment. </param>
		/// <param name="name"> The name of the error, e.g "RangeError". </param>
		/// <param name="message"> A description of the error. </param>
		/// <param name="lineNumber"> The line number in the source file the error occurred on. </param>
		/// <param name="sourcePath"> The path or URL of the source file.  Can be <c>null</c>. </param>
		/// <param name="functionName"> The name of the function.  Can be <c>null</c>. </param>
		public JavaScriptException(ScriptEngine engine, string name, string message, int lineNumber, string sourcePath, string functionName)
			: base(string.Format("{0}: {1}", name, message))
		{
			this.ErrorObject = CreateError(engine, name, message);
			this.LineNumber = lineNumber;
			this.SourcePath = sourcePath;
			this.FunctionName = functionName;
			this.PopulateStackTrace(0);
		}



		//	 SERIALIZATION
		//_________________________________________________________________________________________

#if !SILVERLIGHT

		/// <summary>
		/// Initializes a new instance of the JavaScriptException class with serialized data.
		/// </summary>
		/// <param name="info"> The SerializationInfo that holds the serialized object data about
		/// the exception being thrown. </param>
		/// <param name="context"> The StreamingContext that contains contextual information about
		/// the source or destination. </param>
		protected JavaScriptException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			// State cannot be restored here because there is no way to serialize it without
			// falling afoul of .NET 4 security restrictions (Exception.GetObjectData is marked
			// as SecurityCritical).  The recommended workaround is to implement the
			// ISafeSerializationData interface, but since this library isn't targeting .NET 4
			// that doesn't work either!  Instead, we store the state in the Data dictionary
			// and let the Exception class serialize and deserialize everything.
		}

#endif



		//	 .NET ACCESSOR PROPERTIES
		//_________________________________________________________________________________________

		/// <summary>
		/// Gets a reference to the JavaScript Error object.
		/// </summary>
		public object ErrorObject
		{
			get { return this.Data["ErrorObject"]; }
			private set { this.Data["ErrorObject"] = value; }
		}

		/// <summary>
		/// Gets the type of error, e.g. "TypeError" or "SyntaxError".
		/// </summary>
		public string Name
		{
			get
			{
				if (this.ErrorObject is Library.Error)
					return ((Library.Error)this.ErrorObject).Name;
				return null;
			}
		}

		/// <summary>
		/// Gets the line number in the source file the error occurred on.  Can be <c>0</c> if no
		/// line number information is available.
		/// </summary>
		public int LineNumber
		{
			get
			{
				object line = this.Data["LineNumber"];
				if (line == null)
					return 0;
				return (int)line;
			}
			internal set { this.Data["LineNumber"] = value; }
		}

		/// <summary>
		/// Gets the path or URL of the source file.  Can be <c>null</c> if no source information
		/// is available.
		/// </summary>
		public string SourcePath
		{
			get { return (string)this.Data["SourcePath"]; }
			internal set { this.Data["SourcePath"] = value; }
		}

		/// <summary>
		/// Gets the name of the function where the exception occurred.  Can be <c>null</c> if no
		/// source information is available.
		/// </summary>
		public string FunctionName
		{
			get { return (string)this.Data["FunctionName"]; }
			internal set { this.Data["FunctionName"] = value; }
		}



		//	 PRIVATE IMPLEMENTATION METHODS
		//_________________________________________________________________________________________

		/// <summary>
		/// Creates an error object with the given message.
		/// </summary>
		/// <param name="engine"> The script engine used to create the error object. </param>
		/// <param name="name"> The name of the error, e.g "RangeError". </param>
		/// <param name="message"> A description of the error. </param>
		/// <returns> A new Error instance. </returns>
		private static Library.Error CreateError(ScriptEngine engine, string name, string message)
		{
			if (engine == null)
				throw new ArgumentNullException("engine");

			// Get the constructor corresponding to the error name.
			System.Reflection.MethodBase constructor;
			if (name == "Error")
				constructor = engine.Prototypes.Error;
			else if (name == "RangeError")
				constructor = engine.Prototypes.RangeError;
			else if (name == "TypeError")
				constructor = engine.Prototypes.TypeError;
			else if (name == "SyntaxError")
				constructor = engine.Prototypes.SyntaxError;
			else if (name == "URIError")
				constructor = engine.Prototypes.URIError;
			else if (name == "EvalError")
				constructor = engine.Prototypes.EvalError;
			else if (name == "ReferenceError")
				constructor = engine.Prototypes.ReferenceError;
			else
				throw new ArgumentException(string.Format("Unrecognised error type '{0}'.", name), "name");

			// Create an error instance.
			return (Library.Error)constructor.Invoke(null,new object[]{message});
		}

		/// <summary>
		/// Populates the error object stack trace, if the error object is an Error.
		/// </summary>
		internal void PopulateStackTrace(int depth)
		{
			// Ensure the error object is an Error or derived instance.
			var errorObject = this.ErrorObject as Library.Error;
			if (errorObject == null)
				return;
			errorObject.SetStackTrace(this.SourcePath, this.FunctionName, this.LineNumber, depth+2);
		}
	}
}
