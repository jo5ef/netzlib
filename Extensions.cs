using System;
using System.IO;
using System.Web;

namespace netzlib
{
	internal static class Extensions
	{
		public static uint GetKey(this Uri uri)
		{
			return (uint) uri.GetHashCode();
		}

		public static uint GetKey(this string s)
		{
			return (uint) s.GetHashCode();
		}

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
