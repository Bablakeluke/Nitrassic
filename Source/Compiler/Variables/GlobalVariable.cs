using System;
using System.Collections;
using System.Collections.Generic;
using Nitrassic.Library;
using System.Reflection;


namespace Nitrassic.Compiler
{
	
	/// <summary>
	/// Represents a variable declared in the global scope.
	/// </summary>
	
	public class GlobalVariable : Variable
	{
		
		/// <summary>The window this variable is declared in.</summary>
		internal Window Window;
		/// <summary>The field holding the value.</summary>
		internal FieldInfo Field;
		/// <summary>Compiled functions which use this variable.
		/// If the type changes, they essentially get recompiled.</summary>
		internal List<UserDefinedFunction> Users;
		
		
		internal GlobalVariable(string name,Window window):base(name)
		{
			Window=window;
		}
		
		/// <summary>
		/// The engine that this variable is declared in.
		/// </summary>
		internal ScriptEngine Engine
		{
			get{
				return Window.Engine;
			}
		}
		
		/// <summary>Gets the current global value.</summary>
		internal object GetValue()
		{
			if(Field==null)
			{
				throw new JavaScriptException(Engine,"TypeError","Global value has not been setup correctly.");
			}
			
			Close();
			
			// Read from the field:
			return Field.GetValue(null);
		}
		
		/// <summary>Sets the global to the given value.</summary>
		internal void SetValue(object value)
		{
			Close();
			
			// Set the field value:
			Field.SetValue(null,value);
			
		}
		
		/// <summary>Closes the type builder if it's still open.</summary>
		private void Close()
		{
			
			if(Field is System.Reflection.Emit.FieldBuilder)
			{
				
				// Get the builder:
				var builder=(Field.DeclaringType as System.Reflection.Emit.TypeBuilder);
				
				// Get the host type:
				Type hostType=builder.CreateType();
				
				if(builder==Engine.ReflectionEmitInfo.MainType)
				{
					// Clear the main type:
					Engine.ReflectionEmitInfo.MainType=null;
				}
				
				// Get the new field:
				Field=hostType.GetField(Field.Name);
				
			}
			
		}
		
		/// <summary>The method info, if this is a method.</summary>
		internal MethodInfo Method
		{
			get
			{
				return GetValue() as MethodInfo;
			}
		}
		
		internal override Type Get(ILGenerator generator)
		{
			
			if(Field==null)
			{
				
				// Not actually been set yet; field unknown!
				EmitHelpers.EmitUndefined(generator);
				
				return typeof(Undefined);
				
			}else
			{
			
				// Load the field:
				generator.LoadField(Field);
				
				// Note: Collapsed variables are processed elsewhere.
				
			}
			
			return Type;
			
		}
		
		internal override void Set(ILGenerator generator, OptimizationInfo optimizationInfo, Type valueType, SetValueMethod value)
		{
			
			// Update type:
			Type=valueType;
			
			// Grab the value:
			value();
			
			// Store into it:
			generator.StoreField(Field);
			
		}
		
		/// <summary>Sets this global to the given type, or gets its current type.
		/// Note that if a global is requested to change type, it 'collapses' and becomes typeof(object).</summary>
		internal override Type Type
		{
			get
			{
				if(Field==null)
				{
					return null;
				}
				
				return Field.FieldType;
			}
			set
			{
				
				if(Field==null)
				{
					
					// Declare it now:
					Field=Engine.ReflectionEmitInfo.CreateGlobal(value);
					
				}else if(Field.FieldType!=value)
				{
					
					if(Engine.CollapseWarning)
					{
						Engine.Log("Warning: Global variable '"+Name+"' collapsed. This often triggers recompiles. Avoid setting a global to different types.");
					}
					
					// Must redeclare this field and request a recompile of all using methods.
					Field=Engine.ReflectionEmitInfo.CreateGlobal(typeof(object));
					
					if(Users==null)
					{
						return;
					}
					
					int count=Users.Count;
					
					for(int i=0;i<count;i++)
					{
						
						UserDefinedFunction current=Users[i];
						
						#warning request current to recompile if its entirely compiled.
						// Important Note: Set MUST be called before any IL is emitted.
						// Aka it must have resolved all types before emitting any IL.
						// (This is because a parent scope may have already emitted code handling this global
						// and now its being told to recompile already)
						
					}
					
				}
			}
		}
		
		/// <summary>Adds the given function as a user of this global.</summary>
		internal void AddUser(UserDefinedFunction user)
		{
			
			if(Field!=null && Field.FieldType==typeof(object))
			{
				// Already 'collapsed' - no need to track users anymore.
				return;
			}
			
			if(Users==null)
			{
				Users=new List<UserDefinedFunction>();
			}
			
			Users.Add(user);
			
		}
		
	}
	
	
}