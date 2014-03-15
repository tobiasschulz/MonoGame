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
	internal class UInt64Reader : ContentTypeReader<ulong>
	{
		internal UInt64Reader()
		{
		}

		protected internal override ulong Read(
			ContentReader input,
			ulong existingInstance
		) {
			return input.ReadUInt64();
		}
	}
}
