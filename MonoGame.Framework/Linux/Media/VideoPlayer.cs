// TODO: Actually write the Theora player!
// TODO: TheoraPlay's stream must eventually end...
// TODO: IsLooped usage

using System;
using System.Diagnostics;
using System.Threading;
using OpenTK.Audio.OpenAL;
using Microsoft.Xna.Framework.Graphics;

namespace Microsoft.Xna.Framework.Media
{
    public sealed class VideoPlayer : IDisposable
    {
        #region Public Member Data: XNA VideoPlayer Implementation
        public bool IsDisposed
        {
            get;
            private set;
        }
        
        public bool IsLooped
        {
            get;
            set;
        }
        
        private bool backing_ismuted;
        public bool IsMuted
        {
            get
            {
                return backing_ismuted;
            }
            set
            {
                backing_ismuted = value;
                UpdateVolume();
            }
        }
        
        public TimeSpan PlayPosition
        {
            get
            {
                return timer.Elapsed;
            }
        }
        
        public MediaState State
        {
            get;
            private set;
        }
        
        public Video Video
        {
            get;
            private set;
        }
        
        private float backing_volume;
        public float Volume
        {
            get
            {
                return backing_volume;
            }
            set
            {
                if (value > 1.0f)
                {
                    backing_volume = 1.0f;
                }
                else if (value < 0.0f)
                {
                    backing_volume = 0.0f;
                }
                else
                {
                    backing_volume = value;
                }
                UpdateVolume();
            }
        }
        #endregion
        
        #region Private Member Data: XNA VideoPlayer Implementation
        // We use this to update our PlayPosition.
        private Stopwatch timer;
        
        // Thread containing our video player.
        private Thread playerThread;
        #endregion
        
        #region Private Member Data: TheoraPlay
        private IntPtr theoraDecoder;
        private IntPtr videoStream;
        private IntPtr audioStream;
        
        private TheoraPlay.THEORAPLAY_VideoFrame currentVideo;
        private TheoraPlay.THEORAPLAY_AudioPacket currentAudio;
        #endregion
        
        #region Private Member Data: OpenAL
        private int audioBufferIndex;
        private int audioSourceIndex;
        #endregion
        
        #region Private Methods: XNA VideoPlayer Implementation
        private void checkDisposed()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException("VideoPlayer");
            }
        }
        #endregion
        
        #region Private Methods: TheoraPlay
        private unsafe TheoraPlay.THEORAPLAY_VideoFrame getVideoFrame(IntPtr frame)
        {
            TheoraPlay.THEORAPLAY_VideoFrame theFrame;
            unsafe
            {
                TheoraPlay.THEORAPLAY_VideoFrame* framePtr = (TheoraPlay.THEORAPLAY_VideoFrame*) frame;
                theFrame = *framePtr;
            }
            return theFrame;
        }
        
        private unsafe TheoraPlay.THEORAPLAY_AudioPacket getAudioPacket(IntPtr packet)
        {
            TheoraPlay.THEORAPLAY_AudioPacket thePacket;
            unsafe
            {
                TheoraPlay.THEORAPLAY_AudioPacket* packetPtr = (TheoraPlay.THEORAPLAY_AudioPacket*) packet;
                thePacket = *packetPtr;
            }
            return thePacket;
        }
        
        private byte[] getPixels(IntPtr pixels, int imageSize)
        {
            byte[] thePixels = new byte[imageSize];
            System.Runtime.InteropServices.Marshal.Copy(pixels, thePixels, 0, imageSize);
            return thePixels;
        }
        #endregion
        
        #region Private Methods: OpenAL
        private void UpdateVolume()
        {
            if (IsMuted)
            {
                AL.Source(audioSourceIndex, ALSourcef.Gain, 0.0f);
            }
            else
            {
                AL.Source(audioSourceIndex, ALSourcef.Gain, Volume);
            }
        }
        #endregion
        
        #region Public Methods: XNA VideoPlayer Implementation
        public VideoPlayer()
        {
            // Set everything to NULL. Yes, this actually matters later.
            theoraDecoder = IntPtr.Zero;
            videoStream = IntPtr.Zero;
            audioStream = IntPtr.Zero;
            
            // Initialize the OpenAL source.
            audioBufferIndex = AL.GenBuffer();
            audioSourceIndex = AL.GenSource();
            
            // Initialize public members.
            IsDisposed = false;
            IsLooped = false;
            IsMuted = false;
            State = MediaState.Stopped;
            Volume = 1.0f;
            
            // Initialize private members.
            timer = new Stopwatch();
            playerThread = new Thread(new ThreadStart(this.RunVideo));
        }
        
        public void Dispose()
        {
            // Stop the VideoPlayer. This gets almost everything...
            Stop();
            
            // Get rid of the OpenAL data.
            AL.DeleteSource(audioSourceIndex);
            AL.DeleteBuffer(audioBufferIndex);
            
            // Okay, we out.
            IsDisposed = true;
        }
        
        public Texture2D GetTexture()
        {
            checkDisposed();
            
            // Be sure we can even get something from TheoraPlay...
            if (    theoraDecoder == IntPtr.Zero ||
                    TheoraPlay.THEORAPLAY_isInitialized(theoraDecoder) == 0 ||
                    videoStream == IntPtr.Zero  )
            {
                return null;
            }
            
            // Assign this locally, or else the thread will ruin your face.
            TheoraPlay.THEORAPLAY_VideoFrame currentFrame = currentVideo;
            
            // Create the Texture2D from the frame data.
            // FIXME: ASSUMING IYUV!!!
            Texture2D currentTexture = new Texture2D(
                null, // FIXME: How do we even get the device?!
                (int) currentFrame.width,
                (int) currentFrame.height,
                false,
                SurfaceFormat.Color // FIXME: Uhhhh
            );
            currentTexture.SetData<byte>(
                getPixels(
                    currentFrame.pixels,
                    (int) currentFrame.width * (int) currentFrame.height * 12 / 8
                )
            );

            return currentTexture;
        }
        
        public void Play(Video video)
        {
            checkDisposed();
            
            // We need to assign this regardless of what happens next.
            Video = video;
            
            // Check the player state before attempting anything.
            if (State != MediaState.Stopped)
            {
                return;
            }
            
            // Initialize the decoder.
            // FIXME: ASSUMING IYUV!!!
            theoraDecoder = TheoraPlay.THEORAPLAY_startDecodeFile(
                Video.FileName,
                uint.MaxValue,
                TheoraPlay.THEORAPLAY_VideoFormat.THEORAPLAY_VIDFMT_IYUV
            );
            
            // Initialize the video stream pointer and get our first frame.
            if (TheoraPlay.THEORAPLAY_hasVideoStream(theoraDecoder) != 0)
            {
                TheoraPlay.THEORAPLAY_getVideo(videoStream);
                currentVideo = getVideoFrame(videoStream);
            }
            
            // Initialize the audio stream pointer and get our first packet.
            if (TheoraPlay.THEORAPLAY_hasAudioStream(theoraDecoder) != 0)
            {
                TheoraPlay.THEORAPLAY_getAudio(audioStream);
                currentAudio = getAudioPacket(audioStream);
            }
            
            // Update the player state now, for the thread we're about to make.
            State = MediaState.Playing;
            
            // Initialize the thread!
            System.Console.Write("Starting Theora player...");
            playerThread.Start();
            System.Console.Write(" Waiting for initialization...");
            while (!playerThread.IsAlive);
            System.Console.WriteLine(" Done!");
        }
        
        public void Stop()
        {
            checkDisposed();
            
            // Check the player state before attempting anything.
            if (State == MediaState.Stopped)
            {
                return;
            }
            
            // Update the player state now, to try and end the thread early.
            State = MediaState.Stopped;
            
            // Stop the thread hardcore anyway!
            System.Console.Write("Stopping Theora player...");
            playerThread.Abort();
            System.Console.Write(" Waiting for termination...");
            playerThread.Join();
            System.Console.WriteLine(" Done!");
            
            // Reset the video timer.
            timer.Stop();
            timer.Reset();
            
            // Force stop the OpenAL source.
            AL.SourceStop(audioSourceIndex);
            
            // Stop and unassign the decoder.
            if (theoraDecoder != IntPtr.Zero)
            {
                if (TheoraPlay.THEORAPLAY_isDecoding(theoraDecoder) != 0)
                {
                    TheoraPlay.THEORAPLAY_stopDecode(theoraDecoder);
                }
                theoraDecoder = IntPtr.Zero;
            }
            
            // Free and unassign the video stream.
            if (videoStream != IntPtr.Zero)
            {
                TheoraPlay.THEORAPLAY_freeVideo(videoStream);
                videoStream = IntPtr.Zero;
            }
            
            // Free and unassign the audio stream.
            if (audioStream != IntPtr.Zero)
            {
                TheoraPlay.THEORAPLAY_freeAudio(audioStream);
                audioStream = IntPtr.Zero;
            }
            
            // We're not playing any video anymore.
            Video = null;
        }
        
        public void Pause()
        {
            checkDisposed();
            
            // Check the player state before attempting anything.
            if (State != MediaState.Playing)
            {
                return;
            }
            
            // Stop the timer.
            timer.Stop();
            
            // Update the player state.
            State = MediaState.Paused;
            
            // Pause the OpenAL source.
            // We do this as late as possible in case of leaking audio packets.
            AL.SourcePause(audioSourceIndex);
        }
        
        public void Resume()
        {
            checkDisposed();
            
            // Check the player state before attempting anything.
            if (State != MediaState.Paused)
            {
                return;
            }
            
            // Resume the timer.
            timer.Start();
            
            // Update the player state.
            State = MediaState.Playing;
            
            // Force resume the OpenAL source.
            // We do this in case the stream fell behind when we paused.
            AL.SourcePlay(audioSourceIndex);
        }
        #endregion
        
        #region The Theora video player thread
        private void RunVideo()
        {
            timer.Start();
            
            while (State != MediaState.Stopped)
            {
                if (State == MediaState.Paused)
                {
                    // Arbitrarily 1 frame in a 30fps movie.
                    Thread.Sleep(33);
                }
                else
                {
                    // FIXME: The actual freaking player.
                    
                    // Get the next audio packet from the decoder, if a stream exists.
                    if (audioStream != IntPtr.Zero)
                    {
                        // FIXME: OpenAL buffer data and play it.
                        // FIXME: Framerate timing.
                        
                        // Step at the end.
                        currentAudio = getAudioPacket(currentAudio.next);
                    }
                    
                    if (videoStream != IntPtr.Zero)
                    {
                        // FIXME: Framerate timing.
                        
                        // Step at the end.
                        currentVideo = getVideoFrame(currentVideo.next);
                    }
                }
            }
        }
        #endregion
    }
}
