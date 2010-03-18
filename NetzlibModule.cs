using System;
using System.Web;
using netzlib.Javascript;

namespace netzlib
{
	public class NetzlibModule : IHttpModule
	{
		internal static readonly string RESOURCE_REPOSITORY = typeof(IResourceRepository).FullName;

		static readonly IResourceRepository resources = new ResourceRepository();

		public void Init(HttpApplication context)
		{
			context.Application[RESOURCE_REPOSITORY] = resources;
			context.BeginRequest += context_BeginRequest;
		}

		static void context_BeginRequest(object sender, EventArgs e)
		{
			var ctx = HttpContext.Current;

			var tagFilter = new TagFilter();
			ctx.Response.Filter = tagFilter;

			if (Javascript.Settings.Default.Enabled)
			{
				var jsCombinator = new JavascriptCombiner(ctx.Request.Url, resources);
				tagFilter.Filter += jsCombinator.Filter;
			}
		}

		public void Dispose()
		{
		}
	}
}
