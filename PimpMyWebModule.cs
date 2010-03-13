using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using PimpMyWeb.Javascript;

namespace PimpMyWeb
{
	public class PimpMyWebModule : IHttpModule
	{
		internal static readonly string RESOURCE_REPOSITORY = typeof(IResourceRepository).FullName;

		static IResourceRepository resources = new ResourceRepository();

		public void Init(HttpApplication context)
		{
			context.Application[RESOURCE_REPOSITORY] = resources;
			context.BeginRequest += new EventHandler(context_BeginRequest);
		}

		void context_BeginRequest(object sender, EventArgs e)
		{
			HttpContext.Current.Response.Filter = new ScriptTagFilter(new ScriptCombinator(resources));
		}

		public void Dispose()
		{
		}
	}
}
