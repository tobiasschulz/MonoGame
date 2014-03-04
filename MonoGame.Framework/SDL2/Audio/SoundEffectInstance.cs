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

using OpenTK.Audio.OpenAL;

using Microsoft.Xna.Framework;

#endregion Statements

namespace Microsoft.Xna.Framework.Audio
{
    /// <summary>
    /// Implements the SoundEffectInstance, which is used to access high level features of a SoundEffect. This class uses the OpenAL
    /// sound system to play and control the sound effects. Please refer to the OpenAL 1.x specification from Creative Labs to better
    /// understand the features provides by SoundEffectInstance. 
    /// </summary>
    public class SoundEffectInstance : IDisposable
    {
        #region Private Variables: XNA Implementation

        private SoundEffect INTERNAL_parentEffect;

        /* MonoGame XACT wraps around SoundEffect for audio output.
         * Only problem: XACT pitch has no boundaries, SoundEffect does.
         * So, we're going to use this to tell the pitch clamp to STFU.
         * -flibit
         */
        internal bool INTERNAL_isXACTSource = false;

        #endregion

        #region Private Variables: OpenAL Source, EffectSlot

        protected int INTERNAL_alSource = -1;
        private int INTERNAL_alEffectSlot = -1;

        #endregion
  
        #region Private Variables: 3D Audio

        protected Vector3 position = new Vector3(0.0f, 0.0f, 0.1f);

        // Used to prevent outdated positional audio data from being used
        protected bool INTERNAL_positionalAudio = false;

        #endregion

        #region Private XNA-to-OpenAL Pitch Converter

        private float INTERNAL_XNA_To_AL_Pitch(float xnaPitch)
        {
            /* XNA sets pitch bounds to [-1.0f, 1.0f], each end being one octave.
             * OpenAL's AL_PITCH boundaries are (0.0f, INF).
             * Consider the function f(x) = 2 ^ x
             * The domain is (-INF, INF) and the range is (0, INF).
             * 0.0f is the original pitch for XNA, 1.0f is the original pitch for OpenAL.
             * Note that f(0) = 1, f(1) = 2, f(-1) = 0.5, and so on.
             * XNA's pitch values are on the domain, OpenAL's are on the range.
             * Remember: the XNA limit is arbitrarily between two octaves on the domain.
             * To convert, we just plug XNA pitch into f(x).
             * -flibit
             */
            if (!INTERNAL_isXACTSource && (xnaPitch < -1.0f || xnaPitch > 1.0f))
            {
                throw new Exception("XNA PITCH MUST BE WITHIN [-1.0f, 1.0f]!");
            }
            return (float) Math.Pow(2, xnaPitch);
        }

        #endregion

        #region Constructors, Deconstructors, Dispose Method

        internal SoundEffectInstance(SoundEffect parent)
        {
            INTERNAL_parentEffect = parent;
        }

        ~SoundEffectInstance()
        {
            Dispose();
        }

        public virtual void Dispose()
        {
            if (!IsDisposed)
            {
                Stop(true);
                IsDisposed = true;
            }
        }

        #endregion

        #region Public Properties

        public bool IsDisposed
        {
            get;
            protected set;
        }

        private bool INTERNAL_looped = false;
        public virtual bool IsLooped
        {
            get
            {
                return INTERNAL_looped;
            }
            set
            {
                INTERNAL_looped = value;
                if (INTERNAL_alSource != -1)
                {
                    AL.Source(INTERNAL_alSource, ALSourceb.Looping, INTERNAL_looped);
                }
            }
        }

        private float INTERNAL_pan = 0.0f;
        public float Pan
        {
            get
            {
                return INTERNAL_pan;
            }
            set
            {
                INTERNAL_pan = value;
                if (INTERNAL_alSource != -1)
                {
                    AL.Source(
                        INTERNAL_alSource,
                        ALSource3f.Position,
                        INTERNAL_pan,
                        0.0f,
                        (float) Math.Sqrt(1 - Math.Pow(INTERNAL_pan, 2))
                    );
                }
            }
        }

        private float INTERNAL_pitch = 0f;
        public float Pitch
        {
            get
            {
                return INTERNAL_pitch;
            }
            set
            {
                INTERNAL_pitch = value;
                if (INTERNAL_alSource != -1)
                {
                    AL.Source(INTERNAL_alSource, ALSourcef.Pitch, INTERNAL_XNA_To_AL_Pitch(INTERNAL_pitch));
                }
            }
        }

        public SoundState State
        {
            get
            {
                if (INTERNAL_alSource == -1)
                {
                    return SoundState.Stopped;
                }
                ALSourceState state = AL.GetSourceState(INTERNAL_alSource);
                if (state == ALSourceState.Playing)
                {
                    return SoundState.Playing;
                }
                else if (state == ALSourceState.Paused)
                {
                    return SoundState.Paused;
                }
                return SoundState.Stopped;
            }
        }

        private float INTERNAL_volume = 1.0f;
        public float Volume
        {
            get
            {
                return INTERNAL_volume;
            }
            set
            {
                INTERNAL_volume = value;
                if (INTERNAL_alSource != -1)
                {
                    AL.Source(INTERNAL_alSource, ALSourcef.Gain, INTERNAL_volume * SoundEffect.MasterVolume);
                }
            }
        }

        #endregion

        #region Public 3D Audio Methods

        public void Apply3D(AudioListener listener, AudioEmitter emitter)
        {
            if (INTERNAL_alSource == -1)
            {
                return;
            }

            // Set up orientation matrix
            Matrix orientation = Matrix.CreateWorld(Vector3.Zero, listener.Forward, listener.Up);

            // Set up our final position according to orientation of listener
            position = Vector3.Transform(emitter.Position - listener.Position, orientation);
            if (position != Vector3.Zero)
            {
                position.Normalize();
            }

            // Set the position based on relative positon
            AL.Source(INTERNAL_alSource, ALSource3f.Position, position.X, position.Y, position.Z);

            // We positional now
            INTERNAL_positionalAudio = true;
        }

        public void Apply3D(AudioListener[] listeners, AudioEmitter emitter)
        {
            throw new NotSupportedException("OpenAL can only make use of one listener.");
        }

        #endregion

        #region Public Playback Methods

        public virtual void Play()
        {
            if (State != SoundState.Stopped)
            {
                // FIXME: Is this XNA4 behavior?
                Stop();
            }

            if (INTERNAL_alSource != -1)
            {
                // The sound has stopped, but hasn't cleaned up yet...
                AL.SourceStop(INTERNAL_alSource);
                AL.DeleteSource(INTERNAL_alSource);
                INTERNAL_alSource = -1;
            }

            INTERNAL_alSource = AL.GenSource();
            if (INTERNAL_alSource == 0)
            {
                System.Console.WriteLine("WARNING: AL SOURCE WAS NOT AVAILABLE. SKIPPING.");
                return;
            }
   
            // Attach the buffer to this source
            AL.Source(
                INTERNAL_alSource,
                ALSourcei.Buffer,
                INTERNAL_parentEffect.INTERNAL_buffer
            );

            // Apply Pan/Position
            if (INTERNAL_positionalAudio)
            {
                INTERNAL_positionalAudio = false;
                AL.Source(INTERNAL_alSource, ALSource3f.Position, position.X, position.Y, position.Z);
            }
            else
            {
                Pan = Pan;
            }

            // Reassign Properties, in case the AL properties need to be applied.
            Volume = Volume;
            IsLooped = IsLooped;
            Pitch = Pitch;

            // Apply EFX
            if (INTERNAL_alEffectSlot != -1)
            {
                AL.Source(
                    INTERNAL_alSource,
                    ALSource3i.EfxAuxiliarySendFilter,
                    INTERNAL_alEffectSlot,
                    0,
                    0
                );
            }

            AL.SourcePlay(INTERNAL_alSource);
        }

        public void Pause()
        {
            if (INTERNAL_alSource != -1 && State == SoundState.Playing)
            {
                AL.SourcePause(INTERNAL_alSource);
            }
        }

        public void Resume()
        {
            if (INTERNAL_alSource != -1 && State == SoundState.Paused)
            {
                AL.SourcePlay(INTERNAL_alSource);
            }
        }

        public void Stop()
        {
            if (INTERNAL_alSource != -1)
            {
                AL.SourceStop(INTERNAL_alSource);
                AL.DeleteSource(INTERNAL_alSource);
                INTERNAL_alSource = -1;
            }
        }

        public void Stop(bool immediate)
        {
            Stop();
        }

        #endregion

        #region Internal EFX Methods

        internal void INTERNAL_applyEffect(int effectSlotHandle)
        {
            INTERNAL_alEffectSlot = effectSlotHandle;
            if (INTERNAL_alSource != -1)
            {
                AL.Source(
                    INTERNAL_alSource,
                    ALSource3i.EfxAuxiliarySendFilter,
                    INTERNAL_alEffectSlot,
                    0,
                    0
                );
            }
        }

        #endregion
    }
}