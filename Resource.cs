using System;
using System.Text;
using System.Threading;
using System.IO;

namespace netzlib
{
	internal class Resource
	{
		public virtual string Content { get; set; }
		public virtual int Key { get { return Content.GetHashCode(); } }
	}

	internal abstract class ExternalResource : Resource
	{
		public readonly ManualResetEvent Loaded = new ManualResetEvent(false);
	}

	internal class LocalResource : ExternalResource
	{
		private readonly int key;

		public LocalResource(int key)
		{
			this.key = key;
		}

		public FileInfo File { get; set; }
		public override int Key { get { return key; } }
	}

	internal class RemoteResource : ExternalResource
	{
		public Uri Uri { get; set; }
		public override int Key { get { return Uri.GetHashCode(); } }
	}

	internal class CompositeResource : Resource
	{
		public Resource[] Resources { get; set; }

		public ContentFilter Filter { get; set; }

		public override string Content
		{
			get
			{
				var sb = new StringBuilder();
				foreach (var r in Resources)
				{
					if (r is ExternalResource)
					{
						(r as ExternalResource).Loaded.WaitOne();
					}

					sb.AppendLine(r.Content);
				}

				var content = sb.ToString();

				if (Filter != null)
				{
					content = Filter(content);
				}

				return content;
			}
			set
			{
				throw new InvalidOperationException();
			}
		}

		public override int Key
		{
			get
			{
				int key = 0;
				foreach (var r in Resources)
				{
					key ^= r.Key;
				}
				return key;
			}
		}
	}
}
