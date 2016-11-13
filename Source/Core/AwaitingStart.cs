using System;
using System.Collections.Generic;
using Nitrassic.Library;

namespace Nitrassic{
	
	/// <summary>
	/// During compilation, global variables are not directly settable.
	/// So, we have to create a bunch of 'waiting' set objects which will
	/// set the global values when it's ready.
	/// </summary>
	
	internal class AwaitingStart{
		
		private PropertyVariable Variable;
		private object Value;
		private bool Global;
		
		
		public AwaitingStart(PropertyVariable variable,object value){
			
			Variable=variable;
			Value=value;
			
		}
		
		public AwaitingStart(PropertyVariable variable,object value,bool global){
			
			Variable=variable;
			Value=value;
			Global=global;
			
		}
		
		public void OnStart(ScriptEngine engine){
			
			if(Global){
				Value=engine.Global;
			}
			
			// Set now:
			Variable.SetValue(engine.Global,Value);
			
		}
	}
	
}