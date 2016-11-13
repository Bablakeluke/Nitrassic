using System;


namespace Nitrassic.Compiler{
	
	/// <summary>
	/// Represents a variable declared in a scope.
	/// </summary>
	internal class DeclaredVariable : Variable
	{
		
		public Scope Scope;
		
		// The initial value of the variable (used for function declarations only).
		public Expression ValueAtTopOfScope;

		// true if the variable has been set with the initial value.
		public bool Initialized;
		
		// The storage container for the variable.
		public ILLocalVariable Store;
		
		// The statically-determined storage type for the variable.
		private Type _Type = null;
		
		internal override bool IsHoisted(Scope relativeTo)
		{
			return Scope!=relativeTo;
		}
		
		internal override Type Get(ILGenerator generator)
		{
			
			// Load the value in the variable:
			generator.LoadVariable(Store);
			
			return _Type;
			
		}
		
		internal override void Set(ILGenerator generator, OptimizationInfo optimizationInfo,bool rIU, Type valueType, SetValueMethod value)
		{
			
			if(_Type==null)
			{
				_Type=valueType;
			}
			else if(_Type!=valueType)
			{
				// Essentially declaring a new variable.
				_Type=valueType;
				Store=null;
			}
			
			// Declare an IL local variable if no storage location has been allocated yet.
			if (Store == null)
			{
				Store = generator.DeclareVariable(valueType, Name);
			}
			
			// Load the value:
			value(rIU);
			
			// Store the value in the variable.
			generator.StoreVariable(Store);
			
		}
		
		internal override Type Type
		{
			get
			{
				return _Type;
			}
			set
			{
				
				if(value==null || value==typeof(Nitrassic.Null)){
					// Null has no effect on variable types.
					return;
				}
				
				if(_Type!=value)
				{
					Store=null;
				}
				
				_Type=value;
			}
		}
		
		
	}
	
}