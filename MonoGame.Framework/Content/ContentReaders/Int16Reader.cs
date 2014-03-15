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
	internal class Int16Reader : ContentTypeReader<short>
	{
		internal Int16Reader()
		{
		}

		protected internal override short Read(
			ContentReader input,
			short existingInstance
		) {
			return input.ReadInt16();
		}
	}
}
