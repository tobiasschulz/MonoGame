using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
#if MONOMAC
using MonoMac.OpenAL;
// FIXME: MONOMAC OPENGL!!!
#else
using OpenTK.Audio.OpenAL;
using OpenTK.Graphics.OpenGL;
#endif
using Microsoft.Xna.Framework.Graphics;

namespace Microsoft.Xna.Framework.Media
{
    public sealed class VideoPlayer : IDisposable
    {
        #region YUV Shaders
        private int shaderProgram;
        
        private struct vert_struct
        {
            public float[] pos;
            public float[] tex;
        };
        
        private const string shader_vertex =
            "#version 110\n" +
            "attribute vec2 pos;\n" +
            "attribute vec2 tex;\n" +
            "void main() {\n" +
            "   gl_Position = vec4(pos.xy, 0.0, 1.0);\n" +
            "   gl_TexCoord[0].xy = tex;\n" +
            "}\n";
        private const string shader_fragment =
            "#version 110\n" +
            "uniform sampler2D samp0;\n" +
            "uniform sampler2D samp1;\n" +
            "uniform sampler2D samp2;\n" +
            "const vec3 offset = vec3(-0.0625, -0.5, -0.5);\n" +
            "const vec3 Rcoeff = vec3(1.164,  0.000,  1.596);\n" +
            "const vec3 Gcoeff = vec3(1.164, -0.391, -0.813);\n" +
            "const vec3 Bcoeff = vec3(1.164,  2.018,  0.000);\n" +
            "void main() {\n" +
            "   vec2 tcoord;\n" +
            "   vec3 yuv, rgb;\n" +
            "   tcoord = gl_TexCoord[0].xy;\n" +
            "   yuv.x = texture2D(samp0, tcoord).r;\n" +
            "   yuv.y = texture2D(samp1, tcoord).r;\n" +
            "   yuv.z = texture2D(samp2, tcoord).r;\n" +
            "   yuv += offset;\n" +
            "   rgb.r = dot(yuv, Rcoeff);\n" +
            "   rgb.g = dot(yuv, Gcoeff);\n" +
            "   rgb.b = dot(yuv, Bcoeff);\n" +
            "   gl_FragColor = vec4(rgb, 1.0);\n" +
            "}\n";
        #endregion
        
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
        
        private Thread audioDecoderThread;
        #endregion
        
        #region Private Member Data: OpenAL
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
        
        private float[] getSamples(IntPtr samples, int packetSize)
        {
            float[] theSamples = new float[packetSize];
            System.Runtime.InteropServices.Marshal.Copy(samples, theSamples, 0, packetSize);
            return theSamples;
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
            audioDecoderThread = new Thread(new ThreadStart(this.DecodeAudio));
            
            // Create the vertex/fragment shaders.
            int vshader_id = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vshader_id, shader_vertex);
            GL.CompileShader(vshader_id);
            int fshader_id = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fshader_id, shader_fragment);
            GL.CompileShader(fshader_id);
            
            // Create the shader program.
            shaderProgram = GL.CreateProgram();
            GL.AttachShader(shaderProgram, vshader_id);
            GL.AttachShader(shaderProgram, fshader_id);
            GL.BindAttribLocation(shaderProgram, 0, "pos");
            GL.BindAttribLocation(shaderProgram, 1, "tex");
            GL.LinkProgram(shaderProgram);
            GL.DeleteShader(vshader_id);
            GL.DeleteShader(fshader_id);
        }
        
        public void Dispose()
        {
            // Stop the VideoPlayer. This gets almost everything...
            Stop();
            
            // Get rid of the OpenAL source.
            AL.DeleteSource(audioSourceIndex);
            
            // Delete the shader program.
            GL.DeleteProgram(shaderProgram);
            
            // Okay, we out.
            IsDisposed = true;
        }
        
        // FIXME: HACK!!! THIS BREAKS THE API!!!
        public Texture2D GetTexture(GraphicsDevice device)
        {
            checkDisposed();
            
            // Be sure we can even get something from TheoraPlay...
            if (    State == MediaState.Stopped ||
                    theoraDecoder == IntPtr.Zero ||
                    TheoraPlay.THEORAPLAY_isInitialized(theoraDecoder) == 0 ||
                    videoStream == IntPtr.Zero  )
            {
                return null;
            }
            
            // Assign this locally, or else the thread will ruin your face.
            TheoraPlay.THEORAPLAY_VideoFrame currentFrame = currentVideo;
            
            // Create the Texture2D.
            Texture2D currentTexture = new Texture2D(
                device,
                (int) currentFrame.width,
                (int) currentFrame.height,
                false,
                SurfaceFormat.Color
            );
            
            // FIXME: Pull some of this to class-wide. Less glGenCrap().
            
            // YUV textures
            int[] glTextures = new int[3];
            GL.GenTextures(3, glTextures);
            
            // Framebuffer and texture to dump to
            int glFramebuffer = GL.GenFramebuffer();
            int glResult = GL.GenTexture();
            
            // Used to restore our previous GL state.
            int[] oldTextures = new int[3];
            int oldShader;
            int oldFramebuffer;
            int oldActiveTexture;
            
            // FIXME: Oh Christ, how much do we need to push?
            
            // flibitPushState();
            GL.GetInteger(GetPName.CurrentProgram, out oldShader);
            GL.GetInteger(GetPName.ActiveTexture, out oldActiveTexture);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.GetInteger(GetPName.TextureBinding2D, out oldTextures[0]);
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.GetInteger(GetPName.TextureBinding2D, out oldTextures[1]);
            GL.ActiveTexture(TextureUnit.Texture2);
            GL.GetInteger(GetPName.TextureBinding2D, out oldTextures[2]);
            GL.GetInteger(GetPName.FramebufferBinding, out oldFramebuffer);
            
            // Bind our shader program.
            GL.UseProgram(shaderProgram);
            
            // Set uniform values.
            GL.Uniform1(
                GL.GetUniformLocation(shaderProgram, "samp0"),
                0
            );
            GL.Uniform1(
                GL.GetUniformLocation(shaderProgram, "samp1"),
                1
            );
            GL.Uniform1(
                GL.GetUniformLocation(shaderProgram, "samp2"),
                2
            );
            
            // Our pile of vertices.
            vert_struct[] verts = new vert_struct[4];
                verts[0].pos = new float[2];
                    verts[0].pos[0] = -1.0f;
                    verts[0].pos[1] =  1.0f;
                verts[0].tex = new float[2];
                    verts[0].tex[0] =  0.0f;
                    verts[0].tex[1] =  0.0f;
                verts[1].pos = new float[2];
                    verts[1].pos[0] =  1.0f;
                    verts[1].pos[1] =  1.0f;
                verts[1].tex = new float[2];
                    verts[1].tex[0] =  1.0f;
                    verts[1].tex[1] =  0.0f;
                verts[2].pos = new float[2];
                    verts[2].pos[0] = -1.0f;
                    verts[2].pos[1] = -1.0f;
                verts[2].tex = new float[2];
                    verts[2].tex[0] =  0.0f;
                    verts[2].tex[1] =  1.0f;
                verts[3].pos = new float[2];
                    verts[3].pos[0] =  1.0f;
                    verts[3].pos[1] = -1.0f;
                verts[3].tex = new float[2];
                    verts[3].tex[0] =  1.0f;
                    verts[3].tex[1] =  1.0f;
            
            // Set up the vertex pointers/arrays.
            GL.VertexAttribPointer(
                0,
                2,
                VertexAttribPointerType.Float,
                false,
                16, // FIXME: CHECK THIS!!!
                verts[0].pos
            );
            GL.VertexAttribPointer(
                1,
                2,
                VertexAttribPointerType.Float,
                false,
                16, // FIXME: CHECK THIS!!!
                verts[0].tex
            );
            GL.EnableVertexAttribArray(0);
            GL.EnableVertexAttribArray(1);
            
            // Bind our framebuffer, create and attach our result texture.
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, glFramebuffer);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, glResult);
            GL.TexImage2D(
                TextureTarget.Texture2D,
                0,
                PixelInternalFormat.Rgba,
                (int) currentFrame.width,
                (int) currentFrame.height,
                0,
                PixelFormat.Rgba,
                PixelType.UnsignedInt,
                IntPtr.Zero
            );
            GL.FramebufferTexture2D(
                FramebufferTarget.Framebuffer,
                FramebufferAttachment.ColorAttachment0,
                TextureTarget.Texture2D,
                glResult,
                0
            );
            
            // Prepare YUV GL textures
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, glTextures[0]);
            GL.TexSubImage2D(
                TextureTarget.Texture2D,
                0,
                0,
                0,
                (int) currentFrame.width,
                (int) currentFrame.height,
                PixelFormat.Luminance,
                PixelType.UnsignedByte,
                currentFrame.pixels
            );
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, glTextures[1]);
            GL.TexSubImage2D(
                TextureTarget.Texture2D,
                0,
                0,
                0,
                (int) currentFrame.width / 2,
                (int) currentFrame.height / 2,
                PixelFormat.Luminance,
                PixelType.UnsignedByte,
                new IntPtr(
                    currentFrame.pixels.ToInt64() +
                    (currentFrame.width * currentFrame.height)
                )
            );
            GL.ActiveTexture(TextureUnit.Texture2);
            GL.BindTexture(TextureTarget.Texture2D, glTextures[2]);
            GL.TexSubImage2D(
                TextureTarget.Texture2D,
                0,
                0,
                0,
                (int) currentFrame.width / 2,
                (int) currentFrame.height / 2,
                PixelFormat.Luminance,
                PixelType.UnsignedByte,
                new IntPtr(
                    currentFrame.pixels.ToInt64() +
                    (currentFrame.width / 2 * currentFrame.height / 2)
                )
            );
            
            // FIXME: Uh, I think something is fucked.
            // GL.DrawArrays(BeginMode.TriangleStrip, 0, 4);
            
            // FIXME: Oh gracious, how much are we cleaning up...
            
            // flibitPopState();
            GL.UseProgram(oldShader);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, oldTextures[0]);
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, oldTextures[1]);
            GL.ActiveTexture(TextureUnit.Texture2);
            GL.BindTexture(TextureTarget.Texture2D, oldTextures[2]);
            GL.ActiveTexture(TextureUnit.Texture0 + oldActiveTexture);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, oldFramebuffer);
            
            uint[] theoraPixels = new uint[currentFrame.width * currentFrame.height];
            GL.GetTexImage(
                TextureTarget.Texture2D,
                0,
                PixelFormat.Rgba,
                PixelType.UnsignedInt,
                theoraPixels
            );
            
            // TexImage2D.
            currentTexture.SetData<uint>(theoraPixels);
            
            // Clean up after ourselves.
            GL.DeleteTextures(3, glTextures);
            GL.DeleteTexture(glResult);
            GL.DeleteFramebuffer(glFramebuffer);

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
            theoraDecoder = TheoraPlay.THEORAPLAY_startDecodeFile(
                Video.FileName,
                uint.MaxValue,
                TheoraPlay.THEORAPLAY_VideoFormat.THEORAPLAY_VIDFMT_IYUV
            );
            
            // Initialize the audio stream pointer and get our first packet.
            // FIXME: This check doesn't work.
            // if (TheoraPlay.THEORAPLAY_hasAudioStream(theoraDecoder) != 0)
            {
                while (audioStream == IntPtr.Zero)
                {
                    audioStream = TheoraPlay.THEORAPLAY_getAudio(theoraDecoder);
                    Thread.Sleep(10);
                }
                currentAudio = getAudioPacket(audioStream);
                
                // We're trying to start the decoding ASAP.
                audioDecoderThread.Start();
            }
            
            // Initialize the video stream pointer and get our first frame.
            // FIXME: This check doesn't work.
            //if (TheoraPlay.THEORAPLAY_hasVideoStream(theoraDecoder) != 0)
            {
                while (videoStream == IntPtr.Zero)
                {
                    videoStream = TheoraPlay.THEORAPLAY_getVideo(theoraDecoder);
                    Thread.Sleep(10);
                }
                currentVideo = getVideoFrame(videoStream);
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
            
            // Update the player state.
            State = MediaState.Stopped;
            
            // Wait for the player to end if it's still going.
            if (!playerThread.IsAlive)
            {
                return;
            }
            System.Console.Write("Signaled Theora player to stop, waiting...");
            playerThread.Join();
            System.Console.WriteLine(" Done!");
        }
        
        public void Pause()
        {
            checkDisposed();
            
            // Check the player state before attempting anything.
            if (State != MediaState.Playing)
            {
                return;
            }
            
            // Update the player state.
            State = MediaState.Paused;
        }
        
        public void Resume()
        {
            checkDisposed();
            
            // Check the player state before attempting anything.
            if (State != MediaState.Paused)
            {
                return;
            }
            
            // Update the player state.
            State = MediaState.Playing;
        }
        #endregion
        
        #region The Theora audio decoder thread
        private void StreamAudio(int buffer)
        {
            // The size of our abstracted buffer.
            const int BUFFER_SIZE = 4096 * 16;
            
            // Store our abstracted buffer into here.
            List<float> data = new List<float>();
            
            // Add to the buffer from the decoder until it's large enough.
            while (data.Count < BUFFER_SIZE)
            {
                data.AddRange(
                    getSamples(
                        currentAudio.samples,
                        currentAudio.frames * currentAudio.channels
                    )
                );
                
                do
                {
                    audioStream = TheoraPlay.THEORAPLAY_getAudio(theoraDecoder);
                } while (audioStream == IntPtr.Zero);
                currentAudio = getAudioPacket(audioStream);
                
                if ((BUFFER_SIZE - data.Count) < 4096)
                {
                    break;
                }
            }
            
            // If we actually got data, buffer it into OpenAL.
            if (data.Count > 0)
            {
                AL.BufferData(
                    buffer,
                    (currentAudio.channels == 2) ? ALFormat.StereoFloat32Ext : ALFormat.MonoFloat32Ext,
                    data.ToArray(),
                    data.Count * 2 * currentAudio.channels, // Dear OpenAL: WTF?! Love, flibit
                    currentAudio.freq
                );
            }
        }
        
        private void DecodeAudio()
        {
            // The number of AL buffers to queue into the source.
            const int NUM_BUFFERS = 2;
            
            // Generate the alternating buffers.
            int[] buffers = AL.GenBuffers(NUM_BUFFERS);
            
            // Fill and queue the buffers.
            for (int i = 0; i < NUM_BUFFERS; i++)
            {
                StreamAudio(buffers[i]);
            }
            AL.SourceQueueBuffers(audioSourceIndex, NUM_BUFFERS, buffers);
            
            while (State != MediaState.Stopped)
            {
                // When a buffer has been processed, refill it.
                int processed;
                AL.GetSource(audioSourceIndex, ALGetSourcei.BuffersProcessed, out processed);
                while (processed-- > 0)
                {
                    int buffer = AL.SourceUnqueueBuffer(audioSourceIndex);
                    StreamAudio(buffer);
                    AL.SourceQueueBuffer(audioSourceIndex, buffer);
                }
            }
        }
        #endregion
        
        #region The Theora video player thread
        private void RunVideo()
        {
            while (State != MediaState.Stopped)
            {
                // Sleep when paused, update the video state when playing.
                if (State == MediaState.Paused)
                {
                    // Pause the OpenAL source.
                    if (AL.GetSourceState(audioSourceIndex) == ALSourceState.Playing)
                    {
                        AL.SourcePause(audioSourceIndex);
                    }
                    
                    // Stop the timer in here so we know when we really stopped.
                    if (timer.IsRunning)
                    {
                        timer.Stop();
                    }
                    
                    // Arbitrarily 1 frame in a 30fps movie.
                    Thread.Sleep(33);
                }
                else
                {
                    // Start the timer, whether we're starting or unpausing.
                    if (!timer.IsRunning)
                    {
                        timer.Start();
                    }
                    
                    // If we're getting here, we should be playing the audio...
                    // FIXME: Need a proper check for this.
                    //if (audioStream != IntPtr.Zero)
                    {
                        if (AL.GetSourceState(audioSourceIndex) != ALSourceState.Playing)
                        {
                            AL.SourcePlay(audioSourceIndex);
                        }
                    }
                    
                    // Get the next video from from the decoder, if a stream exists.
                    if (videoStream != IntPtr.Zero)
                    {
                        // Only step when it's time to do so.
                        if (currentVideo.playms <= timer.ElapsedMilliseconds)
                        {
                            // Free current frame, get next frame.
                            videoStream = TheoraPlay.THEORAPLAY_getVideo(theoraDecoder);
                            if (videoStream != IntPtr.Zero)
                            {
                                currentVideo = getVideoFrame(videoStream);
                            }
                        }
                    }
                    else
                    {
                        // Stop and reset the timer.
                        // If we're looping, the loop will start it again.
                        timer.Stop();
                        timer.Reset();
                        
                        // If looping, go back to the start. Otherwise, we'll be exiting.
                        if (IsLooped)
                        {
                            // FIXME: Er, wait, shit.
                            throw new NotImplementedException("Theora looping not implemented!");
                            // Revert to first frame.
                            // AL Stop
                            // AL Rewind
                        }
                        else
                        {
                            State = MediaState.Stopped;
                        }
                    }
                }
            }
            
            // Reset the video timer.
            timer.Stop();
            timer.Reset();
            
            // Stop the decoding, we don't need it anymore.
            audioDecoderThread.Join();
            
            // Force stop the OpenAL source.
            if (AL.GetSourceState(audioSourceIndex) != ALSourceState.Stopped)
            {
                AL.SourceStop(audioSourceIndex);
            }
            AL.SourceRewind(audioSourceIndex);
            AL.DeleteBuffers(AL.SourceUnqueueBuffers(audioSourceIndex, 2));
            
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
        #endregion
    }
}