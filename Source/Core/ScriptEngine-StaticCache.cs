using System;

namespace Nitrassic
{
	public partial class ScriptEngine{
		
#if !WINDOWS_PHONE
		
		/// <summary>
		/// Gets or sets information needed by Reflection.Emit.
		/// </summary>
		internal ReflectionEmitModuleInfo ReflectionEmitInfo{
			get{
				if (reflectionEmitInfo == null)
				{
					reflectionEmitInfo = new ReflectionEmitModuleInfo(this);
				}
				return reflectionEmitInfo;
			}
		}
		
		/// <summary>
		/// Gets or sets information needed by Reflection.Emit.
		/// </summary>
		[NonSerialized]
		private ReflectionEmitModuleInfo reflectionEmitInfo;
#endif //!WINDOWS_PHONE
		
	}
	
}