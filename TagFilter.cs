using System.IO;
using System;
using System.Text;
using System.Web;
using System.Text.RegularExpressions;
using PimpMyWeb.Javascript;

namespace PimpMyWeb
{
	internal class TagFilter : Stream
	{
		public delegate string TagFilterAction(string buffer, StreamWriter writer);

		public event TagFilterAction Filter;

		private readonly StreamWriter writer;
		private readonly Encoding encoding;
		private StringBuilder patternBuffer = new StringBuilder();

		public TagFilter()
			: this(
				HttpContext.Current.Response.Filter,
				HttpContext.Current.Response.ContentEncoding) { }

		public TagFilter(Stream stream, Encoding encoding)
		{
			this.writer = new StreamWriter(stream, encoding);
			this.encoding = encoding;
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
			if (Filter != null)
			{
				var buffer = patternBuffer.ToString();

				foreach (TagFilterAction filter in Filter.GetInvocationList())
				{
					buffer = filter(buffer, writer);
				}

				patternBuffer = new StringBuilder(buffer);
			}
			else
			{
				writer.Write(patternBuffer);
				patternBuffer = new StringBuilder();
			}
		}
	}
}