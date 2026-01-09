using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;

namespace OpenCraft
{
	public class Background
	{
	    private static int _bgVao;
	    private static int _bgVbo;
	    private static int _bgEbo;
	    private static Shader _bgShader = null!;

		public static void LoadBackground()
		{
		    float[] verts =
		    {
		        // x,    y,     u,   v
		        -1f, -1f,    0f,  0f, // left-bottom
		         1f, -1f,    1f,  0f, // right-bottom
		         1f,  1f,    1f,  1f, // right-top
		        -1f,  1f,    0f,  1f  // left-top
		    };

		    uint[] indices =
		    {
		        0, 1, 2,
		        2, 3, 0
		    };

		    _bgVao = GL.GenVertexArray();
		    _bgVbo = GL.GenBuffer();
		    _bgEbo = GL.GenBuffer();

		    GL.BindVertexArray(_bgVao);

		    GL.BindBuffer(BufferTarget.ArrayBuffer, _bgVbo);
		    GL.BufferData(BufferTarget.ArrayBuffer, verts.Length * sizeof(float), verts, BufferUsageHint.StaticDraw);

		    GL.BindBuffer(BufferTarget.ElementArrayBuffer, _bgEbo);
		    GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

		    int stride = 4 * sizeof(float);

		    // location 0: position (vec2)
		    GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, 0);
		    GL.EnableVertexAttribArray(0);

		    // location 1: uv (vec2)
		    GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, 2 * sizeof(float));
		    GL.EnableVertexAttribArray(1);

		    GL.BindVertexArray(0);
		    
		    _bgShader = new Shader("Background.vert", "Background.frag");
		}

		public static void DrawBackground(float pitch)
        {
            GL.Disable(EnableCap.DepthTest);

            _bgShader.Use();

            _bgShader.SetFloat("uPitch", pitch / 90f);
            _bgShader.SetVector3("uTopColor",    new Vector3(0.20f, 0.30f, 0.60f));
            _bgShader.SetVector3("uBottomColor", new Vector3(0.90f, 0.80f, 0.70f));

            GL.BindVertexArray(_bgVao);
            GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);

            GL.Enable(EnableCap.DepthTest);
        }
	}
}