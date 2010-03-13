﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Web;

namespace PimpMyWeb
{
	internal interface IResourceRepository
	{
		void Add(Uri resource);
		void Add(string resource);
		void Add(int key, int[] resources, ContentFilter filter);
		string GetContent(int key);
	}

	internal delegate string ContentFilter(string content);

	internal class ResourceRepository : IResourceRepository
	{
		ReaderWriterLockSlim locks = new ReaderWriterLockSlim();
		Dictionary<int, Resource> resources = new Dictionary<int, Resource>();

		ExternalResourceFetcher fetcher = new ExternalResourceFetcher();

		public void AddInternal(int key, Action add)
		{
			locks.EnterUpgradeableReadLock();
			try
			{
				if (!resources.ContainsKey(key))
				{
					locks.EnterWriteLock();
					try
					{
						add();
					}
					finally
					{
						locks.ExitWriteLock();
					}
				}
			}
			finally
			{
				locks.ExitUpgradeableReadLock();
			}
		}

		public void Add(Uri resourceUri)
		{
			var key = resourceUri.GetHashCode();

			AddInternal(key, () =>
			{
				FileInfo file;
				ExternalResource resource;

				if (resourceUri.TryMapPath(out file))
				{
					resource = new LocalResource(key) { File = file };
					fetcher.Fetch(resource);
				}
				else
				{
					resource = new RemoteResource { Uri = resourceUri };
					fetcher.Fetch(resource);
				}

				resources.Add(key, resource);
			});
		}

		public void Add(string content)
		{
			var key = content.GetHashCode();

			AddInternal(key, () =>
			{
				resources.Add(key, new Resource { Content = content });
			
			});
		}

		public void Add(int key, int[] resourceList, ContentFilter filter)
		{
			AddInternal(key, () =>
			{
				var l = new List<Resource>();

				foreach (var resourceKey in resourceList)
				{
					if (!resources.ContainsKey(resourceKey))
					{
						throw new InvalidOperationException(
							"members of composite resources must be added to the repository first");
					}

					l.Add(resources[resourceKey]);
				}

				resources.Add(key, new CompositeResource { Resources = l.ToArray(), Filter = filter });
			});
		}

		public string GetContent(int key)
		{
			locks.EnterReadLock();
			try
			{
				return resources.ContainsKey(key) ? resources[key].Content : null;
			}
			finally
			{
				locks.ExitReadLock();
			}
		}
	}
}