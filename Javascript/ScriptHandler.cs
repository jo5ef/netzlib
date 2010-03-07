using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace PimpMyWeb.Javascript
{
	public class ScriptHandler : IHttpHandler
	{
		IResourceRepository repository = ResourceRepository.Current;

		public bool IsReusable
		{
			get { return true; }
		}

		public void ProcessRequest(HttpContext ctx)
		{
			int hash;
			if (!int.TryParse(ctx.Request["v"], out hash))
			{
				NotFound(ctx);
				return;
			}

			var script = repository.GetContent(hash);
			if (script == null)
			{
				NotFound(ctx);
				return;
			}

			ctx.Response.ContentType = "text/javascript";
			ctx.Response.Write(script);
		}

		private void NotFound(HttpContext ctx)
		{
			ctx.Response.StatusCode = 404;
			ctx.Response.End();
		}
	}
}
