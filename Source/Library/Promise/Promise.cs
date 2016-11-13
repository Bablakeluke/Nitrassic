using System;
using System.Collections.Generic;

namespace Nitrassic.Library
{
	/// <summary>
	/// Represents an instance of the Promise object.
	/// </summary>
	public partial class Promise
	{
		
		public enum PromiseState
		{
			Pending,
			Fulfilled,
			Rejected,
		}
		
		/// <summary>The executor function.</summary>
		private FunctionInstance Executor;
		
		/// <summary>Governs how a promise will react to incoming calls to its then() method.</summary>
		public PromiseState state = PromiseState.Pending;

		/// <summary>The value with which the promise has been fulfilled or rejected, if any.  Only
		/// meaningful if state is not Pending.</summary>
		public object result;

		/// <summary>A list of PromiseReaction records to be processed when/if the promise transitions
		/// from the Pending state to the Fulfilled state.</summary>
		private readonly List<object> fulfillReactions = new List<object>();

		/// <summary>	A list of PromiseReaction records to be processed when/if the promise transitions
		/// from the Pending state to the Rejected state.</summary>
		private readonly List<object> rejectReactions = new List<object>();
		
		
		/// <summary>
		/// Creates a new Promise instance.
		/// </summary>
		/// <param name="prototype"></param>
		/// <param name="executor"></param>
		internal Promise(FunctionInstance executor)
		{
			Executor=executor;
		}
		
		//	 JAVASCRIPT FUNCTIONS
		//_________________________________________________________________________________________

		/// <summary>
		/// Returns a Promise and deals with rejected cases only. It behaves the same as calling
		/// Promise.prototype.then(undefined, onRejected).
		/// </summary>
		/// <param name="onRejected"> A Function called when the Promise is rejected. This function
		/// has one argument, the rejection reason. </param>
		/// <returns></returns>
		public Promise Catch(FunctionInstance onRejected){
			return Then(null, onRejected);
		}
		
		/// <summary>
		/// Returns a Promise. It takes two arguments: callback functions for the success and
		/// failure cases of the Promise.
		/// </summary>
		/// <param name="onFulfilled"> A Function called when the Promise is fulfilled. This
		/// function has one argument, the fulfillment value. </param>
		/// <param name="onRejected"> A Function called when the Promise is rejected. This function
		/// has one argument, the rejection reason. </param>
		/// <returns></returns>
		public Promise Then(FunctionInstance onFulfilled, FunctionInstance onRejected){
			throw new NotImplementedException();
		}
		
	}
	
}