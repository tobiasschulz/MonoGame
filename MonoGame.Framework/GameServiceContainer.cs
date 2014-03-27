#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */

/*
MIT License
Copyright (C) 2006 The Mono.Xna Team

All rights reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
#endregion

#region Using Statements
using System;
using System.Collections.Generic;
#endregion

namespace Microsoft.Xna.Framework
{
    public class GameServiceContainer : IServiceProvider
    {
        #region Private Fields

        Dictionary<Type, object> services;

        #endregion

        #region Public Constructors

        public GameServiceContainer()
        {
            services = new Dictionary<Type, object>();
        }

        #endregion

        #region Public Methods

        public void AddService(Type type, object provider)
        {
            if (type == null)
                throw new ArgumentNullException("type");
            if (provider == null)
                throw new ArgumentNullException("provider");
            if (!type.IsAssignableFrom(provider.GetType()))
                throw new ArgumentException("The provider does not match the specified service type!");

            services.Add(type, provider);
        }

        public object GetService(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");
						
            object service;
            if (services.TryGetValue(type, out service))
                return service;

            return null;
        }

        public void RemoveService(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            services.Remove(type);
        }

        #endregion
    }
}
