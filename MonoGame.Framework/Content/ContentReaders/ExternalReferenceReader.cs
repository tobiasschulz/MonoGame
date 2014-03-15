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
using System.Linq;
using System.Text;

namespace Microsoft.Xna.Framework.Content
{
	/// <summary>
	/// External reference reader, provided for compatibility with XNA Framework built content
	/// </summary>
	internal class ExternalReferenceReader : ContentTypeReader
	{
		public ExternalReferenceReader() : base(null)
		{

		}

		protected internal override object Read(
			ContentReader input,
			object existingInstance
		) {
			return input.ReadExternalReference<object>();
		}
	}
}
