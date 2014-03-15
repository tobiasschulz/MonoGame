#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

#if IOS
using MonoTouch.UIKit;
#elif ANDROID
using Android.Views;
#endif

namespace Microsoft.Xna.Framework.Graphics
{
    public sealed class GraphicsAdapter : IDisposable
    {
        private static ReadOnlyCollection<GraphicsAdapter> adapters;
        
#if IOS
		private UIScreen _screen;
        internal GraphicsAdapter(UIScreen screen)
        {
            _screen = screen;
        }
#elif ANDROID
        private View _view;
        internal GraphicsAdapter(View screen)
        {
            _view = screen;
        }
#else
        internal GraphicsAdapter()
        {
        }
#endif
        
        public void Dispose()
        {
        }

        public DisplayMode CurrentDisplayMode
        {
            get
            {
#if SDL2
                SDL2.SDL.SDL_DisplayMode mode;
                SDL2.SDL.SDL_GetCurrentDisplayMode(0, out mode);
                return new DisplayMode(
                    mode.w,
                    mode.h,
                    SurfaceFormat.Color
                );
#elif IOS
                return new DisplayMode((int)(_screen.Bounds.Width * _screen.Scale),
                       (int)(_screen.Bounds.Height * _screen.Scale),
                       60,
                       SurfaceFormat.Color);
#elif ANDROID
                return new DisplayMode(_view.Width, _view.Height, 60, SurfaceFormat.Color);
#else
                return new DisplayMode(800, 600, 60, SurfaceFormat.Color);
#endif
            }
        }

        public static GraphicsAdapter DefaultAdapter
        {
            get { return Adapters[0]; }
        }
        
        public static ReadOnlyCollection<GraphicsAdapter> Adapters {
            get {
                if (adapters == null) {
#if IOS
					adapters = new ReadOnlyCollection<GraphicsAdapter>(
						new GraphicsAdapter[] {new GraphicsAdapter(UIScreen.MainScreen)});
#elif ANDROID
                    adapters = new ReadOnlyCollection<GraphicsAdapter>(new GraphicsAdapter[] { new GraphicsAdapter(Game.Instance.Window) });
#else
                    adapters = new ReadOnlyCollection<GraphicsAdapter>(
						new GraphicsAdapter[] {new GraphicsAdapter()});
#endif
                }
                return adapters;
            }
        } 
		
        /*
		public bool QueryRenderTargetFormat(
			GraphicsProfile graphicsProfile,
			SurfaceFormat format,
			DepthFormat depthFormat,
			int multiSampleCount,
			out SurfaceFormat selectedFormat,
			out DepthFormat selectedDepthFormat,
			out int selectedMultiSampleCount)
		{
			throw new NotImplementedException();
		}

		public bool QueryBackBufferFormat(
			GraphicsProfile graphicsProfile,
			SurfaceFormat format,
			DepthFormat depthFormat,
			int multiSampleCount,
			out SurfaceFormat selectedFormat,
			out DepthFormat selectedDepthFormat,
			out int selectedMultiSampleCount)
		{
			throw new NotImplementedException("flibit put this here.");
		}

        public string Description
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public int DeviceId
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public Guid DeviceIdentifier
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public string DeviceName
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public string DriverDll
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public Version DriverVersion
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool IsDefaultAdapter
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool IsWideScreen
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IntPtr MonitorHandle
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public int Revision
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public int SubSystemId
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        */

        private DisplayModeCollection supportedDisplayModes = null;
        
        public DisplayModeCollection SupportedDisplayModes
        {
            get
            {

                if (supportedDisplayModes == null)
                {
                    List<DisplayMode> modes = new List<DisplayMode>(new DisplayMode[] { CurrentDisplayMode, });
#if SDL2
                    SDL2.SDL.SDL_DisplayMode filler = new SDL2.SDL.SDL_DisplayMode();
                    int numModes = SDL2.SDL.SDL_GetNumDisplayModes(0);
                    for (int i = 0; i < numModes; i += 1)
                    {
                        SDL2.SDL.SDL_GetDisplayMode(0, i, out filler);

                        // Check for dupes caused by varying refresh rates.
                        bool dupe = false;
                        foreach (DisplayMode mode in modes)
                        {
                            if (filler.w == mode.Width && filler.h == mode.Height)
                            {
                                dupe = true;
                            }
                        }
                        if (dupe)
                        {
                            continue;
                        }

                        modes.Add(
                            new DisplayMode(
                                filler.w,
                                filler.h,
                                SurfaceFormat.Color // FIXME: Assumption!
                            )
                        );
                    }
#endif
                    supportedDisplayModes = new DisplayModeCollection(modes);
                }
                return supportedDisplayModes;
            }
        }

        /*
        public int VendorId
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        */
    }
}

