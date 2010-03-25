using System;
using System.Collections.Generic;
using System.Threading;
using System.IO;

namespace netzlib
{
	internal interface IResourceRepository
	{
		void Add(Uri resource);
		void Add(string resource);
		void Add(uint key, uint[] resources, ContentFilter filter);
		string GetContent(uint key);
	}

	internal delegate string ContentFilter(string content);

	internal class ResourceRepository : IResourceRepository
	{
		readonly ReaderWriterLockSlim locks = new ReaderWriterLockSlim();
		readonly Dictionary<uint, Resource> resources = new Dictionary<uint, Resource>();

		readonly ExternalResourceFetcher fetcher = new ExternalResourceFetcher();

		public void Add(Uri resourceUri)
		{
			var key = resourceUri.GetKey();

			AddInternal(key, () =>
			{
				FileInfo file;
				ExternalResource resource;
				
				if (resourceUri.TryMapPath(out file))
				{
					resource = new ExternalResource { Uri = new Uri(file.FullName) };
				}
				else
				{
					resource = new ExternalResource { Uri = resourceUri };
				}

				resources.Add(key, resource);
				fetcher.Fetch(resource);
			});
		}

		public void Add(string content)
		{
			var key = content.GetKey();
			AddInternal(key, () => resources.Add(key, new Resource { Content = content }));
		}

		public void Add(uint key, uint[] resourceList, ContentFilter filter)
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

		public void AddInternal(uint key, Action add)
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

		public string GetContent(uint key)
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