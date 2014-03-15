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
	internal class SingleReader : ContentTypeReader<float>
	{
		internal SingleReader()
		{
		}

		protected internal override float Read(
			ContentReader input,
			float existingInstance
		) {
			return input.ReadSingle();
		}
	}
}
