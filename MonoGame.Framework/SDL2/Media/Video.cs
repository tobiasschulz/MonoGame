#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

#region VIDEOPLAYER_OPENGL Option
/* By default we use a small fragment shader to perform the YUV-RGBA conversion.
 * If for some reason you need to use the software converter in TheoraPlay,
 * comment out this define.
 * -flibit
 */
#define VIDEOPLAYER_OPENGL
#endregion

using System;
using System.IO;
using System.Threading;

using Microsoft.Xna.Framework.Graphics;

namespace Microsoft.Xna.Framework.Media
{
	public sealed class Video : IDisposable
	{
		#region Public Properties

		public int Width
		{
			get;
			private set;
		}

		public int Height
		{
			get;
			private set;
		}

		public string FileName
		{
			get;
			private set;
		}

		private float INTERNAL_fps = 0.0f;
		public float FramesPerSecond
		{
			get
			{
				return INTERNAL_fps;
			}
			internal set
			{
				INTERNAL_fps = value;
			}
		}

		// FIXME: This is hacked, look up "This is a part of the Duration hack!"
		public TimeSpan Duration
		{
			get;
			internal set;
		}

		#endregion

		#region Internal Properties

		internal bool IsDisposed
		{
			get;
			private set;
		}

		internal bool AttachedToPlayer
		{
			get;
			set;
		}

		#endregion

		#region Internal Variables: TheoraPlay

		internal IntPtr theoraDecoder;
		internal IntPtr videoStream;

		#endregion

		#region Internal Video Constructor

		internal Video(string fileName)
		{
			// Check out the file...
			FileName = Normalize(fileName);
			if (FileName == null)
			{
				throw new Exception("File " + fileName + " does not exist!");
			}

			// Set everything to NULL. Yes, this actually matters later.
			theoraDecoder = IntPtr.Zero;
			videoStream = IntPtr.Zero;

			// Initialize the decoder nice and early...
			IsDisposed = true;
			AttachedToPlayer = false;
			Initialize();

			// FIXME: This is a part of the Duration hack!
			Duration = TimeSpan.MaxValue;
		}

		#endregion

		#region Public Dispose Method

		public void Dispose()
		{
			if (AttachedToPlayer)
			{
				return; // NOPE. VideoPlayer will do the honors.
			}

			// Stop and unassign the decoder.
			if (theoraDecoder != IntPtr.Zero)
			{
				TheoraPlay.THEORAPLAY_stopDecode(theoraDecoder);
				theoraDecoder = IntPtr.Zero;
			}

			// Free and unassign the video stream.
			if (videoStream != IntPtr.Zero)
			{
				TheoraPlay.THEORAPLAY_freeVideo(videoStream);
				videoStream = IntPtr.Zero;
			}

			IsDisposed = true;
		}

		#endregion

		#region Internal Filename Normalizer

		internal static string Normalize(string FileName)
		{
			if (File.Exists(FileName))
			{
				return FileName;
			}

			// Check the file extension
			if (!string.IsNullOrEmpty(Path.GetExtension(FileName)))
			{
				return null;
			}

			// Concat the file name with valid extensions
			if (File.Exists(FileName + ".ogv"))
			{
				return FileName + ".ogv";
			}
			if (File.Exists(FileName + ".ogg"))
			{
				return FileName + ".ogg";
			}

			return null;
		}

		#endregion

		#region Internal TheoraPlay Initialization

		internal void Initialize()
		{
			if (!IsDisposed)
			{
				Dispose(); // We need to start from the beginning, don't we? :P
			}

			// Initialize the decoder.
			theoraDecoder = TheoraPlay.THEORAPLAY_startDecodeFile(
				FileName,
				150, // Arbitrarily 5 seconds in a 30fps movie.
#if VIDEOPLAYER_OPENGL
				TheoraPlay.THEORAPLAY_VideoFormat.THEORAPLAY_VIDFMT_IYUV
#else
				// Use the TheoraPlay software converter.
				TheoraPlay.THEORAPLAY_VideoFormat.THEORAPLAY_VIDFMT_RGBA
#endif
			);

			// Wait until the decoder is ready.
			while (TheoraPlay.THEORAPLAY_isInitialized(theoraDecoder) == 0)
			{
				Thread.Sleep(10);
			}

			// Initialize the video stream pointer and get our first frame.
			if (TheoraPlay.THEORAPLAY_hasVideoStream(theoraDecoder) != 0)
			{
				while (videoStream == IntPtr.Zero)
				{
					videoStream = TheoraPlay.THEORAPLAY_getVideo(theoraDecoder);
					Thread.Sleep(10);
				}

				TheoraPlay.THEORAPLAY_VideoFrame frame = TheoraPlay.getVideoFrame(videoStream);

				// We get the FramesPerSecond from the first frame.
				FramesPerSecond = (float) frame.fps;
				Width = (int) frame.width;
				Height = (int) frame.height;
			}

			IsDisposed = false;
		}

		#endregion
	}
}
