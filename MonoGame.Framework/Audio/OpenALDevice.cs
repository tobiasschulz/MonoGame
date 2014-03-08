#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE.txt for details.
 */
#endregion

#region Using Statements
using System;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Audio.OpenAL;
#endregion

namespace Microsoft.Xna.Framework.Audio
{
	internal sealed class OpenALDevice
	{
		#region The OpenAL Device Instance

		public static OpenALDevice Instance
		{
			get;
			private set;
		}

		#endregion

		#region Public EFX Entry Points

		public EffectsExtension EFX
		{
			get;
			private set;
		}

		#endregion

		#region Private ALC Variables

		// OpenAL Device/Context Handles
		private IntPtr alDevice;
		private ContextHandle alContext;

		#endregion

		#region Private SoundEffect Management Variables

		// Used to store SoundEffectInstances generated internally.
		internal List<SoundEffectInstance> instancePool;

		// Used to store all DynamicSoundEffectInstances, to check buffer counts.
		internal List<DynamicSoundEffectInstance> dynamicInstancePool;

		#endregion

		private void CheckALError()
		{
			ALError err = AL.GetError();

			if (err == ALError.NoError)
			{
				return;
			}

			System.Console.WriteLine("OpenAL Error: " + err);
		}

		private bool CheckALCError(string message)
		{
			AlcError err = Alc.GetError(alDevice);

			if (err == AlcError.NoError)
			{
				return false;
			}

			throw new Exception(message + " - OpenAL Device Error: " + err);
		}

		public OpenALDevice()
		{
			if (Instance != null)
			{
				throw new Exception("OpenALDevice already created!");
			}

			alDevice = Alc.OpenDevice(string.Empty);
			if (CheckALCError("Could not open AL device") || alDevice == IntPtr.Zero)
			{
				throw new Exception("Could not open AL device!");
			}

			int[] attribute = new int[0];
			alContext = Alc.CreateContext(alDevice, attribute);
			if (CheckALCError("Could not create OpenAL context") || alContext == ContextHandle.Zero)
			{
				Dispose();
				throw new Exception("Could not create OpenAL context");
			}

			Alc.MakeContextCurrent(alContext);
			if (CheckALCError("Could not make OpenAL context current"))
			{
				Dispose();
				throw new Exception("Could not make OpenAL context current");
			}

			EFX = new EffectsExtension();

			float[] ori = new float[]
			{
				0.0f, 0.0f, -1.0f, 0.0f, 1.0f, 0.0f
			};
			AL.Listener(ALListenerfv.Orientation, ref ori);
			AL.Listener(ALListener3f.Position, 0.0f, 0.0f, 0.0f);
			AL.Listener(ALListener3f.Velocity, 0.0f, 0.0f, 0.0f);
			AL.Listener(ALListenerf.Gain, 1.0f);

			// We do NOT use automatic attenuation! XNA does not do this!
			AL.DistanceModel(ALDistanceModel.None);

			instancePool = new List<SoundEffectInstance>();
			dynamicInstancePool = new List<DynamicSoundEffectInstance>();

			Instance = this;
		}

		public void Dispose()
		{
			Alc.MakeContextCurrent(ContextHandle.Zero);
			if (alContext != ContextHandle.Zero)
			{
				Alc.DestroyContext(alContext);
				alContext = ContextHandle.Zero;
			}
			if (alDevice != IntPtr.Zero)
			{
				Alc.CloseDevice(alDevice);
				alDevice = IntPtr.Zero;
			}
			Instance = null;
		}

		public void Update()
		{
#if DEBUG
			CheckALError();
#endif
			for (int i = 0; i < instancePool.Count; i++)
			{
				if (instancePool[i].State == SoundState.Stopped)
				{
					instancePool[i].Dispose();
					instancePool.RemoveAt(i);
					i--;
				}
			}

			for (int i = 0; i < dynamicInstancePool.Count; i++)
			{
				if (!dynamicInstancePool[i].Update())
				{
					dynamicInstancePool.Remove(dynamicInstancePool[i]);
					i -= 1;
				}
			}
		}
	}
}
