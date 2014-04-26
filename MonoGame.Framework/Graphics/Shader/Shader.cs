// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System.IO;
using System.Collections.Generic;
using System;

namespace Microsoft.Xna.Framework.Graphics
{
    internal enum SamplerType
    {
        Sampler2D = 0,
        SamplerCube = 1,
        SamplerVolume = 2,
        Sampler1D = 3,
    }

    // TODO: We should convert the sampler info below 
    // into the start of a Shader reflection API.

    internal struct SamplerInfo
    {
        public SamplerType type;
        public int textureSlot;
        public int samplerSlot;
        public string name;
		public SamplerState state;

        // TODO: This should be moved to EffectPass.
        public int parameter;
    }

    internal partial class Shader : GraphicsResource
	{
        /// <summary>
        /// A hash value which can be used to compare shaders.
        /// </summary>
        internal int HashKey { get; private set; }

        public SamplerInfo[] Samplers { get; private set; }

	    public int[] CBuffers { get; private set; }

        public ShaderStage Stage { get; private set; }
		
        internal Shader(GraphicsDevice device, ShaderStage stage, int[] constantBuffers, string[] lines, ref int g,
                        ref List<ConstantBuffer> constantBuffersList, ref List<EffectParameter> effectParameterList)
        {
            Stage = stage;
            CBuffers = constantBuffers;
            List<SamplerInfo> SamplerList = new List<SamplerInfo>();
            List<Attribute> AttributeList = new List<Attribute>();
            string preMainCode = "";
            
            while (g < lines.Length) {
                string command;
                if (EffectUtilities.MatchesMetaDeclaration (lines [g], "Sampler", out command)) {
                    SamplerInfo sampler = new SamplerInfo ();
                    sampler.name = EffectUtilities.ParseParam (command, "name", "");
                    string typeStr = EffectUtilities.ParseParam (command, "type", "");
                    sampler.type = typeStr == "Sampler1D" ? SamplerType.Sampler1D
                            : typeStr == "Sampler2D" ? SamplerType.Sampler2D
                            : typeStr == "SamplerCube" ? SamplerType.SamplerCube
                            : SamplerType.SamplerVolume;
                    sampler.textureSlot = EffectUtilities.ParseParam (command, "textureSlot", 0);
                    sampler.samplerSlot = EffectUtilities.ParseParam (command, "samplerSlot", 0);
                    sampler.parameter = EffectUtilities.ParseParam (command, "parameter", 0);
                    SamplerList.Add (sampler);
                    ++g;
                }
                else if (EffectUtilities.MatchesMetaDeclaration (lines [g], "Attribute", out command)) {
                    int[] indices = EffectUtilities.ParseParam (command, "index", new int[] { 0 });
                    if (indices.Length == 4) {
                        for (int i = 0; i < 4; ++i) {
                            Attribute attribute = new Attribute ();
                            attribute.name = EffectUtilities.ParseParam (command, "name", "") + i;
                            string usageStr = EffectUtilities.ParseParam (command, "usage", "");
                            attribute.usage = (VertexElementUsage)Enum.Parse (typeof(VertexElementUsage), usageStr);
                            attribute.index = indices [i];
                            attribute.format = (short)EffectUtilities.ParseParam (command, "format", 0);
                            AttributeList.Add (attribute);
                        }
                    }
                    else if (indices.Length == 1) {
                        Attribute attribute = new Attribute ();
                        attribute.name = EffectUtilities.ParseParam (command, "name", "");
                        string usageStr = EffectUtilities.ParseParam (command, "usage", "");
                        attribute.usage = (VertexElementUsage)Enum.Parse (typeof(VertexElementUsage), usageStr);
                        attribute.index = indices [0];
                        attribute.format = (short)EffectUtilities.ParseParam (command, "format", 0);
                        AttributeList.Add (attribute);
                    }
                    else {
                        throw new ArgumentException ("Invalid Attribute: '" + lines [g] + "': There have to be 1 oder 4 indices!");
                    }
                    ++g;
                }
                else if (EffectUtilities.MatchesMetaDeclaration (lines [g], "EndShader", out command)) {
                    ++g;
                    break;
                }
                // "in mat4 name;" -> "in vec4 name0; in vec4 name1; in vec4 name2; in vec4 name3; mat4 name;"
                else if (lines[g].StartsWith("in mat4 ") && lines[g].Contains(";"))
                {
                    // replace the "in mat4" statement with four "in vec4" statements and one global variable
                    string name = lines[g].Split(new[]{"in mat4 "}, StringSplitOptions.None)[1].Split(new[]{';'})[0];
                    for (int i = 0; i < 4; ++i) {
                        _glslCode += "in vec4 " + name + i + "; ";
                    }
                    _glslCode += "mat4 " + name + ";\n";

                    // construct code to generate the full matrix in the main method
                    preMainCode += name + " = mat4 (";
                    for (int i = 0; i < 4; ++i) {
                        preMainCode += (i > 0 ? ", " : "") + name + i;
                    }
                    preMainCode += ");\n";

                    ++g;
                }
                // "uniform mat4 name;" -> "uniform vec4 name[4]; mat4 name;"
                else if (lines[g].StartsWith("uniform mat4 ") && lines[g].Contains(";"))
                {
                    if (lines [g].Contains ("[") && lines [g].Contains ("]")) {
                        throw new ArgumentException ("Unform matrix arrays are unsupported! Use separate matrices! '" + lines [g] + "'");
                    }
                    // replace the "uniform mat4" statement with an array of four vec4's and one global variable
                    string effectParameterName = lines[g].Split(new[]{"uniform mat4 "}, StringSplitOptions.None)[1].Split(new[]{';'})[0];
                    string constantBufferName = "CB_"+effectParameterName;
                    _glslCode += "uniform vec4 " + constantBufferName + "[4]; ";
                    _glslCode += "mat4 " + effectParameterName + ";\n";

                    // construct code to generate the full matrix in the main method
                    preMainCode += effectParameterName + " = mat4 (";
                    for (int i = 0; i < 4; ++i) {
                        preMainCode += (i > 0 ? ", " : "") + constantBufferName + "[" + i + "]";
                    }
                    preMainCode += ");\n";

                    // add the corresponding effect parameter
                    var buffer = new float[4 * 4];
                    for (var j = 0; j < buffer.Length; j++)
                        buffer[j] = 0;
                    var effectParameter = new EffectParameter(
                        class_: EffectParameterClass.Matrix,
                        type: EffectParameterType.Single,
                        name: effectParameterName,
                        rowCount: 4,
                        columnCount: 4,
                        semantic: "",
                        annotations: EffectAnnotationCollection.Empty,
                        elements: EffectParameterCollection.Empty,
                        structMembers: EffectParameterCollection.Empty,
                        data: buffer
                    );
                    effectParameterList.Add(effectParameter);
                    
                    // add the corresponding constant buffer
                    var constantBuffer = new ConstantBuffer(
                        device: GraphicsDevice,
                        sizeInBytes: 64,
                        parameterIndexes: new int[] { effectParameterList.Count - 1 },
                        parameterOffsets: new int[] { 0 },
                        name: constantBufferName
                    );
                    constantBuffersList.Add(constantBuffer);

                    // add the constant buffer to the constant buffer array of this shader!
                    int[] ourConstantBuffers = CBuffers;
                    Array.Resize (ref ourConstantBuffers, ourConstantBuffers.Length + 1);
                    ourConstantBuffers [ourConstantBuffers.Length - 1] = constantBuffersList.Count - 1;
                    CBuffers = ourConstantBuffers;

                    ++g;
                }
                // Add sampler and effect parameter
                else if (lines[g].StartsWith("uniform sampler2D ") && lines[g].Contains(";"))
                {
                    string effectParameterName = lines[g].Split(new[]{"uniform sampler2D "}, StringSplitOptions.None)[1].Split(new[]{';'})[0];
                    _glslCode += "uniform sampler2D " + effectParameterName + ";\n";
                    
                    var effectParameter = new EffectParameter(
                        class_: EffectParameterClass.Object,
                        type: EffectParameterType.Texture2D,
                        name: effectParameterName,
                        rowCount: 0,
                        columnCount: 0,
                        semantic: "",
                        annotations: EffectAnnotationCollection.Empty,
                        elements: EffectParameterCollection.Empty,
                        structMembers: EffectParameterCollection.Empty,
                        data: null
                    );
                    effectParameterList.Add(effectParameter);

                    SamplerInfo sampler = new SamplerInfo ();
                    sampler.name = effectParameterName;
                    sampler.type = SamplerType.Sampler2D;
                    sampler.textureSlot = SamplerList.Count;
                    sampler.samplerSlot = SamplerList.Count;
                    sampler.parameter = effectParameterList.Count - 1;
                    SamplerList.Add (sampler);

                    ++g;
                }
                // Add a vec4
                else if (lines[g].StartsWith("uniform vec4 ") && lines[g].Contains(";"))
                {
                    string effectParameterName = lines[g].Split(new[]{"uniform vec4 "}, StringSplitOptions.None)[1].Split(new[]{';'})[0];
                    _glslCode += "uniform vec4 " + effectParameterName + ";\n";

                    // add the corresponding effect parameter
                    var buffer = new float[4];
                    for (var j = 0; j < buffer.Length; j++)
                        buffer[j] = 0;
                    var effectParameter = new EffectParameter(
                        class_: EffectParameterClass.Vector,
                        type: EffectParameterType.Single,
                        name: effectParameterName,
                        rowCount: 1,
                        columnCount: 4,
                        semantic: "",
                        annotations: EffectAnnotationCollection.Empty,
                        elements: EffectParameterCollection.Empty,
                        structMembers: EffectParameterCollection.Empty,
                        data: buffer
                    );
                    effectParameterList.Add(effectParameter);

                    // add the corresponding constant buffer
                    var constantBuffer = new ConstantBuffer(
                        device: GraphicsDevice,
                        sizeInBytes: 16,
                        parameterIndexes: new int[] { effectParameterList.Count - 1 },
                        parameterOffsets: new int[] { 0 },
                        name: effectParameterName
                    );
                    constantBuffersList.Add(constantBuffer);

                    // add the constant buffer to the constant buffer array of this shader!
                    int[] ourConstantBuffers = CBuffers;
                    Array.Resize (ref ourConstantBuffers, ourConstantBuffers.Length + 1);
                    ourConstantBuffers [ourConstantBuffers.Length - 1] = constantBuffersList.Count - 1;
                    CBuffers = ourConstantBuffers;

                    ++g;
                }
                else if (lines[g].StartsWith("void main"))
                {
                    _glslCode += lines[g].Replace("void main", "void user_main") + "\n";
                    ++g;
                }
                else {
                    _glslCode += lines[g] + "\n";
                    ++g;
                }
            }

            if (stage == ShaderStage.Vertex) {
                // see: https://github.com/flibitijibibo/MonoGame/blob/e9f61e3efbae6f11ebbf45012e7c692c8d0ee529/MonoGame.Framework/Graphics/GraphicsDevice.cs#L1209
                _glslCode += "uniform vec4 posFixup; void main () {";
                _glslCode += preMainCode;
                _glslCode += "user_main();";
                _glslCode += "gl_Position.y = gl_Position.y * posFixup.y; gl_Position.xy += posFixup.zw * gl_Position.ww;";
                _glslCode += "}\n";
            }
            else {
                _glslCode += "void main () { " + preMainCode + " user_main(); }\n";
            }

            //Console.WriteLine (_glslCode);
            HashKey = MonoGame.Utilities.Hash.ComputeHash(System.Text.Encoding.ASCII.GetBytes(_glslCode));

            Samplers = SamplerList.ToArray();
            _attributes = AttributeList.ToArray();
        }

        internal Shader(GraphicsDevice device, BinaryReader reader, ref string readableCode)
        {
            GraphicsDevice = device;

            var isVertexShader = reader.ReadBoolean();
            Stage = isVertexShader ? ShaderStage.Vertex : ShaderStage.Pixel;

            var shaderLength = reader.ReadInt32();
            var shaderBytecode = reader.ReadBytes(shaderLength);

            var samplerCount = (int)reader.ReadByte();
            Samplers = new SamplerInfo[samplerCount];
            for (var s = 0; s < samplerCount; s++)
            {
                Samplers[s].type = (SamplerType)reader.ReadByte();
                Samplers[s].textureSlot = reader.ReadByte();
                Samplers[s].samplerSlot = reader.ReadByte();

                if (reader.ReadBoolean())
                {
                    Samplers[s].state = new SamplerState();
                    Samplers[s].state.AddressU = (TextureAddressMode)reader.ReadByte();
                    Samplers[s].state.AddressV = (TextureAddressMode)reader.ReadByte();
                    Samplers[s].state.AddressW = (TextureAddressMode)reader.ReadByte();
                    Samplers[s].state.Filter = (TextureFilter)reader.ReadByte();
                    Samplers[s].state.MaxAnisotropy = reader.ReadInt32();
                    Samplers[s].state.MaxMipLevel = reader.ReadInt32();
                    Samplers[s].state.MipMapLevelOfDetailBias = reader.ReadSingle();
                }

#if OPENGL
                Samplers[s].name = reader.ReadString();
#else
                Samplers[s].name = null;
#endif
                Samplers[s].parameter = reader.ReadByte();
            }

            var cbufferCount = (int)reader.ReadByte();
            CBuffers = new int[cbufferCount];
            for (var c = 0; c < cbufferCount; c++)
                CBuffers[c] = reader.ReadByte();

            PlatformConstruct(reader, isVertexShader, shaderBytecode, ref readableCode);
        }

        internal protected override void GraphicsDeviceResetting()
        {
            PlatformGraphicsDeviceResetting();
        }

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                PlatformDispose();
            }

            base.Dispose(disposing);
        }
	}
}

