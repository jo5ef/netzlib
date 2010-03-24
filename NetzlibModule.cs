using System;
using System.Web;
using System.Web.UI;
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
			context.PostMapRequestHandler += OnPostMapRequestHandler;
		}

		static void OnPostMapRequestHandler(object sender, EventArgs e)
		{
			var ctx = HttpContext.Current;
			if(ctx != null && ctx.CurrentHandler is Page)
			{
				var tagFilter = new TagFilter();
				ctx.Response.Filter = tagFilter;



				if (Javascript.Settings.Default.Enabled)
				{
					var jsCombinator = new JavascriptCombiner(ctx.Request.Url, resources);
					tagFilter.Filter += jsCombinator.Filter;
				}
			}
		}

		public void Dispose()
		{
		}
	}
}
