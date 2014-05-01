// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.IO;
using System.Linq;

#if MONOMAC
using MonoMac.OpenGL;
#elif WINDOWS || LINUX || SDL2
using OpenTK.Graphics.OpenGL;
#elif GLES
using System.Text;
using OpenTK.Graphics.ES20;
using ShaderType = OpenTK.Graphics.ES20.All;
using ShaderParameter = OpenTK.Graphics.ES20.All;
using TextureUnit = OpenTK.Graphics.ES20.All;
using TextureTarget = OpenTK.Graphics.ES20.All;
#endif

namespace Microsoft.Xna.Framework.Graphics
{
    internal partial class Shader
    {
        // The shader handle.
        private int _shaderHandle = -1;

        // We keep this around for recompiling on context lost and debugging.
        private string _glslCode;

        private struct Attribute
        {
            public VertexElementUsage usage;
            public int index;
            public string name;
            public int location;
        }

        private Attribute[] _attributes;

        private void PlatformConstruct(BinaryReader reader, bool isVertexShader, byte[] shaderBytecode, ref string readableCode)
        {
            readableCode += "#monogame BeginShader("+EffectUtilities.Params(
                "stage", (isVertexShader ? "vertex" : "pixel"),
                "constantBuffers", EffectUtilities.Join(CBuffers)
                )+")\n";

            for (var s = 0; s < Samplers.Length; s++)
            {
                readableCode += "#monogame Sampler("+EffectUtilities.Params(
                    "name", Samplers[s].name,
                    "type", Samplers[s].type,
                    "textureSlot", Samplers[s].textureSlot,
                    "samplerSlot", Samplers[s].samplerSlot,
                    "parameter", Samplers[s].parameter
                    )+")\n";
            }

            _glslCode = System.Text.Encoding.ASCII.GetString(shaderBytecode);

            HashKey = MonoGame.Utilities.Hash.ComputeHash(shaderBytecode);

            var attributeCount = (int)reader.ReadByte();
            _attributes = new Attribute[attributeCount];
            for (var a = 0; a < attributeCount; a++)
            {
                _attributes[a].name = reader.ReadString();
                _attributes[a].usage = (VertexElementUsage)reader.ReadByte();
                _attributes[a].index = reader.ReadByte();
<<<<<<< HEAD
                _attributes[a].format = reader.ReadInt16();

                readableCode += "#monogame Attribute("+EffectUtilities.Params(
                    "name", _attributes[a].name,
                    "usage", _attributes[a].usage,
                    "index", _attributes[a].index
                    // "format", _attributes[a].format // seems to be always 0
                    )+")\n";
=======
                reader.ReadInt16(); // format, unused
>>>>>>> monogame-sdl2
            }

            string readableGlslCode = _glslCode;
            // remove posFixup
            readableGlslCode = string.Join("\n", from line in readableGlslCode.Split(new string []{"\n"}, StringSplitOptions.None) where !line.Contains("posFixup") select line);

            readableCode += "\n";
            readableCode += readableGlslCode;
            readableCode += "\n";
            readableCode += "#monogame EndShader()\n";
        }

        internal int GetShaderHandle()
        {
            // If the shader has already been created then return it.
            if (_shaderHandle != -1)
                return _shaderHandle;
            
            //
            _shaderHandle = GL.CreateShader(Stage == ShaderStage.Vertex ? ShaderType.VertexShader : ShaderType.FragmentShader);
#if GLES
			GL.ShaderSource(_shaderHandle, 1, new string[] { _glslCode }, (int[])null);
#else
            GL.ShaderSource(_shaderHandle, _glslCode);
#endif
            GL.CompileShader(_shaderHandle);

            var compiled = 0;
#if GLES && !ANGLE
			GL.GetShader(_shaderHandle, ShaderParameter.CompileStatus, ref compiled);
#else
            GL.GetShader(_shaderHandle, ShaderParameter.CompileStatus, out compiled);
#endif
            if (compiled == (int)All.False)
            {
#if GLES && !ANGLE
                string log = "";
                int length = 0;
				GL.GetShader(_shaderHandle, ShaderParameter.InfoLogLength, ref length);
                if (length > 0)
                {
                    var logBuilder = new StringBuilder(length);
					GL.GetShaderInfoLog(_shaderHandle, length, ref length, logBuilder);
                    log = logBuilder.ToString();
                }
#else
                var log = GL.GetShaderInfoLog(_shaderHandle);
#endif
                Console.WriteLine(log);

                if (GL.IsShader(_shaderHandle))
                {
                    GL.DeleteShader(_shaderHandle);
                }
                _shaderHandle = -1;

                throw new InvalidOperationException("Shader Compilation Failed");
            }

            return _shaderHandle;
        }

        internal void GetVertexAttributeLocations(int program)
        {
            for (int i = 0; i < _attributes.Length; ++i)
            {
                _attributes[i].location = GL.GetAttribLocation(program, _attributes[i].name);
            }
        }

        internal int GetAttribLocation(VertexElementUsage usage, int index)
        {
            for (int i = 0; i < _attributes.Length; ++i)
            {
                if ((_attributes[i].usage == usage) && (_attributes[i].index == index))
                    return _attributes[i].location;
            }
            return -1;
        }

        internal void ApplySamplerTextureUnits(int program)
        {
            // Assign the texture unit index to the sampler uniforms.
            foreach (var sampler in Samplers)
            {
                var loc = GL.GetUniformLocation(program, sampler.name);
                if (loc != -1)
                {
                    GL.Uniform1(loc, sampler.textureSlot);
                }
            }
        }

        private void PlatformGraphicsDeviceResetting()
        {
            if (_shaderHandle != -1)
            {
                if (GL.IsShader(_shaderHandle))
                {
                    GL.DeleteShader(_shaderHandle);
                }
                _shaderHandle = -1;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                GraphicsDevice.AddDisposeAction(() =>
                {
                    if (_shaderHandle != -1)
                    {
                        if (GL.IsShader(_shaderHandle))
                        {
                            GL.DeleteShader(_shaderHandle);
                        }
                        _shaderHandle = -1;
                    }
                });
            }

            base.Dispose(disposing);
        }
    }
}
