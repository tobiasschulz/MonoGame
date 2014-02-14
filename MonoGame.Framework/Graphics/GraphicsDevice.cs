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
        private Viewport _viewport;

        private bool _isDisposed;

        private BlendState _blendState = BlendState.Opaque;
        private DepthStencilState _depthStencilState = DepthStencilState.Default;
		private RasterizerState _rasterizerState = RasterizerState.CullCounterClockwise;
  
        private VertexBufferBinding[] _vertexBuffers;
        private IndexBuffer _indexBuffer;

        private readonly RenderTargetBinding[] _currentRenderTargetBindings = new RenderTargetBinding[4];
        private int _currentRenderTargetCount;

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

		const FramebufferTarget GLFramebuffer = FramebufferTarget.FramebufferExt;
		const RenderbufferTarget GLRenderbuffer = RenderbufferTarget.RenderbufferExt;
		const FramebufferAttachment GLDepthAttachment = FramebufferAttachment.DepthAttachmentExt;
		const FramebufferAttachment GLStencilAttachment = FramebufferAttachment.StencilAttachment;
		const FramebufferAttachment GLColorAttachment0 = FramebufferAttachment.ColorAttachment0;
		const GetPName GLFramebufferBinding = GetPName.FramebufferBinding;
		const RenderbufferStorage GLDepthComponent16 = RenderbufferStorage.DepthComponent16;
		const RenderbufferStorage GLDepthComponent24 = RenderbufferStorage.DepthComponent24;
		const RenderbufferStorage GLDepth24Stencil8 = RenderbufferStorage.Depth24Stencil8;
		const FramebufferErrorCode GLFramebufferComplete = FramebufferErrorCode.FramebufferComplete;
		
		// TODO Graphics Device events need implementing
		public event EventHandler<EventArgs> DeviceLost;
		public event EventHandler<EventArgs> DeviceReset;
		public event EventHandler<EventArgs> DeviceResetting;
		public event EventHandler<ResourceCreatedEventArgs> ResourceCreated;
		public event EventHandler<ResourceDestroyedEventArgs> ResourceDestroyed;
        public event EventHandler<EventArgs> Disposing;

        private bool SuppressEventHandlerWarningsUntilEventsAreProperlyImplemented()
        {
            return
                DeviceLost != null &&
                ResourceCreated != null &&
                ResourceDestroyed != null &&
                Disposing != null;
        }

        internal int glFramebuffer = 0;
        internal int glRenderTargetFrameBuffer;
        internal int MaxVertexAttributes;        
        internal List<string> _extensions = new List<string>();
        internal int _maxTextureSize = 0;
        
        internal int MaxTextureSlots;

        public bool IsDisposed
        {
            get
            {
                return _isDisposed;
            }
        }
		
		public bool IsContentLost { 
			get {
				// We will just return IsDisposed for now
				// as that is the only case I can see for now
				return IsDisposed;
			}
		}
	
	/// <summary>
        /// Returns a handle to internal device object. Valid only on DirectX platforms.
        /// For usage, convert this to SharpDX.Direct3D11.Device.
        /// </summary>
        public object Handle
        {
            get
            {
                return null;
            }
        }
	
        internal bool IsRenderTargetBound
        {
            get
            {
                return _currentRenderTargetCount > 0;
            }
        }

        public GraphicsAdapter Adapter
        {
            get;
            private set;
        }

        internal GraphicsDevice(GraphicsDeviceInformation gdi)
        {
            SetupGL();
            if (gdi.PresentationParameters == null)
                throw new ArgumentNullException("presentationParameters");
            PresentationParameters = gdi.PresentationParameters;
            GraphicsProfile = gdi.GraphicsProfile;
            Initialize();
        }

        internal GraphicsDevice ()
		{
            SetupGL();
            PresentationParameters = new PresentationParameters();
            PresentationParameters.DepthStencilFormat = DepthFormat.Depth24;
            Initialize();
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
                throw new ArgumentNullException("presentationParameters");
            SetupGL();
            PresentationParameters = presentationParameters;
            GraphicsProfile = graphicsProfile;
            Initialize();
        }

        private void SetupGL() 
        {
			// Initialize the main viewport
			_viewport = new Viewport (0, 0, DisplayMode.Width, DisplayMode.Height);
			_viewport.MaxDepth = 1.0f;
   
            MaxTextureSlots = 16;

            GL.GetInteger(GetPName.MaxTextureImageUnits, out MaxTextureSlots);
            GraphicsExtensions.CheckGLError();

            GL.GetInteger(GetPName.MaxVertexAttribs, out MaxVertexAttributes);
            GraphicsExtensions.CheckGLError();
            
            GL.GetInteger(GetPName.MaxTextureSize, out _maxTextureSize);
            GraphicsExtensions.CheckGLError();

            _extensions = GetGLExtensions();

            Textures = new TextureCollection (MaxTextureSlots);
			SamplerStates = new SamplerStateCollection (MaxTextureSlots);

        }

        ~GraphicsDevice()
        {
            Dispose(false);
        }

        List<string> GetGLExtensions()
        {
            // Setup extensions.
            List<string> extensions = new List<string>();

            var extstring = GL.GetString(StringName.Extensions);
            GraphicsExtensions.CheckGLError();
            if (!string.IsNullOrEmpty(extstring))
            {
                extensions.AddRange(extstring.Split(' '));
                System.Diagnostics.Debug.WriteLine("Supported extensions:");
                foreach (string extension in extensions)
                    System.Diagnostics.Debug.WriteLine(extension);
            }

            return extensions;
        }

        internal void Initialize()
        {
            GraphicsCapabilities.Initialize(this);

            _viewport = new Viewport(0, 0, PresentationParameters.BackBufferWidth, PresentationParameters.BackBufferHeight);

            // Force set the default render states.
            BlendState = BlendState.Opaque;
            DepthStencilState = DepthStencilState.Default;
            RasterizerState = RasterizerState.CullCounterClockwise;

            // Clear the texture and sampler collections forcing
            // the state to be reapplied.
            Textures.Clear();
            SamplerStates.Clear();

            // Clear constant buffers
            _vertexConstantBuffers.Clear();
            _pixelConstantBuffers.Clear();

            // Force set the buffers and shaders on next ApplyState() call
            int maxVertexBufferSlots = MaxVertexAttributes;
            _vertexBuffers = new VertexBufferBinding[maxVertexBufferSlots];

            _vertexShaderDirty = true;
            _pixelShaderDirty = true;

            // Set the default scissor rect.
            ScissorRectangle = _viewport.Bounds;

            // Free all the cached shader programs. 
            _programCache.Clear();
            _shaderProgram = -1;
        }

        public RasterizerState RasterizerState
        {
            get
            {
                return _rasterizerState;
            }

            set
            {
                _rasterizerState = value;
            }
        }

        public BlendState BlendState 
        {
			get
            {
                return _blendState;
            }
			set 
            {
				_blendState = value;
            }
		}

        public DepthStencilState DepthStencilState
        {
            get
            {
                return _depthStencilState;
            }
            set
            {
                _depthStencilState = value;
            }
        }

        public void Clear(Color color)
        {
			var options = ClearOptions.Target;

            // TODO: We need to figure out how to detect if
            // we have a depth stencil buffer or not!
            options |= ClearOptions.DepthBuffer;
            options |= ClearOptions.Stencil;

            Clear(options, color.ToVector4(), _viewport.MaxDepth, 0);
        }

        public void Clear(ClearOptions options, Color color, float depth, int stencil)
        {
            Clear (options, color.ToVector4 (), depth, stencil);
        }

		public void Clear(ClearOptions options, Vector4 color, float depth, int stencil)
		{
            // Unlike with XNA and DirectX...  GL.Clear() obeys several
            // different render states:
            //
            //  - The color write flags.
            //  - The scissor rectangle.
            //  - The depth/stencil state.
            //
            // So overwrite these states with what is needed to perform
            // the clear correctly and restore it afterwards.
            //
		    var prevScissorRect = ScissorRectangle;
		    var prevDepthStencilState = DepthStencilState;
            var prevBlendState = BlendState;
            ScissorRectangle = _viewport.Bounds;
            DepthStencilState = DepthStencilState.Default;
		    BlendState = BlendState.Opaque;
            ApplyState(false);

            ClearBufferMask bufferMask = 0;
            if ((options & ClearOptions.Target) == ClearOptions.Target)
            {
                GL.ClearColor(color.X, color.Y, color.Z, color.W);
                GraphicsExtensions.CheckGLError();
                bufferMask = bufferMask | ClearBufferMask.ColorBufferBit;
            }
			if ((options & ClearOptions.Stencil) == ClearOptions.Stencil)
            {
				GL.ClearStencil(stencil);
                GraphicsExtensions.CheckGLError();
                bufferMask = bufferMask | ClearBufferMask.StencilBufferBit;
			}

			if ((options & ClearOptions.DepthBuffer) == ClearOptions.DepthBuffer) 
            {
                GL.ClearDepth ((double)depth);
				bufferMask = bufferMask | ClearBufferMask.DepthBufferBit;
			}

			GL.Clear(bufferMask);

            // Restore the previous render state.
		    ScissorRectangle = prevScissorRect;
		    DepthStencilState = prevDepthStencilState;
		    BlendState = prevBlendState;
        }
		
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // Dispose of all remaining graphics resources before disposing of the graphics device
                    GraphicsResource.DisposeAll();

                    // Free all the cached shader programs.
                    _programCache.Dispose();

                    GraphicsDevice.AddDisposeAction(() =>
                                                    {
                        if (this.glRenderTargetFrameBuffer > 0)
                        {
                            GL.DeleteFramebuffers(1, ref this.glRenderTargetFrameBuffer);
                            GraphicsExtensions.CheckGLError();
                        }
                    });
                }

                _isDisposed = true;
            }
        }

        /// <summary>
        /// Adds a dispose action to the list of pending dispose actions. These are executed at the end of each call to Present().
        /// This allows GL resources to be disposed from other threads, such as the finalizer.
        /// </summary>
        /// <param name="disposeAction">The action to execute for the dispose.</param>
        static internal void AddDisposeAction(Action disposeAction)
        {
            if (disposeAction == null)
                throw new ArgumentNullException("disposeAction");
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
                        action();
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
                DeviceResetting(this, EventArgs.Empty);

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
                DeviceReset(this, EventArgs.Empty);
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
                return _viewport;
            }

            set
            {
                _viewport = value;

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
                OpenGLDevice.Instance.DepthRangeMin.Set(value.MaxDepth);

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
            get
            {
                return _currentRenderTargetCount;
            }
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
            Array.Clear(_currentRenderTargetBindings, 0, _currentRenderTargetBindings.Length);
            if (renderTargets == null || renderTargets.Length == 0)
            {
                OpenGLDevice.Instance.SetRenderTargets(null, 0, DepthFormat.None);

                // Set the viewport to the size of the backbuffer.
                Viewport = new Viewport(0, 0, PresentationParameters.BackBufferWidth, PresentationParameters.BackBufferHeight);

                // Set the scissor rectangle to the size of the backbuffer.
                ScissorRectangle = new Rectangle(0, 0, PresentationParameters.BackBufferWidth, PresentationParameters.BackBufferHeight);

                _currentRenderTargetCount = 0;
            }
            else
            {
                // TODO: CubeMapFace, other target types??
                int[] glTarget = new int[renderTargets.Length];
                for (int i = 0; i < renderTargets.Length; i += 1)
                {
                    glTarget[i] = renderTargets[i].RenderTarget.glTexture;
                }
                RenderTarget2D target = (RenderTarget2D) renderTargets[0].RenderTarget;
                OpenGLDevice.Instance.SetRenderTargets(glTarget, target.glDepthBuffer, target.DepthStencilFormat);

                // Set the viewport to the size of the first render target.
                Viewport = new Viewport(0, 0, target.Width, target.Height);

                // Set the scissor rectangle to the size of the first render target.
                ScissorRectangle = new Rectangle(0, 0, target.Width, target.Height);

                Array.Copy(renderTargets, _currentRenderTargetBindings, renderTargets.Length);
                _currentRenderTargetCount = renderTargets.Length;
            }

            if (PresentationParameters.RenderTargetUsage == RenderTargetUsage.DiscardContents)
            {
                Clear(DiscardColor);
            }
        }

		public RenderTargetBinding[] GetRenderTargets()
		{
            // Return a correctly sized copy our internal array.
            var bindings = new RenderTargetBinding[_currentRenderTargetCount];
            Array.Copy(_currentRenderTargetBindings, bindings, _currentRenderTargetCount);
            return bindings;
		}

        public void GetRenderTargets(RenderTargetBinding[] outTargets)
        {
            Debug.Assert(outTargets.Length == _currentRenderTargetCount, "Invalid outTargets array length!");
            Array.Copy(_currentRenderTargetBindings, outTargets, _currentRenderTargetCount);
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
                !(  _blendState.ColorSourceBlend == Blend.One && 
                    _blendState.ColorDestinationBlend == Blend.Zero &&
                    _blendState.AlphaSourceBlend == Blend.One &&
                    _blendState.AlphaDestinationBlend == Blend.Zero    )
            );
            OpenGLDevice.Instance.BlendColor.Set(_blendState.BlendFactor);
            OpenGLDevice.Instance.BlendOp.Set(_blendState.ColorBlendFunction);
            OpenGLDevice.Instance.BlendOpAlpha.Set(_blendState.AlphaBlendFunction);
            OpenGLDevice.Instance.SrcBlend.Set(_blendState.ColorSourceBlend);
            OpenGLDevice.Instance.DstBlend.Set(_blendState.ColorDestinationBlend);
            OpenGLDevice.Instance.SrcBlendAlpha.Set(_blendState.AlphaSourceBlend);
            OpenGLDevice.Instance.DstBlendAlpha.Set(_blendState.AlphaDestinationBlend);
            OpenGLDevice.Instance.ColorWriteEnable.Set(_blendState.ColorWriteChannels);
            
            // Apply DepthStencilState
            OpenGLDevice.Instance.ZEnable.Set(_depthStencilState.DepthBufferEnable);
            OpenGLDevice.Instance.ZWriteEnable.Set(_depthStencilState.DepthBufferWriteEnable);
            OpenGLDevice.Instance.DepthFunc.Set(_depthStencilState.DepthBufferFunction);
            OpenGLDevice.Instance.StencilEnable.Set(_depthStencilState.StencilEnable);
            OpenGLDevice.Instance.SeparateStencilEnable.Set(_depthStencilState.TwoSidedStencilMode);
            OpenGLDevice.Instance.StencilRef.Set(_depthStencilState.ReferenceStencil);
            OpenGLDevice.Instance.StencilMask.Set(_depthStencilState.StencilMask);
            OpenGLDevice.Instance.StencilFunc.Set(_depthStencilState.StencilFunction);
            OpenGLDevice.Instance.CCWStencilFunc.Set(_depthStencilState.CounterClockwiseStencilFunction);
            OpenGLDevice.Instance.StencilFail.Set(_depthStencilState.StencilFail);
            OpenGLDevice.Instance.StencilZFail.Set(_depthStencilState.StencilDepthBufferFail);
            OpenGLDevice.Instance.StencilPass.Set(_depthStencilState.StencilPass);
            OpenGLDevice.Instance.CCWStencilFail.Set(_depthStencilState.CounterClockwiseStencilFail);
            OpenGLDevice.Instance.CCWStencilZFail.Set(_depthStencilState.CounterClockwiseStencilDepthBufferFail);
            OpenGLDevice.Instance.CCWStencilPass.Set(_depthStencilState.CounterClockwiseStencilPass);
            
            // Apply RasterizerState
            OpenGLDevice.Instance.CullFrontFace.Set(_rasterizerState.CullMode);
            if (GetRenderTargets().Length > 0)
            {
                // When rendering offscreen the faces change order.
                OpenGLDevice.Instance.CullFrontFace.Set(
                    _rasterizerState.CullMode == CullMode.CullClockwiseFace ?
                        CullMode.CullCounterClockwiseFace :
                        CullMode.CullClockwiseFace
                );
            }
            else
            {
                OpenGLDevice.Instance.CullFrontFace.Set(_rasterizerState.CullMode);
            }

            OpenGLDevice.Instance.GLFillMode.Set(_rasterizerState.FillMode);
            OpenGLDevice.Instance.ScissorTestEnable.Set(_rasterizerState.ScissorTestEnable);

            OpenGLDevice.Instance.DepthBias.Set(_rasterizerState.DepthBias);
            OpenGLDevice.Instance.SlopeScaleDepthBias.Set(_rasterizerState.SlopeScaleDepthBias);

            // TODO: MSAA?

            // If we're not applying shaders then early out now.
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
            for (int i = 0; i < MaxTextureSlots; i += 1)
            {
                SamplerState sampler = SamplerStates[i];
                Texture texture = Textures[i];

                if (sampler != null && texture != null)
                {
                    OpenGLDevice.Instance.Samplers[i].Texture.Set(texture.glTexture);
                    OpenGLDevice.Instance.Samplers[i].Target.Set(texture.glTarget);
                    OpenGLDevice.Instance.Samplers[i].WrapS.Set(sampler.AddressU);
                    OpenGLDevice.Instance.Samplers[i].WrapS.Set(sampler.AddressV);
                    OpenGLDevice.Instance.Samplers[i].WrapS.Set(sampler.AddressW);
                    OpenGLDevice.Instance.Samplers[i].Filter.Set(sampler.Filter);
                }
                else if (texture == null)
                {
                    OpenGLDevice.Instance.Samplers[i].Texture.Set(0);
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
            if (!GraphicsCapabilities.SupportsHardwareInstancing)
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
            GL.DrawArrays(PrimitiveTypeGL(primitiveType),
                          vertexOffset,
                          vertexCount);
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
