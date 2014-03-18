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
using System.Text;

using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Microsoft.Xna.Framework.Content
{
	internal class BooleanReader : ContentTypeReader<bool>
	{
		internal BooleanReader()
		{
		}

		protected internal override bool Read(
			ContentReader input,
			bool existingInstance
		) {
			return input.ReadBoolean();
		}
	}
}
