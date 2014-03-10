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
#endregion

namespace Microsoft.Xna.Framework.Input
{
	/// <summary>
	/// Allows reading position and button click information from mouse.
	/// </summary>
	public static class Mouse
	{
		#region Internal Properties and Variables

		internal static GameWindow PrimaryWindow;

		internal static IntPtr INTERNAL_sdlWindowHandle;

		public static IntPtr WindowHandle
		{
			get
			{
				return INTERNAL_sdlWindowHandle;
			}
		}

		internal static int INTERNAL_MouseWheel
		{
			get;
			set;
		}

		internal static int INTERNAL_BackbufferWidth = 800;
		internal static int INTERNAL_BackbufferHeight = 600;
		internal static int INTERNAL_WindowWidth = 800;
		internal static int INTERNAL_WindowHeight = 600;

		internal static bool INTERNAL_IsWarped = false;

		#endregion

		#region Public Interface

		/// <summary>
		/// Gets mouse state information that includes position and button
		/// presses for the provided window
		/// </summary>
		/// <returns>Current state of the mouse.</returns>
		public static MouseState GetState(GameWindow window)
		{
			int x, y;
			uint flags = SDL.SDL_GetMouseState(out x, out y);

			x = (int)((double)x * Graphics.OpenGLDevice.Instance.Backbuffer.Width / INTERNAL_WindowWidth);
			y = (int)((double)y * Graphics.OpenGLDevice.Instance.Backbuffer.Height / INTERNAL_WindowHeight);

			if (!INTERNAL_IsWarped)
			{
				window.MouseState.X = x;
				window.MouseState.Y = y;
			}

			window.MouseState.LeftButton = (ButtonState) (flags & SDL.SDL_BUTTON_LMASK);
			window.MouseState.RightButton = (ButtonState) ((flags & SDL.SDL_BUTTON_RMASK) >> 2);
			window.MouseState.MiddleButton = (ButtonState) ((flags & SDL.SDL_BUTTON_MMASK) >> 1);

			window.MouseState.ScrollWheelValue = INTERNAL_MouseWheel;

			return window.MouseState;
		}

		/// <summary>
		/// Gets mouse state information that includes position and button presses
		/// for the primary window
		/// </summary>
		/// <returns>Current state of the mouse.</returns>
		public static MouseState GetState()
		{
			return GetState(PrimaryWindow);
		}

		/// <summary>
		/// Sets mouse cursor's relative position to game-window.
		/// </summary>
		/// <param name="x">Relative horizontal position of the cursor.</param>
		/// <param name="y">Relative vertical position of the cursor.</param>
		public static void SetPosition(int x, int y)
		{
			x = (int)((double)x * INTERNAL_WindowWidth / Graphics.OpenGLDevice.Instance.Backbuffer.Width);
			y = (int)((double)y * INTERNAL_WindowHeight / Graphics.OpenGLDevice.Instance.Backbuffer.Height);
			PrimaryWindow.MouseState.X = x;
			PrimaryWindow.MouseState.Y = y;

			SDL.SDL_WarpMouseInWindow(INTERNAL_sdlWindowHandle, x, y);
			INTERNAL_IsWarped = true;
		}

		#endregion
	}
}

