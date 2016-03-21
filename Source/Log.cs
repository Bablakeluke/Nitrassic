//--------------------------------------
//          Wrench Framework
//
//        For documentation or 
//    if you have any issues, visit
//         wrench.kulestar.com
//
//    Copyright © 2013 Kulestar Ltd
//          www.kulestar.com
//--------------------------------------

namespace Wrench{
	
	/// <summary>The delegate used when Wrench logs an event. It's used by <see cref="Wrench.Log.Onlog"/>.</summary>
	public delegate void OnLogEvent(string text);
	
	/// <summary>
	/// This class handles logging messages. When a message occurs, it fires an
	/// event so a specific application can handle them how it wishes. All logging 
	/// can also be disabled with <see cref="Wrench.Log.Active"/>.
	/// </summary>
	
	public static class Log{
		
		/// <summary>Set active to false to prevent Wrench logging any messages.</summary>
		public static bool Active=true;
		/// <summary>An event fired whenever Wrench logs a message, unless logging is not active.</summary>
		public static event OnLogEvent OnLog; 
		
		/// <summary>Adds a message to log.</summary>
		/// <param name="text">The message to log.</param>
		public static void Add(string text){
			if(Active && OnLog!=null){
				OnLog(text);
			}
		}
		
		/// <summary>Adds an object of any kind to log.
		/// This is just a convenience method as it internally calls ToString on the object.</summary>
		public static void Add(object e){
			Add(e.ToString());
		}
		
	}
	
}