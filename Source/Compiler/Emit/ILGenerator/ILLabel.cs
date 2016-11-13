using System;
using System.Collections.Generic;

namespace Nitrassic.Compiler
{
	/// <summary>
	/// Represents a label in IL code.
	/// </summary>
	public abstract class ILLabel
	{
	}
	
	/// <summary>
	/// Represents a label in IL code.
	/// </summary>
	public class ReflectionEmitILLabel : ILLabel
	{
		/// <summary>
		/// Creates a new label instance.
		/// </summary>
		/// <param name="label"> The underlying label. </param>
		public ReflectionEmitILLabel(System.Reflection.Emit.Label label)
		{
			if (label == null)
				throw new ArgumentNullException("label");
			this.UnderlyingLabel = label;
		}

		/// <summary>
		/// Gets the underlying label.
		/// </summary>
		public System.Reflection.Emit.Label UnderlyingLabel;
	}

}
