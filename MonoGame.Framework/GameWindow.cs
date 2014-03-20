#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using System;
using System.ComponentModel;

namespace Microsoft.Xna.Framework {
	public abstract class GameWindow {
		#region Properties

		[DefaultValue(false)]
		public abstract bool AllowUserResizing { get; set; }

		public abstract Rectangle ClientBounds { get; }

		public abstract DisplayOrientation CurrentOrientation { get; }

		public abstract IntPtr Handle { get; }

		public abstract string ScreenDeviceName { get; }

		private string _title;
		public string Title {
			get { return _title; }
			set {
				if (_title != value) {
					SetTitle(value);
					_title = value;
				}
			}
		}

        /// <summary>
        /// Determines whether the border of the window is visible. Currently only supported on the WinDX and SDL2 platforms.
        /// </summary>
        /// <exception cref="System.NotImplementedException">
        /// Thrown when trying to use this property on a platform other than the WinDX and SDL2 platforms.
        /// </exception>
        public virtual bool IsBorderless
        {
            get
            {
                return false;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        internal MouseState MouseState;
	    internal TouchPanelState TouchPanelState;

        protected GameWindow()
        {
            // TODO: Fix the AndroidGameWindow!
            TouchPanelState = new TouchPanelState(this);
        }

		#endregion Properties

		#region Events

		public event EventHandler<EventArgs> ClientSizeChanged;
		public event EventHandler<EventArgs> OrientationChanged;
		public event EventHandler<EventArgs> ScreenDeviceNameChanged;

		/// <summary>
		/// Use this event to retrieve text for objects like textbox's.
		/// This event is not raised by noncharacter keys.
		/// This event also supports key repeat.
		/// For more information this event is based off:
		/// http://msdn.microsoft.com/en-AU/library/system.windows.forms.control.keypress.aspx
		/// </summary>
		/// <remarks>
		/// This event is only supported on the Windows DirectX and SDL2 platforms.
		/// </remarks>
		public event EventHandler<TextInputEventArgs> TextInput;

		#endregion Events

		public abstract void BeginScreenDeviceChange (bool willBeFullScreen);

		public abstract void EndScreenDeviceChange (
			string screenDeviceName, int clientWidth, int clientHeight);

		public void EndScreenDeviceChange (string screenDeviceName)
		{
			EndScreenDeviceChange(screenDeviceName, ClientBounds.Width, ClientBounds.Height);
		}

		protected void OnActivated ()
		{
		}

		protected void OnClientSizeChanged ()
		{
			if (ClientSizeChanged != null)
				ClientSizeChanged (this, EventArgs.Empty);
		}

		protected void OnDeactivated ()
		{
		}
         
		protected void OnOrientationChanged ()
		{
			if (OrientationChanged != null)
				OrientationChanged (this, EventArgs.Empty);
		}

		protected void OnPaint ()
		{
		}

		protected void OnScreenDeviceNameChanged ()
		{
			if (ScreenDeviceNameChanged != null)
				ScreenDeviceNameChanged (this, EventArgs.Empty);
		}

		protected void OnTextInput(object sender, TextInputEventArgs e)
		{
			if (TextInput != null)
				TextInput(sender, e);
		}

		protected internal abstract void SetSupportedOrientations (DisplayOrientation orientations);
		protected abstract void SetTitle (string title);

    }
}
