using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        [Conditional("DEBUG")]
        public static void Check()
        {
            ALError error;
            if ((error = AL.GetError()) != ALError.NoError)
#if DEBUG
                throw new InvalidOperationException(AL.GetErrorString(error));
#else
//                Log("AL Error : " + AL.GetErrorString(error));
                Console.WriteLine("AL Error : " + AL.GetErrorString(error));
#endif
        }

        public static void Log(string message)
        {
            try
            {
                Console.WriteLine("({0}) [{1}] {2}", DateTime.Now.ToString("HH:mm:ss.fff"), "OpenAL", message);
                using (var stream = File.Open("Debug Log.txt", FileMode.Append))
                {
                    using (var writer = new StreamWriter(stream))
                    {
                        writer.WriteLine("({0}) [{1}] {2}", DateTime.Now.ToString("HH:mm:ss.fff"), "OpenAL", message);
                    }
                }
            }
            catch (Exception ex)
            {
                // NOT THAT BIG A DEAL GUYS
            }
        }
    }

    public class OggStream : IDisposable
    {
        const int DefaultBufferCount = 6;

        internal readonly object stopMutex = new object();
        internal readonly object prepareMutex = new object();

        internal int alSourceId;
        internal readonly int[] alBufferIds;
        internal readonly Stack<int> bufferStack;

        Stream underlyingStream;

        internal VorbisReader Reader { get; private set; }
        internal bool Precaching { get; private set; }
        internal bool FirstBufferPrecached { get; set; }

        internal int QueuedBuffers { get; set; }
        internal int ProcessedBuffers { get; set; }

        public int BufferCount { get; private set; }
        public string Name { get; private set; }
        public string RealName { get; set; }

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
            bufferStack = new Stack<int>(alBufferIds);
            alSourceId = OpenALSoundController.Instance.TakeSource();
#endif
            if (ALHelper.XRam.IsInitialized)
            {
                ALHelper.XRam.SetBufferMode(BufferCount, ref alBufferIds[0], XRamExtension.XRamStorage.Hardware);
                ALHelper.Check();
            }

            Volume = 1;
            lowPass = true;

            underlyingStream = stream;
        }

        public void Prepare(bool asynchronous = false)
        {
            var state = AL.GetSourceState(alSourceId);

            lock (stopMutex)
            {
                switch (state)
                {
                    case ALSourceState.Playing:
                    case ALSourceState.Paused:
                        return;
                }

                lock (prepareMutex)
                {
                    if (Reader == null)
                        Open();

                    if (!Precaching)
                    {
                        Precaching = true;
                        Precache(asynchronous: asynchronous);
                    }
                }
            }
        }

        bool lowPass;
        public bool LowPass
        {
            get { return lowPass; }
            set
            {
#if !FAKE
                if (lowPass != value)
                    OpenALSoundController.Instance.SetSourceFiltered(alSourceId, value);
#endif
                lowPass = value;
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

            if (bufferStack.Count == BufferCount)
                Prepare();
            else if (!FirstBufferPrecached)
                ALHelper.Log("Buffers lost for " + RealName + " with source " + alSourceId);

            AL.SourcePlay(alSourceId);
            ALHelper.Check();

            Precaching = false;

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

            //Trace.WriteLine("[OpenAL] Disposing " + RealName + " with source " + alSourceId);

            lock (prepareMutex)
            {
                if (OggStreamer.HasInstance)
                    OggStreamer.Instance.RemoveStream(this);

                StopPlayback();
                Empty();
                Close();
            
#if !FAKE
                if (OpenALSoundController.Instance != null)
                {
                    OpenALSoundController.Instance.ReturnSource(alSourceId);
                    OpenALSoundController.Instance.ReturnBuffers(alBufferIds);
                }
#endif
            }
        }

        void StopPlayback()
        {
            AL.SourceStop(alSourceId);
            ALHelper.Check();
        }

        void Empty()
        {
            int queued;
            AL.GetSource(alSourceId, ALGetSourcei.BuffersQueued, out queued);
            if (queued > 0)
            {
                AL.SourceUnqueueBuffers(alSourceId, queued);

                if (!ALHelper.TryCheck())
                {
                    // This is no good... let's regenerate the source
                    //Console.WriteLine("Source " + alSourceId + " was corrupted and needs to be regenerated (used for " + RealName + ")");

                    AL.DeleteSource(alSourceId);
                    alSourceId = AL.GenSource();
                }
            }
        }

        void Open()
        {
            underlyingStream.Seek(0, SeekOrigin.Begin);
            lock (OggStreamer.Instance.readMutex)
                Reader = new VorbisReader(underlyingStream, true);
        }

        void Precache(bool asynchronous = false)
        {
            if (!asynchronous)
            {
                // Fill first buffer synchronously
                int buffer = bufferStack.Pop();
                OggStreamer.Instance.FillBuffer(this, buffer);
                AL.SourceQueueBuffer(alSourceId, buffer);
                ALHelper.Check();

                //Trace.WriteLine("[fp] Q (1) : " + RealName + " - s = " + alSourceId);

                FirstBufferPrecached = true;
            }
            else
                Interlocked.Increment(ref OggStreamer.Instance.PendingPrecaches);

            // Schedule the others asynchronously
            OggStreamer.Instance.AddStream(this);
        }

        void Close()
        {
            if (Reader != null)
            {
                Reader.Dispose();
                Reader = null;

                underlyingStream.Close();
                underlyingStream.Dispose();
                underlyingStream = null;
            }
        }
    }

    public class OggStreamer : IDisposable
    {
        const float DefaultUpdateRate = 60;
        const int DefaultBufferSize = 22050;

        static readonly object singletonMutex = new object();

        readonly object iterationMutex = new object();
        public readonly object readMutex = new object();

        readonly float[] readSampleBuffer;
        readonly short[] castBuffer;

        readonly HashSet<OggStream> streams = new HashSet<OggStream>();
        readonly List<OggStream> threadLocalStreams = new List<OggStream>();

        readonly Thread underlyingThread;
        volatile bool cancelled;

        public int PendingPrecaches;

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
                underlyingThread = new Thread(EnsureBuffersFilled) { Priority = ThreadPriority.BelowNormal, Name = "Ogg Streamer" };
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
                lock (iterationMutex)
                {
                    foreach (var s in streams) if (s.Category == "Music") s.GlobalVolume = value;
                }
            }
        }
        float ambienceVolume = 1;
        public float AmbienceVolume
        {
            get { return ambienceVolume; }
            set
            {
                ambienceVolume = value;
                lock (iterationMutex)
                {
                    foreach (var s in streams) if (s.Category == "Ambience") s.GlobalVolume = value;
                }
            }
        }

        internal bool AddStream(OggStream stream)
        {
            //stream.GlobalVolume = stream.Category == "Music" ? musicVolume : ambienceVolume;

            bool added;
            lock (iterationMutex)
                added = streams.Add(stream);

            //if (added)
            //    Trace.WriteLine("[OpenAL] Stream " + stream.RealName + " added with source " + stream.alSourceId);

            return added;
        }
        internal bool RemoveStream(OggStream stream)
        {
            bool removed;
            lock (iterationMutex)
                removed = streams.Remove(stream);

            //if (removed)
            //    Trace.WriteLine("[OpenAL] Stream " + stream.RealName + " removed with source " + stream.alSourceId);

            if (removed && !stream.FirstBufferPrecached)
                Interlocked.Decrement(ref PendingPrecaches);

            return removed;
        }

        public bool FillBuffer(OggStream stream, int bufferId)
        {
            int readSamples;
            lock (readMutex)
            {
                readSamples = stream.Reader.ReadSamples(readSampleBuffer, 0, BufferSize);

                for (int i = 0; i < readSamples; i++)
                    castBuffer[i] = (short)(short.MaxValue * readSampleBuffer[i]);

                AL.BufferData(bufferId, stream.Reader.Channels == 1 ? ALFormat.Mono16 : ALFormat.Stereo16, castBuffer,
                              readSamples * sizeof(short), stream.Reader.SampleRate);
            }
            ALHelper.Check();

            return readSamples != BufferSize;
        }

        void EnsureBuffersFilled()
        {
            while (!cancelled)
            {
                //Thread.Sleep((int)(1000 / UpdateRate));
                if (cancelled) break;

                threadLocalStreams.Clear();
                lock (iterationMutex) threadLocalStreams.AddRange(streams);

                if (threadLocalStreams.Count == 0) continue;

                // look through buffers to know what the heck we should be doing
                int lowestPlayingBufferCount = int.MaxValue;
                for (int i = threadLocalStreams.Count - 1; i >= 0; i--)
                {
                    var stream = threadLocalStreams[i];

                    lock (iterationMutex)
                        if (!streams.Contains(stream))
                        {
                            threadLocalStreams.RemoveAt(i);
                            continue;
                        }

                    int queued;
                    AL.GetSource(stream.alSourceId, ALGetSourcei.BuffersQueued, out queued);
                    ALHelper.Check();
                    stream.QueuedBuffers = queued;

                    int processed;
                    AL.GetSource(stream.alSourceId, ALGetSourcei.BuffersProcessed, out processed);
                    ALHelper.Check();
                    stream.ProcessedBuffers = processed;

                    if (!stream.Precaching)
                        lowestPlayingBufferCount = Math.Min(lowestPlayingBufferCount, queued - processed);
                }

                //if (lowestPlayingBufferCount < 5)
                //    Console.WriteLine("lpbc : " + lowestPlayingBufferCount);

                foreach (var stream in threadLocalStreams)
                {
                    lock (stream.prepareMutex)
                    {
                        lock (iterationMutex)
                            if (!streams.Contains(stream))
                                continue;

                        if (stream.ProcessedBuffers == 0 && stream.bufferStack.Count == 0)
                        {
                            if (stream.QueuedBuffers != stream.BufferCount)
                                ALHelper.Log("Buffers were lost for " + stream.RealName + " with source " + stream.alSourceId);
                            continue;
                        }

                        if (stream.QueuedBuffers + stream.bufferStack.Count != stream.BufferCount)
                            ALHelper.Log("Stray buffers in source for " + stream.RealName + " with source " + stream.alSourceId);

                        if (stream.QueuedBuffers - stream.ProcessedBuffers > lowestPlayingBufferCount)
                            continue;
                        if (stream.Precaching && lowestPlayingBufferCount <= stream.BufferCount * 2 / 3)
                            continue;

                        int buffer;
                        if (stream.ProcessedBuffers > 0)
                            buffer = AL.SourceUnqueueBuffer(stream.alSourceId);
                        else
                            buffer = stream.bufferStack.Pop();

                        bool finished = FillBuffer(stream, buffer);
                        if (finished)
                        {
                            if (stream.IsLooped)
                                stream.Reader.DecodedTime = TimeSpan.Zero;
                            else
                                lock (iterationMutex)
                                    streams.Remove(stream);
                        }

                        AL.SourceQueueBuffer(stream.alSourceId, buffer);
                        ALHelper.Check();
                        //Trace.WriteLine((stream.Precaching ? "[p] " : "") + "Q (" + (stream.QueuedBuffers - stream.ProcessedBuffers + 1) + ") : " + stream.RealName + " - s = " + stream.alSourceId + " | lpbc = " + lowestPlayingBufferCount);

                        if (!stream.FirstBufferPrecached)
                        {
                            Interlocked.Decrement(ref PendingPrecaches);
                            stream.FirstBufferPrecached = true;
                            //Trace.WriteLine("[OpenAL] Buffer " + stream.RealName + " precached with source " + stream.alSourceId);
                        }

                        if (finished && !stream.IsLooped)
                            continue;
                    }

                    lock (stream.stopMutex)
                    {
                        if (stream.Precaching) continue;

                        lock (iterationMutex)
                            if (!streams.Contains(stream))
                                continue;

                        var state = AL.GetSourceState(stream.alSourceId);
                        ALHelper.Check();
                        if (state == ALSourceState.Stopped)
                        {
                            ALHelper.Log("Buffer underrun on " + stream.RealName + " with source " + stream.alSourceId);
                            AL.SourcePlay(stream.alSourceId);
                            ALHelper.Check();
                        }
                    }
                }
            }
        }
    }
}
