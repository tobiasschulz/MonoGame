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
		private PlaylistCollection PlayLists {
			get;
			set;
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

		public void SavePicture (string name, byte[] imageBuffer)
		{
			// Only is relivant on mobile devices, should throw error on desktops.
			throw new NotSupportedException ();
		}

		public void SavePicture (string name, Stream source)
		{
			// Only is relivant on mobile devices, should throw error on desktops.
			throw new NotSupportedException ();
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

