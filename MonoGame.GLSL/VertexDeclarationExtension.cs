using System;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace MonoGame.GLSL
{
    public static class VertexDeclarationExtension
    {
        private static Dictionary<int, VertexDeclarationAttributeInfo> shaderAttributeInfo = new Dictionary<int, VertexDeclarationAttributeInfo>();

        internal static void Apply (this VertexDeclaration vertexDeclaration, GLShader shader, IntPtr offset, int divisor = 0)
        {
            VertexDeclarationAttributeInfo attrInfo;
            int shaderHash = shader.GetHashCode ();
            if (!shaderAttributeInfo.TryGetValue (shaderHash, out attrInfo)) {
                // Get the vertex attribute info and cache it
                attrInfo = new VertexDeclarationAttributeInfo (OpenGLDevice.Instance.MaxVertexAttributes);

                foreach (var ve in vertexDeclaration.GetVertexElements()) {
                    var attributeLocation = shader.GetAttribLocation (ve.VertexElementUsage, ve.UsageIndex);
                    // XNA appears to ignore usages it can't find a match for, so we will do the same
                    if (attributeLocation >= 0) {
                        attrInfo.Elements.Add (new VertexDeclarationAttributeInfo.Element () {
                            Offset = ve.Offset,
                            AttributeLocation = attributeLocation,
                            NumberOfElements = ve.VertexElementFormat.OpenGLNumberOfElements(),
                            VertexAttribPointerType = ve.VertexElementFormat.OpenGLVertexAttribPointerType(),
                            Normalized = ve.OpenGLVertexAttribNormalized(),
                        });
                        attrInfo.EnabledAttributes [attributeLocation] = true;
                    }
                }

                shaderAttributeInfo.Add (shaderHash, attrInfo);
            }

            // Apply the vertex attribute info
            foreach (var element in attrInfo.Elements) {
                OpenGLDevice.Instance.AttributeEnabled [element.AttributeLocation] = true;
                OpenGLDevice.Instance.Attributes [element.AttributeLocation].Divisor.Set (divisor);
                OpenGLDevice.Instance.VertexAttribPointer (
                    element.AttributeLocation,
                    element.NumberOfElements,
                    element.VertexAttribPointerType,
                    element.Normalized,
                    vertexDeclaration.VertexStride,
                    (IntPtr)(offset.ToInt64 () + element.Offset)
                );
            }
        }
    }
}

