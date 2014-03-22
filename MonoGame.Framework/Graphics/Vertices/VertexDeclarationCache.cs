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
using System.Collections.Generic;
using System.Linq;
using System.Text;
#endregion

namespace Microsoft.Xna.Framework.Graphics
{
    /// <summary>
    /// Helper class which ensures we only lookup a vertex 
    /// declaration for a particular type once.
    /// </summary>
    /// <typeparam name="T">A vertex structure which implements IVertexType.</typeparam>
    internal class VertexDeclarationCache<T>
        where T : struct, IVertexType
    {

        #region Public Static Property

        static public VertexDeclaration VertexDeclaration
        {
            get
            {
                if (_cached == null)
                    _cached = VertexDeclaration.FromType(typeof(T));

                return _cached;
            }
        }

        #endregion

        #region Private Static Variable

        static private VertexDeclaration _cached;

        #endregion

    }
}
