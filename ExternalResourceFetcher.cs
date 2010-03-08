using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO.Compression;
using System.IO;
using System.Web;
using System.Web.Caching;
using System.Threading;

namespace PimpMyWeb
{
	internal class ExternalResourceFetcher
	{
		class RequestState
		{
			public WebRequest Request { get; set; }
			public ExternalResource Resource { get; set; }
		}

		public static void Fetch(ExternalResource resource)
		{
			if (resource != null)
			{
				var wr = WebRequest.Create(resource.Uri);
				var rs = new RequestState { Request = wr, Resource = resource };
				var ar = wr.BeginGetResponse(new AsyncCallback(ResponseCallback), rs);
				
				ThreadPool.RegisterWaitForSingleObject(ar.AsyncWaitHandle, TimeoutCallback,
					rs, Settings.Default.ExternalResourceTimeout * 1000, true);
			}
		}

		private static void ResponseCallback(IAsyncResult ar)
		{
			var rs = ar.AsyncState as RequestState;

			try
			{
				var response = rs.Request.EndGetResponse(ar);
				var stream = response.GetResponseStream();

				var enc = response.Headers["Content-Encoding"];
				if (enc != null && enc.Contains("gzip"))
				{
					stream = new GZipStream(stream, CompressionMode.Decompress);
				}

				using (var reader = new StreamReader(stream))
				{
					rs.Resource.Content = reader.ReadToEnd();
				}
			}
			catch (WebException ex)
			{
				rs.Resource.Content = string.Format("// {0}: {1}", rs.Request.RequestUri, ex.Message);
			}
			finally
			{
				rs.Resource.Loaded.Set();
				CacheRefetchItem(rs.Resource);
			}
		}

		private static void TimeoutCallback(object state, bool timedOut)
		{
			if (timedOut)
			{
				var rs = state as RequestState;
				rs.Request.Abort();
				rs.Resource.Content = string.Format("// {0}: timed out", rs.Resource.Uri);
				rs.Resource.Loaded.Set();
				CacheRefetchItem(rs.Resource);
			}
		}

		private static void CacheRefetchItem(ExternalResource resource)
		{
			if (Settings.Default.ExternalResourceRefreshInterval > 0)
			{
				HttpRuntime.Cache.Add(Guid.NewGuid().ToString(), resource, null,
					DateTime.Now.AddSeconds(Settings.Default.ExternalResourceRefreshInterval),
					Cache.NoSlidingExpiration, CacheItemPriority.Default, Refetch);
			}
		}

		private static void Refetch(string key, object value, CacheItemRemovedReason reason)
		{
			Fetch(value as ExternalResource);
		}
	}
}
