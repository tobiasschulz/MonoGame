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
	public sealed class PlaylistCollection	: ICollection<Playlist>, IEnumerable<Playlist>,
						  IEnumerable, IDisposable
	{
		private List<Playlist> innerlist;

		internal PlaylistCollection()
		{
			IsReadOnly = false;
			innerlist = new List<Playlist>();
		}

		public void Dispose()
		{
			innerlist.Clear();
		}

		public IEnumerator<Playlist> GetEnumerator()
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
			get;
			private set;
		}

		public Playlist this[int index]
		{
			get
			{
				return innerlist[index];
			}
		}

		public void Add(Playlist item)
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
				if (item.Duration < innerlist[i].Duration)
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

		public PlaylistCollection Clone()
		{
			PlaylistCollection plc = new PlaylistCollection();
			foreach (Playlist playlist in this.innerlist)
			{
				plc.Add(playlist);
			}

			return plc;
		}

		public bool Contains(Playlist item)
		{
			return innerlist.Contains(item);
		}

		public void CopyTo(Playlist[] array, int arrayIndex)
		{
			innerlist.CopyTo(array, arrayIndex);
		}

		public int IndexOf(Playlist item)
		{
			return innerlist.IndexOf(item);
		}

		public bool Remove(Playlist item)
		{
			return innerlist.Remove(item);
		}
	}
}
