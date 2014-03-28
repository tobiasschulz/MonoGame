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
using System.Collections.ObjectModel;
#endregion

namespace Microsoft.Xna.Framework
{
    public sealed class GameComponentCollection : Collection<IGameComponent>
    {
        #region Events

        public event EventHandler<GameComponentCollectionEventArgs> ComponentAdded;

        public event EventHandler<GameComponentCollectionEventArgs> ComponentRemoved;

        #endregion

        #region Protected Methods

        protected override void ClearItems()
        {
            for (int i = 0; i < base.Count; i++)
            {
                this.OnComponentRemoved(new GameComponentCollectionEventArgs(base[i]));
            }
            base.ClearItems();
        }

        protected override void InsertItem(int index, IGameComponent item)
        {
            if (base.IndexOf(item) != -1)
            {
                throw new ArgumentException("Cannot Add Same Component Multiple Times");
            }
            base.InsertItem(index, item);
            if (item != null)
            {
                this.OnComponentAdded(new GameComponentCollectionEventArgs(item));
            }
        }

        protected override void RemoveItem(int index)
        {
            IGameComponent gameComponent = base[index];
            base.RemoveItem(index);
            if (gameComponent != null)
            {
                this.OnComponentRemoved(new GameComponentCollectionEventArgs(gameComponent));
            }
        }

        protected override void SetItem(int index, IGameComponent item)
        {
            throw new NotSupportedException();
        }

        #endregion

        #region Private Methods

        private void OnComponentAdded(GameComponentCollectionEventArgs eventArgs)
        {
            if (this.ComponentAdded != null)
            {
                this.ComponentAdded(this, eventArgs);
            }
        }

        private void OnComponentRemoved(GameComponentCollectionEventArgs eventArgs)
        {
            if (this.ComponentRemoved != null)
            {
                this.ComponentRemoved(this, eventArgs);
            }
        }

        #endregion
    }
}

