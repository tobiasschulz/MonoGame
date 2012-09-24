using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using TwoMGFX;

namespace Microsoft.Xna.Framework.Graphics
{
    internal partial class DXConstantBufferData
    {
        public void Write(BinaryWriter writer, Options options)
        {
            if (!options.DX11Profile)
                writer.Write(Name);

            writer.Write((ushort)Size);

            writer.Write((byte)Parameters.Count);
            for (var i = 0; i < Parameters.Count; i++)
            {
                writer.Write((byte)ParameterIndex[i]);
                writer.Write((ushort)ParameterOffset[i]);
            }
        }
    }
}