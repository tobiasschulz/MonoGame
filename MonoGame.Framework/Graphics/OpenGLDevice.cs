using System;
using OpenTK.Graphics.OpenGL;

namespace Microsoft.Xna.Framework.Graphics
{
    internal class OpenGLDevice
    {
        #region OpenGL State Container Class

        public class OpenGLState<T>
        {
            private T current;
            private T latch;

            public OpenGLState(T defaultValue)
            {
                current = defaultValue;
                latch = defaultValue;
            }

            public T Get()
            {
                return latch;
            }

            public T GetCurrent()
            {
                return current;
            }

            public void Set(T newValue)
            {
                latch = newValue;
            }

            public bool NeedsFlush()
            {
                return !current.Equals(latch);
            }

            public T Flush()
            {
                current = latch;
                return current;
            }
        }

        #endregion

        #region OpenGL Sampler State Container Class

        public class OpenGLSampler
        {
            public OpenGLState<int> Texture;
            public OpenGLState<TextureAddressMode> WrapS;
            public OpenGLState<TextureAddressMode> WrapT;
            public OpenGLState<TextureAddressMode> WrapR;
            public OpenGLState<TextureFilter> Filter;

            public OpenGLSampler()
            {
                Texture = new OpenGLState<int>(0);
                WrapS = new OpenGLState<TextureAddressMode>(TextureAddressMode.Wrap);
                WrapT = new OpenGLState<TextureAddressMode>(TextureAddressMode.Wrap);
                WrapR = new OpenGLState<TextureAddressMode>(TextureAddressMode.Wrap);
                Filter = new OpenGLState<TextureFilter>(TextureFilter.Linear);
            }
        }

        #endregion

        #region Alpha Blending State Variables

        public OpenGLState<bool> AlphaBlendEnable = new OpenGLState<bool>(false);

        public OpenGLState<bool> AlphaTestEnable = new OpenGLState<bool>(false);

        public OpenGLState<bool> SeparateAlphaBlendEnable = new OpenGLState<bool>(false);

        public OpenGLState<Blend> SrcBlend = new OpenGLState<Blend>(Blend.One);

        public OpenGLState<Blend> DstBlend = new OpenGLState<Blend>(Blend.Zero);

        public OpenGLState<Blend> SrcBlendAlpha = new OpenGLState<Blend>(Blend.One);

        public OpenGLState<Blend> DstBlendAlpha = new OpenGLState<Blend>(Blend.Zero);

        public OpenGLState<BlendFunction> BlendOp = new OpenGLState<BlendFunction>(BlendFunction.Add);

        public OpenGLState<BlendFunction> BlendOpAlpha = new OpenGLState<BlendFunction>(BlendFunction.Add);

        // TODO: glAlphaFunc? -flibit

        #endregion

        #region Depth State Variables

        public OpenGLState<bool> ZEnable = new OpenGLState<bool>(false);

        public OpenGLState<bool> ZWriteEnable = new OpenGLState<bool>(false);

        public OpenGLState<CompareFunction> DepthFunc = new OpenGLState<CompareFunction>(CompareFunction.Less);

        public OpenGLState<float> DepthBias = new OpenGLState<float>(0.0f);

        public OpenGLState<float> SlopeScaleDepthBias = new OpenGLState<float>(0.0f);

        #endregion

        #region Stencil State Variables

        public OpenGLState<bool> StencilEnable = new OpenGLState<bool>(false);

        // TODO: glStencilMask(StencilWriteMask)? -flibit

        public OpenGLState<bool> SeparateStencilEnable = new OpenGLState<bool>(false);

        public OpenGLState<uint> StencilRef = new OpenGLState<uint>(0);

        public OpenGLState<uint> StencilMask = new OpenGLState<uint>(0xFFFFFFFF);

        public OpenGLState<CompareFunction> StencilFunc = new OpenGLState<CompareFunction>(CompareFunction.Always);

        public OpenGLState<StencilOperation> StencilFail = new OpenGLState<StencilOperation>(StencilOperation.Keep);

        public OpenGLState<StencilOperation> StencilZFail = new OpenGLState<StencilOperation>(StencilOperation.Keep);

        public OpenGLState<StencilOperation> StencilPass = new OpenGLState<StencilOperation>(StencilOperation.Keep);

        public OpenGLState<CompareFunction> CCWStencilFunc = new OpenGLState<CompareFunction>(CompareFunction.Always);

        public OpenGLState<StencilOperation> CCWStencilFail = new OpenGLState<StencilOperation>(StencilOperation.Keep);

        public OpenGLState<StencilOperation> CCWStencilZFail = new OpenGLState<StencilOperation>(StencilOperation.Keep);

        public OpenGLState<StencilOperation> CCWStencilPass = new OpenGLState<StencilOperation>(StencilOperation.Keep);

        #endregion

        #region Miscellaneous Write State Variables

        public OpenGLState<bool> ScissorTestEnable = new OpenGLState<bool>(false);

        public OpenGLState<CullMode> CullFrontFace = new OpenGLState<CullMode>(CullMode.None);

        public OpenGLState<FillMode> GLFillMode = new OpenGLState<FillMode>(FillMode.Solid);

        public OpenGLState<ColorWriteChannels> ColorWriteEnable = new OpenGLState<ColorWriteChannels>(ColorWriteChannels.All);

        public OpenGLState<Rectangle> GLViewport = new OpenGLState<Rectangle>(
            new Rectangle(
                0,
                0,
                GraphicsDeviceManager.DefaultBackBufferWidth,
                GraphicsDeviceManager.DefaultBackBufferHeight
            )
        );

        #endregion

        #region Sampler State Variables

        public OpenGLSampler[] Samplers
        {
            get;
            private set;
        }

        #endregion

        #region Constructor

        public OpenGLDevice()
        {
            // Load OpenGL entry points
            GL.LoadAll();

            // Initialize sampler state array
            int numSamplers;
            GL.GetInteger(GetPName.MaxTextureUnits, out numSamplers);
            Samplers = new OpenGLSampler[numSamplers];
            for (int i = 0; i < numSamplers; i += 1)
            {
                Samplers[i] = new OpenGLSampler();
            }

            // Flush GL state to default state
            FlushGLState(true);
        }

        #endregion

        #region glEnable/glDisable Method

        private void ToggleGLState(EnableCap feature, bool enable)
        {
            if (enable)
            {
                GL.Enable(feature);
            }
            else
            {
                GL.Disable(feature);
            }
        }

        #endregion

        #region Flush Method

        public void FlushGLState(bool force = false)
        {
            if (force || AlphaBlendEnable.NeedsFlush())
            {
                ToggleGLState(EnableCap.Blend, AlphaBlendEnable.Flush());
            }

            if (force || AlphaTestEnable.NeedsFlush())
            {
                ToggleGLState(EnableCap.AlphaTest, AlphaTestEnable.Flush());
            }

            if (    force ||
                    SeparateAlphaBlendEnable.NeedsFlush() ||
                    SrcBlend.NeedsFlush() ||
                    DstBlend.NeedsFlush() ||
                    SrcBlendAlpha.NeedsFlush() ||
                    DstBlendAlpha.NeedsFlush()  )
            {
                if (SeparateAlphaBlendEnable.Flush())
                {
                    GL.BlendFuncSeparate(
                        GetBlendModeSrc(SrcBlend.Flush()),
                        GetBlendModeDst(DstBlend.Flush()),
                        GetBlendModeSrc(SrcBlendAlpha.Flush()),
                        GetBlendModeDst(DstBlendAlpha.Flush())
                    );
                }
                else
                {
                    GL.BlendFunc(
                        GetBlendModeSrc(SrcBlend.Flush()),
                        GetBlendModeDst(DstBlend.Flush())
                    );
                    SrcBlendAlpha.Flush();
                    DstBlendAlpha.Flush();
                }
            }

            if (force || BlendOp.NeedsFlush() || BlendOpAlpha.NeedsFlush())
            {
                GL.BlendEquationSeparate(
                    GetBlendEquation(BlendOp.Flush()),
                    GetBlendEquation(BlendOpAlpha.Flush())
                );
            }

            // TODO: glAlphaFunc? -flibit

            // END ALPHA BLENDING STATES

            // DEPTH STATES

            if (force || ZEnable.NeedsFlush())
            {
                ToggleGLState(EnableCap.DepthTest, ZEnable.Flush());
            }

            if (force || ZWriteEnable.NeedsFlush())
            {
                GL.DepthMask(ZWriteEnable.Flush());
            }

            if (force || DepthFunc.NeedsFlush())
            {
                GL.DepthFunc(GetDepthFunc(DepthFunc.Flush()));
            }

            if (    force ||
                    DepthBias.NeedsFlush() ||
                    SlopeScaleDepthBias.NeedsFlush()    )
            {
                float depthBias = DepthBias.NeedsFlush();
                float slopeScaleDepthBias = SlopeScaleDepthBias.NeedsFlush();
                if (depthBias == 0.0f && slopeScaleDepthBias == 0.0f)
                {
                    ToggleGLState(EnableCap.PolygonOffsetFill, false);
                }
                else
                {
                    ToggleGLState(EnableCap.PolygonOffsetFill, true);
                    GL.PolygonOffset(slopeScaleDepthBias, depthBias);
                }
            }

            // END DEPTHS STATES

            // STENCIL STATES

            if (force || StencilEnable.NeedsFlush())
            {
                ToggleGLState(EnableCap.StencilTest, StencilEnable.Flush());
            }

            // TODO: glStencilMask? -flibit

            if (    force ||
                    SeparateStencilEnable.NeedsFlush() ||
                    StencilRef.NeedsFlush() ||
                    StencilMask.NeedsFlush() ||
                    StencilFunc.NeedsFlush() ||
                    CCWStencilFunc.NeedsFlush() )
            {
                if (SeparateStencilEnable.Flush())
                {
                    GL.StencilFuncSeparate(
                        (Version20) CullFaceMode.Front,
                        GetStencilFunc(StencilFunc.Flush()),
                        (int) StencilRef.Flush(),
                        StencilMask.Flush()
                    );
                    GL.StencilFuncSeparate(
                        (Version20) CullFaceMode.Back,
                        GetStencilFunc(CCWStencilFunc.Flush()),
                        (int) StencilRef.Flush(),
                        StencilMask.Flush()
                    );
                }
                else
                {
                    GL.StencilFunc(
                        GetStencilFunc(StencilFunc.Flush()),
                        (int) StencilRef.Flush(),
                        StencilMask.Flush()
                    );
                }
            }

            if (    force ||
                    SeparateStencilEnable.NeedsFlush() ||
                    StencilFail.NeedsFlush() ||
                    StencilZFail.NeedsFlush() ||
                    StencilPass.NeedsFlush() ||
                    CCWStencilFail.NeedsFlush() ||
                    CCWStencilZFail.NeedsFlush() ||
                    CCWStencilPass.NeedsFlush() )
            {
                if (SeparateStencilEnable.Flush())
                {
                    GL.StencilOpSeparate(
                        StencilFace.Front,
                        GetStencilOp(StencilFail.Flush()),
                        GetStencilOp(StencilZFail.Flush()),
                        GetStencilOp(StencilPass.Flush())
                    );
                    GL.StencilOpSeparate(
                        StencilFace.Back,
                        GetStencilOp(CCWStencilFail.Flush()),
                        GetStencilOp(CCWStencilZFail.Flush()),
                        GetStencilOp(CCWStencilPass.Flush())
                    );
                }
                else
                {
                    GL.StencilOp(
                        GetStencilOp(StencilFail.Flush()),
                        GetStencilOp(StencilZFail.Flush()),
                        GetStencilOp(StencilPass.Flush())
                    );
                }
            }

            // END STENCIL STATES

            // MISCELLANEOUS WRITE STATES

            if (force || ScissorTestEnable.NeedsFlush())
            {
                ToggleGLState(EnableCap.ScissorTest, ScissorTestEnable.Flush());
            }

            if (force || CullFrontFace.NeedsFlush())
            {
                CullMode current = CullFrontFace.GetCurrent();
                CullMode latched = CullFrontFace.Flush();
                if (force || (latched == CullMode.None) != (current == CullMode.None))
                {
                    ToggleGLState(EnableCap.CullFace, latched != CullMode.None);
                }
                if (latched != CullMode.None)
                {
                    GL.FrontFace(GetFrontFace(latched));
                }
            }

            if (force || GLFillMode.NeedsFlush())
            {
                GL.PolygonMode(
                    MaterialFace.FrontAndBack,
                    GetFillMode(GLFillMode.Flush())
                );
            }

            if (force || ColorWriteEnable.NeedsFlush())
            {
                ColorWriteChannels write = ColorWriteEnable.Flush();
                GL.ColorMask(
                    (write & ColorWriteChannels.Red) != 0,
                    (write & ColorWriteChannels.Green) != 0,
                    (write & ColorWriteChannels.Blue) != 0,
                    (write & ColorWriteChannels.Alpha) != 0
                );
            }

            if (force || GLViewport.NeedsFlush())
            {
                Rectangle viewport = GLViewport.Flush();
                GL.Viewport(
                    viewport.X,
                    viewport.Y,
                    viewport.Width,
                    viewport.Height
                );
            }

            // END MISCELLANEOUS WRITE STATES

            // SAMPLER STATES

            for (int i = 0; i < Samplers.Length; i += 1)
            {
                OpenGLSampler sampler = Samplers[i];
                // TODO
            }
            GL.ActiveTexture(TextureUnit.Texture0); // Keep this state sane. -flibit

            // END SAMPLER STATES

            // Check for errors.
            GraphicsExtensions.CheckGLError();
        }

        #endregion

        #region Private XNA->GL Enum Conversion Methods

        private BlendingFactorSrc GetBlendModeSrc(Blend mode)
        {
            // TODO
            return BlendingFactorSrc.One;
        }

        private BlendingFactorDest GetBlendModeDst(Blend mode)
        {
            // TODO
            return BlendingFactorDest.Zero;
        }

        private BlendEquationMode GetBlendEquation(BlendFunction func)
        {
            // TODO
            return BlendEquationMode.FuncAdd;
        }

        private DepthFunction GetDepthFunc(CompareFunction func)
        {
            // TODO
            return DepthFunction.Always;
        }

        private StencilFunction GetStencilFunc(CompareFunction func)
        {
            // TODO
            return StencilFunction.Always;
        }

        private StencilOp GetStencilOp(StencilOperation op)
        {
            // TODO
            return StencilOp.Keep;
        }

        private FrontFaceDirection GetFrontFace(CullMode mode)
        {
            // TODO
            return FrontFaceDirection.Cw;
        }

        private PolygonMode GetFillMode(FillMode mode)
        {
            // TODO
            return PolygonMode.Fill;
        }

        #endregion
    }
}