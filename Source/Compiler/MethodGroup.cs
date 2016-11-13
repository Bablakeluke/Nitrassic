using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace Nitrassic.Compiler
{

	public class MethodGroup
	{
		
		/// <summary>
		/// True if this group is representing a jump table.
		/// The method to jump to is defined as an integer instance property.
		/// </summary>
		public bool JumpTable;
		
		/// <summary>
		/// The methods in this group.
		/// Note that they always have the same name.
		/// </summary>
		public List<MethodBase> Methods=new List<MethodBase>();
		
		/// <summary>
		/// The number of methods in the group.
		/// </summary>
		public int Length{
			get{
				return Methods.Count;
			}
		}
		
		/// <summary>
		/// Emits a jump table for this method group. The integer index must be on the stack.
		/// Each index in the table is simply a Call instruction.
		/// </summary>
		public void GenerateJumpTable(ILGenerator generator)
		{
			
			// The number of methods:
			int methodCount=Methods.Count;
			
			// Create the set of switch labels, one for each method:
			ILLabel[] switchLabels = new ILLabel[Methods.Count];
			for (int i = 0; i < methodCount; i++)
			{
				switchLabels[i] = generator.CreateLabel();
			}
			
			// Emit the switch instruction now:
			generator.Switch(switchLabels);
			
			// Create the end label:
			ILLabel end=generator.CreateLabel();
			
			// For each method, mark the label and emit the call.
			for (int i = 0; i < methodCount; i++)
			{
				// Mark the label:
				generator.DefineLabelPosition(switchLabels[i]);
				
				// Get the method:
				MethodBase method=Methods[i];
				
				// Emit the call now:
				if(method.IsConstructor)
				{
					// Actual constructor call:
					generator.NewObject(method as System.Reflection.ConstructorInfo);
				}
				else
				{
					// Call it now:
					generator.Call(method);
				}
				
				generator.Branch(end);
			}
			
			generator.DefineLabelPosition(end);
			
		}
		
		/// <summary>
		/// The property attributes for these methods.
		/// </summary>
		public Nitrassic.Library.PropertyAttributes PropertyAttributes;
		
		/// <summary>
		/// Adds a given method into this group.
		/// </summary>
		public void Add(MethodBase method)
		{
			Methods.Add(method);
		}
		
		/// <summary>
		/// Finds the best method for the given parameter set.
		/// </summary>
		public MethodBase MatchObjects(object[] paramObjects)
		{
			int typeCount=paramObjects==null?0:paramObjects.Length;
			
			int count=Methods.Count;
			
			for(int i=0;i<count;i++)
			{
				MethodBase method=Methods[i];
				ParameterInfo[] parameters=method.GetParameters();
				
				if(parameters.Length==typeCount)
				{
					return method;
				}
			}
			
			return Methods[0];
		}
		
		public MethodBase Match(Type[] types)
		{
			return GetOverload(Methods,null,types,false,true);
		}
		
		/// <summary>Checks if one type is castable to another, reporting if it must be done explicitly or not if its possible.</summary>
		/// <param name="from">The type to cast from.</param>
		/// <param name="to">The type to cast to.</param>
		/// <param name="isExplicit">True if the cast must be done explicity.</param>
		/// <returns>The casting methods MethodInfo if a cast is possible at all; Null otherwise.</returns>
		public static MethodBase IsCastableTo(Type from,Type to,out bool isExplicit){
			isExplicit=false;
			// Does convertible hold it?
			MethodBase convertMethod=typeof(System.Convert).GetMethod("To"+NameWithoutNamespace(to.Name),new Type[]{from});
			if(convertMethod!=null){
				return convertMethod;
			}
			
			// Nope - look for its own personal cast operators.
			MethodInfo[] methods=from.GetMethods(BindingFlags.Public|BindingFlags.Static);
			
			foreach(MethodInfo method in methods){
				if(method.ReturnType!=to){
					continue;
				}
				isExplicit=(method.Name=="op_Explicit");
				if(isExplicit||method.Name=="op_Implicit"){
					return method;
				}
			}
			
			return null;
		}
		
		/// <summary>Gets the MethodInfo from the given set of methods which matches the given argument set and name.</summary>
		/// <param name="overloads">The set of overloads to find the right overload from.</param>
		/// <param name="name">The name of the method to find.
		/// This is used if the method set is a full set of methods from a type.</param>
		/// <param name="argSet">The set of arguments to find a matching overload with.</param>
		/// <param name="ignoreCase">Defines if the search should ignore case on the method names or not.</param>
		/// <param name="cast">True if the search should allow casting when performing the match.</param>
		/// <returns>The overload if found; null otherwise.</returns>
		private static MethodBase GetOverload(List<MethodBase> overloads,string name,Type[] argSet,bool ignoreCase,bool cast){
			if(overloads==null){
				return null;
			}
			
			int bestResult=0;
			MethodBase bestSoFar=null;
			
			for(int i=overloads.Count-1;i>=0;i--){
				MethodBase mInfo=overloads[i];
				string mName=mInfo.Name;
				if(ignoreCase){
					mName=mName.ToLower();
				}
				if(name!=null&&mName!=name){
					continue;
				}
				int result=WillAccept(argSet,mInfo.GetParameters(),cast);
				if(result==-1){
					continue;
				}else if(result==0){
					return mInfo;
				}
				if(bestSoFar==null || result<bestResult){
					bestSoFar=mInfo;
					bestResult=result;
				}
			}
			return bestSoFar;
		}
		
		/// <summary>Checks if the given ParameterInfo uses the params keyword.</summary>
		/// <param name="param">The parameter to check.</param>
		/// <returns>True if the parameter uses the params keyword; false otherwise.</returns>
		public static bool IsParams(ParameterInfo param){
			
			if(param==null || string.IsNullOrEmpty(param.Name)){
				return false;
			}
			
			return Attribute.IsDefined(param,typeof(ParamArrayAttribute));
		}
		
		/// <summary>Gets the name of a type without the namespace. E.g. System.Int32 becomes Int32.</summary>
		/// <param name="typeName">The full typename, including the namespace.</param>
		/// <returns>The type name without the namespace.</returns>
		public static string NameWithoutNamespace(string typeName){
			string[] pieces=typeName.Split('.');
			return pieces[pieces.Length-1];
		}
		
		/// <summary>Checks if the given parameter set will accept the given argument set. Used to find an overload.</summary>
		/// <param name="args">The set of arguments.</param>
		/// <param name="parameters">The set of parameters.</param>
		/// <param name="withCasting">True if casting is allowed to perform the match.</param>
		/// <returns>A number which represents how many casts must occur to accept (minimise when selecting an overload).
		/// -1 is returned if it cannot accept at all.</returns>
		public static int WillAccept(Type[] args,ParameterInfo[] parameters,bool withCasting){
			// We will consider accepting this if:
			// Args and parameters are the same length AND parameters does not end with params.
			int argCount=(args==null)?0:args.Length;
			int paramCount=(parameters==null)?0:parameters.Length;
			bool endsWithParams=(paramCount!=0 && IsParams(parameters[parameters.Length-1]));
			if(argCount==0&&paramCount==0){
				return 0;
			}
			
			// Starts with an engine parameter?
			bool startsWithEngine=(paramCount>0 && parameters[0].ParameterType==typeof(ScriptEngine));
			
			
			if(startsWithEngine){
				
				// We've essentially got a discounted parameter at the start.
				
				if(!endsWithParams){
					
					// They must be the same length, otherwise fail it.
				
					if(argCount!=(paramCount-1)){
						return -1;
					}
				
				}else if(argCount<(paramCount-2)){
					// Not enough arguments.
					// E.g. we have 3 arguments and 4 parameters (where the last is 'params') - that's OK.
					// 2 args and 4 parameters - that isn't.
					return -1;
				}
					
			}else if(!endsWithParams){
				
				// They must be the same length, otherwise fail it.
				
				if(argCount!=paramCount){
					return -1;
				}
				
			}else if(argCount<(paramCount-1)){
				// Not enough arguments.
				// E.g. we have 3 arguments and 4 parameters (where the last is 'params') - that's OK.
				// 2 args and 4 parameters - that isn't.
				return -1;
			}
			
			// Next, for each arg, find which parameter they fit into.
			// Args take priority because if there are no args, there must be no parameters OR one parameter with 'params'.
			
			// We're going to count how many times a cast is required.
			// This resulting value then acts as a weighting as to which overload may be selected.
			int castsRequired=0;
			
			int atParameter=0;
			
			// If it starts with scriptEngine, skip it:
			if(startsWithEngine){
				atParameter++;
			}
			
			if(args==null){
				return castsRequired;
			}
			
			for(int i=0;i<args.Length;i++){
				// Does args[i] fit in parameters[atParameter]?
				// If it doesn't, return false. 
				ParameterInfo parameter=parameters[atParameter];
				Type parameterType=parameter.ParameterType;
				if(endsWithParams && (atParameter==paramCount-1)){
					parameterType=parameterType.GetElementType();
				}else{
					atParameter++;
				}
				
				Type argumentType=args[i];
				if(argumentType==null){
					// Passing NULL. This is ok if parameterType is not a value type.
					if(parameterType.IsValueType){
						return -1;
					}
				}else{
					if(!parameterType.IsAssignableFrom(argumentType)){
						// Argument type is not equal to or does not inherit parameterType.
						if(!withCasting){
							return -1;
						}else{
							// Can we cast argumentType to parameterType?
							bool explicitCast;
							if(IsCastableTo(argumentType,parameterType,out explicitCast)==null){
								castsRequired++;
							}
						}
					}
				}
			}
			return castsRequired;
		}
		
		/// <summary>
		/// Invokes a method in this set which matches the parameters.
		/// </summary>
		public object Invoke(object thisObj,object[] parameters)
		{
			// Find the method which matches the parameters. Error otherwise.
			MethodBase method=MatchObjects(parameters);
			
			if(method==null)
			{
				// throw new JavaScriptException();
			}
			
			return null;
		}
		
		public override string ToString()
		{
			return Methods[0].Name;
		}
		
	}
	
	
}