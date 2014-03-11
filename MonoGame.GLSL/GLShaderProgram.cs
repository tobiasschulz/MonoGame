/*
 * Copyright (c) 2013-2014 Tobias Schulz, Maximilian Reuter, Pascal Knodel,
 *                         Gerd Augsburg, Christina Erler, Daniel Warzel
 *
 * This source code file is part of Knot3. Copying, redistribution and
 * use of the source code in this file in source and binary forms,
 * with or without modification, are permitted provided that the conditions
 * of the MIT license are met:
 *
 *   Permission is hereby granted, free of charge, to any person obtaining a copy
 *   of this software and associated documentation files (the "Software"), to deal
 *   in the Software without restriction, including without limitation the rights
 *   to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 *   copies of the Software, and to permit persons to whom the Software is
 *   furnished to do so, subject to the following conditions:
 *
 *   The above copyright notice and this permission notice shall be included in all
 *   copies or substantial portions of the Software.
 *
 *   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *   IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *   FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *   AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *   LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *   OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 *   SOFTWARE.
 *
 * See the LICENSE file for full license details of the Knot3 project.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using OpenTK.Graphics.OpenGL;

namespace MonoGame.GLSL
{
    internal class GLShaderProgram
    {
        public GLShader VertexShader { get; private set; }

        public GLShader PixelShader { get; private set; }

        public int Program { get; private set; }

        public GLShaderProgram (GLShader pixel, GLShader vertex)
        {
            VertexShader = vertex;
            PixelShader = pixel;
            
            Program = GL.CreateProgram ();
            GraphicsExtensions.CheckGLError ();

            GL.AttachShader (Program, vertex.ShaderHandle);
            GraphicsExtensions.CheckGLError ();

            GL.AttachShader (Program, pixel.ShaderHandle);
            GraphicsExtensions.CheckGLError ();

            //vertexShader.BindVertexAttributes(program);

            GL.LinkProgram (Program);
            GraphicsExtensions.CheckGLError ();

            GL.UseProgram (Program);
            GraphicsExtensions.CheckGLError ();

            var linked = 0;
            GL.GetProgram (Program, ProgramParameter.LinkStatus, out linked);
            GraphicsExtensions.LogGLError ("VertexShaderCache.Link(), GL.GetProgram");
            if (linked == 0) {
                var log = GL.GetProgramInfoLog (Program);
                Console.WriteLine (log);
                GL.DetachShader (Program, vertex.ShaderHandle);
                GL.DetachShader (Program, pixel.ShaderHandle);
                GL.DeleteProgram (Program);
                throw new InvalidOperationException ("Unable to link effect program");
            }
        }

        public void Bind ()
        {
            GL.UseProgram (Program);
            GraphicsExtensions.CheckGLError ();
        }

        public void Unbind ()
        {
            GL.UseProgram (0);
            GraphicsExtensions.CheckGLError ();
        }
    }
}
