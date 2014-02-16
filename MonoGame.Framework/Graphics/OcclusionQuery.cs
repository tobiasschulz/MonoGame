using OpenTK.Graphics.OpenGL;

namespace Microsoft.Xna.Framework.Graphics
{
    public class OcclusionQuery : GraphicsResource
    {
        private uint glQueryId;

        public OcclusionQuery(GraphicsDevice graphicsDevice)
        {
            this.GraphicsDevice = graphicsDevice;
            GL.GenQueries(1, out glQueryId);
            GraphicsExtensions.CheckGLError();
        }

        public void Begin()
        {
            GL.BeginQuery(QueryTarget.SamplesPassed, glQueryId);
            GraphicsExtensions.CheckGLError();
        }

        public void End()
        {
            GL.EndQuery(QueryTarget.SamplesPassed);
            GraphicsExtensions.CheckGLError();
        }

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                GraphicsDevice.AddDisposeAction(() =>
                {
                    GL.DeleteQueries(1, ref glQueryId);
                    GraphicsExtensions.CheckGLError();
                });
            }
            base.Dispose(disposing);
        }

        public bool IsComplete
        {
            get
            {
                int[] resultReady = {0};
                GL.GetQueryObject(glQueryId, GetQueryObjectParam.QueryResultAvailable, resultReady);
                GraphicsExtensions.CheckGLError();
                return resultReady[0] != 0;
            }
        }

        public int PixelCount
        {
            get
            {
                int[] result = {0};
                GL.GetQueryObject(
                    glQueryId,
                    GetQueryObjectParam.QueryResultAvailable,
                    result
                );
                GraphicsExtensions.CheckGLError();
                return result[0];
            }
        }
    }
}

