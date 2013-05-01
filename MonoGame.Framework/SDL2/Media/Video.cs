 #region License
// /*
// Microsoft Public License (Ms-PL)
// MonoGame - Copyright © 2009 The MonoGame Team
// 
// All rights reserved.
// 
// This license governs use of the accompanying software. If you use the software, you accept this license. If you do not
// accept the license, do not use the software.
// 
// 1. Definitions
// The terms "reproduce," "reproduction," "derivative works," and "distribution" have the same meaning here as under 
// U.S. copyright law.
// 
// A "contribution" is the original software, or any additions or changes to the software.
// A "contributor" is any person that distributes its contribution under this license.
// "Licensed patents" are a contributor's patent claims that read directly on its contribution.
// 
// 2. Grant of Rights
// (A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, 
// each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution, prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
// (B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, 
// each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or derivative works of the contribution in the software.
// 
// 3. Conditions and Limitations
// (A) No Trademark License- This license does not grant you rights to use any contributors' name, logo, or trademarks.
// (B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software, 
// your patent license from such contributor to the software ends automatically.
// (C) If you distribute any portion of the software, you must retain all copyright, patent, trademark, and attribution 
// notices that are present in the software.
// (D) If you distribute any portion of the software in source code form, you may do so only under this license by including 
// a complete copy of this license with your distribution. If you distribute any portion of the software in compiled or object 
// code form, you may only do so under a license that complies with this license.
// (E) The software is licensed "as-is." You bear the risk of using it. The contributors give no express warranties, guarantees
// or conditions. You may have additional consumer rights under your local laws which this license cannot change. To the extent
// permitted under your local laws, the contributors exclude the implied warranties of merchantability, fitness for a particular
// purpose and non-infringement.
// */
#endregion License 

#region VideoPlayer Graphics Define
#if SDL2
#define VIDEOPLAYER_OPENGL
#endif
#endregion

using System;
using System.IO;
using System.Threading;

using Microsoft.Xna.Framework.Graphics;

namespace Microsoft.Xna.Framework.Media
{
    public sealed class Video : IDisposable
    {
        #region Private Variables: Video Implementation
		private string _fileName;
		private Color _backColor = Color.Black;
        private bool disposed;
		#endregion
     
        #region Public Properties
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
         
        public string FileName
        {
            get 
            {
                return _fileName;
            }
        }
        
        private float INTERNAL_fps = 0.0f;
        public float FramesPerSecond
        {
            get
            {
                return INTERNAL_fps;
            }
            internal set
            {
                INTERNAL_fps = value;
            }
        }
        
        // FIXME: This is hacked, look this up in VideoPlayer.
        public TimeSpan Duration
        {
            get;
            internal set;
        }
        #endregion
        
        #region Internal Video Constructor
		internal Video(string FileName)
		{
            // Check out the file...
			_fileName = Normalize(FileName);
            if (_fileName == null)
            {
                throw new Exception("File " + FileName + " does not exist!");
            }
		}
        #endregion
		
        #region File name normalizer
		internal static string Normalize(string FileName)
		{
			if (File.Exists(FileName))
            {
				return FileName;
			}
            
			// Check the file extension
			if (!string.IsNullOrEmpty(Path.GetExtension(FileName)))
			{
				return null;
			}
			
			// Concat the file name with valid extensions
			if (File.Exists(FileName + ".ogv"))
            {
				return FileName + ".ogv";
            }
			if (File.Exists(FileName + ".ogg"))
            {
				return FileName + ".ogg";
            }
			
			return null;
		}
        #endregion
        
        #region Disposal Method
		public void Dispose()
		{
            disposed = true;
		}
        #endregion
    }
}
