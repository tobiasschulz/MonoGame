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
    public class GameComponent : IGameComponent, IUpdateable, IComparable<GameComponent>, IDisposable
    {
        bool _enabled = true;
        int _updateOrder;

        public Game Game { get; private set; }

        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                if (_enabled != value)
                {
                    _enabled = value;
                    if (this.EnabledChanged != null)
                        this.EnabledChanged(this, EventArgs.Empty);
                    OnEnabledChanged(this, null);
                }
            }
        }

        public int UpdateOrder
        {
            get { return _updateOrder; }
            set
            {
                if (_updateOrder != value)
                {
                    _updateOrder = value;
                    if (this.UpdateOrderChanged != null)
                        this.UpdateOrderChanged(this, EventArgs.Empty);
                    OnUpdateOrderChanged(this, null);
                }
            }
        }

        public event EventHandler<EventArgs> EnabledChanged;
        public event EventHandler<EventArgs> UpdateOrderChanged;

        public GameComponent(Game game)
        {
            this.Game = game;
        }

        ~GameComponent()
        {
            Dispose(false);
        }

        public virtual void Initialize() { }

        public virtual void Update(GameTime gameTime) { }

        protected virtual void OnUpdateOrderChanged(object sender, EventArgs args) { }

        protected virtual void OnEnabledChanged(object sender, EventArgs args) { }

        /// <summary>
        /// Shuts down the component.
        /// </summary>
        protected virtual void Dispose(bool disposing) { }
        
        /// <summary>
        /// Shuts down the component.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #region IComparable<GameComponent> Members
        // TODO: Should be removed, as it is not part of XNA 4.0
        public int CompareTo(GameComponent other)
        {
            return other.UpdateOrder - this.UpdateOrder;
        }

        #endregion
    }
}
