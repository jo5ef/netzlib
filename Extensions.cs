using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Web;

namespace PimpMyWeb
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
