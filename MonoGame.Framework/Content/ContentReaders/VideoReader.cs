#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

#region Using Statements
using System;
using System.IO;

using Microsoft.Xna.Framework.Media;
#endregion

namespace Microsoft.Xna.Framework.Content
{
	internal class VideoReader : ContentTypeReader<Video>
	{
		#region Private Supported File Extensions Variable

		static string[] supportedExtensions = new string[] {
			".ogv", ".ogg"
		};

		#endregion

		#region Internal Filename Normalizer Method

		internal static string Normalize(string fileName)
		{
			return Normalize(fileName, supportedExtensions);
		}

		#endregion

		#region Protected Read Method

		protected internal override Video Read(
			ContentReader input,
			Video existingInstance
		) {
			string path = input.ReadObject<string>();
			path = Path.Combine(input.ContentManager.RootDirectory, path);
			path = TitleContainer.GetFilename(path);

			/*int durationMS =*/ input.ReadObject<int>();
			/*int width =*/ input.ReadObject<int>();
			/*int height =*/ input.ReadObject<int>();
			/*float framesPerSecond =*/ input.ReadObject<Single>();
			// 0 = Music, 1 = Dialog, 2 = Music and dialog
			/*int soundTrackType =*/ input.ReadObject<int>();
			return new Video(path);
		}

		#endregion
	}
}
