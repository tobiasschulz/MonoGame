#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

using System;
using System.IO;

namespace Microsoft.Xna.Framework.Media
{
	public class MediaLibrary : IDisposable
	{
		#region Public Properties

		public SongCollection Songs
		{
			get
			{
				/* This is meant to return a pre-made collection, based on the
				 * WMP library.
				 * -flibit
				 */
				return new SongCollection();
			}
		}

		#endregion

		#region Private Properties

		private PlaylistCollection PlayLists
		{
			get;
			set;
		}

		#endregion

		#region Public Constructors and Dispose Method

		public MediaLibrary()
		{
		}

		public MediaLibrary(MediaSource mediaSource)
		{
		}

		public void Dispose()
		{
		}

		#endregion

		#region Public Methods

		public void SavePicture(string name, byte[] imageBuffer)
		{
			// On XNA4, this fails on Windows/Xbox. Only Phone is supported.
			throw new NotSupportedException();
		}

		public void SavePicture(string name, Stream source)
		{
			// On XNA4, this fails on Windows/Xbox. Only Phone is supported.
			throw new NotSupportedException();
		}

		#endregion

	}
}
