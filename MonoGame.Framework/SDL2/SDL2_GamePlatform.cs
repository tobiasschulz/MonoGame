#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

#region Using Statements
using System;

using SDL2;

using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
#endregion

namespace Microsoft.Xna.Framework
{
	class SDL2_GamePlatform : GamePlatform
	{
		#region Public GamePlatform Properties

		public override GameRunBehavior DefaultRunBehavior
		{
			get
			{
				return GameRunBehavior.Synchronous;
			}
		}

		public override bool VSyncEnabled
		{
			get
			{
				int result = 0;
				result = SDL.SDL_GL_GetSwapInterval();
				return (result == 1 || result == -1);
			}
			set
			{
				if (value)
				{
					if (SDL2_GamePlatform.OSVersion.Equals("Mac OS X"))
					{
						// Apple is a big fat liar about swap_control_tear. Use stock VSync.
						SDL.SDL_GL_SetSwapInterval(1);
					}
					else
					{
						if (SDL.SDL_GL_SetSwapInterval(-1) != -1)
						{
							System.Console.WriteLine("Using EXT_swap_control_tear VSync!");
						}
						else
						{
							System.Console.WriteLine("EXT_swap_control_tear unsupported. Fall back to standard VSync.");
							SDL.SDL_ClearError();
							SDL.SDL_GL_SetSwapInterval(1);
						}
					}
				}
				else
				{
					SDL.SDL_GL_SetSwapInterval(0);
				}
			}
		}

		#endregion

		#region SDL2 OS String

		public static readonly string OSVersion = SDL.SDL_GetPlatform();

		#endregion

		#region Internal SDL2 Window

		private SDL2_GameWindow INTERNAL_window;

		#endregion

		#region Public Constructor

		public SDL2_GamePlatform(Game game) : base(game)
		{
			// Set and initialize the SDL2 window
			INTERNAL_window = new SDL2_GameWindow(game, this);
			this.Window = INTERNAL_window;

			// Create the OpenAL device
			new OpenALDevice();
		}

		#endregion

		#region Public GamePlatform Methods

		public override void RunLoop()
		{
			INTERNAL_window.INTERNAL_RunLoop();
		}

		public override void StartRunLoop()
		{
			throw new NotImplementedException("SDL2_GamePlatform does not use this!");
		}

		public override void Exit()
		{
			// Stop the game loop
			INTERNAL_window.INTERNAL_StopLoop();

			// End the network subsystem
			Net.NetworkSession.Exit();

			// Close SDL2_mixer if needed
			Media.Song.closeMixer();
		}

		public override bool BeforeUpdate(GameTime gameTime)
		{
			// Update our OpenAL sound buffer pools
			OpenALDevice.Instance.Update();

			return true;
		}

		public override bool BeforeDraw(GameTime gameTime)
		{
			return true;
		}

		public override void EnterFullScreen()
		{
			BeginScreenDeviceChange(true);
			EndScreenDeviceChange(
				"SDL2",
				Graphics.OpenGLDevice.Instance.Backbuffer.Width,
				Graphics.OpenGLDevice.Instance.Backbuffer.Height
			);
		}

		public override void ExitFullScreen()
		{
			BeginScreenDeviceChange(false);
			EndScreenDeviceChange(
				"SDL2",
				Graphics.OpenGLDevice.Instance.Backbuffer.Width,
				Graphics.OpenGLDevice.Instance.Backbuffer.Height
			);
		}

		public override void EndScreenDeviceChange(string screenDeviceName, int clientWidth, int clientHeight)
		{
			Window.EndScreenDeviceChange(screenDeviceName, clientWidth, clientHeight);
		}

		public override void BeginScreenDeviceChange(bool willBeFullScreen)
		{
			Window.BeginScreenDeviceChange(willBeFullScreen);
		}

		public override void Log(string Message)
		{
			Console.WriteLine(Message);
		}

		public override void Present()
		{
			base.Present();

			GraphicsDevice device = Game.GraphicsDevice;
			if (device != null)
			{
				device.Present();
			}

			if (INTERNAL_window != null)
			{
				INTERNAL_window.INTERNAL_SwapBuffers();
			}
		}

		protected override void OnIsMouseVisibleChanged()
		{
			SDL.SDL_ShowCursor(IsMouseVisible ? 1 : 0);
		}

		protected override void Dispose(bool disposing)
		{
			if (!IsDisposed)
			{
				if (INTERNAL_window != null)
				{
					INTERNAL_window.INTERNAL_Destroy();
					INTERNAL_window = null;
				}

				if (OpenALDevice.Instance != null)
				{
					OpenALDevice.Instance.Dispose();
				}
			}

			base.Dispose(disposing);
		}

		#endregion
	}
}
