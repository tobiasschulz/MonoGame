using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using NVorbis;
using OpenTK.Audio.OpenAL;

namespace Microsoft.Xna.Framework.Audio
{
    internal static class ALHelper
    {
        public static readonly XRamExtension XRam;
        public static readonly EffectsExtension Efx;

        static ALHelper()
        {
            XRam = new XRamExtension();
            Efx = new EffectsExtension();
        }

        public static bool TryCheck()
        {
            return AL.GetError() != ALError.NoError;
        }
        public static void Check()
        {
            ALError error;
            if ((error = AL.GetError()) != ALError.NoError)
                throw new InvalidOperationException(AL.GetErrorString(error));
        }
    }

    public class OggStream : IDisposable
    {
        const int DefaultBufferCount = 3;

        internal readonly object stopMutex = new object();
        internal readonly object prepareMutex = new object();

        internal readonly int alSourceId;
        internal readonly int[] alBufferIds;

        readonly Stream underlyingStream;

        internal VorbisReader Reader { get; private set; }
        internal bool Ready { get; private set; }
        internal bool Preparing { get; private set; }

        public int BufferCount { get; private set; }
        public string Name { get; private set; }

        public OggStream(string filename, int bufferCount = DefaultBufferCount) : this(File.OpenRead(filename), bufferCount)
        {
            Name = filename;
        }
        public OggStream(Stream stream, int bufferCount = DefaultBufferCount)
        {
            ALHelper.Check();

            BufferCount = bufferCount;
#if !FAKE
            alBufferIds = OpenALSoundController.Instance.TakeBuffers(BufferCount);
            alSourceId = OpenALSoundController.Instance.TakeSource();
#endif
            if (ALHelper.XRam.IsInitialized)
            {
                ALHelper.XRam.SetBufferMode(BufferCount, ref alBufferIds[0], XRamExtension.XRamStorage.Hardware);
                ALHelper.Check();
            }

            Volume = 1;

            underlyingStream = stream;
        }

        public void Prepare(bool asynchronous = false)
        {
            if (Preparing) return;

            var state = AL.GetSourceState(alSourceId);

            lock (stopMutex)
            {
                switch (state)
                {
                    case ALSourceState.Playing:
                    case ALSourceState.Paused:
                        return;

                    case ALSourceState.Stopped:
                        lock (prepareMutex)
                        {
                            Reader.DecodedTime = TimeSpan.Zero;
                            Ready = false;
                            Empty();
                        }
                        break;
                }

                if (!Ready)
                {
                    lock (prepareMutex)
                    {
                        Preparing = true;
                        Open(precache: true, asyncPrecache: asynchronous);
                    }
                }
            }
        }

        public void Play()
        {
            var state = AL.GetSourceState(alSourceId);

            switch (state)
            {
                case ALSourceState.Playing: return;
                case ALSourceState.Paused:
                    Resume();
                    return;
            }

            Prepare();

            AL.SourcePlay(alSourceId);
            ALHelper.Check();

            Preparing = false;

            OggStreamer.Instance.AddStream(this);
        }

        public void Pause()
        {
            if (AL.GetSourceState(alSourceId) != ALSourceState.Playing)
                return;

            OggStreamer.Instance.RemoveStream(this);
            AL.SourcePause(alSourceId);
            ALHelper.Check();
        }

        public void Resume()
        {
            if (AL.GetSourceState(alSourceId) != ALSourceState.Paused)
                return;

            OggStreamer.Instance.AddStream(this);
            AL.SourcePlay(alSourceId);
            ALHelper.Check();
        }

        public void Stop()
        {
            var state = AL.GetSourceState(alSourceId);
            if (state == ALSourceState.Playing || state == ALSourceState.Paused)
                StopPlayback();

            lock (stopMutex)
                if (OggStreamer.HasInstance)
                    OggStreamer.Instance.RemoveStream(this);
        }

        float volume;
        public float Volume
        {
            get { return volume; }
            set
            {
                AL.Source(alSourceId, ALSourcef.Gain, MathHelper.Clamp((volume = value) * globalVolume, 0, 1));
                ALHelper.Check();
            }
        }

        float globalVolume = 1;
        string category;

        public float GlobalVolume
        {
            set
            {
                globalVolume = value;
                Volume = volume;
            }
        }

        public string Category
        {
            get { return category; }
            set
            {
                category = value;
                if (OggStreamer.HasInstance)
                    GlobalVolume = category == "Ambience"
                                       ? OggStreamer.Instance.AmbienceVolume
                                       : OggStreamer.Instance.MusicVolume;
            }
        }

        public bool IsLooped { get; set; }

        public bool IsStopped
        {
            get
            {
                var state = AL.GetSourceState(alSourceId);
                return state == ALSourceState.Stopped;
            }
        }
        public bool IsPlaying
        {
            get
            {
                var state = AL.GetSourceState(alSourceId);
                return state == ALSourceState.Playing;
            }
        }

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            if (IsDisposed) return;
            IsDisposed = true;

            var state = AL.GetSourceState(alSourceId);
            if (state == ALSourceState.Playing || state == ALSourceState.Paused)
                StopPlayback();

            lock (prepareMutex)
            {
                if (OggStreamer.HasInstance)
                    OggStreamer.Instance.RemoveStream(this);

                if (state != ALSourceState.Initial)
                    Empty();

                Close();

                underlyingStream.Dispose();
            }
#if !FAKE
            OpenALSoundController.Instance.ReturnSource(alSourceId);
            OpenALSoundController.Instance.ReturnBuffers(alBufferIds);
#endif
            ALHelper.Check();
        }

        void StopPlayback()
        {
            AL.SourceStop(alSourceId);
            ALHelper.Check();
        }

        void Empty(bool giveUp = false)
        {
            int queued;
            AL.GetSource(alSourceId, ALGetSourcei.BuffersQueued, out queued);
            if (queued > 0)
            {
                AL.SourceUnqueueBuffers(alSourceId, queued);

                if (!ALHelper.TryCheck())
                {
                    if (giveUp) return;

                    // This is a bug in the OpenAL implementation
                    // Salvage what we can
                    int processed;
                    AL.GetSource(alSourceId, ALGetSourcei.BuffersProcessed, out processed);
                    var salvaged = new int[processed];
                    if (processed > 0)
                    {
                        AL.SourceUnqueueBuffers(alSourceId, processed, salvaged);
                        ALHelper.Check();
                    }

                    // Try turning it off again?
                    AL.SourceStop(alSourceId);
                    ALHelper.Check();

                    Empty(true);
                }
            }
        }

        internal void Open(bool precache = false, bool asyncPrecache = false)
        {
            underlyingStream.Seek(0, SeekOrigin.Begin);
            Reader = new VorbisReader(underlyingStream, false);

            if (precache)
            {
                if (!asyncPrecache)
                {
                    // Fill first buffer synchronously
                    OggStreamer.Instance.FillBuffer(this, alBufferIds[0]);
                    AL.SourceQueueBuffer(alSourceId, alBufferIds[0]);
                    ALHelper.Check();
                }

                // Schedule the others asynchronously
                OggStreamer.Instance.AddStream(this);
            }

            Ready = true;
        }

        internal void Close()
        {
            if (Reader != null)
            {
                Reader.Dispose();
                Reader = null;
            }
            Ready = false;
        }
    }

    public class OggStreamer : IDisposable
    {
        const float DefaultUpdateRate = 10;
        const int DefaultBufferSize = 44100;

        static readonly object singletonMutex = new object();

        readonly object iterationMutex = new object();
        readonly object readMutex = new object();

        readonly float[] readSampleBuffer;
        readonly short[] castBuffer;

        readonly HashSet<OggStream> streams = new HashSet<OggStream>();
        readonly List<OggStream> threadLocalStreams = new List<OggStream>();

        readonly Thread underlyingThread;
        volatile bool cancelled;

        public float UpdateRate { get; private set; }
        public int BufferSize { get; private set; }

        static OggStreamer instance;
        public static OggStreamer Instance
        {
            get
            {
                lock (singletonMutex)
                {
                    if (instance == null)
                        throw new InvalidOperationException("No instance running");
                    return instance;
                }
            }
            private set { lock (singletonMutex) instance = value; }
        }
        public static bool HasInstance
        {
            get { lock (singletonMutex) return instance != null; }
        }

        public OggStreamer(int bufferSize = DefaultBufferSize, float updateRate = DefaultUpdateRate)
        {
            lock (singletonMutex)
            {
                if (instance != null)
                    throw new InvalidOperationException("Already running");

                Instance = this;
                underlyingThread = new Thread(EnsureBuffersFilled) { Priority = ThreadPriority.Lowest, Name = "Ogg Streamer" };
                underlyingThread.Start();
            }

            UpdateRate = updateRate;
            BufferSize = bufferSize;

            readSampleBuffer = new float[bufferSize];
            castBuffer = new short[bufferSize];
        }

        public void Dispose()
        {
            lock (singletonMutex)
            {
                Debug.Assert(Instance == this, "Two instances running, somehow...?");

                cancelled = true;
                lock (iterationMutex)
                    streams.Clear();

                Instance = null;
            }
        }

        public float LowPassHFGain
        {
            set
            {
#if !FAKE
                OpenALSoundController.Instance.LowPassHFGain = value;
#endif
            }
        }

        float musicVolume = 1;
        public float MusicVolume
        {
            get { return musicVolume; }
            set
            {
                musicVolume = value;
                foreach (var s in streams) if (s.Category == "Music") s.GlobalVolume = value;
            }
        }
        float ambienceVolume = 1;
        public float AmbienceVolume
        {
            get { return ambienceVolume; }
            set
            {
                ambienceVolume = value;
                foreach (var s in streams) if (s.Category == "Ambience") s.GlobalVolume = value;
            }
        }

        internal bool AddStream(OggStream stream)
        {
            //stream.GlobalVolume = stream.Category == "Music" ? musicVolume : ambienceVolume;

            lock (iterationMutex)
                return streams.Add(stream);
        }
        internal bool RemoveStream(OggStream stream)
        {
            lock (iterationMutex)
                return streams.Remove(stream);
        }

        public bool FillBuffer(OggStream stream, int bufferId)
        {
            int readSamples;
            lock (readMutex)
            {
                readSamples = stream.Reader.ReadSamples(readSampleBuffer, 0, BufferSize);
                CastBuffer(readSampleBuffer, castBuffer, readSamples);
            }
            AL.BufferData(bufferId, stream.Reader.Channels == 1 ? ALFormat.Mono16 : ALFormat.Stereo16, castBuffer,
                          readSamples * sizeof(short), stream.Reader.SampleRate);
            ALHelper.Check();

            return readSamples != BufferSize;
        }
        static void CastBuffer(float[] inBuffer, short[] outBuffer, int length)
        {
            for (int i = 0; i < length; i++)
            {
                var temp = (int)(short.MaxValue * inBuffer[i]);
                if (temp > short.MaxValue) temp = short.MaxValue;
                else if (temp < short.MinValue) temp = short.MinValue;
                outBuffer[i] = (short)temp;
            }
        }

        void EnsureBuffersFilled()
        {
            while (!cancelled)
            {
                Thread.Sleep((int)(1000 / UpdateRate));
                if (cancelled) break;

                threadLocalStreams.Clear();
                lock (iterationMutex) threadLocalStreams.AddRange(streams);

                if (threadLocalStreams.Count == 0) continue;
                foreach (var stream in threadLocalStreams)
                {
                    lock (stream.prepareMutex)
                    {
                        lock (iterationMutex)
                            if (!streams.Contains(stream))
                                continue;

                        bool finished = false;

                        int queued;
                        AL.GetSource(stream.alSourceId, ALGetSourcei.BuffersQueued, out queued);
                        ALHelper.Check();
                        int processed;
                        AL.GetSource(stream.alSourceId, ALGetSourcei.BuffersProcessed, out processed);
                        ALHelper.Check();

                        if (processed == 0 && queued == stream.BufferCount) continue;

                        int[] tempBuffers;
                        if (processed > 0)
                            tempBuffers = AL.SourceUnqueueBuffers(stream.alSourceId, processed);
                        else
                            tempBuffers = stream.alBufferIds.Skip(queued).ToArray();

                        for (int i = 0; i < tempBuffers.Length; i++)
                        {
                            finished |= FillBuffer(stream, tempBuffers[i]);

                            if (finished)
                            {
                                if (stream.IsLooped)
                                    stream.Reader.DecodedTime = TimeSpan.Zero;
                                else
                                {
                                    streams.Remove(stream);
                                    i = tempBuffers.Length;
                                }
                            }
                        }

                        AL.SourceQueueBuffers(stream.alSourceId, tempBuffers.Length, tempBuffers);
                        ALHelper.Check();

                        if (finished && !stream.IsLooped)
                            continue;
                    }

                    lock (stream.stopMutex)
                    {
                        if (stream.Preparing) continue;

                        lock (iterationMutex)
                            if (!streams.Contains(stream))
                                continue;

                        var state = AL.GetSourceState(stream.alSourceId);
                        if (state == ALSourceState.Stopped)
                        {
                            Trace.WriteLine("[OpenAL] Buffer underrun on " + stream.Name);
                            AL.SourcePlay(stream.alSourceId);
                            ALHelper.Check();
                        }
                    }
                }
            }
        }
    }
}
