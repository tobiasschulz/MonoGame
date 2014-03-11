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
    public class GLEffect : IEffectMatrices
    {
        private GLShaderProgram ShaderProgram;

        public Matrix Projection { set { Parameters.SetMatrix ("Projection", value); } get { return Parameters.GetMatrix ("Projection"); } }

        public Matrix View { set { Parameters.SetMatrix ("View", value); } get { return Parameters.GetMatrix ("View"); } }

        public Matrix World { set { Parameters.SetMatrix ("World", value); } get { return Parameters.GetMatrix ("World"); } }

        public GLParamaterCollection Parameters { get; private set; }

        private GLEffect (GLShaderProgram shaderProgram)
        {
            ShaderProgram = shaderProgram;
            Parameters = new GLParamaterCollection ();
        }

        public static GLEffect FromFiles (string pixelShaderFilename, string vertexShaderFilename)
        {
            GLShader pixelShader = new GLShader (ShaderStage.Pixel, File.ReadAllText (pixelShaderFilename));
            GLShader vertexShader = new GLShader (ShaderStage.Vertex, File.ReadAllText (vertexShaderFilename));
            GLShaderProgram shaderProgram = new GLShaderProgram (vertex: vertexShader, pixel: pixelShader);
            return new GLEffect (shaderProgram: shaderProgram);
        }

        public void Draw (Model model)
        {
            int boneCount = model.Bones.Count;

            // Look up combined bone matrices for the entire model.
            Matrix[] sharedDrawBoneMatrices = new Matrix [boneCount];
            model.CopyAbsoluteBoneTransformsTo (sharedDrawBoneMatrices);

            // Draw the model.
            foreach (ModelMesh mesh in model.Meshes) {
                //Matrix world = sharedDrawBoneMatrices [mesh.ParentBone.Index] * World;
                Draw (mesh, sharedDrawBoneMatrices [mesh.ParentBone.Index]);
            }
        }

        public void Draw (ModelMesh mesh, Matrix transform)
        {
            foreach (ModelMeshPart meshPart in mesh.MeshParts) {
                if (meshPart.PrimitiveCount > 0) {
                    Draw (meshPart, ref transform);
                }
            }
        }

        private Dictionary<ModelMeshPart, VertexIndexBuffers> bufferCache = new Dictionary<ModelMeshPart, VertexIndexBuffers> ();

        private int AttribLocation = -1;

        private void Draw (ModelMeshPart meshPart, ref Matrix transform)
        {
            GL.EnableClientState(ArrayCap.VertexArray);
            GraphicsExtensions.CheckGLError ();

            int verticesBuffer;
            int indicesBuffer;
            if (!bufferCache.ContainsKey (meshPart)) {
                List<Vector3> vertices = new List<Vector3> ();
                List<TriangleVertexIndices> indices = new List<TriangleVertexIndices> ();
                VertexHelper.ExtractModelMeshPartData (meshPart, ref transform, vertices, indices);
                float[] verticesData = new float[vertices.Count * 3];
                for (int i = 0; i < vertices.Count; ++i) {
                    verticesData [i * 3 + 0] = vertices [i].X;
                    verticesData [i * 3 + 1] = vertices [i].Y;
                    verticesData [i * 3 + 2] = vertices [i].Z;
                }
                int[] indicesData = new int[indices.Count * 3];
                for (int i = 0; i < indices.Count; ++i) {
                    indicesData [i * 3 + 0] = indices [i].A;
                    indicesData [i * 3 + 1] = indices [i].B;
                    indicesData [i * 3 + 2] = indices [i].C;
                }

                //Create a new VBO and use the variable id to store the VBO id
                GL.GenBuffers (1, out verticesBuffer);
                /* Make the new VBO active */
                GL.BindBuffer (BufferTarget.ArrayBuffer, verticesBuffer);
                GraphicsExtensions.CheckGLError ();
                /* Upload vertex data to the video device */
                GL.BufferData (BufferTarget.ArrayBuffer, (IntPtr)(verticesData.Length * sizeof(float)), verticesData, BufferUsageHint.StaticDraw);
                GraphicsExtensions.CheckGLError ();
                GL.BindBuffer (BufferTarget.ArrayBuffer, 0);

                //Create a new VBO and use the variable id to store the VBO id
                GL.GenBuffers (1, out indicesBuffer);
                /* Make the new VBO active */
                GL.BindBuffer (BufferTarget.ElementArrayBuffer, indicesBuffer);
                GraphicsExtensions.CheckGLError ();
                /* Upload vertex data to the video device */
                GL.BufferData (BufferTarget.ElementArrayBuffer, (IntPtr)(indicesData.Length * sizeof(int)), indicesData, BufferUsageHint.StaticDraw);
                GraphicsExtensions.CheckGLError ();
                GL.BindBuffer (BufferTarget.ArrayBuffer, 0);

                bufferCache [meshPart] = new VertexIndexBuffers {
                    VerticesBuffer = verticesBuffer,
                    IndicesBuffer = indicesBuffer
                };
            } else {
                verticesBuffer = bufferCache [meshPart].VerticesBuffer;
                indicesBuffer = bufferCache [meshPart].IndicesBuffer;
            }

            ShaderProgram.Bind ();
            Parameters.Apply (program: ShaderProgram);

            /* Specify that our coordinate data is going into attribute index 0(shaderAttribute), and contains three floats per vertex */
            if (AttribLocation == -1)
                AttribLocation = GL.GetAttribLocation (ShaderProgram.Program, "Position");

            /* Make the new VBO active. */
            GL.BindBuffer (BufferTarget.ArrayBuffer, verticesBuffer);
            GraphicsExtensions.CheckGLError ();
            GL.VertexAttribPointer (AttribLocation, 3, VertexAttribPointerType.Float, false, 0, 0);
            GraphicsExtensions.CheckGLError ();
            GL.BindBuffer (BufferTarget.ArrayBuffer, 0);

            GL.BindBuffer (BufferTarget.ElementArrayBuffer, indicesBuffer);
            GraphicsExtensions.CheckGLError ();
            /* Actually draw the triangle, giving the number of vertices provided by invoke glDrawArrays
               while telling that our data is a triangle and we want to draw 0-3 vertexes 
            */
            //GL.DrawArrays (BeginMode.Triangles, vertexOffset, numVertices);


            GL.ClearColor(0.1f,0.5f,0.6f,1.0f);
            
            GL.EnableVertexAttribArray (AttribLocation);
            GraphicsExtensions.CheckGLError ();

            Console.WriteLine (
                "mode: " + BeginMode.Triangles + ", " +
                "start: " + meshPart.VertexOffset + ", " +
                "end: " + (meshPart.VertexOffset + meshPart.NumVertices) + ", " +
                "count: " + (meshPart.PrimitiveCount * 3) + ", " +
                "type: " + DrawElementsType.UnsignedInt + ", " +
                "indices: " + (IntPtr)(meshPart.StartIndex * 4)
            );
            GL.DrawRangeElements (
                mode: BeginMode.Triangles,
                start: meshPart.VertexOffset,
                end: meshPart.VertexOffset + meshPart.NumVertices,
                count: meshPart.PrimitiveCount * 3,
                type: DrawElementsType.UnsignedInt,
                indices: (IntPtr)(meshPart.StartIndex * 4)
            );

            // Check for errors in the debug context
            GraphicsExtensions.CheckGLError ();
            
            GL.DisableVertexAttribArray (AttribLocation);
            GraphicsExtensions.CheckGLError ();

            
            GL.Finish();
            ShaderProgram.Unbind ();
        }
        
        private void DrawCube()
        {
            GL.Begin(BeginMode.Quads);

            GL.Color3(System.Drawing.Color.Silver);
            GL.Vertex3(-1.0f, -1.0f, -1.0f);
            GL.Vertex3(-1.0f, 1.0f, -1.0f);
            GL.Vertex3(1.0f, 1.0f, -1.0f);
            GL.Vertex3(1.0f, -1.0f, -1.0f);

            GL.Color3(System.Drawing.Color.Honeydew);
            GL.Vertex3(-1.0f, -1.0f, -1.0f);
            GL.Vertex3(1.0f, -1.0f, -1.0f);
            GL.Vertex3(1.0f, -1.0f, 1.0f);
            GL.Vertex3(-1.0f, -1.0f, 1.0f);

            GL.Color3(System.Drawing.Color.Moccasin);

            GL.Vertex3(-1.0f, -1.0f, -1.0f);
            GL.Vertex3(-1.0f, -1.0f, 1.0f);
            GL.Vertex3(-1.0f, 1.0f, 1.0f);
            GL.Vertex3(-1.0f, 1.0f, -1.0f);

            GL.Color3(System.Drawing.Color.IndianRed);
            GL.Vertex3(-1.0f, -1.0f, 1.0f);
            GL.Vertex3(1.0f, -1.0f, 1.0f);
            GL.Vertex3(1.0f, 1.0f, 1.0f);
            GL.Vertex3(-1.0f, 1.0f, 1.0f);

            GL.Color3(System.Drawing.Color.PaleVioletRed);
            GL.Vertex3(-1.0f, 1.0f, -1.0f);
            GL.Vertex3(-1.0f, 1.0f, 1.0f);
            GL.Vertex3(1.0f, 1.0f, 1.0f);
            GL.Vertex3(1.0f, 1.0f, -1.0f);

            GL.Color3(System.Drawing.Color.ForestGreen);
            GL.Vertex3(1.0f, -1.0f, -1.0f);
            GL.Vertex3(1.0f, 1.0f, -1.0f);
            GL.Vertex3(1.0f, 1.0f, 1.0f);
            GL.Vertex3(1.0f, -1.0f, 1.0f);

            GL.End();
        }

        private struct VertexIndexBuffers
        {
            public int VerticesBuffer;
            public int IndicesBuffer;
        };
        /*public void Draw (ModelMesh mesh)
        {
            foreach (ModelMeshPart part in mesh.MeshParts) {
                if (part.PrimitiveCount > 0) {
                    GraphicsDevice.SetVertexBuffer (part.VertexBuffer);
                    GraphicsDevice.Indices = part.IndexBuffer;
                    GraphicsDevice.VertexShader = Shaders [0].VertexShader;
                    //Console.WriteLine (Shaders [0].VertexShader);
                    GraphicsDevice.PixelShader = Shaders [0].PixelShader;
                    //part.Effect.CurrentTechnique.Passes [0].Apply ();
                    GraphicsDevice.DrawIndexedPrimitives (PrimitiveType.TriangleList, part.VertexOffset, 0, part.NumVertices, part.StartIndex, part.PrimitiveCount);
                    //DrawIndexedPrimitives (PrimitiveType.TriangleList, part.VertexOffset, 0, part.NumVertices, part.StartIndex, part.PrimitiveCount);
                }
            }
        }

        /// <summary>
        /// Draw geometry by indexing into the vertex buffer.
        /// </summary>
        /// <param name="primitiveType">The type of primitives in the index buffer.</param>
        /// <param name="baseVertex">Used to offset the vertex range indexed from the vertex buffer.</param>
        /// <param name="minVertexIndex">A hint of the lowest vertex indexed relative to baseVertex.</param>
        /// <param name="numVertices">An hint of the maximum vertex indexed.</param>
        /// <param name="startIndex">The index within the index buffer to start drawing from.</param>
        /// <param name="primitiveCount">The number of primitives to render from the index buffer.</param>
        /// <remarks>Note that minVertexIndex and numVertices are unused in MonoGame and will be ignored.</remarks>
        public void DrawIndexedPrimitives (
            PrimitiveType primitiveType,
            int baseVertex,
            int minVertexIndex,
            int numVertices,
            int startIndex,
            int primitiveCount
        )
        {
                pass.Apply (parameters: Parameters);

                // Unsigned short or unsigned int?
                bool shortIndices = GraphicsDevice.Indices.IndexElementSize == IndexElementSize.SixteenBits;

                // Set up the vertex buffers
                foreach (VertexBufferBinding vertBuffer in GraphicsDevice.vertexBufferBindings) {
                    if (vertBuffer.VertexBuffer != null) {
                        OpenGLDevice.Instance.BindVertexBuffer (vertBuffer.VertexBuffer.Handle);
                        vertBuffer.VertexBuffer.VertexDeclaration.Apply (
                            pass.VertexShader,
                            (IntPtr)(vertBuffer.VertexBuffer.VertexDeclaration.VertexStride * (vertBuffer.VertexOffset + baseVertex))
                        );
                    }
                }

                // Enable the appropriate vertex attributes.
                OpenGLDevice.Instance.FlushGLVertexAttributes ();

                // Bind the index buffer
                OpenGLDevice.Instance.BindIndexBuffer (GraphicsDevice.Indices.Handle);

                // Draw!
                GL.DrawRangeElements (
                    PrimitiveTypeGL (primitiveType),
                    minVertexIndex,
                    minVertexIndex + numVertices,
                    GetElementCountArray (primitiveType, primitiveCount),
                    shortIndices ? DrawElementsType.UnsignedShort : DrawElementsType.UnsignedInt,
                    (IntPtr)(startIndex * (shortIndices ? 2 : 4))
                );

                // Check for errors in the debug context
                GraphicsExtensions.CheckGLError ();
        }

        #region Private XNA->GL Conversion Methods

        private static int GetElementCountArray(PrimitiveType primitiveType, int primitiveCount)
        {
            switch (primitiveType)
            {
                case PrimitiveType.LineList:
                return primitiveCount * 2;
                case PrimitiveType.LineStrip:
                return primitiveCount + 1;
                case PrimitiveType.TriangleList:
                return primitiveCount * 3;
                case PrimitiveType.TriangleStrip:
                return 3 + (primitiveCount - 1);
            }

            throw new NotSupportedException();
        }

        private static BeginMode PrimitiveTypeGL(PrimitiveType primitiveType)
        {
            switch (primitiveType)
            {
                case PrimitiveType.LineList:
                return BeginMode.Lines;
                case PrimitiveType.LineStrip:
                return BeginMode.LineStrip;
                case PrimitiveType.TriangleList:
                return BeginMode.Triangles;
                case PrimitiveType.TriangleStrip:
                return BeginMode.TriangleStrip;
            }

            throw new ArgumentException();
        }

        #endregion*/
    }
}
