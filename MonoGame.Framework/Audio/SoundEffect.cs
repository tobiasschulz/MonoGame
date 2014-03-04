#region License
/*
Microsoft Public License (Ms-PL)
MonoGame - Copyright © 2009 The MonoGame Team

All rights reserved.

This license governs use of the accompanying software. If you use the software, you accept this license. If you do not
accept the license, do not use the software.

1. Definitions
The terms "reproduce," "reproduction," "derivative works," and "distribution" have the same meaning here as under 
U.S. copyright law.

A "contribution" is the original software, or any additions or changes to the software.
A "contributor" is any person that distributes its contribution under this license.
"Licensed patents" are a contributor's patent claims that read directly on its contribution.

2. Grant of Rights
(A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, 
each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution, prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
(B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, 
each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or derivative works of the contribution in the software.

3. Conditions and Limitations
(A) No Trademark License- This license does not grant you rights to use any contributors' name, logo, or trademarks.
(B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software, 
your patent license from such contributor to the software ends automatically.
(C) If you distribute any portion of the software, you must retain all copyright, patent, trademark, and attribution 
notices that are present in the software.
(D) If you distribute any portion of the software in source code form, you may do so only under this license by including 
a complete copy of this license with your distribution. If you distribute any portion of the software in compiled or object 
code form, you may only do so under a license that complies with this license.
(E) The software is licensed "as-is." You bear the risk of using it. The contributors give no express warranties, guarantees
or conditions. You may have additional consumer rights under your local laws which this license cannot change. To the extent
permitted under your local laws, the contributors exclude the implied warranties of merchantability, fitness for a particular
purpose and non-infringement.
*/
#endregion License
﻿
#region Using Statements
using System;
using System.Collections.Generic;
using System.IO;

using Microsoft.Xna;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

using OpenTK.Audio.OpenAL;
#endregion

namespace Microsoft.Xna.Framework.Audio
{
    public sealed class SoundEffect : IDisposable
    {
        private bool isDisposed = false;

        #region Internal Audio Data

        internal int INTERNAL_buffer;

        #endregion

        #region Internal Constructors

        internal SoundEffect(string fileName)
        {
            if (fileName == string.Empty)
            {
                throw new FileNotFoundException("Supported Sound Effect formats are wav, mp3, acc, aiff");
            }

            Name = Path.GetFileNameWithoutExtension(fileName);

            Stream s;
            try
            {
                s = File.OpenRead(fileName);
            }
            catch (IOException e)
            {
                throw new Content.ContentLoadException("Could not load audio data", e);
            }

            INTERNAL_loadAudioStream(s);
            s.Close();
        }

        internal SoundEffect(Stream s)
        {
            INTERNAL_loadAudioStream(s);
        }

        internal SoundEffect(
            string name,
            byte[] buffer,
            uint sampleRate,
            uint channels,
            uint loopStart,
            uint loopLength,
            uint compressionAlign
        ) {
            Name = name;
            INTERNAL_bufferData(
                buffer,
                sampleRate,
                channels,
                loopStart,
                loopStart + loopLength,
                compressionAlign
            );
        }

        #endregion

        #region Public Constructors

        public SoundEffect(byte[] buffer, int sampleRate, AudioChannels channels)
        {
            INTERNAL_bufferData(
                buffer,
                (uint) sampleRate,
                (uint) channels,
                0,
                0,
                0
            );
        }

        public SoundEffect(byte[] buffer, int offset, int count, int sampleRate, AudioChannels channels, int loopStart, int loopLength)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Additional SoundEffect/SoundEffectInstance Creation Methods

        public SoundEffectInstance CreateInstance()
        {
            return new SoundEffectInstance(this);
        }

        public static SoundEffect FromStream(Stream stream)
        {            
            return new SoundEffect(stream);
        }

        #endregion

        #region Play

        public bool Play()
        {
            // FIXME: Perhaps MasterVolume should be applied to alListener? -flibit
            return Play(MasterVolume, 0.0f, 0.0f);
        }

        public bool Play(float volume, float pitch, float pan)
        {
            SoundEffectInstance instance = CreateInstance();
            instance.Volume = volume;
            instance.Pitch = pitch;
            instance.Pan = pan;
            instance.Play();
            if (instance.State != SoundState.Playing)
            {
                // Ran out of AL sources, probably.
                instance.Dispose();
                return false;
            }
            OpenALDevice.Instance.instancePool.Add(instance);
            return true;
        }

        #endregion

        #region Public Properties

        public TimeSpan Duration
        {
            get;
            private set;
        }

        public string Name
        {
            get;
            set;
        }

        #endregion

        #region Static Members

        public static float MasterVolume 
        { 
            get;
            set;
        }

        private static float INTERNAL_distanceScale = 1.0f;
        public static float DistanceScale
        {
            get
            {
                return INTERNAL_distanceScale;
            }
            set
            {
                if (value <= 0.0f)
                {
                    throw new ArgumentOutOfRangeException("value of DistanceScale");
                }
                INTERNAL_distanceScale = value;
            }
        }

        private static float INTERNAL_dopplerScale = 1.0f;
        public static float DopplerScale
        {
            get
            {
                return INTERNAL_dopplerScale;
            }
            set
            {
                if (value <= 0.0f)
                {
                    throw new ArgumentOutOfRangeException("value of DistanceScale");
                }
                INTERNAL_dopplerScale = value;
            }
        }

        public static float SpeedOfSound
        {
            get;
            set;
        }

        #endregion

        #region IDisposable Members

        public bool IsDisposed
        {
            get;
            private set;
        }

        public void Dispose()
        {
            if (!IsDisposed)
            {
                AL.DeleteBuffer(INTERNAL_buffer);
                IsDisposed = true;
            }
        }

        #endregion

        #region Additional OpenAL SoundEffect Code

        private void INTERNAL_loadAudioStream(Stream s)
        {
            byte[] data;
            uint sampleRate = 0;
            uint numChannels = 0;

            using (BinaryReader reader = new BinaryReader(s))
            {
                // RIFF Signature
                string signature = new string(reader.ReadChars(4));
                if (signature != "RIFF")
                {
                    throw new NotSupportedException("Specified stream is not a wave file.");
                }

                reader.ReadUInt32(); // Riff Chunk Size

                string wformat = new string(reader.ReadChars(4));
                if (wformat != "WAVE")
                {
                    throw new NotSupportedException("Specified stream is not a wave file.");
                }

                // WAVE Header
                string format_signature = new string(reader.ReadChars(4));
                while (format_signature != "fmt ")
                {
                    reader.ReadBytes(reader.ReadInt32());
                    format_signature = new string(reader.ReadChars(4));
                }

                int format_chunk_size = reader.ReadInt32();

                // Header Information
                uint audio_format = reader.ReadUInt16(); // 2
                numChannels = reader.ReadUInt16();      // 4
                sampleRate = reader.ReadUInt32();       // 8
                reader.ReadUInt32();                    // 12, Byte Rate
                reader.ReadUInt16();                    // 14, Block Align
                reader.ReadUInt16();                    // 16, Bits Per Sample

                if (audio_format != 1)
                {
                    throw new NotSupportedException("Wave compression is not supported.");
                }

                // Reads residual bytes
                if (format_chunk_size > 16)
                {
                    reader.ReadBytes(format_chunk_size - 16);
                }

                // data Signature
                string data_signature = new string(reader.ReadChars(4));
                while (data_signature.ToLower() != "data")
                {
                    reader.ReadBytes(reader.ReadInt32());
                    data_signature = new string(reader.ReadChars(4));
                }
                if (data_signature != "data")
                {
                    throw new NotSupportedException("Specified wave file is not supported.");
                }

                int waveDataLength = reader.ReadInt32();
                data = reader.ReadBytes(waveDataLength);
            }

            INTERNAL_bufferData(
                data,
                sampleRate,
                numChannels,
                0,
                0,
                0
            );
        }

        private void INTERNAL_bufferData(
            byte[] data,
            uint sampleRate,
            uint channels,
            uint loopStart,
            uint loopEnd,
            uint compressionAlign
        ) {
            // FIXME: MSADPCM Duration
            Duration = TimeSpan.FromSeconds(data.Length / 2 / channels / ((double) sampleRate));

            ALFormat format;
            if (compressionAlign > 0)
            {
                if (AL.Get(ALGetString.Extensions).Contains("AL_EXT_MSADPCM"))
                {
                    if (compressionAlign == 262)
                    {
                        format = (channels == 2) ? ALFormat.StereoMsadpcm512Ext : ALFormat.MonoMsadpcm512Ext;
                    }
                    else if (compressionAlign == 134)
                    {
                        format = (channels == 2) ? ALFormat.StereoMsadpcm256Ext : ALFormat.MonoMsadpcm256Ext;
                    }
                    else if (compressionAlign == 70)
                    {
                        format = (channels == 2) ? ALFormat.StereoMsadpcm128Ext : ALFormat.MonoMsadpcm128Ext;
                    }
                    else if (compressionAlign == 38)
                    {
                        format = (channels == 2) ? ALFormat.StereoMsadpcm64Ext : ALFormat.MonoMsadpcm64Ext;
                    }
                    else if (compressionAlign == 22)
                    {
                        format = (channels == 2) ? ALFormat.StereoMsadpcm32Ext : ALFormat.MonoMsadpcm32Ext;
                    }
                    else
                    {
                        throw new Exception("MSADPCM blockAlign unsupported in AL_EXT_MSADPCM!");
                    }
                }
                else
                {
                    byte[] newData;
                    using (MemoryStream stream = new MemoryStream(data))
                    {
                        using (BinaryReader reader = new BinaryReader(stream))
                        {
                            newData = MSADPCMToPCM.MSADPCM_TO_PCM(
                                reader,
                                (short) channels,
                                (short) (compressionAlign - 22)
                            );
                        }
                    }
                    data = newData;
                    compressionAlign = 0;
                    format = (channels == 2) ? ALFormat.Stereo16 : ALFormat.Mono16;
                }
            }
            else
            {
                format = (channels == 2) ? ALFormat.Stereo16 : ALFormat.Mono16;
            }

            // Create the buffer and load it!
            INTERNAL_buffer = AL.GenBuffer();
            AL.BufferData(
                INTERNAL_buffer,
                format,
                data,
                data.Length,
                (int) sampleRate
            );

            // Set the loop points, if applicable
            if (loopStart > 0 || loopEnd > 0)
            {
                AL.Buffer(
                    INTERNAL_buffer,
                    ALBufferiv.LoopPointsSoft,
                    new uint[]
                    {
                        loopStart,
                        loopEnd
                    }
                );
            }
        }

        #endregion
    }
}