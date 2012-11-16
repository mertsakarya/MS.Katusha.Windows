using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Raven.Database.Util
{
	public class ConcurrentSet<T> : IEnumerable<T>
	{
		readonly ConcurrentDictionary<T,object> inner = new ConcurrentDictionary<T, object>();

		public int Count
		{
			get { return inner.Count; }
		}

		public void Add(T item)
		{
			TryAdd(item);
		}

		public bool TryAdd(T item)
		{
			return inner.TryAdd(item, null);
		}

		public bool Contains(T item)
		{
			return inner.ContainsKey(item);
		}

		public bool TryRemove(T item)
		{
			object _;
			return inner.TryRemove(item, out _);
		}

		public IEnumerator<T> GetEnumerator()
		{
			return inner.Keys.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}