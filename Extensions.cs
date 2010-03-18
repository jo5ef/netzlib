using System;
using System.IO;
using System.Web;

namespace netzlib
{
	internal static class Extensions
	{
		public static bool TryMapPath(this Uri uri, out FileInfo file)
		{
			try
			{
				file = new FileInfo(HttpContext.Current.Server.MapPath(uri.PathAndQuery));
				return file.Exists;
			}
			catch (HttpException)
			{
				file = null;
				return false;
			}
		}
	}
}
