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
		public void Fetch(ExternalResource resource)
		{
			if (resource is LocalResource)
			{
				Fetch(resource as LocalResource);
			}
			else if (resource is RemoteResource)
			{
				Fetch(resource as RemoteResource);
			}
			else
			{
				throw new NotImplementedException();
			}
		}

		#region local fetch

		class WatchedDirectory
		{
			public FileSystemWatcher Watcher { get; set; }
			public readonly Dictionary<string, LocalResource> Resources = new Dictionary<string, LocalResource>();
		}

		private Dictionary<string, WatchedDirectory> directories = new Dictionary<string, WatchedDirectory>();

		public void Fetch(LocalResource resource)
		{
			resource.Content = File.ReadAllText(resource.File.FullName);
			resource.Loaded.Set();

			if (Settings.Default.WatchFiles)
			{
				// setup file watch

				if (!directories.ContainsKey(resource.File.DirectoryName))
				{
					var fsw = new FileSystemWatcher(resource.File.DirectoryName);
					fsw.NotifyFilter = NotifyFilters.LastWrite;
					fsw.Changed += new FileSystemEventHandler(OnFileChanged);
					directories.Add(resource.File.DirectoryName, new WatchedDirectory { Watcher = fsw });
				}

				var dir = directories[resource.File.DirectoryName];
				if (!dir.Resources.ContainsKey(resource.File.Name))
				{
					dir.Resources.Add(resource.File.Name, resource);
					dir.Watcher.EnableRaisingEvents = true;
				}
			}
		}

		private void OnFileChanged(object sender, FileSystemEventArgs e)
		{
			if (e.ChangeType == WatcherChangeTypes.Changed)
			{
				var file = new FileInfo(e.FullPath);
				if (directories.ContainsKey(file.DirectoryName))
				{
					var dir = directories[file.DirectoryName];
					if (dir.Resources.ContainsKey(file.Name))
					{
						Fetch(dir.Resources[file.Name]);
					}
				}
			}
		}

		#endregion

		#region remote fetch

		class RequestState
		{
			public WebRequest Request { get; set; }
			public RemoteResource Resource { get; set; }
		}

		public void Fetch(RemoteResource resource)
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

		private void ResponseCallback(IAsyncResult ar)
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

		private void TimeoutCallback(object state, bool timedOut)
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

		private void CacheRefetchItem(RemoteResource resource)
		{
			if (Settings.Default.ExternalResourceRefreshInterval > 0)
			{
				HttpRuntime.Cache.Add(Guid.NewGuid().ToString(), resource, null,
					DateTime.Now.AddSeconds(Settings.Default.ExternalResourceRefreshInterval),
					Cache.NoSlidingExpiration, CacheItemPriority.Default, Refetch);
			}
		}

		private void Refetch(string key, object value, CacheItemRemovedReason reason)
		{
			Fetch(value as RemoteResource);
		}

		#endregion
	}
}
