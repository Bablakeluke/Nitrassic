using System;
using System.Collections.Generic;
using Nitrassic.Compiler;

namespace Nitrassic
{
	/// <summary>
	/// Represents the result of compiling a script.
	/// </summary>
	public sealed class CompiledScript
	{
		private GlobalMethodGenerator methodGen;

		internal CompiledScript(GlobalMethodGenerator methodGen)
		{
			if (methodGen == null)
				throw new ArgumentNullException("methodGen");
			this.methodGen = methodGen;
		}

		/// <summary>
		/// Executes the compiled script.
		/// </summary>
		public void Execute()
		{
			methodGen.Execute();
		}
	}
}
