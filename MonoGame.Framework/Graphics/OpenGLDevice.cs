#region DISABLE_FAUXBACKBUFFER Option
// #define DISABLE_FAUXBACKBUFFER
/* If you want to debug GL without the extra FBO in your way, you can use this.
 * -flibit
 */
#endregion

#region Using Statements
using System;
using OpenTK.Graphics.OpenGL;
#endregion

namespace Microsoft.Xna.Framework.Graphics
{
    internal class OpenGLDevice
    {
        #region The OpenGL Device Instance

        public static OpenGLDevice Instance
        {
            get;
            private set;
        }

        #endregion

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
            public OpenGLState<TextureTarget> Target;

            public OpenGLSampler()
            {
                Texture = new OpenGLState<int>(0);
                WrapS = new OpenGLState<TextureAddressMode>(TextureAddressMode.Wrap);
                WrapT = new OpenGLState<TextureAddressMode>(TextureAddressMode.Wrap);
                WrapR = new OpenGLState<TextureAddressMode>(TextureAddressMode.Wrap);
                Filter = new OpenGLState<TextureFilter>(TextureFilter.Linear);
                Target = new OpenGLState<TextureTarget>(TextureTarget.Texture2D);
            }
        }

        #endregion

        #region OpenGL Vertex Attribute State Container Class

        public class OpenGLVertexAttribute
        {
            // Checked in FlushVertexAttributes
            public OpenGLState<bool> Enabled;
            public OpenGLState<int> Divisor;

            // Checked in VertexAttribPointer
            public int CurrentBuffer;
            public int CurrentSize;
            public VertexAttribPointerType CurrentType;
            public bool CurrentNormalized;
            public int CurrentStride;
            public IntPtr CurrentPointer;

            public OpenGLVertexAttribute()
            {
                Enabled = new OpenGLState<bool>(false);
                Divisor = new OpenGLState<int>(0);
                CurrentBuffer = 0;
                CurrentSize = 4;
                CurrentType = VertexAttribPointerType.Float;
                CurrentNormalized = false;
                CurrentStride = 0;
                CurrentPointer = IntPtr.Zero;
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

        #region Vertex Attribute State Variables

        public OpenGLVertexAttribute[] Attributes
        {
            get;
            private set;
        }

        #endregion

        #region Buffer Binding Cache Variables

        private int currentVertexBuffer = 0;
        private int currentIndexBuffer = 0;

        #endregion

        #region Render Target State Variables

        private int currentFramebuffer = 0;
        private int targetFramebuffer = 0;
        private int[] currentAttachments;
        private DrawBuffersEnum[] currentDrawBuffers;
        private int currentRenderbuffer;
        private DepthFormat currentDepthStencilFormat;

        #endregion

        #region Faux-backbuffer Variable

        public FauxBackbuffer Backbuffer
        {
            get;
            private set;
        }

        #endregion

        #region Constructor

        public OpenGLDevice()
        {
            // We should only have one of these!
            if (Instance != null)
            {
                throw new Exception("OpenGLDevice already created!");
            }
            Instance = this;

            // Load OpenGL entry points
            GL.LoadAll();

            // Initialize the faux-backbuffer
            Backbuffer = new FauxBackbuffer(
                GraphicsDeviceManager.DefaultBackBufferWidth,
                GraphicsDeviceManager.DefaultBackBufferHeight,
                DepthFormat.Depth16
            );

            // Initialize sampler state array
            int numSamplers;
            GL.GetInteger(GetPName.MaxTextureUnits, out numSamplers);
            Samplers = new OpenGLSampler[numSamplers];
            for (int i = 0; i < numSamplers; i += 1)
            {
                Samplers[i] = new OpenGLSampler();
            }

            // Initialize vertex attribute state array
            int numAttributes;
            GL.GetInteger(GetPName.MaxVertexAttribs, out numAttributes);
            Attributes = new OpenGLVertexAttribute[numAttributes];
            for (int i = 0; i < numAttributes; i += 1)
            {
                Attributes[i] = new OpenGLVertexAttribute();
            }

            // Initialize render target FBO and state arrays
            int numAttachments;
            GL.GetInteger(GetPName.MaxDrawBuffers, out numAttachments);
            currentAttachments = new int[numAttachments];
            currentDrawBuffers = new DrawBuffersEnum[numAttachments];
            for (int i = 0; i < numAttachments; i += 1)
            {
                currentAttachments[i] = 0;
                currentDrawBuffers[i] = DrawBuffersEnum.None;
            }
            currentRenderbuffer = 0;
            currentDepthStencilFormat = DepthFormat.None;
            targetFramebuffer = GL.GenFramebuffer();

            // Flush GL state to default state
            FlushGLState(true);
        }

        #endregion

        #region Dispose Method

        public void Dispose()
        {
            GL.DeleteFramebuffer(targetFramebuffer);
            targetFramebuffer = 0;
            Backbuffer.Dispose();
            Backbuffer = null;

            Instance = null;
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

        #region Flush State Method

        public void FlushGLState(bool force = false)
        {
            // ALPHA BLENDING STATES

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
                float depthBias = DepthBias.Flush();
                float slopeScaleDepthBias = SlopeScaleDepthBias.Flush();
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

            // END DEPTH STATES

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

            int activeTexture = 0;
            for (int i = 0; i < Samplers.Length; i += 1)
            {
                OpenGLSampler sampler = Samplers[i];
                if (    force ||
                        sampler.Target.NeedsFlush() ||
                        sampler.Texture.NeedsFlush() ||
                        sampler.WrapS.NeedsFlush() ||
                        sampler.WrapT.NeedsFlush() ||
                        sampler.WrapR.NeedsFlush() ||
                        sampler.Filter.NeedsFlush() )
                {
                    // Nothing changed in this sampler, skip it.
                    continue;
                }

                activeTexture = i;
                GL.ActiveTexture(TextureUnit.Texture0 + i);

                TextureTarget target = sampler.Target.GetCurrent();
                bool targetForce = force;
                if (force || sampler.Target.NeedsFlush())
                {
                    force = true; // Reset the ENTIRE state when we change target!
                    GL.BindTexture(sampler.Target.GetCurrent(), 0);
                    target = sampler.Target.Flush();
                }

                if (force || sampler.Texture.NeedsFlush())
                {
                    GL.BindTexture(target, sampler.Texture.Flush());
                }

                if (force || sampler.WrapS.NeedsFlush())
                {
                    GL.TexParameter(
                        target,
                        TextureParameterName.TextureWrapS,
                        (int) GetWrap(sampler.WrapS.Flush())
                    );
                }

                if (force || sampler.WrapT.NeedsFlush())
                {
                    GL.TexParameter(
                        target,
                        TextureParameterName.TextureWrapT,
                        (int) GetWrap(sampler.WrapT.Flush())
                    );
                }

                if (force || sampler.WrapR.NeedsFlush())
                {
                    GL.TexParameter(
                        target,
                        TextureParameterName.TextureWrapR,
                        (int) GetWrap(sampler.WrapR.Flush())
                    );
                }

                if (force || sampler.Filter.NeedsFlush())
                {
                    TextureFilter filter = sampler.Filter.Flush();
                    TextureMagFilter magFilter;
                    TextureMinFilter minmipFilter;
                    float anistropicFilter = 1.0f;
                    if (filter == TextureFilter.Point)
                    {
                        magFilter = TextureMagFilter.Nearest;
                        minmipFilter = TextureMinFilter.NearestMipmapNearest;
                    }
                    else if (filter == TextureFilter.Linear)
                    {
                        magFilter = TextureMagFilter.Linear;
                        minmipFilter = TextureMinFilter.LinearMipmapLinear;
                    }
                    else if (filter == TextureFilter.Anisotropic)
                    {
                        anistropicFilter = 4.0f;
                        magFilter = TextureMagFilter.Linear;
                        minmipFilter = TextureMinFilter.LinearMipmapLinear;
                    }
                    else if (filter == TextureFilter.LinearMipPoint)
                    {
                        magFilter = TextureMagFilter.Linear;
                        minmipFilter = TextureMinFilter.LinearMipmapNearest;
                    }
                    else if (filter == TextureFilter.MinPointMagLinearMipPoint)
                    {
                        magFilter = TextureMagFilter.Linear;
                        minmipFilter = TextureMinFilter.NearestMipmapNearest;
                    }
                    else if (filter == TextureFilter.MinPointMagLinearMipLinear)
                    {
                        magFilter = TextureMagFilter.Linear;
                        minmipFilter = TextureMinFilter.NearestMipmapLinear;
                    }
                    else if (filter == TextureFilter.MinLinearMagPointMipPoint)
                    {
                        magFilter = TextureMagFilter.Nearest;
                        minmipFilter = TextureMinFilter.LinearMipmapNearest;
                    }
                    else if (filter == TextureFilter.MinLinearMagPointMipLinear)
                    {
                        magFilter = TextureMagFilter.Nearest;
                        minmipFilter = TextureMinFilter.LinearMipmapLinear;
                    }
                    else
                    {
                        throw new Exception("Unhandled TextureFilter!");
                    }
                    GL.TexParameter(
                        target,
                        TextureParameterName.TextureMagFilter,
                        (int) magFilter
                    );
                    GL.TexParameter(
                        target,
                        TextureParameterName.TextureMinFilter,
                        (int) minmipFilter
                    );
                    GL.TexParameter(
                        target,
                        (TextureParameterName) ExtTextureFilterAnisotropic.TextureMaxAnisotropyExt,
                        anistropicFilter
                    );
                }

                // You didn't see nothin'.
                force = targetForce;
            }
            if (activeTexture != 0)
            {
                // Keep this state sane. -flibit
                GL.ActiveTexture(TextureUnit.Texture0);
            }

            // END SAMPLER STATES

            // Check for errors.
            GraphicsExtensions.CheckGLError();
        }

        #endregion

        #region Flush Vertex Attribute Method

        public void FlushGLVertexAttributes(bool force)
        {
            for (int i = 0; i < Attributes.Length; i += 1)
            {
                OpenGLVertexAttribute attrib = Attributes[i];

                if (force || attrib.Enabled.NeedsFlush())
                {
                    if (attrib.Enabled.Flush())
                    {
                        GL.EnableVertexAttribArray(i);
                    }
                    else
                    {
                        GL.DisableVertexAttribArray(i);
                    }
                }

                if (force || attrib.Divisor.NeedsFlush())
                {
                    GL.VertexAttribDivisor(i, attrib.Divisor.Flush());
                }
            }
        }

        #endregion

        #region glVertexAttribPointer Method

        public void VertexAttribPointer(
            int location,
            int size,
            VertexAttribPointerType type,
            bool normalized,
            int stride,
            IntPtr pointer
        ) {
            if (    Attributes[location].CurrentBuffer != currentVertexBuffer ||
                    Attributes[location].CurrentPointer != pointer ||
                    Attributes[location].CurrentSize != size ||
                    Attributes[location].CurrentType != type ||
                    Attributes[location].CurrentNormalized != normalized ||
                    Attributes[location].CurrentStride != stride    )
            {
                GL.VertexAttribPointer(
                    location,
                    size,
                    type,
                    normalized,
                    stride,
                    pointer
                );
                Attributes[location].CurrentBuffer = currentVertexBuffer;
                Attributes[location].CurrentPointer = pointer;
                Attributes[location].CurrentSize = size;
                Attributes[location].CurrentType = type;
                Attributes[location].CurrentNormalized = normalized;
                Attributes[location].CurrentStride = stride;
            }
        }

        #endregion

        #region glBindBuffer Methods

        public void BindVertexBuffer(int buffer)
        {
            if (buffer != currentVertexBuffer)
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, buffer);
                currentVertexBuffer = buffer;
            }
        }

        public void BindIndexBuffer(int buffer)
        {
            if (buffer != currentIndexBuffer)
            {
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, buffer);
                currentIndexBuffer = buffer;
            }
        }

        #endregion

        #region SetRenderTargets Method

        public void SetRenderTargets(int[] attachments, int renderbuffer, DepthFormat depthFormat)
        {
            // Bind the right framebuffer, if needed
            if (attachments == null)
            {
                if (Backbuffer.Handle != currentFramebuffer)
                {
                    GL.BindFramebuffer(
                        FramebufferTarget.Framebuffer,
                        Backbuffer.Handle
                    );
                    currentFramebuffer = Backbuffer.Handle;
                }
                return;
            }
            else if (targetFramebuffer != currentFramebuffer)
            {
                GL.BindFramebuffer(
                    FramebufferTarget.Framebuffer,
                    targetFramebuffer
                );
                currentFramebuffer = targetFramebuffer;
            }

            // Update the color attachments, DrawBuffers state
            bool drawBuffersChanged = false;
            int i = 0;
            for (i = 0; i < attachments.Length; i += 1)
            {
                if (attachments[i] != currentAttachments[i])
                {
                    GL.FramebufferTexture2D(
                        FramebufferTarget.Framebuffer,
                        FramebufferAttachment.ColorAttachment0 + i,
                        TextureTarget.Texture2D,
                        attachments[i],
                        0
                    );
                    drawBuffersChanged = currentAttachments[i] == 0;
                    currentAttachments[i] = attachments[i];
                }
            }
            while (i < currentDrawBuffers.Length)
            {
                drawBuffersChanged = currentDrawBuffers[i] != DrawBuffersEnum.None;
                currentDrawBuffers[i] = DrawBuffersEnum.None;
                i += 1;
            }
            if (drawBuffersChanged)
            {
                GL.DrawBuffers(currentDrawBuffers.Length, currentDrawBuffers);
            }

            // Update the depth/stencil attachment
            if (renderbuffer != currentRenderbuffer)
            {
                if (depthFormat != currentDepthStencilFormat)
                {
                    // Changing formats, unbind the current renderbuffer first.
                    GL.FramebufferRenderbuffer(
                        FramebufferTarget.Framebuffer,
                        GetDepthStencilAttachment(currentDepthStencilFormat),
                        RenderbufferTarget.Renderbuffer,
                        0
                    );
                    currentDepthStencilFormat = depthFormat;
                }
                GL.FramebufferRenderbuffer(
                    FramebufferTarget.Framebuffer,
                    GetDepthStencilAttachment(currentDepthStencilFormat),
                    RenderbufferTarget.Renderbuffer,
                    renderbuffer
                );
                currentRenderbuffer = renderbuffer;
            }
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

        private TextureWrapMode GetWrap(TextureAddressMode mode)
        {
            // TODO
            return TextureWrapMode.Repeat;
        }

        private FramebufferAttachment GetDepthStencilAttachment(DepthFormat format)
        {
            // TODO
            return FramebufferAttachment.DepthStencilAttachment;
        }

        #endregion

        #region The Faux-Backbuffer

        public class FauxBackbuffer
        {
            public int Handle
            {
                get;
                private set;
            }

            public int Width
            {
                get;
                private set;
            }

            public int Height
            {
                get;
                private set;
            }

            private int colorAttachment;
            private int depthStencilAttachment;
            private DepthFormat depthStencilFormat;

            public FauxBackbuffer(int width, int height, DepthFormat depthFormat)
            {
#if DISABLE_FAUXBACKBUFFER
                Handle = 0;
#else
                Handle = GL.GenFramebuffer();
                colorAttachment = GL.GenTexture();
                depthStencilAttachment = GL.GenTexture();

                GL.BindFramebuffer(FramebufferTarget.Framebuffer, Handle);
                GL.BindTexture(TextureTarget.Texture2D, colorAttachment);
                GL.TexImage2D(
                    TextureTarget.Texture2D,
                    0,
                    PixelInternalFormat.Rgba,
                    width,
                    height,
                    0,
                    PixelFormat.Rgba,
                    PixelType.UnsignedByte,
                    IntPtr.Zero
                );
                GL.BindTexture(TextureTarget.Texture2D, depthStencilAttachment);
                GL.TexImage2D(
                    TextureTarget.Texture2D,
                    0,
                    PixelInternalFormat.DepthComponent16,
                    width,
                    height,
                    0,
                    PixelFormat.DepthComponent,
                    PixelType.UnsignedByte,
                    IntPtr.Zero
                );
                GL.FramebufferTexture2D(
                    FramebufferTarget.Framebuffer,
                    FramebufferAttachment.ColorAttachment0,
                    TextureTarget.Texture2D,
                    colorAttachment,
                    0
                );
                GL.FramebufferTexture2D(
                    FramebufferTarget.Framebuffer,
                    FramebufferAttachment.DepthAttachment,
                    TextureTarget.Texture2D,
                    depthStencilAttachment,
                    0
                );
                GL.BindTexture(TextureTarget.Texture2D, 0);

                Width = width;
                Height = height;
#endif
            }

            public void Dispose()
            {
#if !DISABLE_FAUXBACKBUFFER
                GL.DeleteFramebuffer(Handle);
                GL.DeleteTexture(colorAttachment);
                GL.DeleteTexture(depthStencilAttachment);
                Handle = 0;
#endif
            }

            public void ResetFramebuffer(int width, int height, DepthFormat depthFormat)
            {
#if !DISABLE_FAUXBACKBUFFER
                // Update our color attachment to the new resolution
                GL.BindTexture(TextureTarget.Texture2D, colorAttachment);
                GL.TexImage2D(
                    TextureTarget.Texture2D,
                    0,
                    PixelInternalFormat.Rgba,
                    width,
                    height,
                    0,
                    PixelFormat.Rgba,
                    PixelType.UnsignedByte,
                    IntPtr.Zero
                );

                // Update the depth attachment based on the desired DepthFormat.
                PixelFormat depthPixelFormat;
                PixelInternalFormat depthPixelInternalFormat;
                PixelType depthPixelType;
                FramebufferAttachment depthAttachmentType;
                if (depthFormat == DepthFormat.Depth16)
                {
                    depthPixelFormat = PixelFormat.DepthComponent;
                    depthPixelInternalFormat = PixelInternalFormat.DepthComponent16;
                    depthPixelType = PixelType.UnsignedByte;
                    depthAttachmentType = FramebufferAttachment.DepthAttachment;
                }
                else if (depthFormat == DepthFormat.Depth24)
                {
                    depthPixelFormat = PixelFormat.DepthComponent;
                    depthPixelInternalFormat = PixelInternalFormat.DepthComponent24;
                    depthPixelType = PixelType.UnsignedByte;
                    depthAttachmentType = FramebufferAttachment.DepthAttachment;
                }
                else
                {
                    depthPixelFormat = PixelFormat.DepthStencil;
                    depthPixelInternalFormat = PixelInternalFormat.Depth24Stencil8;
                    depthPixelType = PixelType.UnsignedInt248;
                    depthAttachmentType = FramebufferAttachment.DepthStencilAttachment;
                }

                GL.BindTexture(TextureTarget.Texture2D, depthStencilAttachment);
                GL.TexImage2D(
                    TextureTarget.Texture2D,
                    0,
                    depthPixelInternalFormat,
                    width,
                    height,
                    0,
                    depthPixelFormat,
                    depthPixelType,
                    IntPtr.Zero
                );

                // If the depth format changes, detach before reattaching!
                if (depthFormat != depthStencilFormat)
                {
                    FramebufferAttachment attach;
                    if (depthStencilFormat == DepthFormat.Depth24Stencil8)
                    {
                        attach = FramebufferAttachment.DepthStencilAttachment;
                    }
                    else
                    {
                        attach = FramebufferAttachment.DepthAttachment;
                    }

                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, Handle);

                    GL.FramebufferTexture2D(
                        FramebufferTarget.Framebuffer,
                        attach,
                        TextureTarget.Texture2D,
                        0,
                        0
                    );
                    GL.FramebufferTexture2D(
                        FramebufferTarget.Framebuffer,
                        depthAttachmentType,
                        TextureTarget.Texture2D,
                        depthStencilAttachment,
                        0
                    );

                    if (Game.Instance.GraphicsDevice.IsRenderTargetBound)
                    {
                        GL.BindFramebuffer(
                            FramebufferTarget.Framebuffer,
                            OpenGLDevice.Instance.targetFramebuffer
                        );
                    }

                    depthStencilFormat = depthFormat;
                }

                GL.BindTexture(
                    TextureTarget.Texture2D,
                    OpenGLDevice.Instance.Samplers[0].Texture.Get()
                );

                Width = width;
                Height = height;
#endif
            }
        }

        #endregion
    }
}