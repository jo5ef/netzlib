using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace PimpMyWeb
{
	internal class Resource
	{
		public virtual string Content { get; set; }

		public override int GetHashCode()
		{
			return Content.GetHashCode();
		}
	}

	internal class ExternalResource : Resource
	{
		public ManualResetEvent Loaded = new ManualResetEvent(false);
		
		public Uri Uri { get; set; }

		public override int GetHashCode()
		{
			return Uri.GetHashCode();
		}
	}

	internal class CompositeResource : Resource
	{
		public Resource[] Resources { get; set; }

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
				return sb.ToString();
			}
			set
			{
				throw new InvalidOperationException();
			}
		}

		public override int GetHashCode()
		{
			int hash = 0;
			foreach (var r in Resources)
			{
				hash ^= r.GetHashCode();
			}
			return hash;
		}
	}
}
