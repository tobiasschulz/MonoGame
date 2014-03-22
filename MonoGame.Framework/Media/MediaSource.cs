#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

using System.Collections.Generic;

namespace Microsoft.Xna.Framework.Media
{
	public sealed class MediaSource
	{
		internal MediaSource(string name, MediaSourceType type)
		{
			Name = name;
			MediaSourceType = type;
		}

		public MediaSourceType MediaSourceType
		{
			get;
			private set;
		}

		public string Name
		{
			get;
			private set;
		}

		public static IList<MediaSource> GetAvailableMediaSources()
		{
			MediaSource[] result = 
			{ 
				new MediaSource("DummyMediaSource", MediaSourceType.LocalDevice)
			};

			return result;
		}
	}
}
