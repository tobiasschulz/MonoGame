#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */

/* Author: Kenneth James Pouncey */
#endregion

using System;

using Microsoft.Xna.Framework.Content;

namespace Microsoft.Xna.Framework.Content
{
	internal class Vector2Reader : ContentTypeReader<Vector2>
	{
		internal Vector2Reader()
		{
		}

		protected internal override Vector2 Read(
			ContentReader input,
			Vector2 existingInstance
		) {
			return input.ReadVector2();
		}
	}
}
