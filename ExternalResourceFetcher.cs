using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO.Compression;
using System.IO;
using System.Web;
using System.Web.Caching;

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
				wr.BeginGetResponse(new AsyncCallback(ResponseCallback),
					new RequestState { Request = wr, Resource = resource });
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

				HttpRuntime.Cache.Add(Guid.NewGuid().ToString(), rs.Resource, null,
					DateTime.Now.AddMinutes(1), Cache.NoSlidingExpiration, CacheItemPriority.Default, Refetch);
			}
		}

		private static void Refetch(string key, object value, CacheItemRemovedReason reason)
		{
			Fetch(value as ExternalResource);
		}
	}
}
