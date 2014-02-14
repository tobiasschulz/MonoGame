using System;
using System.Diagnostics;
using System.Collections.Generic;

using OpenTK.Graphics.OpenGL;
using GLStencilFunction = OpenTK.Graphics.OpenGL.StencilFunction;

namespace Microsoft.Xna.Framework.Graphics
{
	public class DepthStencilState : GraphicsResource
    {

        // TODO: We should be asserting if the state has
        // been changed after it has been bound to the device!

        public bool DepthBufferEnable { get; set; }
        public bool DepthBufferWriteEnable { get; set; }
        public StencilOperation CounterClockwiseStencilDepthBufferFail { get; set; }
        public StencilOperation CounterClockwiseStencilFail { get; set; }
        public CompareFunction CounterClockwiseStencilFunction { get; set; }
        public StencilOperation CounterClockwiseStencilPass { get; set; }
        public CompareFunction DepthBufferFunction { get; set; }
        public int ReferenceStencil { get; set; }
        public StencilOperation StencilDepthBufferFail { get; set; }
        public bool StencilEnable { get; set; }
        public StencilOperation StencilFail { get; set; }
        public CompareFunction StencilFunction { get; set; }
        public int StencilMask { get; set; }
        public StencilOperation StencilPass { get; set; }
        public int StencilWriteMask { get; set; }
        public bool TwoSidedStencilMode { get; set; }

		public DepthStencilState ()
		{
            DepthBufferEnable = true;
            DepthBufferWriteEnable = true;
			DepthBufferFunction = CompareFunction.LessEqual;
			StencilEnable = false;
			StencilFunction = CompareFunction.Always;
			StencilPass = StencilOperation.Keep;
			StencilFail = StencilOperation.Keep;
			StencilDepthBufferFail = StencilOperation.Keep;
			TwoSidedStencilMode = false;
			CounterClockwiseStencilFunction = CompareFunction.Always;
			CounterClockwiseStencilFail = StencilOperation.Keep;
			CounterClockwiseStencilPass = StencilOperation.Keep;
			CounterClockwiseStencilDepthBufferFail = StencilOperation.Keep;
			StencilMask = Int32.MaxValue;
			StencilWriteMask = Int32.MaxValue;
			ReferenceStencil = 0;
		}

        private static readonly Utilities.ObjectFactoryWithReset<DepthStencilState> _default;
        private static readonly Utilities.ObjectFactoryWithReset<DepthStencilState> _depthRead;
        private static readonly Utilities.ObjectFactoryWithReset<DepthStencilState> _none;

        public static DepthStencilState Default { get { return _default.Value; } }
        public static DepthStencilState DepthRead { get { return _depthRead.Value; } }
        public static DepthStencilState None { get { return _none.Value; } }
		
		static DepthStencilState ()
		{
			_default = new Utilities.ObjectFactoryWithReset<DepthStencilState>(() => new DepthStencilState
            {
                Name = "DepthStencilState.Default",
				DepthBufferEnable = true,
				DepthBufferWriteEnable = true
			});
			
			_depthRead = new Utilities.ObjectFactoryWithReset<DepthStencilState>(() => new DepthStencilState
            {
                Name = "DepthStencilState.DepthRead",
                DepthBufferEnable = true,
				DepthBufferWriteEnable = false
			});
			
			_none = new Utilities.ObjectFactoryWithReset<DepthStencilState>(() => new DepthStencilState
            {
                Name = "DepthStencilState.None",
                DepthBufferEnable = false,
				DepthBufferWriteEnable = false
			});
		}

        internal void ApplyState(GraphicsDevice device)
        {
            if (!DepthBufferEnable)
            {
                GL.Disable(EnableCap.DepthTest);
                GraphicsExtensions.CheckGLError();
            }
            else
            {
                // enable Depth Buffer
                GL.Enable(EnableCap.DepthTest);
                GraphicsExtensions.CheckGLError();

                DepthFunction func;
                switch (DepthBufferFunction)
                {
                    default:
                    case CompareFunction.Always:
                        func = DepthFunction.Always;
                        break;
                    case CompareFunction.Equal:
                        func = DepthFunction.Equal;
                        break;
                    case CompareFunction.Greater:
                        func = DepthFunction.Greater;
                        break;
                    case CompareFunction.GreaterEqual:
                        func = DepthFunction.Gequal;
                        break;
                    case CompareFunction.Less:
                        func = DepthFunction.Less;
                        break;
                    case CompareFunction.LessEqual:
                        func = DepthFunction.Lequal;
                        break;
                    case CompareFunction.Never:
                        func = DepthFunction.Never;
                        break;
                    case CompareFunction.NotEqual:
                        func = DepthFunction.Notequal;
                        break;
                }

                GL.DepthFunc(func);
                GraphicsExtensions.CheckGLError();
            }

            GL.DepthMask(DepthBufferWriteEnable);
            GraphicsExtensions.CheckGLError();

            if (!StencilEnable)
            {
                GL.Disable(EnableCap.StencilTest);
                GraphicsExtensions.CheckGLError();
            }
            else
            {
                // enable Stencil
                GL.Enable(EnableCap.StencilTest);
                GraphicsExtensions.CheckGLError();

                // set function
                if (this.TwoSidedStencilMode)
                {
                    var cullFaceModeFront = (Version20)CullFaceMode.Front;
                    var cullFaceModeBack = (Version20)CullFaceMode.Back;
                    var stencilFaceFront = StencilFace.Front;
                    var stencilFaceBack = StencilFace.Back;

                    GL.StencilFuncSeparate(cullFaceModeFront, GetStencilFunc(this.StencilFunction), 
                                           this.ReferenceStencil, this.StencilMask);
                    GraphicsExtensions.CheckGLError();
                    GL.StencilFuncSeparate(cullFaceModeBack, GetStencilFunc(this.CounterClockwiseStencilFunction), 
                                           this.ReferenceStencil, this.StencilMask);
                    GraphicsExtensions.CheckGLError();
                    GL.StencilOpSeparate(stencilFaceFront, GetStencilOp(this.StencilFail), 
                                         GetStencilOp(this.StencilDepthBufferFail), 
                                         GetStencilOp(this.StencilPass));
                    GraphicsExtensions.CheckGLError();
                    GL.StencilOpSeparate(stencilFaceBack, GetStencilOp(this.CounterClockwiseStencilFail), 
                                         GetStencilOp(this.CounterClockwiseStencilDepthBufferFail), 
                                         GetStencilOp(this.CounterClockwiseStencilPass));
                    GraphicsExtensions.CheckGLError();
                }
                else
                {
                    GL.StencilFunc(GetStencilFunc(this.StencilFunction), ReferenceStencil, StencilMask);
                    GraphicsExtensions.CheckGLError();
                    
                    GL.StencilOp(GetStencilOp(StencilFail),
                                 GetStencilOp(StencilDepthBufferFail),
                                 GetStencilOp(StencilPass));
                    GraphicsExtensions.CheckGLError();
                }

            }
        }

        private static GLStencilFunction GetStencilFunc(CompareFunction function)
        {
            switch (function)
            {
            case CompareFunction.Always:
                return GLStencilFunction.Always;
            case CompareFunction.Equal:
                return GLStencilFunction.Equal;
            case CompareFunction.Greater:
                return GLStencilFunction.Greater;
            case CompareFunction.GreaterEqual:
                return GLStencilFunction.Gequal;
            case CompareFunction.Less:
                return GLStencilFunction.Less;
            case CompareFunction.LessEqual:
                return GLStencilFunction.Lequal;
            case CompareFunction.Never:
                return GLStencilFunction.Never;
            case CompareFunction.NotEqual:
                return GLStencilFunction.Notequal;
            default:
                return GLStencilFunction.Always;
            }
        }

        private static StencilOp GetStencilOp(StencilOperation operation)
        {
            switch (operation)
            {
                case StencilOperation.Keep:
                    return StencilOp.Keep;
                case StencilOperation.Decrement:
                    return StencilOp.DecrWrap;
                case StencilOperation.DecrementSaturation:
                    return StencilOp.Decr;
                case StencilOperation.IncrementSaturation:
                    return StencilOp.Incr;
                case StencilOperation.Increment:
                    return StencilOp.IncrWrap;
                case StencilOperation.Invert:
                    return StencilOp.Invert;
                case StencilOperation.Replace:
                    return StencilOp.Replace;
                case StencilOperation.Zero:
                    return StencilOp.Zero;
                default:
                    return StencilOp.Keep;
            }
        }
	}
}

