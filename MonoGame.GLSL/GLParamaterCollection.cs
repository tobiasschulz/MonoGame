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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OpenTK.Graphics.OpenGL;

namespace MonoGame.GLSL
{
    public class GLParamaterCollection
    {
        private Dictionary<string, float> parametersFloat = new Dictionary<string, float> ();
        private Dictionary<string, Matrix> parametersMatrix = new Dictionary<string, Matrix> ();

        internal GLParamaterCollection ()
        {
        }

        public float GetFloat (string name)
        {
            if (parametersFloat.ContainsKey (name))
                return parametersFloat [name];
            else
                return 0f;
        }

        public void SetFloat (string name, float value)
        {
            parametersFloat [name] = value;
        }

        public Matrix GetMatrix (string name)
        {
            if (parametersMatrix.ContainsKey (name))
                return parametersMatrix [name];
            else
                return default (Matrix);
        }

        public void SetMatrix (string name, Matrix value)
        {
            Console.WriteLine ("set name: " + name + ", value: " + value);
            parametersMatrix [name] = value;
        }

        internal void Apply (GLShaderProgram program)
        {
            foreach (KeyValuePair<string, float> pair in parametersFloat) {
                int loc = GL.GetUniformLocation (program: program.Program, name: pair.Key);
                if (loc != -1) {
                    GL.Uniform1 (location: loc, v0: pair.Value);
                }
            }

            // Console.WriteLine ("parametersMatrix.Count=" + parametersMatrix.Count);
            string[] keys = parametersMatrix.Keys.ToArray ();
            for (int i = 0; i < keys.Length; ++i) {
                string name = keys [i];
                Matrix matrix = parametersMatrix [name];
                int loc = GL.GetUniformLocation (program: program.Program, name: name);
                if (loc != -1) {
                    Console.WriteLine ("name: " + name + ", loc: " + loc);
                    OpenTK.Matrix4 matrixTK = new OpenTK.Matrix4 (
                        matrix.M11, matrix.M12, matrix.M13, matrix.M14,
                        matrix.M21, matrix.M22, matrix.M23, matrix.M24,
                        matrix.M31, matrix.M32, matrix.M33, matrix.M34,
                        matrix.M41, matrix.M42, matrix.M43, matrix.M44
                    );
                    GL.UniformMatrix4 (location: loc, count: 1, transpose: false, value: ref matrixTK.Row0.X);

                }
            }
        }
    }
}
