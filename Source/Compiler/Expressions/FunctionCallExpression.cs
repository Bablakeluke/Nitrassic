using System;
using System.Collections.Generic;

namespace Nitrassic.Compiler
{
	/// <summary>
	/// Represents a function call expression.
	/// </summary>
	internal class FunctionCallExpression : OperatorExpression
	{
		
		/// <summary>
		/// The resolved method being called, if possible to directly resolve it.
		/// This is either a MethodBase or MethodGroup.
		/// </summary>
		public System.Reflection.MethodBase ResolvedMethod;
		
		/// <summary>
		/// Creates a new instance of FunctionCallJSExpression.
		/// </summary>
		/// <param name="operator"> The binary operator to base this expression on. </param>
		public FunctionCallExpression(Operator @operator)
			: base(@operator)
		{
		}

		/// <summary>
		/// Gets an expression that evaluates to the function instance.
		/// </summary>
		public Expression Target
		{
			get { return this.GetOperand(0); }
		}

		/// <summary>
		/// Gets the type that results from evaluating this expression.
		/// </summary>
		public override Type GetResultType(OptimizationInfo optimizationInfo)
		{
			
			if(ResolvedMethod==null)
			{
				Resolve(optimizationInfo);
			}
			
			// Clear ICC:
			optimizationInfo.IsConstructCall=false;
			
			if(ResolvedMethod==null)
			{
				// Totally unknown as it's resolved at runtime only.
				return typeof(object);
			}
			
			// Check if it's a constructor:
			System.Reflection.MethodInfo methodInfo=(ResolvedMethod as System.Reflection.MethodInfo);
			
			Type result=null;
			
			if(methodInfo==null)
			{
				// Constructor.
				result=(ResolvedMethod as System.Reflection.ConstructorInfo).DeclaringType;
			}
			else
			{
				// Normal method.
				result=methodInfo.ReturnType;
			}
			
			if(result==typeof(void))
			{
				return typeof(Nitrassic.Undefined);
			}
			
			return result;
		}
		
		/// <summary>
		/// Used to implement function calls without evaluating the left operand twice.
		/// </summary>
		private class TemporaryVariableExpression : Expression
		{
			private ILLocalVariable variable;

			public TemporaryVariableExpression(ILLocalVariable variable)
			{
				this.variable = variable;
			}

			public override Type GetResultType(OptimizationInfo optimizationInfo)
			{
				return typeof(object);
			}

			public override void GenerateCode(ILGenerator generator, OptimizationInfo optimizationInfo)
			{
				generator.LoadVariable(this.variable);
			}
		}
		
		/// <summary>
		/// Resolves which function is being called where possible.
		/// </summary>
		internal void Resolve(OptimizationInfo optimizationInfo)
		{
			
			object resolvedMethod=null;
			
			if (this.Target is MemberAccessExpression)
			{
				// The function is a member access expression (e.g. "Math.cos()").
				
				// Get the parent of the member (e.g. Math):
				MemberAccessExpression baseExpression = ((MemberAccessExpression)this.Target);
				
				// Ask it to resolve if needed:
				if(baseExpression.ResolvedProperty==null)
				{
					baseExpression.Resolve(optimizationInfo);
				}
				
				// Get the property:
				Nitrassic.Library.PropertyVariable property=baseExpression.ResolvedProperty;
				
				// It should be a callable method:
				if(property.Type==typeof(MethodGroup) || typeof(System.Reflection.MethodBase).IsAssignableFrom(property.Type))
				{
					// Great, grab the value:
					resolvedMethod=property.Value;
					
					if(resolvedMethod==null)
					{
						// This occurs when the method has collapsed.
						// The property is still a method though, so we know for sure it can be invoked.
						// It's now an instance property on the object.
						#warning runtime method property
					}
					
				}
				else if(property.Type!=typeof(object))
				{
					throw new JavaScriptException(
						optimizationInfo.Engine,
						"TypeError",
						"Cannot run '"+property.Name+"' as a method because it's a "+property.Type+"."
					);
				}
				else
				{
					#warning runtime resolve
					// Similar to above, but this time its something that might not even be a method
				}
			}
			
			if(resolvedMethod==null)
			{
				// Something else (e.g. "eval()")
				
				// Get the return type:
				Type returnType=Target.GetResultType(optimizationInfo);
				
				// Get the proto for it:
				Nitrassic.Library.Prototype proto=optimizationInfo.Engine.Prototypes.Get(returnType);
				
				// Note that these two special methods are always methods
				// or method groups so no checking is necessary.
				if(optimizationInfo.IsConstructCall)
				{
					resolvedMethod=proto.OnConstruct;
				}
				else
				{
					resolvedMethod=proto.OnCall;
				}
				
			}
			
			// Clear ICC:
			optimizationInfo.IsConstructCall=false;
			
			if(resolvedMethod==null)
			{
				// Runtime resolve only.
				ResolvedMethod=null;
				return;
			}
			
			// Note that it may be a MethodGroup, so let's resolve it further if needed.
			MethodGroup group=resolvedMethod as MethodGroup;
			
			if(group==null)
			{
				// It must be MethodBase - it can't be anything else:
				ResolvedMethod=resolvedMethod as System.Reflection.MethodBase;
			}
			else
			{
				// We have a group! Find the overload that we're after:
				Type[] argTypes=GetArgumentTypes(optimizationInfo);
				
				// Resolve it:
				ResolvedMethod=group.Match(argTypes);
			}
			
		}
		
		/// <summary>
		/// Generates CIL for the expression.
		/// </summary>
		/// <param name="generator"> The generator to output the CIL to. </param>
		/// <param name="optimizationInfo"> Information about any optimizations that should be performed. </param>
		public override void GenerateCode(ILGenerator generator, OptimizationInfo optimizationInfo)
		{
			// Check if this is a direct call to eval().
			NameExpression nameExpr=Target as NameExpression;
			
			if(ResolvedMethod==null)
			{
				// Try resolving it:
				Resolve(optimizationInfo);
			}
			
			if(ResolvedMethod!=null)
			{
				// We have a known method!
				
				// Emit the args:
				EmitArguments(generator,optimizationInfo);
			
				// Then the call!
				if(typeof(System.Reflection.ConstructorInfo).IsAssignableFrom(ResolvedMethod.GetType()))
				{
					// Actual constructor call:
					generator.NewObject(ResolvedMethod as System.Reflection.ConstructorInfo);
				}
				else
				{
					// Ordinary method:
					generator.Call(ResolvedMethod);
				}
				
				// Got a return type?
				Type returnType=GetResultType(optimizationInfo);
				
				if(returnType==typeof(Nitrassic.Undefined))
				{
					// Put undef on the stack:
					EmitHelpers.EmitUndefined(generator);
				}
				
			}
			else
			{
				// Either runtime resolve it or it's not actually a callable function
			}
			/*
			// Emit the function instance first.
			ILLocalVariable targetBase = null;
			if (this.Target is MemberAccessExpression)
			{
				// The function is a member access expression (e.g. "Math.cos()").

				// Evaluate the left part of the member access expression.
				var baseExpression = ((MemberAccessExpression)this.Target).Base;
				baseExpression.GenerateCode(generator, optimizationInfo);
				EmitConversion.ToAny(generator, baseExpression.GetResultType(optimizationInfo));
				targetBase = generator.CreateTemporaryVariable(typeof(object));
				generator.StoreVariable(targetBase);

				// Evaluate the right part of the member access expression.
				var memberAccessExpression = new MemberAccessExpression(((MemberAccessExpression)this.Target).Operator);
				memberAccessExpression.Push(new TemporaryVariableExpression(targetBase));
				memberAccessExpression.Push(((MemberAccessExpression)this.Target).GetOperand(1));
				memberAccessExpression.GenerateCode(generator, optimizationInfo);
				EmitConversion.ToAny(generator, this.Target.GetResultType(optimizationInfo));
			}
			else
			{
				// Something else (e.g. "eval()").
				this.Target.GenerateCode(generator, optimizationInfo);
				EmitConversion.ToAny(generator, this.Target.GetResultType(optimizationInfo));
			}

			// Check the object really is a function - if not, throw an exception.
			generator.IsInstance(typeof(Library.FunctionInstance));
			generator.Duplicate();
			var endOfTypeCheck = generator.CreateLabel();
			generator.BranchIfNotNull(endOfTypeCheck);

			// Throw an nicely formatted exception.
			generator.Pop();
			
			if(nameExpr==null){
				EmitHelpers.EmitThrow(generator, "TypeError", "'[Object]' is not a function");
			}else{
				EmitHelpers.EmitThrow(generator, "TypeError", "'"+nameExpr.Name+"' is not a function");
			}
			
			generator.DefineLabelPosition(endOfTypeCheck);
			
			// Generate code to produce the "this" value.  There are three cases.
			if (this.Target is NameExpression)
			{
				// 1. The function is a name expression (e.g. "parseInt()").
				//	In this case this = scope.ImplicitThisValue, if there is one, otherwise undefined.
				((NameExpression)this.Target).GenerateThis(generator);
			}
			else if (this.Target is MemberAccessExpression)
			{
				// 2. The function is a member access expression (e.g. "Math.cos()").
				//	In this case this = Math.
				//var baseExpression = ((MemberAccessExpression)this.Target).Base;
				//baseExpression.GenerateCode(generator, optimizationInfo);
				//EmitConversion.ToAny(generator, baseExpression.GetResultType(optimizationInfo));
				generator.LoadVariable(targetBase);
			}
			else
			{
				// 3. Neither of the above (e.g. "(function() { return 5 })()")
				//	In this case this = undefined.
				EmitHelpers.EmitUndefined(generator);
			}

			// Emit an array containing the function arguments.
			GenerateArgumentsArray(generator, optimizationInfo);

			// Call FunctionInstance.CallLateBound(thisValue, argumentValues)
			generator.Call(ReflectionHelpers.FunctionInstance_CallLateBound);

			// Allow reuse of the temporary variable.
			if (targetBase != null)
				generator.ReleaseTemporaryVariable(targetBase);
			*/
		}
		
		/// <summary>
		/// Emits the arguments set.
		/// </summary>
		internal void EmitArguments(ILGenerator generator,OptimizationInfo optimizationInfo)
		{
			
			// - Is the first arg ScriptEngine?
			// - Does it have thisObj / does it want an instance object?
			System.Reflection.ParameterInfo[] paraSet=ResolvedMethod.GetParameters();
			
			int parameterOffset=0;
			bool staticMethod=ResolvedMethod.IsStatic || ResolvedMethod.IsConstructor;
			
			if(paraSet.Length>0)
			{
				
				if(paraSet[0].ParameterType==typeof(ScriptEngine))
				{
					// Emit an engine reference now:
					EmitHelpers.LoadEngine(generator);
					
					parameterOffset=1;
				}
				
				if(paraSet.Length>parameterOffset && paraSet[parameterOffset].Name=="thisObj")
				{
					// It's acting like an instance method.
					parameterOffset=2;
					staticMethod=false;
				}
			}
			
			// Get the args:
			IList<Expression> arguments = null;
			Expression argumentsOperand = null;
			
			if(OperandCount>1)
			{
				argumentsOperand = this.GetRawOperand(1);
				ListExpression argList = argumentsOperand as ListExpression;
				
				if (argList!=null)
				{
					// Multiple parameters were recieved.
					arguments = argList.Items;
					
					// Set the operand to null so it doesn't try to emit it as a single arg:
					argumentsOperand=null;
				}
			}
			
			if(!staticMethod)
			{
				// Generate the 'this' ref:
				var baseExpression = ((MemberAccessExpression)this.Target).Base;
				baseExpression.GenerateCode(generator, optimizationInfo);
			}
			
			// Next, we're matching params starting from parameterOffset with the args,
			// type casting if needed.
			for(int i=parameterOffset;i<paraSet.Length;i++)
			{
				
				// Get the parameter info:
				var param=paraSet[i];
				
				// Get the parameters type:
				Type paramType=param.ParameterType;
				
				Expression expression=null;
				
				// Is it a params array?
				if(Attribute.IsDefined(param, typeof (ParamArrayAttribute)))
				{
					
					// It's always an array - get the element type:
					paramType=paramType.GetElementType();
					
					// For each of the remaining args..
					int offset=i-parameterOffset;
					
					int argCount=0;
					
					if(arguments!=null)
					{
						// Get the full count:
						argCount=arguments.Count;
						
					}
					else if(argumentsOperand!=null)
					{
						// Just one arg and it's still hanging around.
						argCount=offset+1;
					}
					
					// Define an array:
					generator.LoadInt32(argCount);
					generator.NewArray(paramType);
					
					for(int a=offset;a<argCount;a++)
					{
						
						if(arguments!=null)
						{
							// One of many args:
							expression=arguments[a];
						}
						else
						{
							// Just one arg:
							expression=argumentsOperand;
						}
						
						generator.Duplicate();
						generator.LoadInt32(a-offset);
						expression.GenerateCode(generator, optimizationInfo);
						Type res=expression.GetResultType(optimizationInfo);
						EmitConversion.Convert(generator, res,paramType);
						generator.StoreArrayElement(paramType);
					}
					
					// All done - can't be anymore.
					break;
				}
				
				if(arguments!=null && (i-parameterOffset)<=arguments.Count)
				{
					// Get one of many args:
					expression=arguments[i-parameterOffset];
				}
				else if(argumentsOperand!=null)
				{
					// Just the one argument.
					expression=argumentsOperand;
					
					// By setting it to null after, it can't get emitted again
					// (in the event that this method actually accepts >1 args)
					argumentsOperand=null;
				}
				
				if(expression==null)
				{
					// Emit whatever the default is for the parameters type:
					if(param.RawDefaultValue!=null)
					{
						// Emit the default value:
						EmitHelpers.EmitValue(generator,param.RawDefaultValue);
					}
					else if(paramType.IsValueType)
					{
						// E.g. an integer 0
						EmitHelpers.EmitValue(generator,Activator.CreateInstance(paramType));
					}
					else
					{
						// Just a null (a real one):
						generator.LoadNull();
					}
				}
				else
				{
					// Output the arg:
					expression.GenerateCode(generator, optimizationInfo);
				
					// Convert:
					EmitConversion.Convert(generator,expression.GetResultType(optimizationInfo),paramType);
				}
				
			}
			
		}
		
		/// <summary>
		/// Gets the set of types for each argument.
		/// </summary>
		internal Type[] GetArgumentTypes(OptimizationInfo optimizationInfo)
		{
			
			// Get the args:
			Expression argumentsOperand = null;
			
			if(OperandCount>1)
			{
				argumentsOperand = GetRawOperand(1);
				ListExpression argList = argumentsOperand as ListExpression;
				
				if (argList==null)
				{
					// Just one:
					return new Type[]{
						argumentsOperand.GetResultType(optimizationInfo)
					};
				}
				
				// Multiple parameters were recieved.
				IList<Expression> arguments = argList.Items;
				
				// Set the operand to null so it doesn't try to emit it as a single arg:
				argumentsOperand=null;
				
				int count=arguments.Count;
				
				// Create the type set:
				Type[] result=new Type[count];
				
				// Get the result type of each one:
				for(int i=0;i<count;i++)
				{
					result[i]=arguments[i].GetResultType(optimizationInfo);
				}
				
				return result;
				
			}
			
			// No arguments.
			return null;
			
		}
		
	}

}