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
	internal class UInt16Reader : ContentTypeReader<ushort>
	{
		internal UInt16Reader()
		{
		}

		protected internal override ushort Read(
			ContentReader input,
			ushort existingInstance
		) {
			return input.ReadUInt16();
		}
	}
}
