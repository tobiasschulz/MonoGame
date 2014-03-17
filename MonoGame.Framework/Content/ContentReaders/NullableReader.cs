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
	internal class NullableReader<T> : ContentTypeReader<T?> where T : struct
	{
		ContentTypeReader elementReader;

		internal NullableReader()
		{
		}

		protected internal override void Initialize(ContentTypeReaderManager manager)
		{
			Type readerType = typeof(T);
			elementReader = manager.GetTypeReader(readerType);
		}

		protected internal override T? Read(ContentReader input, T? existingInstance)
		{
			if (input.ReadBoolean())
			{
				return input.ReadObject<T>(elementReader);
			}
			return null;
		}
	}
}
