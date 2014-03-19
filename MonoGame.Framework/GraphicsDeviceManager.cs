#region License
/*
Microsoft Public License (Ms-PL)
MonoGame - Copyright © 2009 The MonoGame Team

All rights reserved.

This license governs use of the accompanying software. If you use the software, you accept this license. If you do not
accept the license, do not use the software.

1. Definitions
The terms "reproduce," "reproduction," "derivative works," and "distribution" have the same meaning here as under 
U.S. copyright law.

A "contribution" is the original software, or any additions or changes to the software.
A "contributor" is any person that distributes its contribution under this license.
"Licensed patents" are a contributor's patent claims that read directly on its contribution.

2. Grant of Rights
(A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, 
each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution, prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
(B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, 
each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or derivative works of the contribution in the software.

3. Conditions and Limitations
(A) No Trademark License- This license does not grant you rights to use any contributors' name, logo, or trademarks.
(B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software, 
your patent license from such contributor to the software ends automatically.
(C) If you distribute any portion of the software, you must retain all copyright, patent, trademark, and attribution 
notices that are present in the software.
(D) If you distribute any portion of the software in source code form, you may do so only under this license by including 
a complete copy of this license with your distribution. If you distribute any portion of the software in compiled or object 
code form, you may only do so under a license that complies with this license.
(E) The software is licensed "as-is." You bear the risk of using it. The contributors give no express warranties, guarantees
or conditions. You may have additional consumer rights under your local laws which this license cannot change. To the extent
permitted under your local laws, the contributors exclude the implied warranties of merchantability, fitness for a particular
purpose and non-infringement.
*/
#endregion License

using System;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input.Touch;

namespace Microsoft.Xna.Framework
{
    public class GraphicsDeviceManager : IGraphicsDeviceService, IDisposable, IGraphicsDeviceManager
    {
        private Game _game;
        private GraphicsDevice _graphicsDevice;
        private int _preferredBackBufferHeight;
        private int _preferredBackBufferWidth;
        private SurfaceFormat _preferredBackBufferFormat;
        private DepthFormat _preferredDepthStencilFormat;
        private bool _preferMultiSampling;
        private DisplayOrientation _supportedOrientations;
        private bool _synchronizedWithVerticalRetrace = true;
        private bool _drawBegun;
        bool disposed;

		private bool _SynchronizedWithVerticalRetrace 
		{
			get { return _synchronizedWithVerticalRetrace; }
		}

        private bool _wantFullScreen = false;
        public static readonly int DefaultBackBufferHeight = 480;
        public static readonly int DefaultBackBufferWidth = 800;

        public GraphicsDeviceManager(Game game)
        {
            if (game == null)
                throw new ArgumentNullException("The game cannot be null!");

            _game = game;

            _supportedOrientations = DisplayOrientation.Default;

            _preferredBackBufferHeight = DefaultBackBufferHeight;
            _preferredBackBufferWidth = DefaultBackBufferWidth;

            _preferredBackBufferFormat = SurfaceFormat.Color;
            _preferredDepthStencilFormat = DepthFormat.Depth24;
            _synchronizedWithVerticalRetrace = true;

            if (_game.Services.GetService(typeof(IGraphicsDeviceManager)) != null)
                throw new ArgumentException("Graphics Device Manager Already Present");

            _game.Services.AddService(typeof(IGraphicsDeviceManager), this);
            _game.Services.AddService(typeof(IGraphicsDeviceService), this);
        }

        ~GraphicsDeviceManager()
        {
            Dispose(false);
        }

        public void CreateDevice()
        {
            Initialize();

            OnDeviceCreated(EventArgs.Empty);
        }

        public bool BeginDraw()
        {
            if (_graphicsDevice == null)
                return false;

            _drawBegun = true;
            return true;
        }

        public void EndDraw()
        {
            if (_graphicsDevice != null && _drawBegun)
            {
                _drawBegun = false;
                _graphicsDevice.Present();
            }
        }

        #region IGraphicsDeviceService Members

        public event EventHandler<EventArgs> DeviceCreated;
        public event EventHandler<EventArgs> DeviceDisposing;
        public event EventHandler<EventArgs> DeviceReset;
        public event EventHandler<EventArgs> DeviceResetting;
        public event EventHandler<PreparingDeviceSettingsEventArgs> PreparingDeviceSettings;

        // FIXME: Why does the GraphicsDeviceManager not know enough about the
        //        GraphicsDevice to raise these events without help?
        internal void OnDeviceDisposing(EventArgs e)
        {
            Raise(DeviceDisposing, e);
        }

        // FIXME: Why does the GraphicsDeviceManager not know enough about the
        //        GraphicsDevice to raise these events without help?
        internal void OnDeviceResetting(EventArgs e)
        {
            Raise(DeviceResetting, e);
        }

        // FIXME: Why does the GraphicsDeviceManager not know enough about the
        //        GraphicsDevice to raise these events without help?
        internal void OnDeviceReset(EventArgs e)
        {
            Raise(DeviceReset, e);
        }

        // FIXME: Why does the GraphicsDeviceManager not know enough about the
        //        GraphicsDevice to raise these events without help?
        internal void OnDeviceCreated(EventArgs e)
        {
            Raise(DeviceCreated, e);
        }

        private void Raise<TEventArgs>(EventHandler<TEventArgs> handler, TEventArgs e)
            where TEventArgs : EventArgs
        {
            if (handler != null)
                handler(this, e);
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (_graphicsDevice != null)
                    {
                        _graphicsDevice.Dispose();
                        _graphicsDevice = null;
                    }
                }
                disposed = true;
            }
        }

        #endregion

        public void ApplyChanges()
        {
            // Calling ApplyChanges() before CreateDevice() should have no effect
            if (_graphicsDevice == null)
                return;

            // Notify DeviceResetting EventHandlers
            OnDeviceResetting(null);
            GraphicsDevice.OnDeviceResetting();

            // Apply the GraphicsDevice changes internally.
            GraphicsDevice.PresentationParameters.BackBufferFormat = PreferredBackBufferFormat;
            GraphicsDevice.PresentationParameters.BackBufferWidth = PreferredBackBufferWidth;
            GraphicsDevice.PresentationParameters.BackBufferHeight = PreferredBackBufferHeight;
            GraphicsDevice.PresentationParameters.DepthStencilFormat = PreferredDepthStencilFormat;
            IsFullScreen = _wantFullScreen;
            
            // Make the Platform device changes.
            _game.Platform.BeginScreenDeviceChange(
                GraphicsDevice.PresentationParameters.IsFullScreen
            );
            _game.Platform.EndScreenDeviceChange(
                "SDL2",
                GraphicsDevice.PresentationParameters.BackBufferWidth,
                GraphicsDevice.PresentationParameters.BackBufferHeight
            );
            
            // This platform uses VSyncEnabled rather than PresentationInterval.
            _game.Platform.VSyncEnabled = SynchronizeWithVerticalRetrace;

            // ... But we still need to apply the PresentInterval.
            GraphicsDevice.PresentationParameters.PresentationInterval = (
                SynchronizeWithVerticalRetrace ?
                    PresentInterval.One :
                    PresentInterval.Immediate
            );

            // Notify DeviceReset EventHandlers
            OnDeviceReset(null);
            GraphicsDevice.OnDeviceReset();


            // Set the new display size on the touch panel.
            //
            // TODO: In XNA this seems to be done as part of the 
            // GraphicsDevice.DeviceReset event... we need to get 
            // those working.
            //
            TouchPanel.DisplayWidth = _graphicsDevice.PresentationParameters.BackBufferWidth;
            TouchPanel.DisplayHeight = _graphicsDevice.PresentationParameters.BackBufferHeight;
        }

        private void Initialize()
        {
            var presentationParameters = new PresentationParameters();
            presentationParameters.DepthStencilFormat = DepthFormat.Depth24;

            // It's bad practice to start fullscreen.
            presentationParameters.IsFullScreen = false;

            // TODO: Implement multisampling (aka anti-aliasing) for all platforms!
            if (PreparingDeviceSettings != null)
            {
                GraphicsDeviceInformation gdi = new GraphicsDeviceInformation();
                gdi.GraphicsProfile = GraphicsProfile; // Microsoft defaults this to Reach.
                gdi.Adapter = GraphicsAdapter.DefaultAdapter;
                gdi.PresentationParameters = presentationParameters;
                PreparingDeviceSettingsEventArgs pe = new PreparingDeviceSettingsEventArgs(gdi);
                PreparingDeviceSettings(this, pe);
                presentationParameters = pe.GraphicsDeviceInformation.PresentationParameters;

                // FIXME: PreparingDeviceSettings may change these parameters, update ours too? -flibit
                PreferredBackBufferFormat = presentationParameters.BackBufferFormat;
                PreferredDepthStencilFormat = presentationParameters.DepthStencilFormat;

                GraphicsProfile = pe.GraphicsDeviceInformation.GraphicsProfile;
            }

            // Needs to be before ApplyChanges()
            _graphicsDevice = new GraphicsDevice(GraphicsAdapter.DefaultAdapter, GraphicsProfile, presentationParameters);

            ApplyChanges();

            // Set the new display size on the touch panel.
            //
            // TODO: In XNA this seems to be done as part of the 
            // GraphicsDevice.DeviceReset event... we need to get 
            // those working.
            //
            TouchPanel.DisplayWidth = _graphicsDevice.PresentationParameters.BackBufferWidth;
            TouchPanel.DisplayHeight = _graphicsDevice.PresentationParameters.BackBufferHeight;
            TouchPanel.DisplayOrientation = _graphicsDevice.PresentationParameters.DisplayOrientation;
        }

        public void ToggleFullScreen()
        {
            IsFullScreen = !IsFullScreen;
            
            /* FIXME: This wasn't here before.
             * Shouldn't this toggle happen immediately?
             * -flibit
             */
            if (IsFullScreen)
            {
                _game.Platform.EnterFullScreen();
            }
            else
            {
                _game.Platform.ExitFullScreen();
            }
        }

        public GraphicsProfile GraphicsProfile { get; set; }

        public GraphicsDevice GraphicsDevice
        {
            get
            {
                return _graphicsDevice;
            }
        }

        public bool IsFullScreen
        {
            get
            {
                if (_graphicsDevice != null)
                    return _graphicsDevice.PresentationParameters.IsFullScreen;
                else
                    return _wantFullScreen;
            }
            set
            {
                _wantFullScreen = value;
                if (_graphicsDevice != null)
                {
                    _graphicsDevice.PresentationParameters.IsFullScreen = value;
                }
            }
        }

        public bool PreferMultiSampling
        {
            get
            {
                return _preferMultiSampling;
            }
            set
            {
                _preferMultiSampling = value;
            }
        }

        public SurfaceFormat PreferredBackBufferFormat
        {
            get
            {
                return _preferredBackBufferFormat;
            }
            set
            {
                _preferredBackBufferFormat = value;
            }
        }

        public int PreferredBackBufferHeight
        {
            get
            {
                return _preferredBackBufferHeight;
            }
            set
            {
                _preferredBackBufferHeight = value;
            }
        }

        public int PreferredBackBufferWidth
        {
            get
            {
                return _preferredBackBufferWidth;
            }
            set
            {
                _preferredBackBufferWidth = value;
            }
        }

        public DepthFormat PreferredDepthStencilFormat
        {
            get
            {
                return _preferredDepthStencilFormat;
            }
            set
            {
                _preferredDepthStencilFormat = value;
            }
        }

        public bool SynchronizeWithVerticalRetrace
        {
            get
            {
                return _synchronizedWithVerticalRetrace;
            }
            set
            {
                _synchronizedWithVerticalRetrace = value;
            }
        }

        public DisplayOrientation SupportedOrientations
        {
            get
            {
                return _supportedOrientations;
            }
            set
            {
                _supportedOrientations = value;
                if (_game.Window != null)
                    _game.Window.SetSupportedOrientations(_supportedOrientations);
            }
        }

        /// <summary>
        /// This method is used by MonoGame Android to adjust the game's drawn to area to fill
        /// as much of the screen as possible whilst retaining the aspect ratio inferred from
        /// aspectRatio = (PreferredBackBufferWidth / PreferredBackBufferHeight)
        ///
        /// NOTE: this is a hack that should be removed if proper back buffer to screen scaling
        /// is implemented. To disable it's effect, in the game's constructor use:
        ///
        ///     graphics.IsFullScreen = true;
        ///     graphics.PreferredBackBufferHeight = Window.ClientBounds.Height;
        ///     graphics.PreferredBackBufferWidth = Window.ClientBounds.Width;
        ///
        /// </summary>
        internal void ResetClientBounds()
        {
        }

    }
}
