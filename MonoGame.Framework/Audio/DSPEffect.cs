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

using OpenTK.Audio.OpenAL;
#endregion

namespace Microsoft.Xna.Framework.Audio
{
	/* This class is meant to be a compact container for platform-specific
	 * effect work. Keep general XACT stuff out of here.
	 * -flibit
	 */
	internal abstract class DSPEffect
	{
		#region Public Properties

		public int Handle
		{
			get;
			private set;
		}

		#endregion

		#region Protected Variables

		protected int effectHandle;

		#endregion

		#region Public Constructor

		public DSPEffect()
		{
			// Obtain EFX entry points
			EffectsExtension EFX = OpenALDevice.Instance.EFX;

			// Generate the EffectSlot and Effect
			Handle = EFX.GenAuxiliaryEffectSlot();
			effectHandle = EFX.GenEffect();
		}

		#endregion

		#region Public Dispose Method

		public void Dispose()
		{
			// Obtain EFX entry points
			EffectsExtension EFX = OpenALDevice.Instance.EFX;

			// Delete EFX data
			EFX.DeleteAuxiliaryEffectSlot(Handle);
			EFX.DeleteEffect(effectHandle);
		}

		#endregion
	}

	internal class DSPReverbEffect : DSPEffect
	{
		#region Public Constructor

		public DSPReverbEffect(DSPParameter[] parameters) : base()
		{
			// Obtain EFX entry points
			EffectsExtension EFX = OpenALDevice.Instance.EFX;

			// Set up the Reverb Effect
			EFX.BindEffect(effectHandle, EfxEffectType.EaxReverb);

			// TODO: Use DSP Parameters on EAXReverb Effect. They don't bind very cleanly. :/

			// Bind the Effect to the EffectSlot. XACT will use the EffectSlot.
			EFX.BindEffectToAuxiliarySlot(Handle, effectHandle);
		}

		#endregion

		#region Public Methods

		public void SetGain(float value)
		{
			// Obtain EFX entry points
			EffectsExtension EFX = OpenALDevice.Instance.EFX;

			// Apply the value to the effect
			EFX.Effect(
				effectHandle,
				EfxEffectf.EaxReverbGain,
				value
			);

			// Apply the newly modified effect to the effect slot
			EFX.BindEffectToAuxiliarySlot(Handle, effectHandle);
		}

		public void SetDecayTime(float value)
		{
			// TODO
		}

		public void SetDensity(float value)
		{
			// TODO
		}

		#endregion
	}
}
