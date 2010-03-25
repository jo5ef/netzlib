using System;
using System.Text;
using System.Threading;
using System.IO;

namespace netzlib
{
	internal class Resource
	{
		public virtual string Content { get; set; }
	}

	internal class ExternalResource : Resource
	{
		public readonly ManualResetEvent Loaded = new ManualResetEvent(false);
		public Uri Uri { get; set; }
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
	}
}
