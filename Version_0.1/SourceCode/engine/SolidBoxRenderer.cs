using System;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace OpenCraft
{
    public sealed class SolidBoxRenderer : IDisposable
    {
        private int _vao;
        private int _vbo;
        private int _ebo;
        private int _indexCount;

        private Shader _shader = null!;
        private bool _isLoaded;

        public void Load(string vertexPath, string fragmentPath)
        {
            if (_isLoaded) return;
            float[] verts =
            {
                0f, 0f, 0f, // 0
                1f, 0f, 0f, // 1
                1f, 1f, 0f, // 2
                0f, 1f, 0f, // 3
                0f, 0f, 1f, // 4
                1f, 0f, 1f, // 5
                1f, 1f, 1f, // 6
                0f, 1f, 1f  // 7
            };

            uint[] indices =
            {
                0, 2, 1,  0, 3, 2,
                4, 5, 6,  4, 6, 7,
                0, 7, 3,  0, 4, 7,
                1, 2, 6,  1, 6, 5,
                0, 1, 5,  0, 5, 4,
                3, 6, 2,  3, 7, 6
            };

            _indexCount = indices.Length;

            _vao = GL.GenVertexArray();
            _vbo = GL.GenBuffer();
            _ebo = GL.GenBuffer();

            GL.BindVertexArray(_vao);

            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, verts.Length * sizeof(float), verts, BufferUsageHint.StaticDraw);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

            int stride = 3 * sizeof(float);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
            GL.EnableVertexAttribArray(0);

            GL.BindVertexArray(0);

            _shader = new Shader(vertexPath, fragmentPath);

            _isLoaded = true;
        }

        public void Render(Vector3 position, Vector3 size, Matrix4 view, Matrix4 proj, Vector4 color)
        {
            if (!_isLoaded)
                throw new InvalidOperationException("SolidBoxRenderer.Load() must be called before Render().");

            Matrix4 model = Matrix4.CreateScale(size) * Matrix4.CreateTranslation(position);

            _shader.Use();
            _shader.SetMatrix4("uModel", model);
            _shader.SetMatrix4("uView", view);
            _shader.SetMatrix4("uProj", proj);
            _shader.SetVector4("uColor", color);

            GL.BindVertexArray(_vao);
            GL.DrawElements(PrimitiveType.Triangles, _indexCount, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);
        }

        public void Dispose()
        {
            if (_vao != 0) GL.DeleteVertexArray(_vao);
            if (_vbo != 0) GL.DeleteBuffer(_vbo);
            if (_ebo != 0) GL.DeleteBuffer(_ebo);

            _vao = _vbo = _ebo = 0;
            _indexCount = 0;
            _isLoaded = false;
        }
    }
}
