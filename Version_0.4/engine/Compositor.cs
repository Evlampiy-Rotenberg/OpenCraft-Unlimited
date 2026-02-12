using System;
using OpenTK.Graphics.OpenGL4;

namespace OpenCraft
{
    public static class Compositor
    {
        private static Shader _shader = null!;
        private static bool _loaded;


        public static void Load()
        {
            if (_loaded) return;
            _shader = new Shader("Composite.vert", "Composite.frag");
            _loaded = true;
        }

        public static void DrawToScreen(
            int fullscreenVao,
            int bgTexId,
            int albedoTexId,
            int normalTexId,
            int worldPosTexId,
            int aoTexId,
            int screenWidth,
            int screenHeight,
            float timeOfDay,
            int depthTexId = 0
        )
        {
            if (!_loaded)
                throw new InvalidOperationException("Compositor not loaded. Call Compositor.Load().");

            screenWidth = Math.Max(1, screenWidth);
            screenHeight = Math.Max(1, screenHeight);

            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
            GL.Viewport(0, 0, screenWidth, screenHeight);

            GL.Disable(EnableCap.DepthTest);

            _shader.Use();
            _shader.SetFloat("timeOfDay", timeOfDay);

            // uBg -> unit 0
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, bgTexId);
            _shader.SetInt("uBg", 0);

            // gAlbedo -> unit 1
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, albedoTexId);
            _shader.SetInt("gAlbedo", 1);

            // gNormal -> unit 2
            GL.ActiveTexture(TextureUnit.Texture2);
            GL.BindTexture(TextureTarget.Texture2D, normalTexId);
            _shader.SetInt("gNormal", 2);

            // gWorldPos -> unit 3
            GL.ActiveTexture(TextureUnit.Texture3);
            GL.BindTexture(TextureTarget.Texture2D, worldPosTexId);
            _shader.SetInt("gWorldPos", 3);

            // gMisc/AO -> unit 4
            GL.ActiveTexture(TextureUnit.Texture4);
            GL.BindTexture(TextureTarget.Texture2D, aoTexId);
            _shader.SetInt("gMisc", 4);

            // depth -> unit 5 (optional)
            if (depthTexId != 0)
            {
                GL.ActiveTexture(TextureUnit.Texture5);
                GL.BindTexture(TextureTarget.Texture2D, depthTexId);
                _shader.SetInt("gDepth", 5);
                _shader.SetInt("uHasDepth", 1);
            }
            else
            {
                _shader.SetInt("uHasDepth", 0);
            }

            GL.BindVertexArray(fullscreenVao);
            GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);
        }
    }
}
