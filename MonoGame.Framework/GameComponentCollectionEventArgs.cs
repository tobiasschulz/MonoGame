#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

using System;

namespace Microsoft.Xna.Framework
{
    public class GameComponentCollectionEventArgs : EventArgs
    {
        private IGameComponent _gameComponent;

        public GameComponentCollectionEventArgs(IGameComponent gameComponent)
        {
            _gameComponent = gameComponent;
        }

        public IGameComponent GameComponent
        {
            get
            {
                return _gameComponent;
            }
        }
    }
}

