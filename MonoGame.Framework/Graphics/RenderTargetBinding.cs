#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

using System;

namespace Microsoft.Xna.Framework.Graphics
{
	// http://msdn.microsoft.com/en-us/library/ff434403.aspx
	public struct RenderTargetBinding
    {
        #region Public Properties

        public Texture RenderTarget
        {
            get { return _renderTarget; }
        }

        public int ArraySlice
        {
            get { return _arraySlice; }
        }

        #endregion

        #region Private Variables

        private readonly Texture _renderTarget;
        private readonly int _arraySlice;

        #endregion

        #region Public Constructors

        public RenderTargetBinding(RenderTarget2D renderTarget)
		{
			if (renderTarget == null) 
				throw new ArgumentNullException("renderTarget");

			_renderTarget = renderTarget;
            _arraySlice = (int)CubeMapFace.PositiveX;
		}

        public RenderTargetBinding(RenderTargetCube renderTarget, CubeMapFace cubeMapFace)
        {
            if (renderTarget == null)
                throw new ArgumentNullException("renderTarget");
            if (cubeMapFace < CubeMapFace.PositiveX || cubeMapFace > CubeMapFace.NegativeZ)
                throw new ArgumentOutOfRangeException("cubeMapFace");

            _renderTarget = renderTarget;
            _arraySlice = (int)cubeMapFace;
        }

        #endregion

        #region Public Static Conversion Operator

        public static implicit operator RenderTargetBinding(RenderTarget2D renderTarget)
        {
            return new RenderTargetBinding(renderTarget);
        }

        #endregion
    }
}
