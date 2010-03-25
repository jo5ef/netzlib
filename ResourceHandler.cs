using System;
using System.Globalization;
using System.Text;
using System.Web;
using System.IO;
using System.IO.Compression;

namespace netzlib
{
	public class ResourceHandler : IHttpHandler
	{
		readonly Settings settings = Settings.Default;

		public bool IsReusable
		{
			get { return true; }
		}

		public void ProcessRequest(HttpContext ctx)
		{
			uint hash;
			if (!uint.TryParse(ctx.Request["v"],
				NumberStyles.HexNumber, CultureInfo.CurrentCulture, out hash))
			{
				Throw404(ctx);
			}

			var repository = ctx.Application[NetzlibModule.RESOURCE_REPOSITORY] as IResourceRepository;
			if (repository == null)
			{
				throw new InvalidOperationException("no repository registered");
			}

			var script = repository.GetContent(hash);
			if (script == null)
			{
				Throw404(ctx);
			}

			if (ctx.IsDebuggingEnabled)
			{
				ResponseNoCache(ctx.Response);
			}
			else
			{
				ResponseCache(ctx.Response);
			}

			ctx.Response.ContentType = "text/javascript";
			ctx.Response.ContentEncoding = Encoding.UTF8;

			if (UseCompression(ctx))
			{
				WriteCompressedResponse(ctx, script);
			}
			else
			{
				ctx.Response.Write(script);
			}
		}

		private static bool UseCompression(HttpContext ctx)
		{
			if (ctx.IsDebuggingEnabled)
			{
				return false;
			}

			string acceptEncoding = ctx.Request.Headers["Accept-Encoding"];

			bool result = (!string.IsNullOrEmpty(acceptEncoding)) &&
				(acceptEncoding.Contains("gzip") || acceptEncoding.Contains("deflate"));

			if (ctx.Request.Browser.IsBrowser("IE"))
			{
				return result && ctx.Request.Browser.MajorVersion > 6;
			}

			return result;
		}

		private static void WriteCompressedResponse(HttpContext ctx, string content)
		{
			string acceptEncoding = ctx.Request.Headers["Accept-Encoding"];
			using (var ms = new MemoryStream())
			{
				if (acceptEncoding.Contains("gzip"))
				{
					using (var gzip = new GZipStream(ms, CompressionMode.Compress))
					{
						using (var writer = new StreamWriter(gzip, Encoding.UTF8))
						{
							writer.Write(content);
						}
					}
				}
				else if (acceptEncoding.Contains("deflate"))
				{
					using (var deflate = new DeflateStream(ms, CompressionMode.Compress))
					{
						using (var writer = new StreamWriter(deflate, Encoding.UTF8))
						{
							writer.Write(content);
						}
					}
				}
				byte[] buffer = ms.ToArray();
				ctx.Response.AddHeader("Content-encoding", "gzip");
				ctx.Response.OutputStream.Write(buffer, 0, buffer.Length);
			}
		}

		private static void ResponseNoCache(HttpResponse response)
		{
			var cache = response.Cache;
			
			cache.SetCacheability(HttpCacheability.NoCache);
			cache.SetNoServerCaching();
		}

		private void ResponseCache(HttpResponse response)
		{
			var cache = response.Cache;
			var now = DateTime.Now;

			cache.SetCacheability(HttpCacheability.Public);
			cache.SetMaxAge(TimeSpan.FromSeconds(settings.CombinedScriptCacheDuration));
			cache.VaryByParams["v"] = true;
			cache.SetExpires(now.AddSeconds(settings.CombinedScriptCacheDuration));
			cache.SetValidUntilExpires(true);
			cache.SetLastModified(now);
		}

		private static void Throw404(HttpContext ctx)
		{
			throw new HttpException(404, "could not find " + ctx.Request.Url);
		}
	}
}