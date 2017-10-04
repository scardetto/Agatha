using System.Collections.Generic;
using System.Linq;

namespace Agatha.Common
{
	public class RequestDispatcherStub : RequestDispatcher
	{
		private readonly List<Response> _responsesToReturn = new List<Response>();
		private readonly Dictionary<string, Request> _keyToRequest = new Dictionary<string, Request>();

		public RequestDispatcherStub() : base(null, null) { }

		public void AddResponsesToReturn(params Response[] responses)
		{
			_responsesToReturn.AddRange(responses);
		}

		public void AddResponsesToReturn(Dictionary<string, Response> keyedResponses)
		{
			_responsesToReturn.AddRange(keyedResponses.Values);

			for (int i = 0; i < keyedResponses.Keys.Count; i++)
			{
				var key = keyedResponses.Keys.ElementAt(i);

				if (key != null)
				{
					keyToResultPositions.Add(key, i);
				}
			}
		}

		public void AddResponseToReturn(Response response)
		{
			_responsesToReturn.Add(response);
		}

		public void AddResponseToReturn(string key, Response response)
		{
			_responsesToReturn.Add(response);
			keyToResultPositions.Add(key, _responsesToReturn.Count - 1);
		}

		public override void Clear()
		{
			// this Stub can't clear the state because we have to be able to inspect the sent requests
			// during our tests
		}

		public override void Add(string key, Request request)
		{
			base.Add(key, request);
			_keyToRequest[key] = request;
		}

		public TRequest GetRequest<TRequest>() where TRequest : Request
		{
			return (TRequest)SentRequests.First(r => r.GetType() == typeof(TRequest));
		}

		public TRequest GetRequest<TRequest>(string key) where TRequest : Request
		{
			return (TRequest)_keyToRequest[key];
		}

		public bool HasRequest<TRequest>() where TRequest : Request
		{
			return SentRequests.Any(r => r.GetType() == typeof(TRequest));
		}

		protected override Response[] GetResponses(Request[] requestsToProcess)
		{
			return _responsesToReturn.ToArray();
		}
	}
}