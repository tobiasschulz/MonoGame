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

namespace Microsoft.Xna.Framework.Media
{
	public sealed class MediaSource
	{
		private MediaSourceType _type;
		private string _name;

		internal MediaSource(string name, MediaSourceType type)
		{
			_name = name;
			_type = type;
		}

		public Microsoft.Xna.Framework.Media.MediaSourceType MediaSourceType
		{
			get
			{
				return _type;
			}
		}

		public string Name
		{
			get
			{
				return _name;
			}
		}

		public static IList<MediaSource> GetAvailableMediaSources()
		{
			MediaSource[] result = { new MediaSource("DumpMediaSource",
						 MediaSourceType.LocalDevice) };

			return result;
		}
	}
}
