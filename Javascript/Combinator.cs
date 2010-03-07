using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PimpMyWeb.Javascript
{
	public interface ICombinator
	{
		void Add(Uri script);
		void Add(string script);
		int? Combine();
	}

	internal class Combinator : ICombinator
	{
		IResourceRepository repository = ResourceRepository.Current;
		int hash;
		List<int> scripts = new List<int>();
		
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

			repository.Add(hash, scripts.ToArray());
			return hash;
		}
	}
}
