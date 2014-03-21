#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Xna.Framework.Media
{
	public class SongCollection : ICollection<Song>, IEnumerable<Song>, IEnumerable, IDisposable
	{
		private bool isReadOnly = false;
		private List<Song> innerlist = new List<Song>();

		public void Dispose()
		{
		}

		public IEnumerator<Song> GetEnumerator()
		{
			return innerlist.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return innerlist.GetEnumerator();
		}

		public int Count
		{
			get
			{
				return innerlist.Count;
			}
		}

		public bool IsReadOnly
		{
			get
			{
				return isReadOnly;
			}
		}

		public Song this[int index]
		{
			get
			{
				return innerlist[index];
			}
		}

		public void Add(Song item)
		{
			if (item == null)
			{
				throw new ArgumentNullException();
			}

			if (innerlist.Count == 0)
			{
				innerlist.Add(item);
				return;
			}

			for (int i = 0; i < innerlist.Count; i += 1)
			{
				if (item.TrackNumber < innerlist[i].TrackNumber)
				{
					innerlist.Insert(i, item);
					return;
				}
			}

			innerlist.Add(item);
		}

		public void Clear()
		{
			innerlist.Clear();
		}

		public SongCollection Clone()
		{
			SongCollection sc = new SongCollection();
			foreach (Song song in innerlist)
			{
				sc.Add(song);
			}

			return sc;
		}

		public bool Contains(Song item)
		{
			return innerlist.Contains(item);
		}

		public void CopyTo(Song[] array, int arrayIndex)
		{
			innerlist.CopyTo(array, arrayIndex);
		}

		public int IndexOf(Song item)
		{
			return innerlist.IndexOf(item);
		}

		public bool Remove(Song item)
		{
			return innerlist.Remove(item);
		}
	}
}

