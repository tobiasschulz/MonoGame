#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

using System;

namespace Microsoft.Xna.Framework.Media
{
    public sealed class Playlist : IDisposable
    {
        public TimeSpan Duration
        {
            get;
			internal set;
        }

        public string Name
        {
            get;
			internal set;
        }

		public void Dispose()
        {
        }

		
        /*public SongCollection Songs
        {
            get
            {
            }
        }*/
    }
}

