#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE.txt for details.
 */
#endregion

#region Using Statements
using OpenTK.Graphics.OpenGL;
#endregion

namespace Microsoft.Xna.Framework.Graphics
{
	public class OcclusionQuery : GraphicsResource
	{
		#region Public Properties

		public bool IsComplete
		{
			get
			{
				int[] resultReady = {0};
				GL.GetQueryObject(
			glQueryId,
			GetQueryObjectParam.QueryResultAvailable,
			resultReady
		);
				GraphicsExtensions.CheckGLError();
				return resultReady[0] != 0;
			}
		}

		public int PixelCount
		{
			get
			{
				int[] result = {0};
				GL.GetQueryObject(
					glQueryId,
					GetQueryObjectParam.QueryResultAvailable,
					result
				);
				GraphicsExtensions.CheckGLError();
				return result[0];
			}
		}

		#endregion

		#region Private OpenGL Variables

		private uint glQueryId;

		#endregion

		#region Public Constructor

		public OcclusionQuery(GraphicsDevice graphicsDevice)
		{
			this.GraphicsDevice = graphicsDevice;
			GL.GenQueries(1, out glQueryId);
			GraphicsExtensions.CheckGLError();
		}

		#endregion

		#region Protected Dispose Method

		protected override void Dispose(bool disposing)
		{
			if (!IsDisposed)
			{
				GraphicsDevice.AddDisposeAction(() =>
				{
					GL.DeleteQueries(1, ref glQueryId);
					GraphicsExtensions.CheckGLError();
				});
			}
			base.Dispose(disposing);
		}

		#endregion

		#region Public Begin/End Methods

		public void Begin()
		{
			GL.BeginQuery(QueryTarget.SamplesPassed, glQueryId);
			GraphicsExtensions.CheckGLError();
		}

		public void End()
		{
			GL.EndQuery(QueryTarget.SamplesPassed);
			GraphicsExtensions.CheckGLError();
		}

		#endregion
	}
}

