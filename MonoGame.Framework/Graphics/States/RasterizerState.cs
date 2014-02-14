using System;
using System.Diagnostics;
using System.Collections.Generic;

using OpenTK.Graphics.OpenGL;

namespace Microsoft.Xna.Framework.Graphics
{
	public class RasterizerState : GraphicsResource
	{
        // TODO: We should be asserting if the state has
        // been changed after it has been bound to the device!

        public CullMode CullMode { get; set; }
        public float DepthBias { get; set; }
        public FillMode FillMode { get; set; }
        public bool MultiSampleAntiAlias { get; set; }
        public bool ScissorTestEnable { get; set; }
        public float SlopeScaleDepthBias { get; set; }

		private static readonly Utilities.ObjectFactoryWithReset<RasterizerState> _cullClockwise;
        private static readonly Utilities.ObjectFactoryWithReset<RasterizerState> _cullCounterClockwise;
        private static readonly Utilities.ObjectFactoryWithReset<RasterizerState> _cullNone;

        public static RasterizerState CullClockwise { get { return _cullClockwise.Value; } }
        public static RasterizerState CullCounterClockwise { get { return _cullCounterClockwise.Value; } }
        public static RasterizerState CullNone { get { return _cullNone.Value; } }

        // FIXME: Implement this for all GL operations to prevent redundant calls!
        internal static bool INTERNAL_scissorTestEnable = false;

        public RasterizerState()
		{
			CullMode = CullMode.CullCounterClockwiseFace;
			FillMode = FillMode.Solid;
			DepthBias = 0;
			MultiSampleAntiAlias = true;
			ScissorTestEnable = false;
			SlopeScaleDepthBias = 0;
		}

		static RasterizerState ()
		{
			_cullClockwise = new Utilities.ObjectFactoryWithReset<RasterizerState>(() => new RasterizerState
            {
                Name = "RasterizerState.CullClockwise",
				CullMode = CullMode.CullClockwiseFace
			});

			_cullCounterClockwise = new Utilities.ObjectFactoryWithReset<RasterizerState>(() => new RasterizerState
            {
                Name = "RasterizerState.CullCounterClockwise",
				CullMode = CullMode.CullCounterClockwiseFace
			});

			_cullNone = new Utilities.ObjectFactoryWithReset<RasterizerState>(() => new RasterizerState
            {
                Name = "RasterizerState.CullNone",
				CullMode = CullMode.None
			});
		}

        internal void ApplyState(GraphicsDevice device)
        {
        	// When rendering offscreen the faces change order.
            var offscreen = device.GetRenderTargets().Length > 0;

            if (CullMode == CullMode.None)
            {
                GL.Disable(EnableCap.CullFace);
                GraphicsExtensions.CheckGLError();
            }
            else
            {
                GL.Enable(EnableCap.CullFace);
                GraphicsExtensions.CheckGLError();
                GL.CullFace(CullFaceMode.Back);
                GraphicsExtensions.CheckGLError();

                if (CullMode == CullMode.CullClockwiseFace)
                {
                    if (offscreen)
                        GL.FrontFace(FrontFaceDirection.Cw);
                    else
                        GL.FrontFace(FrontFaceDirection.Ccw);
                    GraphicsExtensions.CheckGLError();
                }
                else
                {
                    if (offscreen)
                        GL.FrontFace(FrontFaceDirection.Ccw);
                    else
                        GL.FrontFace(FrontFaceDirection.Cw);
                    GraphicsExtensions.CheckGLError();
                }
            }

			if (FillMode == FillMode.Solid) 
				GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            else
				GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);

			if (ScissorTestEnable && !INTERNAL_scissorTestEnable)
			{
				GL.Enable(EnableCap.ScissorTest);
				INTERNAL_scissorTestEnable = true;
			}
			else if (!ScissorTestEnable && INTERNAL_scissorTestEnable)
			{
				GL.Disable(EnableCap.ScissorTest);
				INTERNAL_scissorTestEnable = false;
			}
            GraphicsExtensions.CheckGLError();

            if (this.DepthBias != 0 || this.SlopeScaleDepthBias != 0)
            {   
                GL.Enable(EnableCap.PolygonOffsetFill);
                GL.PolygonOffset(this.SlopeScaleDepthBias, this.DepthBias);
            }
            else
                GL.Disable(EnableCap.PolygonOffsetFill);
            GraphicsExtensions.CheckGLError();

            // TODO: Implement MultiSampleAntiAlias
        }
    }
}
