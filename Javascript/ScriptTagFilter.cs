using System.IO;
using System;
using System.Text;
using System.Web;
using System.Text.RegularExpressions;
namespace PimpMyWeb.Javascript
{
	/// <summary>
	/// Filters Javascript (external and inline) from the output,
	/// passes it on to a Combinator, and inserts a single combined
	/// script reference just before the &lt;/body&gt; tag.
	/// </summary>
	public class ScriptTagFilter : Stream
	{
		private static readonly Regex scriptPattern;
		private static readonly Regex externalScriptPattern;
		private static readonly Regex excludePattern;
		private static readonly string combinedScriptHtml;

		static ScriptTagFilter()
		{
			var settings = Settings.Default;
			var patternOptions = RegexOptions.IgnoreCase | RegexOptions.Compiled;
			scriptPattern = new Regex(settings.ScriptPattern, patternOptions);
			externalScriptPattern = new Regex(settings.ExternalScriptPattern, patternOptions);
			excludePattern = new Regex(settings.ExcludeScriptPattern, patternOptions);
			combinedScriptHtml = settings.CombinedScriptHtml;
		}

		private readonly ICombinator combinator;

		private readonly Uri baseUri;
		private readonly StreamWriter writer;
		private readonly Encoding encoding;
		private StringBuilder patternBuffer = new StringBuilder();

		public ScriptTagFilter()
			: this(
				HttpContext.Current.Response.Filter,
				HttpContext.Current.Response.ContentEncoding,
				HttpContext.Current.Request.Url,
				new Combinator()) { }

		public ScriptTagFilter(Stream stream, Encoding encoding, Uri baseUri, ICombinator combinator)
		{
			this.writer = new StreamWriter(stream, encoding);
			this.encoding = encoding;
			this.baseUri = baseUri;
			this.combinator = combinator;
		}

		#region Not Supported

		/// <summary>
		/// 
		/// </summary>
		public override bool CanRead
		{
			get { return false; }
		}

		/// <summary>
		/// 
		/// </summary>
		public override bool CanSeek
		{
			get { return false; }
		}

		/// <summary>
		/// 
		/// </summary>
		public override long Length
		{
			get { throw new NotSupportedException(); }
		}

		/// <summary>
		/// 
		/// </summary>
		public override long Position
		{
			get
			{
				throw new NotSupportedException();
			}
			set
			{
				throw new NotSupportedException();
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="offset"></param>
		/// <param name="count"></param>
		/// <returns></returns>
		public override int Read(byte[] buffer, int offset, int count)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="offset"></param>
		/// <param name="origin"></param>
		/// <returns></returns>
		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="value"></param>
		public override void SetLength(long value)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// 
		/// </summary>
		public override bool CanWrite
		{
			get { return true; }
		}
		#endregion

		/// <summary/>
		public override void Write(byte[] buffer, int offset, int count)
		{
			var s = encoding.GetString(buffer, offset, count);

			if (patternBuffer.Length > 0)
			{
				var idx = s.IndexOf('>');
				if (idx != -1)
				{
					patternBuffer.Append(s.Substring(0, idx + 1));
					EvaluatePatternBuffer();
					patternBuffer.Append(s.Substring(idx + 1));
				}
				else
				{
					patternBuffer.Append(s);
				}
			}
			else
			{
				var idx = s.IndexOf('<');
				if (idx != -1)
				{
					writer.Write(s.Substring(0, idx));
					patternBuffer.Append(s.Substring(idx));
				}
				else
				{
					writer.Write(s);
				}
			}
		}

		/// <summary/>
		public override void Flush()
		{
			if (patternBuffer.Length > 0)
			{
				EvaluatePatternBuffer();
				writer.Write(patternBuffer.ToString());
				patternBuffer.Remove(0, patternBuffer.Length);
			}
			writer.Flush();
		}

		private void EvaluatePatternBuffer()
		{
			var s = patternBuffer.ToString();
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
					combinator.Add(new Uri(baseUri, HttpUtility.HtmlDecode(srcMatch.Groups["src"].Value)));
				}
				else
				{
					combinator.Add(m.Groups["src"].Value);
				}
			}

			var remainder = s.Substring(afterLastMatch);
			patternBuffer = new StringBuilder(remainder);

			var bodyIndex = remainder.IndexOf("</body>");
			var combinedHash = combinator.Combine();
			if (bodyIndex != -1 && combinedHash != null)
			{
				patternBuffer.Insert(bodyIndex, string.Format(combinedScriptHtml, combinedHash));
			}
		}
	}
}