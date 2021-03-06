﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using Yahoo.Yui.Compressor;
using System.Web;

namespace netzlib.Javascript
{
	internal class JavascriptCombiner
	{
		private static readonly Regex scriptPattern;
		private static readonly Regex externalScriptPattern;
		private static readonly Regex excludePattern;

		static JavascriptCombiner()
		{
			var settings = Settings.Default;
			var patternOptions = RegexOptions.IgnoreCase | RegexOptions.Compiled;
			
			scriptPattern = new Regex(settings.ScriptPattern, patternOptions);
			externalScriptPattern = new Regex(settings.ExternalScriptPattern, patternOptions);
			excludePattern = new Regex(settings.ExcludePattern, patternOptions);
		}

		readonly Uri baseUri;
		readonly IResourceRepository repository;
		uint hash;
		readonly List<uint> scripts = new List<uint>();

		public JavascriptCombiner(Uri baseUri, IResourceRepository repository)
		{
			this.baseUri = baseUri;
			this.repository = repository;
		}

		private void Add(Uri script)
		{
			var key = script.GetKey();
			
			repository.Add(script);
			scripts.Add(key);
			hash ^= key;
		}

		private void Add(string script)
		{
			var key = script.GetKey();

			repository.Add(script);
			scripts.Add(key);
			hash ^= key;
		}
		 
		private uint Combine()
		{
			if (Settings.Default.Compression)
			{
				repository.Add(hash, scripts.ToArray(), s =>
					JavaScriptCompressor.Compress(s, false, false, false,
						false, 120, Encoding.UTF8, CultureInfo.CurrentCulture));
			}
			else
			{
				repository.Add(hash, scripts.ToArray(), null);
			}

			return hash;
		}

		public string Filter(string patternBuffer, StreamWriter writer)
		{
			var s = patternBuffer;
			var afterLastMatch = 0;

			foreach (Match m in scriptPattern.Matches(s))
			{
				writer.Write(s.Substring(afterLastMatch, m.Index - afterLastMatch));
				afterLastMatch = m.Index + m.Length;

				var script = m.Groups[0].Value;

				var excludeMatch = excludePattern.Match(script);

				if (excludeMatch.Success)
				{
					writer.Write(script.Substring(0, excludeMatch.Index));
					writer.Write(script.Substring(excludeMatch.Index + excludeMatch.Length));
					continue;
				}

				var srcMatch = externalScriptPattern.Match(m.Groups["tag"].Value);

				if (srcMatch.Success)
				{
					Add(new Uri(baseUri, HttpUtility.HtmlDecode(srcMatch.Groups["src"].Value)));
				}
				else
				{
					Add(m.Groups["src"].Value);
				}
			}

			var remainder = s.Substring(afterLastMatch);

			var bodyIndex = remainder.IndexOf("</body>");
			if (bodyIndex != -1 && scripts.Count > 0)
			{
				return remainder.Insert(bodyIndex,
					string.Format(Settings.Default.CombinedScriptTag, Combine()));
			}

			return remainder;
		}
	}
}
