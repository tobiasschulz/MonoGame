#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

#region Using Statements
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics;
#endregion

namespace Microsoft.Xna.Framework.Content
{
	internal class EffectMaterialReader : ContentTypeReader<EffectMaterial>
	{
		#region Protected Read Method

		protected internal override EffectMaterial Read(
			ContentReader input,
			EffectMaterial existingInstance
		) {
			Effect effect = input.ReadExternalReference<Effect>();
			EffectMaterial effectMaterial = new EffectMaterial(effect);
			Dictionary<string, object> dict = input.ReadObject<Dictionary<string, object>>();
			foreach (KeyValuePair<string, object> item in dict) {
				EffectParameter parameter = effectMaterial.Parameters[item.Key];
				if (parameter != null) {
					if (typeof(Texture).IsAssignableFrom(item.Value.GetType()))
					{
						parameter.SetValue((Texture) item.Value);
					}
					else
					{
						throw new NotImplementedException();
					}
				}
				else
				{
					Debug.WriteLine("No parameter " + item.Key);
				}
			}
			return effectMaterial;
		}

		#endregion
	}
}
