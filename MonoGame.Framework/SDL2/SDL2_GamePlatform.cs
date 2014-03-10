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
		#region Internal SDL2 Window

		private SDL2_GameWindow INTERNAL_window;

		#endregion

		#region SDL2 OS String

		public static readonly string OSVersion = SDL.SDL_GetPlatform();

		#endregion

		#region Public Properties

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
				return INTERNAL_window.IsVSync;
			}
			set
			{
				INTERNAL_window.IsVSync = value;
			}
		}

		#endregion

		public SDL2_GamePlatform(Game game) : base(game)
		{
			// Set and initialize the SDL2 window
			INTERNAL_window = new SDL2_GameWindow();
			INTERNAL_window.Game = game;
			this.Window = INTERNAL_window;

			// Create the OpenAL device
			new OpenALDevice();
		}


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
			if (IsActive != INTERNAL_window.IsActive)
			{
				IsActive = INTERNAL_window.IsActive;
			}

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
			INTERNAL_window.EndScreenDeviceChange(screenDeviceName, clientWidth, clientHeight);
		}

		public override void BeginScreenDeviceChange(bool willBeFullScreen)
		{
			INTERNAL_window.BeginScreenDeviceChange(willBeFullScreen);
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
			INTERNAL_window.IsMouseVisible = IsMouseVisible;
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
	}
}
