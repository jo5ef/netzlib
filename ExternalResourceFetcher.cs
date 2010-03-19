using System;
using System.Collections.Generic;
using System.Net;
using System.IO.Compression;
using System.IO;
using System.Web;
using System.Web.Caching;
using System.Threading;

namespace netzlib
{
	internal class ExternalResourceFetcher
	{
		class RequestState
		{
			public WebRequest Request { get; set; }
			public ExternalResource Resource { get; set; }
		}

		class WatchedDirectory
		{
			public FileSystemWatcher Watcher { get; set; }
			public readonly Dictionary<string, ExternalResource> Resources =
				new Dictionary<string, ExternalResource>();
		}

		private readonly Dictionary<string, WatchedDirectory> directories =
			new Dictionary<string, WatchedDirectory>();

		public void Fetch(ExternalResource resource)
		{
			if (resource != null)
			{
				var wr = WebRequest.Create(resource.Uri);
				var rs = new RequestState { Request = wr, Resource = resource };
				var ar = wr.BeginGetResponse(ResponseCallback, rs);
 
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
			catch (Exception ex)
			{
				rs.Resource.Content = string.Format("// {0}: {1}", rs.Request.RequestUri, ex.Message);
			}
			finally
			{
				CompleteFetch(rs.Resource);
			}
		}

		private void TimeoutCallback(object state, bool timedOut)
		{
			if (timedOut)
			{
				var rs = state as RequestState;
				rs.Request.Abort();
				rs.Resource.Content = string.Format("// {0}: timed out", rs.Resource.Uri);
				CompleteFetch(rs.Resource);
			}
		}

		private void CompleteFetch(ExternalResource resource)
		{
			resource.Loaded.Set();

			if(resource.Uri.IsFile)
			{
				// resource is a local file, setup a file watch

				if (Settings.Default.WatchFiles)
				{
					WatchFile(resource);
				}
			}
			else
			{
				// resource is a remote url, schedule a refetch

				if (Settings.Default.ExternalResourceRefreshInterval > 0)
				{
					HttpRuntime.Cache.Add(Guid.NewGuid().ToString(), resource, null,
						DateTime.Now.AddSeconds(Settings.Default.ExternalResourceRefreshInterval),
						Cache.NoSlidingExpiration, CacheItemPriority.Default, Refetch);
				}
			}
		}

		private void WatchFile(ExternalResource resource)
		{
			var file = new FileInfo(resource.Uri.LocalPath);
			lock (directories)
			{
				if (!directories.ContainsKey(file.DirectoryName))
				{
					var fsw = new FileSystemWatcher(file.DirectoryName);
					fsw.NotifyFilter = NotifyFilters.LastWrite;
					fsw.Changed += OnFileChanged;
					directories.Add(file.DirectoryName, new WatchedDirectory { Watcher = fsw });
				}

				var dir = directories[file.DirectoryName];
				if (!dir.Resources.ContainsKey(file.Name))
				{
					dir.Resources.Add(file.Name, resource);
					dir.Watcher.EnableRaisingEvents = true;
				}
			}
		}

		private void Refetch(string key, object value, CacheItemRemovedReason reason)
		{
			Fetch(value as ExternalResource);
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
	}
}
