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

using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input.Touch;
#endregion

namespace Microsoft.Xna.Framework
{
	public class GraphicsDeviceManager : IGraphicsDeviceService, IDisposable, IGraphicsDeviceManager
	{
		#region Public Properties

		public GraphicsProfile GraphicsProfile
		{ 
			get;
			set;
		}

		public GraphicsDevice GraphicsDevice
		{
			get
			{
				return graphicsDevice;
			}
		}

		public bool IsFullScreen
		{
			get;
			set;
		}

		public bool PreferMultiSampling
		{
			get;
			set;
		}

		public SurfaceFormat PreferredBackBufferFormat
		{
			get;
			set;
		}

		public int PreferredBackBufferHeight
		{
			get;
			set;
		}

		public int PreferredBackBufferWidth
		{
			get;
			set;
		}

		public DepthFormat PreferredDepthStencilFormat
		{
			get;
			set;
		}

		public bool SynchronizeWithVerticalRetrace
		{
			get;
			set;
		}

		public DisplayOrientation SupportedOrientations
		{
			get
			{
				return supportedOrientations;
			}
			set
			{
				supportedOrientations = value;
				if (game.Window != null)
				{
					game.Window.SetSupportedOrientations(supportedOrientations);
				}
			}
		}

		#endregion

		#region Private Variables

		private Game game;
		private GraphicsDevice graphicsDevice;
		private DisplayOrientation supportedOrientations;
		private bool drawBegun;
		private bool disposed;

		#endregion

		#region Public Static Fields

		public static readonly int DefaultBackBufferWidth = 800;
		public static readonly int DefaultBackBufferHeight = 480;

		#endregion

		#region IGraphicsDeviceService Events

		public event EventHandler<EventArgs> DeviceCreated;
		public event EventHandler<EventArgs> DeviceDisposing;
		public event EventHandler<EventArgs> DeviceReset;
		public event EventHandler<EventArgs> DeviceResetting;
		public event EventHandler<PreparingDeviceSettingsEventArgs> PreparingDeviceSettings;

		#endregion

		#region Public Constructor

		public GraphicsDeviceManager(Game game)
		{
			if (game == null)
			{
				throw new ArgumentNullException("The game cannot be null!");
			}

			this.game = game;

			supportedOrientations = DisplayOrientation.Default;

			PreferredBackBufferHeight = DefaultBackBufferHeight;
			PreferredBackBufferWidth = DefaultBackBufferWidth;

			PreferredBackBufferFormat = SurfaceFormat.Color;
			PreferredDepthStencilFormat = DepthFormat.Depth24;

			if (game.Services.GetService(typeof(IGraphicsDeviceManager)) != null)
			{
				throw new ArgumentException("Graphics Device Manager Already Present");
			}

			game.Services.AddService(typeof(IGraphicsDeviceManager), this);
			game.Services.AddService(typeof(IGraphicsDeviceService), this);
		}

		#endregion

		#region Deconstructor

		~GraphicsDeviceManager()
		{
			Dispose(false);
		}

		#endregion

		#region Dispose Methods

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
					if (graphicsDevice != null)
					{
						graphicsDevice.Dispose();
						graphicsDevice = null;
					}
				}
				disposed = true;
			}
		}

		#endregion

		#region Public Methods

		public void CreateDevice()
		{
			PresentationParameters presentationParameters = new PresentationParameters();
			presentationParameters.DepthStencilFormat = DepthFormat.Depth24;

			// It's bad practice to start fullscreen.
			presentationParameters.IsFullScreen = false;

			// TODO: Implement multisampling (aka anti-aliasing) for all platforms!
			if (PreparingDeviceSettings != null)
			{
				GraphicsDeviceInformation gdi = new GraphicsDeviceInformation();

				// Microsoft defaults this to Reach.
				gdi.GraphicsProfile = GraphicsProfile;

				gdi.Adapter = GraphicsAdapter.DefaultAdapter;
				gdi.PresentationParameters = presentationParameters;
				PreparingDeviceSettingsEventArgs pe =
					new PreparingDeviceSettingsEventArgs(gdi);
				PreparingDeviceSettings(this, pe);
				presentationParameters =
					pe.GraphicsDeviceInformation.PresentationParameters;

				/* FIXME: PreparingDeviceSettings may change these parameters,
				 * update ours too? -flibit
				 */
				PreferredBackBufferFormat = presentationParameters.BackBufferFormat;
				PreferredDepthStencilFormat = presentationParameters.DepthStencilFormat;

				GraphicsProfile = pe.GraphicsDeviceInformation.GraphicsProfile;
			}

			// Needs to be before ApplyChanges()
			graphicsDevice = new GraphicsDevice(
				GraphicsAdapter.DefaultAdapter,
				GraphicsProfile,
				presentationParameters
			);

			ApplyChanges();

			/* Set the new display size on the touch panel.
			 *
			 * TODO: In XNA this seems to be done as part of the
			 * GraphicsDevice.DeviceReset event... we need to get
			 * those working.
			 */
			TouchPanel.DisplayWidth =
				graphicsDevice.PresentationParameters.BackBufferWidth;
			TouchPanel.DisplayHeight =
				graphicsDevice.PresentationParameters.BackBufferHeight;
			TouchPanel.DisplayOrientation =
				graphicsDevice.PresentationParameters.DisplayOrientation;

			// Call the DeviceCreated Event
			OnDeviceCreated(EventArgs.Empty);
		}

		public bool BeginDraw()
		{
			if (graphicsDevice == null)
			{
				return false;
			}

			drawBegun = true;
			return true;
		}

		public void EndDraw()
		{
			if (graphicsDevice != null && drawBegun)
			{
				drawBegun = false;
				graphicsDevice.Present();
			}
		}

		public void ApplyChanges()
		{
			// Calling ApplyChanges() before CreateDevice() should have no effect
			if (graphicsDevice == null)
			{
				return;
			}

			// Notify DeviceResetting EventHandlers
			OnDeviceResetting(null);
			GraphicsDevice.OnDeviceResetting();

			// Apply the GraphicsDevice changes internally.
			GraphicsDevice.PresentationParameters.BackBufferFormat =
				PreferredBackBufferFormat;
			GraphicsDevice.PresentationParameters.BackBufferWidth =
				PreferredBackBufferWidth;
			GraphicsDevice.PresentationParameters.BackBufferHeight =
				PreferredBackBufferHeight;
			GraphicsDevice.PresentationParameters.DepthStencilFormat =
				PreferredDepthStencilFormat;
			GraphicsDevice.PresentationParameters.IsFullScreen =
				IsFullScreen;

			// Make the Platform device changes.
			game.Platform.BeginScreenDeviceChange(
				GraphicsDevice.PresentationParameters.IsFullScreen
			);
			game.Platform.EndScreenDeviceChange(
				"FNA",
				GraphicsDevice.PresentationParameters.BackBufferWidth,
				GraphicsDevice.PresentationParameters.BackBufferHeight
			);

			// This platform uses VSyncEnabled rather than PresentationInterval.
			game.Platform.VSyncEnabled = SynchronizeWithVerticalRetrace;

			// ... But we still need to apply the PresentInterval.
			GraphicsDevice.PresentationParameters.PresentationInterval = (
				SynchronizeWithVerticalRetrace ?
					PresentInterval.One :
					PresentInterval.Immediate
			);

			// Notify DeviceReset EventHandlers
			OnDeviceReset(null);
			GraphicsDevice.OnDeviceReset();


			/* Set the new display size on the touch panel.
			 * 
			 * TODO: In XNA this seems to be done as part of the
			 * GraphicsDevice.DeviceReset event... we need to get
			 * those working.
			 */
			TouchPanel.DisplayWidth =
				graphicsDevice.PresentationParameters.BackBufferWidth;
			TouchPanel.DisplayHeight =
				graphicsDevice.PresentationParameters.BackBufferHeight;
		}

		public void ToggleFullScreen()
		{
			IsFullScreen = !IsFullScreen;
			graphicsDevice.PresentationParameters.IsFullScreen = IsFullScreen;

			/* FIXME: This wasn't here before.
			 * Shouldn't this toggle happen immediately?
			 * -flibit
			 */
			if (IsFullScreen)
			{
				game.Platform.EnterFullScreen();
			}
			else
			{
				game.Platform.ExitFullScreen();
			}
		}

		#endregion

		#region Internal IGraphicsDeviceService Methods

		/* FIXME: Why does the GraphicsDeviceManager not know enough about the
		 * GraphicsDevice to raise these events without help?
		 */
		internal void OnDeviceDisposing(EventArgs e)
		{
			Raise(DeviceDisposing, e);
		}

		/* FIXME: Why does the GraphicsDeviceManager not know enough about the
		 * GraphicsDevice to raise these events without help?
		 */
		internal void OnDeviceResetting(EventArgs e)
		{
			Raise(DeviceResetting, e);
		}

		/* FIXME: Why does the GraphicsDeviceManager not know enough about the
		 * GraphicsDevice to raise these events without help?
		 */
		internal void OnDeviceReset(EventArgs e)
		{
			Raise(DeviceReset, e);
		}

		/* FIXME: Why does the GraphicsDeviceManager not know enough about the
		 * GraphicsDevice to raise these events without help?
		 */
		internal void OnDeviceCreated(EventArgs e)
		{
			Raise(DeviceCreated, e);
		}

		#endregion

		#region Private IGraphicsDeviceService Methods

		private void Raise<TEventArgs>(EventHandler<TEventArgs> handler, TEventArgs e)
			where TEventArgs : EventArgs
		{
			if (handler != null)
			{
				handler(this, e);
			}
		}

		#endregion
	}
}
