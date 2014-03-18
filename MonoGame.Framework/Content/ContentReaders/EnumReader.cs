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
	public class EnumReader<T> : ContentTypeReader<T>
	{
		ContentTypeReader elementReader;

		public EnumReader()
		{
		}

		protected internal override void Initialize(ContentTypeReaderManager manager)
		{
			Type readerType = Enum.GetUnderlyingType(typeof(T));
			elementReader = manager.GetTypeReader(readerType);
		}

		protected internal override T Read(ContentReader input, T existingInstance)
		{
			return input.ReadRawObject<T>(elementReader);
		}
	}
}
