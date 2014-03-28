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
#endregion

namespace Microsoft.Xna.Framework
{
    public class GameComponentCollectionEventArgs : EventArgs
    {
        #region Public Properties

        public IGameComponent GameComponent
        {
            get
            {
                return _gameComponent;
            }
        }

        #endregion

        #region Private Variables

        private IGameComponent _gameComponent;

        #endregion

        #region Public Constructors

        public GameComponentCollectionEventArgs(IGameComponent gameComponent)
        {
            _gameComponent = gameComponent;
        }

        #endregion
    }
}

