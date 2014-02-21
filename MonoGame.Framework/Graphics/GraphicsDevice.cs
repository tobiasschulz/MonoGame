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
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input.Touch;
using System.Diagnostics;

using OpenTK.Graphics.OpenGL;

namespace Microsoft.Xna.Framework.Graphics
{
    public class GraphicsDevice : IDisposable
    {
        private Viewport INTERNAL_viewport;

        private VertexBufferBinding[] _vertexBuffers;
        private IndexBuffer _indexBuffer;

        private readonly RenderTargetBinding[] _currentRenderTargetBindings = new RenderTargetBinding[4];

        public TextureCollection Textures { get; private set; }

        public SamplerStateCollection SamplerStates { get; private set; }

        // On Intel Integrated graphics, there is a fast hw unit for doing
        // clears to colors where all components are either 0 or 255.
        // Despite XNA4 using Purple here, we use black (in Release) to avoid
        // performance warnings on Intel/Mesa
#if DEBUG
        private static readonly Color DiscardColor = new Color(68, 34, 136, 255);
#else
        private static readonly Color DiscardColor = new Color(0, 0, 0, 255);
#endif

        /// <summary>
        /// The active vertex shader.
        /// </summary>
        private Shader _vertexShader;
        private bool _vertexShaderDirty;
        private bool VertexShaderDirty 
        {
            get { return _vertexShaderDirty; }
        }

        /// <summary>
        /// The active pixel shader.
        /// </summary>
        private Shader _pixelShader;
        private bool _pixelShaderDirty;
        private bool PixelShaderDirty 
        {
            get { return _pixelShaderDirty; }
        }

        static List<Action> disposeActions = new List<Action>();
        static object disposeActionsLock = new object();

        private readonly ConstantBufferCollection _vertexConstantBuffers = new ConstantBufferCollection(ShaderStage.Vertex, 16);
        private readonly ConstantBufferCollection _pixelConstantBuffers = new ConstantBufferCollection(ShaderStage.Pixel, 16);

        private readonly ShaderProgramCache _programCache = new ShaderProgramCache();

        private int _shaderProgram = -1;

        static readonly float[] _posFixup = new float[4];

        // TODO Graphics Device events need implementing
        public event EventHandler<EventArgs> DeviceLost;
        public event EventHandler<EventArgs> DeviceReset;
        public event EventHandler<EventArgs> DeviceResetting;
        public event EventHandler<ResourceCreatedEventArgs> ResourceCreated;
        public event EventHandler<ResourceDestroyedEventArgs> ResourceDestroyed;
        public event EventHandler<EventArgs> Disposing;

        public bool IsDisposed
        {
            get;
            private set;
        }

        public bool IsContentLost
        {
            get
            {
                // We will just return IsDisposed, as that
                // is the only case I can see for now
                return IsDisposed;
            }
        }

        internal bool IsRenderTargetBound
        {
            get
            {
                return RenderTargetCount > 0;
            }
        }

        public GraphicsAdapter Adapter
        {
            get;
            private set;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphicsDevice" /> class.
        /// </summary>
        /// <param name="adapter">The graphics adapter.</param>
        /// <param name="graphicsProfile">The graphics profile.</param>
        /// <param name="presentationParameters">The presentation options.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="presentationParameters"/> is <see langword="null"/>.
        /// </exception>
        public GraphicsDevice(GraphicsAdapter adapter, GraphicsProfile graphicsProfile, PresentationParameters presentationParameters)
        {
            Adapter = adapter;
            if (presentationParameters == null)
            {
                throw new ArgumentNullException("presentationParameters");
            }
            PresentationParameters = presentationParameters;
            GraphicsProfile = graphicsProfile;

            // Force set the default render states.
            BlendState = BlendState.Opaque;
            DepthStencilState = DepthStencilState.Default;
            RasterizerState = RasterizerState.CullCounterClockwise;

            // Clear the texture and sampler collections forcing
            // the state to be reapplied.
            Textures = new TextureCollection(OpenGLDevice.Instance.MaxTextureSlots);
            SamplerStates = new SamplerStateCollection(OpenGLDevice.Instance.MaxTextureSlots);
            Textures.Clear();
            SamplerStates.Clear();

            // Clear constant buffers
            _vertexConstantBuffers.Clear();
            _pixelConstantBuffers.Clear();

            // Force set the buffers and shaders on next ApplyState() call
            _vertexBuffers = new VertexBufferBinding[OpenGLDevice.Instance.MaxVertexAttributes];

            _vertexShaderDirty = true;
            _pixelShaderDirty = true;

            // Set the default scissor rect.
            ScissorRectangle = Viewport.Bounds;

            // Free all the cached shader programs. 
            _programCache.Clear();
            _shaderProgram = -1;
        }

        ~GraphicsDevice()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    // Dispose of all remaining graphics resources before disposing of the GraphicsDevice
                    GraphicsResource.DisposeAll();

                    // Free all the cached shader programs.
                    _programCache.Dispose();
                }

                IsDisposed = true;
            }
        }

        public RasterizerState RasterizerState
        {
            get;
            set;
        }

        public BlendState BlendState 
        {
            get;
            set;
        }

        public DepthStencilState DepthStencilState
        {
            get;
            set;
        }

        public void Clear(Color color)
        {
            Clear(
                ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil,
                color.ToVector4(),
                Viewport.MaxDepth,
                0
            );
        }

        public void Clear(ClearOptions options, Color color, float depth, int stencil)
        {
            Clear(
                options,
                color.ToVector4(),
                depth,
                stencil
            );
        }

        public void Clear(ClearOptions options, Vector4 color, float depth, int stencil)
        {
            OpenGLDevice.Instance.Clear(
                options,
                color,
                depth,
                stencil
            );
        }

        /// <summary>
        /// Adds a dispose action to the list of pending dispose actions. These are executed at the end of each call to Present().
        /// This allows GL resources to be disposed from other threads, such as the finalizer.
        /// </summary>
        /// <param name="disposeAction">The action to execute for the dispose.</param>
        internal static void AddDisposeAction(Action disposeAction)
        {
            if (disposeAction == null)
            {
                throw new ArgumentNullException("disposeAction");
            }
            if (Threading.IsOnUIThread())
            {
                disposeAction();
            }
            else
            {
                lock (disposeActionsLock)
                {
                    disposeActions.Add(disposeAction);
                }
            }
        }

        public void Present()
        {
            GL.Flush();
            GraphicsExtensions.CheckGLError();

            // Dispose of any GL resources that were disposed in another thread
            lock (disposeActionsLock)
            {
                if (disposeActions.Count > 0)
                {
                    foreach (var action in disposeActions)
                    {
                        action();
                    }
                    disposeActions.Clear();
                }
            }
        }

        /*
        public void Present(Rectangle? sourceRectangle, Rectangle? destinationRectangle, IntPtr overrideWindowHandle)
        {
            throw new NotImplementedException();
        }

        public void Reset()
        {
            // Manually resetting the device is not currently supported.
            throw new NotImplementedException();
        }

        public void Reset(PresentationParameters presentationParameters)
        {
            throw new NotImplementedException();
        }

        public void Reset(PresentationParameters presentationParameters, GraphicsAdapter graphicsAdapter)
        {
            throw new NotImplementedException();
        }
        */

        /// <summary>
        /// Trigger the DeviceResetting event
        /// Currently internal to allow the various platforms to send the event at the appropriate time.
        /// </summary>
        internal void OnDeviceResetting()
        {
            if (DeviceResetting != null)
            {
                DeviceResetting(this, EventArgs.Empty);
            }

#if !SDL2
            // FIXME: What, why, why -flibit
            GraphicsResource.DoGraphicsDeviceResetting();
#endif
        }

        /// <summary>
        /// Trigger the DeviceReset event to allow games to be notified of a device reset.
        /// Currently internal to allow the various platforms to send the event at the appropriate time.
        /// </summary>
        internal void OnDeviceReset()
        {
            if (DeviceReset != null)
            {
                DeviceReset(this, EventArgs.Empty);
            }
        }

        public DisplayMode DisplayMode
        {
            get
            {
#if SDL2
                return new DisplayMode(
                    OpenGLDevice.Instance.Backbuffer.Width,
                    OpenGLDevice.Instance.Backbuffer.Height,
                    60, // FIXME: Assumption!
                    SurfaceFormat.Color
                );
#else                    
                return Adapter.CurrentDisplayMode;
#endif
            }
        }

        public GraphicsDeviceStatus GraphicsDeviceStatus
        {
            get
            {
                return GraphicsDeviceStatus.Normal;
            }
        }

        public PresentationParameters PresentationParameters
        {
            get;
            private set;
        }

        public Viewport Viewport
        {
            get
            {
                return INTERNAL_viewport;
            }

            set
            {
                INTERNAL_viewport = value;

                if (IsRenderTargetBound)
                {
                    OpenGLDevice.Instance.GLViewport.Set(new Viewport(
                        value.X,
                        value.Y,
                        value.Width,
                        value.Height
                    ));
                }
                else
                {
                    OpenGLDevice.Instance.GLViewport.Set(new Viewport(
                        value.X,
                        PresentationParameters.BackBufferHeight - value.Y - value.Height,
                        value.Width,
                        value.Height
                    ));
                }

                OpenGLDevice.Instance.DepthRangeMin.Set(value.MinDepth);
                OpenGLDevice.Instance.DepthRangeMax.Set(value.MaxDepth);

                // In OpenGL we have to re-apply the special "posFixup"
                // vertex shader uniform if the viewport changes.
                _vertexShaderDirty = true;
            }
        }

        public GraphicsProfile GraphicsProfile { get; set; }

        public Rectangle ScissorRectangle
        {
            get
            {
                return OpenGLDevice.Instance.ScissorRectangle.Get();
            }

            set
            {
                OpenGLDevice.Instance.ScissorRectangle.Set(value);
            }
        }

        public int RenderTargetCount
        {
            get;
            private set;
        }

        public void SetRenderTarget(RenderTarget2D renderTarget)
        {
            if (renderTarget == null)
                SetRenderTargets(null);
            else
                SetRenderTargets(new RenderTargetBinding(renderTarget));
        }

        public void SetRenderTarget(RenderTargetCube renderTarget, CubeMapFace cubeMapFace)
        {
            if (renderTarget == null)
                SetRenderTargets(null);
            else
                SetRenderTargets(new RenderTargetBinding(renderTarget, cubeMapFace));
        }

        public void SetRenderTargets(params RenderTargetBinding[] renderTargets)
        {
            // Checking for redundant SetRenderTargets...
            if (renderTargets == null && !IsRenderTargetBound)
            {
                return;
            }
            else if (renderTargets != null && renderTargets.Length == RenderTargetCount)
            {
                bool isRedundant = true;
                for (int i = 0; i < renderTargets.Length; i += 1)
                {
                    if (    renderTargets[i].RenderTarget != _currentRenderTargetBindings[i].RenderTarget ||
                            renderTargets[i].ArraySlice != _currentRenderTargetBindings[i].ArraySlice  )
                    {
                        isRedundant = false;
                    }
                }
                if (isRedundant)
                {
                    return;
                }
            }

            Array.Clear(_currentRenderTargetBindings, 0, _currentRenderTargetBindings.Length);
            if (renderTargets == null || renderTargets.Length == 0)
            {
                OpenGLDevice.Instance.SetRenderTargets(null, 0, DepthFormat.None);

                RenderTargetCount = 0;

                // Set the viewport to the size of the backbuffer.
                Viewport = new Viewport(0, 0, PresentationParameters.BackBufferWidth, PresentationParameters.BackBufferHeight);

                // Set the scissor rectangle to the size of the backbuffer.
                ScissorRectangle = new Rectangle(0, 0, PresentationParameters.BackBufferWidth, PresentationParameters.BackBufferHeight);

                if (PresentationParameters.RenderTargetUsage == RenderTargetUsage.DiscardContents)
                {
                    Clear(DiscardColor);
                }
            }
            else
            {
                // TODO: CubeMapFace, other target types??
                int[] glTarget = new int[renderTargets.Length];
                for (int i = 0; i < renderTargets.Length; i += 1)
                {
                    glTarget[i] = renderTargets[i].RenderTarget.texture.Handle;
                }
                RenderTarget2D target = (RenderTarget2D) renderTargets[0].RenderTarget;
                OpenGLDevice.Instance.SetRenderTargets(glTarget, target.glDepthStencilBuffer, target.DepthStencilFormat);

                Array.Copy(renderTargets, _currentRenderTargetBindings, renderTargets.Length);
                RenderTargetCount = renderTargets.Length;

                // Set the viewport to the size of the first render target.
                Viewport = new Viewport(0, 0, target.Width, target.Height);

                // Set the scissor rectangle to the size of the first render target.
                ScissorRectangle = new Rectangle(0, 0, target.Width, target.Height);

                if (target.RenderTargetUsage == RenderTargetUsage.DiscardContents)
                {
                    Clear(DiscardColor);
                }
            }
        }

        public RenderTargetBinding[] GetRenderTargets()
        {
            // Return a correctly sized copy our internal array.
            var bindings = new RenderTargetBinding[RenderTargetCount];
            Array.Copy(_currentRenderTargetBindings, bindings, RenderTargetCount);
            return bindings;
        }

        public void GetRenderTargets(RenderTargetBinding[] outTargets)
        {
            Debug.Assert(outTargets.Length == RenderTargetCount, "Invalid outTargets array length!");
            Array.Copy(_currentRenderTargetBindings, outTargets, RenderTargetCount);
        }


        private static BeginMode PrimitiveTypeGL(PrimitiveType primitiveType)
        {
            switch (primitiveType)
            {
                case PrimitiveType.LineList:
                    return BeginMode.Lines;
                case PrimitiveType.LineStrip:
                    return BeginMode.LineStrip;
                case PrimitiveType.TriangleList:
                    return BeginMode.Triangles;
                case PrimitiveType.TriangleStrip:
                    return BeginMode.TriangleStrip;
            }

            throw new ArgumentException();
        }

        public void SetVertexBuffer(VertexBuffer vertexBuffer)
        {
            if (!ReferenceEquals(_vertexBuffers[0].VertexBuffer, vertexBuffer))
            {
                _vertexBuffers[0] = new VertexBufferBinding(vertexBuffer);
            }

            for (int vertexStreamSlot = 1; vertexStreamSlot < _vertexBuffers.Length; ++vertexStreamSlot)
            {
                if (_vertexBuffers[vertexStreamSlot].VertexBuffer != null)
                {
                    _vertexBuffers[vertexStreamSlot] = VertexBufferBinding.None;
                }
            }
        }

        /// <summary>
        /// Set vertex buffers.
        /// </summary>
        /// <param name="vertexBuffers">Array of vertex buffers to use.</param>
        public void SetVertexBuffers(params VertexBufferBinding[] vertexBuffers)
        {
            if((vertexBuffers != null) && (vertexBuffers.Length > _vertexBuffers.Length))
            {
                throw new ArgumentOutOfRangeException("vertexBuffers", String.Format("Max Vertex Buffers supported is {0}", _vertexBuffers.Length));
            }

            // Set vertex buffers if they are different
            int slot = 0;
            if (vertexBuffers != null)
            {
                for (; slot < vertexBuffers.Length; ++slot)
                {
                    if (!ReferenceEquals(_vertexBuffers[slot].VertexBuffer, vertexBuffers[slot].VertexBuffer)
                        || (_vertexBuffers[slot].VertexOffset != vertexBuffers[slot].VertexOffset)
                        || (_vertexBuffers[slot].InstanceFrequency != vertexBuffers[slot].InstanceFrequency))
                    {
                        _vertexBuffers[slot] = vertexBuffers[slot];
                    }
                }
            }

            // unset any unused vertex buffers
            for (; slot < _vertexBuffers.Length; ++slot)
            {
                if (_vertexBuffers[slot].VertexBuffer != null)
                {
                    _vertexBuffers[slot] = new VertexBufferBinding(null);
                }
            }
        }

        private void SetIndexBuffer(IndexBuffer indexBuffer)
        {
            _indexBuffer = indexBuffer;
        }

        public IndexBuffer Indices { set { SetIndexBuffer(value); } get { return _indexBuffer; } }

        internal Shader VertexShader
        {
            get { return _vertexShader; }

            set
            {
                if (_vertexShader == value)
                    return;

                _vertexShader = value;
                _vertexShaderDirty = true;
            }
        }

        internal Shader PixelShader
        {
            get { return _pixelShader; }

            set
            {
                if (_pixelShader == value)
                    return;

                _pixelShader = value;
                _pixelShaderDirty = true;
            }
        }

        internal void SetConstantBuffer(ShaderStage stage, int slot, ConstantBuffer buffer)
        {
            if (stage == ShaderStage.Vertex)
                _vertexConstantBuffers[slot] = buffer;
            else
                _pixelConstantBuffers[slot] = buffer;
        }

        /// <summary>
        /// Activates the Current Vertex/Pixel shader pair into a program.         
        /// </summary>
        private void ActivateShaderProgram()
        {
            // Lookup the shader program.
            var info = _programCache.GetProgramInfo(VertexShader, PixelShader);
            if (info.program == -1)
                return;
            // Set the new program if it has changed.
            if (_shaderProgram != info.program)
            {
                GL.UseProgram(info.program);
                GraphicsExtensions.CheckGLError();
                _shaderProgram = info.program;
            }

            if (info.posFixupLoc == -1)
                return;

            // Apply vertex shader fix:
            // The following two lines are appended to the end of vertex shaders
            // to account for rendering differences between OpenGL and DirectX:
            //
            // gl_Position.y = gl_Position.y * posFixup.y;
            // gl_Position.xy += posFixup.zw * gl_Position.ww;
            //
            // (the following paraphrased from wine, wined3d/state.c and wined3d/glsl_shader.c)
            //
            // - We need to flip along the y-axis in case of offscreen rendering.
            // - D3D coordinates refer to pixel centers while GL coordinates refer
            //   to pixel corners.
            // - D3D has a top-left filling convention. We need to maintain this
            //   even after the y-flip mentioned above.
            // In order to handle the last two points, we translate by
            // (63.0 / 128.0) / VPw and (63.0 / 128.0) / VPh. This is equivalent to
            // translating slightly less than half a pixel. We want the difference to
            // be large enough that it doesn't get lost due to rounding inside the
            // driver, but small enough to prevent it from interfering with any
            // anti-aliasing.
            //
            // OpenGL coordinates specify the center of the pixel while d3d coords specify
            // the corner. The offsets are stored in z and w in posFixup. posFixup.y contains
            // 1.0 or -1.0 to turn the rendering upside down for offscreen rendering. PosFixup.x
            // contains 1.0 to allow a mad.

            _posFixup[0] = 1.0f;
            _posFixup[1] = 1.0f;
            _posFixup[2] = (63.0f/64.0f)/Viewport.Width;
            _posFixup[3] = -(63.0f/64.0f)/Viewport.Height;

            //If we have a render target bound (rendering offscreen)
            if (IsRenderTargetBound)
            {
                //flip vertically
                _posFixup[1] *= -1.0f;
                _posFixup[3] *= -1.0f;
            }

            GL.Uniform4(info.posFixupLoc, 1, _posFixup);
            GraphicsExtensions.CheckGLError();
        }

        public bool ResourcesLost { get; set; }

        internal void ApplyState(bool applyShaders)
        {
            // Apply BlendState
            OpenGLDevice.Instance.AlphaBlendEnable.Set(
                !(  BlendState.ColorSourceBlend == Blend.One && 
                    BlendState.ColorDestinationBlend == Blend.Zero &&
                    BlendState.AlphaSourceBlend == Blend.One &&
                    BlendState.AlphaDestinationBlend == Blend.Zero  )
            );
            OpenGLDevice.Instance.BlendColor.Set(BlendState.BlendFactor);
            OpenGLDevice.Instance.BlendOp.Set(BlendState.ColorBlendFunction);
            OpenGLDevice.Instance.BlendOpAlpha.Set(BlendState.AlphaBlendFunction);
            OpenGLDevice.Instance.SrcBlend.Set(BlendState.ColorSourceBlend);
            OpenGLDevice.Instance.DstBlend.Set(BlendState.ColorDestinationBlend);
            OpenGLDevice.Instance.SrcBlendAlpha.Set(BlendState.AlphaSourceBlend);
            OpenGLDevice.Instance.DstBlendAlpha.Set(BlendState.AlphaDestinationBlend);
            OpenGLDevice.Instance.ColorWriteEnable.Set(BlendState.ColorWriteChannels);
            
            // Apply DepthStencilState
            OpenGLDevice.Instance.ZEnable.Set(DepthStencilState.DepthBufferEnable);
            OpenGLDevice.Instance.ZWriteEnable.Set(DepthStencilState.DepthBufferWriteEnable);
            OpenGLDevice.Instance.DepthFunc.Set(DepthStencilState.DepthBufferFunction);
            OpenGLDevice.Instance.StencilEnable.Set(DepthStencilState.StencilEnable);
            OpenGLDevice.Instance.StencilWriteMask.Set(DepthStencilState.StencilWriteMask);
            OpenGLDevice.Instance.SeparateStencilEnable.Set(DepthStencilState.TwoSidedStencilMode);
            OpenGLDevice.Instance.StencilRef.Set(DepthStencilState.ReferenceStencil);
            OpenGLDevice.Instance.StencilMask.Set(DepthStencilState.StencilMask);
            OpenGLDevice.Instance.StencilFunc.Set(DepthStencilState.StencilFunction);
            OpenGLDevice.Instance.CCWStencilFunc.Set(DepthStencilState.CounterClockwiseStencilFunction);
            OpenGLDevice.Instance.StencilFail.Set(DepthStencilState.StencilFail);
            OpenGLDevice.Instance.StencilZFail.Set(DepthStencilState.StencilDepthBufferFail);
            OpenGLDevice.Instance.StencilPass.Set(DepthStencilState.StencilPass);
            OpenGLDevice.Instance.CCWStencilFail.Set(DepthStencilState.CounterClockwiseStencilFail);
            OpenGLDevice.Instance.CCWStencilZFail.Set(DepthStencilState.CounterClockwiseStencilDepthBufferFail);
            OpenGLDevice.Instance.CCWStencilPass.Set(DepthStencilState.CounterClockwiseStencilPass);

            // Apply RasterizerState
            if (IsRenderTargetBound)
            {
                OpenGLDevice.Instance.CullFrontFace.Set(RasterizerState.CullMode);
            }
            else
            {
                // When not rendering offscreen the faces change order.
                if (RasterizerState.CullMode == CullMode.None)
                {
                    OpenGLDevice.Instance.CullFrontFace.Set(RasterizerState.CullMode);
                }
                else
                {
                    OpenGLDevice.Instance.CullFrontFace.Set(
                        RasterizerState.CullMode == CullMode.CullClockwiseFace ?
                            CullMode.CullCounterClockwiseFace :
                            CullMode.CullClockwiseFace
                    );
                }
            }
            OpenGLDevice.Instance.GLFillMode.Set(RasterizerState.FillMode);
            OpenGLDevice.Instance.ScissorTestEnable.Set(RasterizerState.ScissorTestEnable);
            OpenGLDevice.Instance.DepthBias.Set(RasterizerState.DepthBias);
            OpenGLDevice.Instance.SlopeScaleDepthBias.Set(RasterizerState.SlopeScaleDepthBias);

            // TODO: MSAA?

            // If we're not applying shaders then drop out now.
            if (!applyShaders)
            {
                return;
            }

            if (_vertexShader == null)
            {
                throw new InvalidOperationException("A vertex shader must be set!");
            }
            if (_pixelShader == null)
            {
                throw new InvalidOperationException("A pixel shader must be set!");
            }

            if (_vertexShaderDirty || _pixelShaderDirty)
            {
                ActivateShaderProgram();
                _vertexShaderDirty = _pixelShaderDirty = false;
            }

            _vertexConstantBuffers.SetConstantBuffers(this, _shaderProgram);
            _pixelConstantBuffers.SetConstantBuffers(this, _shaderProgram);

            // Apply Textures/Samplers
            for (int i = 0; i < OpenGLDevice.Instance.MaxTextureSlots; i += 1)
            {
                SamplerState sampler = SamplerStates[i];
                Texture texture = Textures[i];

                if (sampler != null && texture != null)
                {
                    OpenGLDevice.Instance.Samplers[i].Texture.Set(texture.texture);
                    OpenGLDevice.Instance.Samplers[i].Target.Set(texture.texture.Target);
                    texture.texture.WrapS.Set(sampler.AddressU);
                    texture.texture.WrapT.Set(sampler.AddressV);
                    texture.texture.WrapR.Set(sampler.AddressW);
                    texture.texture.Filter.Set(sampler.Filter);
                }
                else if (texture == null)
                {
                    OpenGLDevice.Instance.Samplers[i].Texture.Set(OpenGLDevice.OpenGLTexture.NullTexture);
                }
            }
        }

        /// <summary>
        /// Draw geometry by indexing into the vertex buffer.
        /// </summary>
        /// <param name="primitiveType">The type of primitives in the index buffer.</param>
        /// <param name="baseVertex">Used to offset the vertex range indexed from the vertex buffer.</param>
        /// <param name="minVertexIndex">A hint of the lowest vertex indexed relative to baseVertex.</param>
        /// <param name="numVertices">An hint of the maximum vertex indexed.</param>
        /// <param name="startIndex">The index within the index buffer to start drawing from.</param>
        /// <param name="primitiveCount">The number of primitives to render from the index buffer.</param>
        /// <remarks>Note that minVertexIndex and numVertices are unused in MonoGame and will be ignored.</remarks>
        public void DrawIndexedPrimitives(PrimitiveType primitiveType, int baseVertex, int minVertexIndex, int numVertices, int startIndex, int primitiveCount)
        {
            Debug.Assert(_vertexBuffers[0].VertexBuffer != null, "The vertex buffer is null!");
            Debug.Assert(_indexBuffer != null, "The index buffer is null!");

            // NOTE: minVertexIndex and numVertices are only hints of the
            // range of vertex data which will be indexed.
            //
            // They will only be used if the graphics API can use
            // this range hint to optimize rendering.

            // Flush the GL state before moving on!
            ApplyState(true);
            OpenGLDevice.Instance.FlushGLState();

            // Unsigned short or unsigned int?
            bool shortIndices = _indexBuffer.IndexElementSize == IndexElementSize.SixteenBits;

            // Set up the vertex buffers
            foreach (VertexBufferBinding vertBuffer in _vertexBuffers)
            {
                if (vertBuffer.VertexBuffer != null)
                {
                    OpenGLDevice.Instance.BindVertexBuffer(vertBuffer.VertexBuffer.vbo);
                    vertBuffer.VertexBuffer.VertexDeclaration.Apply(
                        _vertexShader,
                        (IntPtr) (vertBuffer.VertexBuffer.VertexDeclaration.VertexStride * (vertBuffer.VertexOffset + baseVertex))
                    );
                }
            }

            // Enable the appropriate vertex attributes.
            OpenGLDevice.Instance.FlushGLVertexAttributes();

            // Bind the index buffer
            OpenGLDevice.Instance.BindIndexBuffer(_indexBuffer.ibo);

            // Draw!
            GL.DrawRangeElements(
                PrimitiveTypeGL(primitiveType),
                minVertexIndex,
                minVertexIndex + numVertices,
                GetElementCountArray(primitiveType, primitiveCount),
                shortIndices ? DrawElementsType.UnsignedShort : DrawElementsType.UnsignedInt,
                (IntPtr) (startIndex * (shortIndices ? 2 : 4))
            );

            // Check for errors in the debug context
            GraphicsExtensions.CheckGLError();
        }

        public void DrawInstancedPrimitives(
            PrimitiveType primitiveType,
            int baseVertex,
            int minVertexIndex,
            int numVertices,
            int startIndex,
            int primitiveCount,
            int instanceCount
        ) {
            // Note that minVertexIndex and numVertices are NOT used!

            // If this device doesn't have the support, just explode now before it's too late.
            if (!OpenGLDevice.Instance.SupportsHardwareInstancing)
            {
                throw new Exception("Your hardware does not support hardware instancing!");
            }

            // Flush the GL state before moving on!
            ApplyState(true);
            OpenGLDevice.Instance.FlushGLState();

            // Unsigned short or unsigned int?
            bool shortIndices = _indexBuffer.IndexElementSize == IndexElementSize.SixteenBits;

            // Set up the vertex buffers
            foreach (VertexBufferBinding vertBuffer in _vertexBuffers)
            {
                if (vertBuffer.VertexBuffer != null)
                {
                    OpenGLDevice.Instance.BindVertexBuffer(vertBuffer.VertexBuffer.vbo);
                    vertBuffer.VertexBuffer.VertexDeclaration.Apply(
                        _vertexShader,
                        (IntPtr) (vertBuffer.VertexBuffer.VertexDeclaration.VertexStride * (vertBuffer.VertexOffset + baseVertex)),
                        vertBuffer.InstanceFrequency
                    );
                }
            }

            // Enable the appropriate vertex attributes.
            OpenGLDevice.Instance.FlushGLVertexAttributes();

            // Bind the index buffer
            OpenGLDevice.Instance.BindIndexBuffer(_indexBuffer.ibo);

            // Draw!
            GL.DrawElementsInstanced(
                PrimitiveTypeGL(primitiveType),
                GetElementCountArray(primitiveType, primitiveCount),
                shortIndices ? DrawElementsType.UnsignedShort : DrawElementsType.UnsignedInt,
                (IntPtr) (startIndex * (shortIndices ? 2 : 4)),
                instanceCount
            );

            // Check for errors in the debug context
            GraphicsExtensions.CheckGLError();
        }

        public void DrawUserPrimitives<T>(PrimitiveType primitiveType, T[] vertexData, int vertexOffset, int primitiveCount) where T : struct, IVertexType
        {
            DrawUserPrimitives(primitiveType, vertexData, vertexOffset, primitiveCount, VertexDeclarationCache<T>.VertexDeclaration);
        }

        public void DrawUserPrimitives<T>(PrimitiveType primitiveType, T[] vertexData, int vertexOffset, int primitiveCount, VertexDeclaration vertexDeclaration) where T : struct
        {            
            Debug.Assert(vertexData != null && vertexData.Length > 0, "The vertexData must not be null or zero length!");

            var vertexCount = GetElementCountArray(primitiveType, primitiveCount);

            // Flush the GL state before moving on!
            ApplyState(true);
            OpenGLDevice.Instance.FlushGLState();

            // Unbind current VBOs.
            OpenGLDevice.Instance.BindVertexBuffer(0);

            // Pin the buffers.
            var vbHandle = GCHandle.Alloc(vertexData, GCHandleType.Pinned);

            // Setup the vertex declaration to point at the VB data.
            vertexDeclaration.GraphicsDevice = this;
            vertexDeclaration.Apply(_vertexShader, vbHandle.AddrOfPinnedObject());

            // Enable the appropriate vertex attributes.
            OpenGLDevice.Instance.FlushGLVertexAttributes();

            //Draw
            GL.DrawArrays(
                PrimitiveTypeGL(primitiveType),
                vertexOffset,
                vertexCount
            );
            GraphicsExtensions.CheckGLError();

            // Release the handles.
            vbHandle.Free();
        }

        public void DrawPrimitives(PrimitiveType primitiveType, int vertexStart, int primitiveCount)
        {
            Debug.Assert(_vertexBuffers[0].VertexBuffer != null, "The vertex buffer is null!");

            var vertexCount = GetElementCountArray(primitiveType, primitiveCount);

            // Flush the GL state before moving on!
            ApplyState(true);
            OpenGLDevice.Instance.FlushGLState();

            // Set up the vertex buffers
            foreach (VertexBufferBinding vertBuffer in _vertexBuffers)
            {
                if (vertBuffer.VertexBuffer != null)
                {
                    OpenGLDevice.Instance.BindVertexBuffer(vertBuffer.VertexBuffer.vbo);
                    vertBuffer.VertexBuffer.VertexDeclaration.Apply(
                        _vertexShader,
                        (IntPtr) (vertBuffer.VertexBuffer.VertexDeclaration.VertexStride * vertBuffer.VertexOffset)
                    );
                }
            }

            // Enable the appropriate vertex attributes.
            OpenGLDevice.Instance.FlushGLVertexAttributes();

            // Draw!
            GL.DrawArrays(
                PrimitiveTypeGL(primitiveType),
                vertexStart,
                vertexCount
            );

            // Check for errors in the debug context
            GraphicsExtensions.CheckGLError();
        }

        public void DrawUserIndexedPrimitives<T>(PrimitiveType primitiveType, T[] vertexData, int vertexOffset, int numVertices, short[] indexData, int indexOffset, int primitiveCount) where T : struct, IVertexType
        {
            DrawUserIndexedPrimitives<T>(primitiveType, vertexData, vertexOffset, numVertices, indexData, indexOffset, primitiveCount, VertexDeclarationCache<T>.VertexDeclaration);
        }

        public void DrawUserIndexedPrimitives<T>(PrimitiveType primitiveType, T[] vertexData, int vertexOffset, int numVertices, short[] indexData, int indexOffset, int primitiveCount, VertexDeclaration vertexDeclaration) where T : struct
        {
            Debug.Assert(vertexData != null && vertexData.Length > 0, "The vertexData must not be null or zero length!");
            Debug.Assert(indexData != null && indexData.Length > 0, "The indexData must not be null or zero length!");

            ApplyState(true);
            OpenGLDevice.Instance.FlushGLState();

            // Unbind current buffer objects.
            OpenGLDevice.Instance.BindVertexBuffer(0);
            OpenGLDevice.Instance.BindIndexBuffer(0);

            // Pin the buffers.
            var vbHandle = GCHandle.Alloc(vertexData, GCHandleType.Pinned);
            var ibHandle = GCHandle.Alloc(indexData, GCHandleType.Pinned);

            var vertexAddr = (IntPtr)(vbHandle.AddrOfPinnedObject().ToInt64() + vertexDeclaration.VertexStride * vertexOffset);

            // Setup the vertex declaration to point at the VB data.
            vertexDeclaration.GraphicsDevice = this;
            vertexDeclaration.Apply(_vertexShader, vertexAddr);

            // Enable the appropriate vertex attributes.
            OpenGLDevice.Instance.FlushGLVertexAttributes();

            // Draw!
            GL.DrawRangeElements(
                PrimitiveTypeGL(primitiveType),
                0,
                numVertices,
                GetElementCountArray(primitiveType, primitiveCount),
                DrawElementsType.UnsignedShort,
                (IntPtr) (ibHandle.AddrOfPinnedObject().ToInt64() + (indexOffset * sizeof(short)))
            );

            // Check for errors in the debug context
            GraphicsExtensions.CheckGLError();

            // Release the handles.
            ibHandle.Free();
            vbHandle.Free();
        }

        public void DrawUserIndexedPrimitives<T>(PrimitiveType primitiveType, T[] vertexData, int vertexOffset, int numVertices, int[] indexData, int indexOffset, int primitiveCount) where T : struct, IVertexType
        {
            DrawUserIndexedPrimitives<T>(primitiveType, vertexData, vertexOffset, numVertices, indexData, indexOffset, primitiveCount, VertexDeclarationCache<T>.VertexDeclaration);
        }

        public void DrawUserIndexedPrimitives<T>(PrimitiveType primitiveType, T[] vertexData, int vertexOffset, int numVertices, int[] indexData, int indexOffset, int primitiveCount, VertexDeclaration vertexDeclaration) where T : struct, IVertexType
        {
            Debug.Assert(vertexData != null && vertexData.Length > 0, "The vertexData must not be null or zero length!");
            Debug.Assert(indexData != null && indexData.Length > 0, "The indexData must not be null or zero length!");

            ApplyState(true);
            OpenGLDevice.Instance.FlushGLState();

            // Unbind current buffer objects.
            OpenGLDevice.Instance.BindVertexBuffer(0);
            OpenGLDevice.Instance.BindIndexBuffer(0);

            // Pin the buffers.
            var vbHandle = GCHandle.Alloc(vertexData, GCHandleType.Pinned);
            var ibHandle = GCHandle.Alloc(indexData, GCHandleType.Pinned);

            var vertexAddr = (IntPtr)(vbHandle.AddrOfPinnedObject().ToInt64() + vertexDeclaration.VertexStride * vertexOffset);

            // Setup the vertex declaration to point at the VB data.
            vertexDeclaration.GraphicsDevice = this;
            vertexDeclaration.Apply(_vertexShader, vertexAddr);

            // Enable the appropriate vertex attributes.
            OpenGLDevice.Instance.FlushGLVertexAttributes();

            // Draw!
            GL.DrawRangeElements(
                PrimitiveTypeGL(primitiveType),
                0,
                numVertices,
                GetElementCountArray(primitiveType, primitiveCount),
                DrawElementsType.UnsignedInt,
                (IntPtr) (ibHandle.AddrOfPinnedObject().ToInt64() + (indexOffset * sizeof(int)))
            );

            // Check for errors in the debug context
            GraphicsExtensions.CheckGLError();

            // Release the handles.
            ibHandle.Free();
            vbHandle.Free();
        }

        private static int GetElementCountArray(PrimitiveType primitiveType, int primitiveCount)
        {
            //TODO: Overview the calculation
            switch (primitiveType)
            {
                case PrimitiveType.LineList:
                    return primitiveCount * 2;
                case PrimitiveType.LineStrip:
                    return primitiveCount + 1;
                case PrimitiveType.TriangleList:
                    return primitiveCount * 3;
                case PrimitiveType.TriangleStrip:
                    return 3 + (primitiveCount - 1); // ???
            }

            throw new NotSupportedException();
        }

        public void GetBackBufferData<T>(T[] data) where T : struct
        {
            throw new NotImplementedException("FIXME: flibit put this here.");
        }
    }
}
