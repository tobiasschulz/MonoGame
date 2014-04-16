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
using System.Collections.ObjectModel;
#endregion

namespace Microsoft.Xna.Framework.Graphics
{
	public sealed class GraphicsAdapter : IDisposable
	{
		#region Public Properties

		public DisplayMode CurrentDisplayMode
		{
			get
			{
				return Game.Instance.Platform.GetCurrentDisplayMode();
			}
		}

		public DisplayModeCollection SupportedDisplayModes
		{
			get
			{
				return Game.Instance.Platform.GetDisplayModes();
			}
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

		public int VendorId
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		#endregion

		#region Public Static Properties

		public static GraphicsAdapter DefaultAdapter
		{
			get
			{
				return Adapters[0];
			}
		}

		public static ReadOnlyCollection<GraphicsAdapter> Adapters
		{
			get
			{
				if (adapters == null)
				{
					adapters = new ReadOnlyCollection<GraphicsAdapter>(
						new GraphicsAdapter[]
						{
							new GraphicsAdapter()
						}
					);
				}
				return adapters;
			}
		}

		#endregion

		#region Private Static Variables

		private static ReadOnlyCollection<GraphicsAdapter> adapters;

		#endregion

		#region Internal Constructor

		internal GraphicsAdapter()
		{
		}

		#endregion

		#region Public Dispose Method

		public void Dispose()
		{
		}

		#endregion

		#region Public Methods

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

		#endregion
	}
}
