#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

using System;

namespace Microsoft.Xna.Framework.Content
{
	internal class ByteReader : ContentTypeReader<byte>
	{
		internal ByteReader()
		{
		}

		protected internal override byte Read(
			ContentReader input,
			byte existingInstance
		) {
			return input.ReadByte();
		}
	}
}
