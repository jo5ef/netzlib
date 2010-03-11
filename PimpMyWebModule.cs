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
		public void Dispose()
		{
		}

		public void Init(HttpApplication context)
		{
			context.BeginRequest += new EventHandler(context_BeginRequest);
		}

		void context_BeginRequest(object sender, EventArgs e)
		{
			HttpContext.Current.Response.Filter = new ScriptTagFilter();
		}
	}
}
