#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Xna.Framework.Media
{
	public class MediaLibrary : IDisposable
	{
		private PlaylistCollection _playLists = null;
		private PlaylistCollection PlayLists
		{
			get
			{
				return _playLists;
			}
			set
			{
				_playLists = value;
			}
		}

		public MediaLibrary()
		{
		}

		public MediaLibrary(MediaSource mediaSource)
		{
		}

		public void Dispose()
		{
		}

		public SongCollection Songs
		{
			get
			{
				return new SongCollection();
			}
		}

	}
}

