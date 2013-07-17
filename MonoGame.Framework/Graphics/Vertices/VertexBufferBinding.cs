namespace Microsoft.Xna.Framework.Graphics
{
    public struct VertexBufferBinding
    {
        public int InstanceFrequency
        {
            get;
            private set;
        }
        
        public VertexBuffer VertexBuffer
        {
            get;
            private set;
        }
        
        public int VertexOffset
        {
            get;
            private set;
        }
        
        public VertexBufferBinding(
            VertexBuffer vertexBuffer
        ) : this(vertexBuffer, 0, 1) {}
        
        public VertexBufferBinding(
            VertexBuffer vertexBuffer,
            int vertexOffset
        ) : this(vertexBuffer, vertexOffset, 1) {}
        
        public VertexBufferBinding(
            VertexBuffer vertexBuffer,
            int vertexOffset,
            int instanceFrequency
        ) : this()
        {
            VertexBuffer = vertexBuffer;
            VertexOffset = vertexOffset;
            InstanceFrequency = instanceFrequency;
        }
    }
}