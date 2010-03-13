using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Yahoo.Yui.Compressor;

namespace PimpMyWeb.Javascript
{
	public interface IScriptCombinator
	{
		void Add(Uri script);
		void Add(string script);
		int? Combine();
	}

	internal class ScriptCombinator : IScriptCombinator
	{
		IResourceRepository repository;
		int hash;
		List<int> scripts = new List<int>();

		public ScriptCombinator(IResourceRepository repository)
		{
			this.repository = repository;
		}

		public void Add(Uri script)
		{
			repository.Add(script);
			scripts.Add(script.GetHashCode());
			hash ^= script.GetHashCode();
		}

		public void Add(string script)
		{
			repository.Add(script);
			scripts.Add(script.GetHashCode());
			hash ^= script.GetHashCode();
		}
		 
		public int? Combine()
		{
			if (scripts.Count < 1)
			{
				return null;
			}

			repository.Add(hash, scripts.ToArray(),
				JavaScriptCompressor.Compress);
			return hash;
		}
	}
}
