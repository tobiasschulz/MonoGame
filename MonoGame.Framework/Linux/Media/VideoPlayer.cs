// TODO: TheoraPlay's stream must eventually end...
// TODO: IsLooped usage

using System;
using System.Collections.Generic;
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
        private List<int> audioBuffers;
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
            
            // Initialize the OpenAL source and buffer list.
            audioBuffers = new List<int>();
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
            
            // Get rid of the OpenAL source.
            AL.DeleteSource(audioSourceIndex);
            
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
            
            
            // FIXME: The rest of this method kind of hurts.
            
            // Create the Texture2D.
            Texture2D currentTexture = new Texture2D(
                null, // FIXME: How do we even get the device?!
                (int) currentFrame.width,
                (int) currentFrame.height,
                false,
                SurfaceFormat.Color
            );
            
            // Create the texture data from the Theora image data.
            // FIXME: ASSUMING IYUV!!!
            byte[] theoraPixels = getPixels(
                currentFrame.pixels,
                (int) currentFrame.width * (int) currentFrame.height * 12 / 8
            );
            byte[] pixelBGRA = new byte[(int) currentFrame.width * (int) currentFrame.height * 4];
            for (int i = 0, j = 0; i < theoraPixels.Length; i += 3, j += 8)
            {
                // The IYUV -> BGR formula. Thanks, Google!
                
                // YUV is within 12 bits.
                int Y = theoraPixels[i] >> 4;
                int U = theoraPixels[i] & 0xF;
                int V = theoraPixels[i + 1] >> 4;
                
                // The actual conversion from YUV to RGB.
                int R = (int) (Y + (1.370705 * (V - 128)));
                int G = (int) (Y + (0.698001 * (V - 128)) - (0.337633 * (U - 128)));
                int B = (int) (Y + (1.732446 * (U - 128)));
                
                // Clamp values to 0-255, and set the alpha to max value.
                pixelBGRA[j] = (byte) ((R < 0) ? 0 : ((R > 256) ? 256 : R));
                pixelBGRA[j + 1] = (byte) ((G < 0) ? 0 : ((G > 256) ? 256 : G));
                pixelBGRA[j + 2] = (byte) ((B < 0) ? 0 : ((B > 256) ? 256 : B));
                pixelBGRA[j + 3] = (byte) (255);
                
                // 12 + 12 = 24, conveniently 3 full bytes!
                Y = theoraPixels[i + 1] & 0xF;
                U = theoraPixels[i + 2] >> 4;
                V = theoraPixels[i + 2] & 0xF;
                
                // Convert again...
                R = (int) (Y + (1.370705 * (V - 128)));
                G = (int) (Y + (0.698001 * (V - 128)) - (0.337633 * (U - 128)));
                B = (int) (Y + (1.732446 * (U - 128)));
                
                // Clamp values to 0-255, and set the alpha to max value.
                pixelBGRA[j + 4] = (byte) ((R < 0) ? 0 : ((R > 256) ? 256 : R));
                pixelBGRA[j + 5] = (byte) ((G < 0) ? 0 : ((G > 256) ? 256 : G));
                pixelBGRA[j + 6] = (byte) ((B < 0) ? 0 : ((B > 256) ? 256 : B));
                pixelBGRA[j + 7] = (byte) (255);
            }
            
            // TexImage2D.
            currentTexture.SetData<byte>(pixelBGRA);

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
            if (AL.GetSourceState(audioSourceIndex) != ALSourceState.Stopped)
            {
                AL.SourceStop(audioSourceIndex);
                AL.SourceRewind(audioSourceIndex);
                AL.DeleteBuffers(audioBuffers.ToArray());
                audioBuffers.Clear();
            }
            
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
            if (AL.GetSourceState(audioSourceIndex) == ALSourceState.Playing)
            {
                AL.SourcePause(audioSourceIndex);
            }
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
            if (AL.GetSourceState(audioSourceIndex) == ALSourceState.Paused)
            {
                AL.SourcePlay(audioSourceIndex);
            }
        }
        #endregion
        
        #region The Theora video player thread
        private void RunVideo()
        {
            timer.Start();
            
            while (State != MediaState.Stopped)
            {
                // Regardless of the player state, we should buffer as much audio as possible.
                if (audioStream != IntPtr.Zero)
                {
                    // Buffer...
                    audioBuffers.Add(AL.GenBuffer());
                    AL.BufferData(
                        audioBuffers[audioBuffers.Count - 1],
                        (currentAudio.channels == 2) ? ALFormat.StereoFloat32Ext : ALFormat.MonoFloat32Ext,
                        currentAudio.samples,
                        currentAudio.frames * currentAudio.channels,
                        currentAudio.freq
                    );
                    
                    // Queue...
                    AL.SourceQueueBuffer(audioSourceIndex, audioBuffers[audioBuffers.Count - 1]);
                    
                    // ... Then step.
                    currentAudio = getAudioPacket(currentAudio.next);
                }
                
                // Sleep when paused, update the video state when playing.
                if (State == MediaState.Paused)
                {
                    // Arbitrarily 1 frame in a 30fps movie.
                    Thread.Sleep(33);
                }
                else
                {
                    // If we're getting here, we should be playing the audio...
                    if (audioStream != IntPtr.Zero)
                    {
                        if (AL.GetSourceState(audioSourceIndex) != ALSourceState.Playing)
                        {
                            AL.SourcePlay(audioSourceIndex);
                        }
                    }
                    
                    // Get the next video from from the decoder, if a stream exists.
                    if (videoStream != IntPtr.Zero)
                    {
                        // Only step when it's time to show the next frame.
                        if (currentVideo.playms <= timer.ElapsedMilliseconds)
                        {
                            currentVideo = getVideoFrame(currentVideo.next);
                        }
                    }
                }
            }
        }
        #endregion
    }
}