#region License
// /*
// Microsoft Public License (Ms-PL)
// MonoGame - Copyright © 2009 The MonoGame Team
// 
// All rights reserved.
// 
// This license governs use of the accompanying software. If you use the software, you accept this license. If you do not
// accept the license, do not use the software.
// 
// 1. Definitions
// The terms "reproduce," "reproduction," "derivative works," and "distribution" have the same meaning here as under
// U.S. copyright law.
// 
// A "contribution" is the original software, or any additions or changes to the software.
// A "contributor" is any person that distributes its contribution under this license.
// "Licensed patents" are a contributor's patent claims that read directly on its contribution.
// 
// 2. Grant of Rights
// (A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, 
// each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution, prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
// (B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, 
// each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or derivative works of the contribution in the software.
// 
// 3. Conditions and Limitations
// (A) No Trademark License- This license does not grant you rights to use any contributors' name, logo, or trademarks.
// (B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software, 
// your patent license from such contributor to the software ends automatically.
// (C) If you distribute any portion of the software, you must retain all copyright, patent, trademark, and attribution 
// notices that are present in the software.
// (D) If you distribute any portion of the software in source code form, you may do so only under this license by including 
// a complete copy of this license with your distribution. If you distribute any portion of the software in compiled or object 
// code form, you may only do so under a license that complies with this license.
// (E) The software is licensed "as-is." You bear the risk of using it. The contributors give no express warranties, guarantees
// or conditions. You may have additional consumer rights under your local laws which this license cannot change. To the extent
// permitted under your local laws, the contributors exclude the implied warranties of merchantability, fitness for a particular
// purpose and non-infringement.
// */
#endregion License

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