#region DISABLE_FAUXBACKBUFFER Option
// #define DISABLE_FAUXBACKBUFFER
/* If you want to debug GL without the extra FBO in your way, you can use this.
 * Note that this also affects SDL2/SDL2_GameWindow.cs!
 * -flibit
 */
#endregion

#region Using Statements
using System;
using System.Collections.Generic;
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

        public OpenGLState<Rectangle> ScissorRectangle = new OpenGLState<Rectangle>(
            new Rectangle(
                0,
                0,
                GraphicsDeviceManager.DefaultBackBufferWidth,
                GraphicsDeviceManager.DefaultBackBufferHeight
            )
        );

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

        #region Render Target Cache Variables

        private int currentFramebuffer = 0;
        private int targetFramebuffer = 0;
        private int[] currentAttachments;
        private DrawBuffersEnum[] currentDrawBuffers;
        private int currentRenderbuffer;
        private DepthFormat currentDepthStencilFormat;

        #endregion

        #region Faux-Backbuffer Variable

        public FauxBackbuffer Backbuffer
        {
            get;
            private set;
        }

        #endregion

        #region OpenGL Extensions List

        public string Extensions
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

            // Initialize XNA->GL conversion Dictionaries
            XNAToGL.Initialize();

            // Load the extension list, initialize extension-dependent components
            Extensions = GL.GetString(StringName.Extensions);
            Framebuffer.Initialize();

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
            targetFramebuffer = Framebuffer.GenFramebuffer();

            // Flush GL state to default state
            FlushGLState(true);
        }

        #endregion

        #region Dispose Method

        public void Dispose()
        {
            Framebuffer.DeleteFramebuffer(targetFramebuffer);
            targetFramebuffer = 0;
            Backbuffer.Dispose();
            Backbuffer = null;

            XNAToGL.Clear();

            Instance = null;
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
                        XNAToGL.BlendModeSrc[SrcBlend.Flush()],
                        XNAToGL.BlendModeDst[DstBlend.Flush()],
                        XNAToGL.BlendModeSrc[SrcBlendAlpha.Flush()],
                        XNAToGL.BlendModeDst[DstBlendAlpha.Flush()]
                    );
                }
                else
                {
                    GL.BlendFunc(
                        XNAToGL.BlendModeSrc[SrcBlend.Flush()],
                        XNAToGL.BlendModeDst[DstBlend.Flush()]
                    );
                    SrcBlendAlpha.Flush();
                    DstBlendAlpha.Flush();
                }
            }

            if (force || BlendOp.NeedsFlush() || BlendOpAlpha.NeedsFlush())
            {
                GL.BlendEquationSeparate(
                    XNAToGL.BlendEquation[BlendOp.Flush()],
                    XNAToGL.BlendEquation[BlendOpAlpha.Flush()]
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
                GL.DepthFunc(XNAToGL.DepthFunc[DepthFunc.Flush()]);
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
                        XNAToGL.StencilFunc[StencilFunc.Flush()],
                        (int) StencilRef.Flush(),
                        StencilMask.Flush()
                    );
                    GL.StencilFuncSeparate(
                        (Version20) CullFaceMode.Back,
                        XNAToGL.StencilFunc[CCWStencilFunc.Flush()],
                        (int) StencilRef.Flush(),
                        StencilMask.Flush()
                    );
                }
                else
                {
                    GL.StencilFunc(
                        XNAToGL.StencilFunc[StencilFunc.Flush()],
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
                        XNAToGL.GLStencilOp[StencilFail.Flush()],
                        XNAToGL.GLStencilOp[StencilZFail.Flush()],
                        XNAToGL.GLStencilOp[StencilPass.Flush()]
                    );
                    GL.StencilOpSeparate(
                        StencilFace.Back,
                        XNAToGL.GLStencilOp[CCWStencilFail.Flush()],
                        XNAToGL.GLStencilOp[CCWStencilZFail.Flush()],
                        XNAToGL.GLStencilOp[CCWStencilPass.Flush()]
                    );
                }
                else
                {
                    GL.StencilOp(
                        XNAToGL.GLStencilOp[StencilFail.Flush()],
                        XNAToGL.GLStencilOp[StencilZFail.Flush()],
                        XNAToGL.GLStencilOp[StencilPass.Flush()]
                    );
                }
            }

            // END STENCIL STATES

            // MISCELLANEOUS WRITE STATES

            if (force || ScissorTestEnable.NeedsFlush())
            {
                ToggleGLState(EnableCap.ScissorTest, ScissorTestEnable.Flush());
            }

            if (force || ScissorRectangle.NeedsFlush())
            {
                Rectangle scissorRect = ScissorRectangle.Flush();
                GL.Scissor(
                    scissorRect.X,
                    scissorRect.Y,
                    scissorRect.Width,
                    scissorRect.Height
                );
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
                    GL.FrontFace(XNAToGL.FrontFace[latched]);
                }
            }

            if (force || GLFillMode.NeedsFlush())
            {
                GL.PolygonMode(
                    MaterialFace.FrontAndBack,
                    XNAToGL.GLFillMode[GLFillMode.Flush()]
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
                        (int) XNAToGL.Wrap[sampler.WrapS.Flush()]
                    );
                }

                if (force || sampler.WrapT.NeedsFlush())
                {
                    GL.TexParameter(
                        target,
                        TextureParameterName.TextureWrapT,
                        (int) XNAToGL.Wrap[sampler.WrapT.Flush()]
                    );
                }

                if (force || sampler.WrapR.NeedsFlush())
                {
                    GL.TexParameter(
                        target,
                        TextureParameterName.TextureWrapR,
                        (int) XNAToGL.Wrap[sampler.WrapR.Flush()]
                    );
                }

                if (force || sampler.Filter.NeedsFlush())
                {
                    TextureFilter filter = sampler.Filter.Flush();
                    GL.TexParameter(
                        target,
                        TextureParameterName.TextureMagFilter,
                        (int) XNAToGL.MagFilter[filter]
                    );
                    GL.TexParameter(
                        target,
                        TextureParameterName.TextureMinFilter,
                        (int) XNAToGL.MinMipFilter[filter]
                    );
                    GL.TexParameter(
                        target,
                        (TextureParameterName) ExtTextureFilterAnisotropic.TextureMaxAnisotropyExt,
                        (filter == TextureFilter.Anisotropic) ? 4.0f : 1.0f
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

        #region Flush Vertex Attributes Method

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

        #region SetRenderTargets Method

        public void SetRenderTargets(int[] attachments, int renderbuffer, DepthFormat depthFormat)
        {
            // Bind the right framebuffer, if needed
            if (attachments == null)
            {
                if (Backbuffer.Handle != currentFramebuffer)
                {
                    Framebuffer.BindFramebuffer(Backbuffer.Handle);
                    currentFramebuffer = Backbuffer.Handle;
                }
                return;
            }
            else if (targetFramebuffer != currentFramebuffer)
            {
                Framebuffer.BindFramebuffer(targetFramebuffer);
                currentFramebuffer = targetFramebuffer;
            }

            // Update the color attachments, DrawBuffers state
            bool drawBuffersChanged = false;
            int i = 0;
            for (i = 0; i < attachments.Length; i += 1)
            {
                if (attachments[i] != currentAttachments[i])
                {
                    Framebuffer.AttachColor(attachments[i], i);
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
                if (    depthFormat != currentDepthStencilFormat &&
                        currentDepthStencilFormat != DepthFormat.None   )
                {
                    // Changing formats, unbind the current renderbuffer first.
                    Framebuffer.AttachDepthRenderbuffer(
                        0,
                        XNAToGL.DepthStencilAttachment[currentDepthStencilFormat]
                    );
                    currentDepthStencilFormat = depthFormat;
                }
                if (currentDepthStencilFormat != DepthFormat.None)
                {
                    Framebuffer.AttachDepthRenderbuffer(
                        renderbuffer,
                        XNAToGL.DepthStencilAttachment[currentDepthStencilFormat]
                    );
                }
                currentRenderbuffer = renderbuffer;
            }
        }

        #endregion

        #region XNA->GL Enum Conversion Class

        private static class XNAToGL
        {
            public static Dictionary<Blend, BlendingFactorSrc> BlendModeSrc
            {
                get;
                private set;
            }

            public static Dictionary<Blend, BlendingFactorDest> BlendModeDst
            {
                get;
                private set;
            }

            public static Dictionary<BlendFunction, BlendEquationMode> BlendEquation
            {
                get;
                private set;
            }

            public static Dictionary<CompareFunction, DepthFunction> DepthFunc
            {
                get;
                private set;
            }

            public static Dictionary<CompareFunction, StencilFunction> StencilFunc
            {
                get;
                private set;
            }

            public static Dictionary<StencilOperation, StencilOp> GLStencilOp
            {
                get;
                private set;
            }

            public static Dictionary<CullMode, FrontFaceDirection> FrontFace
            {
                get;
                private set;
            }

            public static Dictionary<FillMode, PolygonMode> GLFillMode
            {
                get;
                private set;
            }

            public static Dictionary<TextureAddressMode, TextureWrapMode> Wrap
            {
                get;
                private set;
            }

            public static Dictionary<TextureFilter, TextureMagFilter> MagFilter
            {
                get;
                private set;
            }

            public static Dictionary<TextureFilter, TextureMinFilter> MinMipFilter
            {
                get;
                private set;
            }

            public static Dictionary<DepthFormat, FramebufferAttachment> DepthStencilAttachment
            {
                get;
                private set;
            }

            public static void Initialize()
            {
                /* Ideally we would be using arrays, rather than Dictionaries.
                 * The problem is that we don't support every enum, and dealing
                 * with gaps would be a headache. So whatever, Dictionaries!
                 * -flibit
                 */

                BlendModeSrc = new Dictionary<Blend, BlendingFactorSrc>();
                BlendModeSrc.Add(Blend.DestinationAlpha,        BlendingFactorSrc.DstAlpha);
                BlendModeSrc.Add(Blend.DestinationColor,        BlendingFactorSrc.DstColor);
                BlendModeSrc.Add(Blend.InverseDestinationAlpha, BlendingFactorSrc.OneMinusDstAlpha);
                BlendModeSrc.Add(Blend.InverseDestinationColor, BlendingFactorSrc.OneMinusDstColor);
                BlendModeSrc.Add(Blend.InverseSourceAlpha,      BlendingFactorSrc.OneMinusSrcAlpha);
                BlendModeSrc.Add(Blend.InverseSourceColor,      (BlendingFactorSrc) All.OneMinusSrcColor); // Why -flibit
                BlendModeSrc.Add(Blend.One,                     BlendingFactorSrc.One);
                BlendModeSrc.Add(Blend.SourceAlpha,             BlendingFactorSrc.SrcAlpha);
                BlendModeSrc.Add(Blend.SourceAlphaSaturation,   BlendingFactorSrc.SrcAlphaSaturate);
                BlendModeSrc.Add(Blend.SourceColor,             (BlendingFactorSrc) All.SrcColor); // Why -flibit
                BlendModeSrc.Add(Blend.Zero,                    BlendingFactorSrc.Zero);

                BlendModeDst = new Dictionary<Blend, BlendingFactorDest>();
                BlendModeDst.Add(Blend.DestinationAlpha,        BlendingFactorDest.DstAlpha);
                BlendModeDst.Add(Blend.InverseDestinationAlpha, BlendingFactorDest.OneMinusDstAlpha);
                BlendModeDst.Add(Blend.InverseSourceAlpha,      BlendingFactorDest.OneMinusSrcAlpha);
                BlendModeDst.Add(Blend.InverseSourceColor,      BlendingFactorDest.OneMinusSrcColor);
                BlendModeDst.Add(Blend.One,                     BlendingFactorDest.One);
                BlendModeDst.Add(Blend.SourceAlpha,             BlendingFactorDest.SrcAlpha);
                BlendModeDst.Add(Blend.SourceColor,             BlendingFactorDest.SrcColor);
                BlendModeDst.Add(Blend.Zero,                    BlendingFactorDest.Zero);

                BlendEquation = new Dictionary<BlendFunction, BlendEquationMode>();
                BlendEquation.Add(BlendFunction.Add,                BlendEquationMode.FuncAdd);
                BlendEquation.Add(BlendFunction.Max,                BlendEquationMode.Max);
                BlendEquation.Add(BlendFunction.Min,                BlendEquationMode.Min);
                BlendEquation.Add(BlendFunction.ReverseSubtract,    BlendEquationMode.FuncReverseSubtract);
                BlendEquation.Add(BlendFunction.Subtract,           BlendEquationMode.FuncSubtract);

                DepthFunc = new Dictionary<CompareFunction, DepthFunction>();
                DepthFunc.Add(CompareFunction.Always,       DepthFunction.Always);
                DepthFunc.Add(CompareFunction.Equal,        DepthFunction.Equal);
                DepthFunc.Add(CompareFunction.Greater,      DepthFunction.Greater);
                DepthFunc.Add(CompareFunction.GreaterEqual, DepthFunction.Gequal);
                DepthFunc.Add(CompareFunction.Less,         DepthFunction.Less);
                DepthFunc.Add(CompareFunction.LessEqual,    DepthFunction.Lequal);
                DepthFunc.Add(CompareFunction.Never,        DepthFunction.Never);
                DepthFunc.Add(CompareFunction.NotEqual,     DepthFunction.Notequal);

                StencilFunc = new Dictionary<CompareFunction, StencilFunction>();
                StencilFunc.Add(CompareFunction.Always,         StencilFunction.Always);
                StencilFunc.Add(CompareFunction.Equal,          StencilFunction.Equal);
                StencilFunc.Add(CompareFunction.Greater,        StencilFunction.Greater);
                StencilFunc.Add(CompareFunction.GreaterEqual,   StencilFunction.Gequal);
                StencilFunc.Add(CompareFunction.Less,           StencilFunction.Less);
                StencilFunc.Add(CompareFunction.LessEqual,      StencilFunction.Lequal);
                StencilFunc.Add(CompareFunction.Never,          StencilFunction.Never);
                StencilFunc.Add(CompareFunction.NotEqual,       StencilFunction.Notequal);

                GLStencilOp = new Dictionary<StencilOperation, StencilOp>();
                GLStencilOp.Add(StencilOperation.Decrement,             StencilOp.DecrWrap);
                GLStencilOp.Add(StencilOperation.DecrementSaturation,   StencilOp.Decr);
                GLStencilOp.Add(StencilOperation.Increment,             StencilOp.IncrWrap);
                GLStencilOp.Add(StencilOperation.IncrementSaturation,   StencilOp.Incr);
                GLStencilOp.Add(StencilOperation.Invert,                StencilOp.Invert);
                GLStencilOp.Add(StencilOperation.Keep,                  StencilOp.Keep);
                GLStencilOp.Add(StencilOperation.Replace,               StencilOp.Replace);
                GLStencilOp.Add(StencilOperation.Zero,                  StencilOp.Zero);

                FrontFace = new Dictionary<CullMode, FrontFaceDirection>();
                FrontFace.Add(CullMode.CullClockwiseFace,           FrontFaceDirection.Cw);
                FrontFace.Add(CullMode.CullCounterClockwiseFace,    FrontFaceDirection.Ccw);

                GLFillMode = new Dictionary<FillMode, PolygonMode>();
                GLFillMode.Add(FillMode.Solid,      PolygonMode.Fill);
                GLFillMode.Add(FillMode.WireFrame,  PolygonMode.Line);

                Wrap = new Dictionary<TextureAddressMode, TextureWrapMode>();
                Wrap.Add(TextureAddressMode.Clamp,  TextureWrapMode.ClampToEdge);
                Wrap.Add(TextureAddressMode.Mirror, TextureWrapMode.MirroredRepeat);
                Wrap.Add(TextureAddressMode.Wrap,   TextureWrapMode.Repeat);

                MagFilter = new Dictionary<TextureFilter, TextureMagFilter>();
                MagFilter.Add(TextureFilter.Point,                      TextureMagFilter.Nearest);
                MagFilter.Add(TextureFilter.Linear,                     TextureMagFilter.Linear);
                MagFilter.Add(TextureFilter.Anisotropic,                TextureMagFilter.Linear);
                MagFilter.Add(TextureFilter.LinearMipPoint,             TextureMagFilter.Linear);
                MagFilter.Add(TextureFilter.MinPointMagLinearMipPoint,  TextureMagFilter.Linear);
                MagFilter.Add(TextureFilter.MinPointMagLinearMipLinear, TextureMagFilter.Linear);
                MagFilter.Add(TextureFilter.MinLinearMagPointMipPoint,  TextureMagFilter.Nearest);
                MagFilter.Add(TextureFilter.MinLinearMagPointMipLinear, TextureMagFilter.Nearest);

                MinMipFilter = new Dictionary<TextureFilter, TextureMinFilter>();
                MinMipFilter.Add(TextureFilter.Point,                       TextureMinFilter.NearestMipmapNearest);
                MinMipFilter.Add(TextureFilter.Linear,                      TextureMinFilter.LinearMipmapLinear);
                MinMipFilter.Add(TextureFilter.Anisotropic,                 TextureMinFilter.LinearMipmapLinear);
                MinMipFilter.Add(TextureFilter.LinearMipPoint,              TextureMinFilter.LinearMipmapNearest);
                MinMipFilter.Add(TextureFilter.MinPointMagLinearMipPoint,   TextureMinFilter.NearestMipmapNearest);
                MinMipFilter.Add(TextureFilter.MinPointMagLinearMipLinear,  TextureMinFilter.NearestMipmapLinear);
                MinMipFilter.Add(TextureFilter.MinLinearMagPointMipPoint,   TextureMinFilter.LinearMipmapNearest);
                MinMipFilter.Add(TextureFilter.MinLinearMagPointMipLinear,  TextureMinFilter.LinearMipmapLinear);

                DepthStencilAttachment = new Dictionary<DepthFormat, FramebufferAttachment>();
                DepthStencilAttachment.Add(DepthFormat.Depth16,         FramebufferAttachment.DepthAttachment);
                DepthStencilAttachment.Add(DepthFormat.Depth24,         FramebufferAttachment.DepthAttachment);
                DepthStencilAttachment.Add(DepthFormat.Depth24Stencil8, FramebufferAttachment.DepthStencilAttachment);
            }

            public void Clear()
            {
                BlendModeSrc.Clear();
                BlendModeSrc = null;
                BlendModeDst.Clear();
                BlendModeDst = null;
                BlendEquation.Clear();
                BlendEquation = null;
                DepthFunc.Clear();
                DepthFunc = null;
                StencilFunc.Clear();
                StencilFunc = null;
                GLStencilOp.Clear();
                GLStencilOp = null;
                FrontFace.Clear();
                FrontFace = null;
                GLFillMode.Clear();
                GLFillMode = null;
                Wrap.Clear();
                Wrap = null;
                MagFilter.Clear();
                MagFilter = null;
                MinMipFilter.Clear();
                MinMipFilter = null;
                DepthStencilAttachment.Clear();
                DepthStencilAttachment = null;
            }
        }

        #endregion

        #region Framebuffer ARB/EXT Wrapper Class

        public static class Framebuffer
        {
            private static bool hasARB = false;

            public static void Initialize()
            {
                hasARB = OpenGLDevice.Instance.Extensions.Contains("ARB_framebuffer_object");
            }

            public static int GenFramebuffer()
            {
                int handle;
                if (hasARB)
                {
                    GL.GenFramebuffers(1, out handle);
                }
                else
                {
                    GL.Ext.GenFramebuffers(1, out handle);
                }
                return handle;
            }

            public static void DeleteFramebuffer(int handle)
            {
                if (hasARB)
                {
                    GL.DeleteFramebuffers(1, ref handle);
                }
                else
                {
                    GL.Ext.DeleteFramebuffers(1, ref handle);
                }
            }

            public static void BindFramebuffer(int handle)
            {
                if (hasARB)
                {
                    GL.BindFramebuffer(
                        FramebufferTarget.Framebuffer,
                        handle
                    );
                }
                else
                {
                    GL.Ext.BindFramebuffer(
                        FramebufferTarget.FramebufferExt,
                        handle
                    );
                }
            }

            public static void AttachColor(int colorAttachment, int index)
            {
                if (hasARB)
                {
                    GL.FramebufferTexture2D(
                        FramebufferTarget.Framebuffer,
                        FramebufferAttachment.ColorAttachment0 + index,
                        TextureTarget.Texture2D,
                        colorAttachment,
                        0
                    );
                }
                else
                {
                    GL.Ext.FramebufferTexture2D(
                        FramebufferTarget.FramebufferExt,
                        FramebufferAttachment.ColorAttachment0Ext + index,
                        TextureTarget.Texture2D,
                        colorAttachment,
                        0
                    );
                }
            }

            public static void AttachDepthTexture(
                int depthAttachment,
                FramebufferAttachment depthFormat
            ) {
                if (hasARB)
                {
                    GL.FramebufferTexture2D(
                        FramebufferTarget.Framebuffer,
                        depthFormat,
                        TextureTarget.Texture2D,
                        depthAttachment,
                        0
                    );
                }
                else
                {
                    GL.Ext.FramebufferTexture2D(
                        FramebufferTarget.FramebufferExt,
                        depthFormat,
                        TextureTarget.Texture2D,
                        depthAttachment,
                        0
                    );
                }
            }

            public static void AttachDepthRenderbuffer(
                int renderbuffer,
                FramebufferAttachment depthFormat
            ) {
                if (hasARB)
                {
                    GL.FramebufferRenderbuffer(
                        FramebufferTarget.Framebuffer,
                        depthFormat,
                        RenderbufferTarget.Renderbuffer,
                        renderbuffer
                    );
                }
                else
                {
                    GL.Ext.FramebufferRenderbuffer(
                        FramebufferTarget.FramebufferExt,
                        depthFormat,
                        RenderbufferTarget.RenderbufferExt,
                        renderbuffer
                    );
                }
            }

            public static void BlitToBackbuffer(
                int srcWidth,
                int srcHeight,
                int dstWidth,
                int dstHeight
            ) {
#if !DISABLE_FAUXBACKBUFFER
                bool scissorTest = OpenGLDevice.Instance.ScissorTestEnable.GetCurrent();
                if (scissorTest)
                {
                    GL.Disable(EnableCap.ScissorTest);
                }
                if (hasARB)
                {
                    GL.BindFramebuffer(
                        FramebufferTarget.ReadFramebuffer,
                        OpenGLDevice.Instance.Backbuffer.Handle
                    );
                    GL.BindFramebuffer(
                        FramebufferTarget.DrawFramebuffer,
                        0
                    );
                    GL.BlitFramebuffer(
                        0, 0, srcWidth, srcHeight,
                        0, 0, dstWidth, dstHeight,
                        ClearBufferMask.ColorBufferBit,
                        BlitFramebufferFilter.Linear
                    );
                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                }
                else
                {
                    GL.Ext.BindFramebuffer(
                        FramebufferTarget.ReadFramebuffer,
                        OpenGLDevice.Instance.Backbuffer.Handle
                    );
                    GL.Ext.BindFramebuffer(
                        FramebufferTarget.DrawFramebuffer,
                        0
                    );
                    GL.Ext.BlitFramebuffer(
                        0, 0, srcWidth, srcHeight,
                        0, 0, dstWidth, dstHeight,
                        ClearBufferMask.ColorBufferBit,
                        (ExtFramebufferBlit) BlitFramebufferFilter.Linear
                    );
                    GL.Ext.BindFramebuffer(FramebufferTarget.FramebufferExt, 0);
                }
                if (scissorTest)
                {
                    GL.Enable(EnableCap.ScissorTest);
                }
#endif
            }
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
                Width = width;
                Height = height;
#else
                Handle = Framebuffer.GenFramebuffer();
                colorAttachment = GL.GenTexture();
                depthStencilAttachment = GL.GenTexture();

                Framebuffer.BindFramebuffer(Handle);
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
                Framebuffer.AttachColor(
                    colorAttachment,
                    0
                );
                Framebuffer.AttachDepthTexture(
                    depthStencilAttachment,
                    FramebufferAttachment.DepthAttachment
                );
                GL.BindTexture(TextureTarget.Texture2D, 0);

                Width = width;
                Height = height;
#endif
            }

            public void Dispose()
            {
#if !DISABLE_FAUXBACKBUFFER
                Framebuffer.DeleteFramebuffer(Handle);
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

                    Framebuffer.BindFramebuffer(Handle);

                    Framebuffer.AttachDepthTexture(
                        0,
                        attach
                    );
                    Framebuffer.AttachDepthTexture(
                        depthStencilAttachment,
                        depthAttachmentType
                    );

                    if (Game.Instance.GraphicsDevice.IsRenderTargetBound)
                    {
                        Framebuffer.BindFramebuffer(
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