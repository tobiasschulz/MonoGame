#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
* Copyright 2009-2014 Ethan Lee and the MonoGame Team
*
* Released under the Microsoft Public License.
* See LICENSE for details.
*/
#endregion

using System;
using Microsoft.Xna.Framework;

namespace Microsoft.Xna.Framework.Content
{
	internal class BoundingSphereReader : ContentTypeReader<BoundingSphere>
	{
		internal BoundingSphereReader()
		{
		}

		protected internal override BoundingSphere Read(
			ContentReader input,
			BoundingSphere existingInstance
		) {
			Vector3 center = input.ReadVector3();
			float radius = input.ReadSingle();
			return new BoundingSphere(center, radius);
		}
	}
}
