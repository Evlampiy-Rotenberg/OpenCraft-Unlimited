using System;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;

namespace OpenCraft
{
    public sealed class WorldRenderTarget : IDisposable
    {
        private int _fbo;

        private int _albedoTex;    // base color
        private int _normalTex;    // normal
        private int _worldPosTex;  // world position (or view position)
        private int _aoTex;        // ambient occlusion / misc

        private int _depthRbo;

        private int _width;
        private int _height;

        public int Width => _width;
        public int Height => _height;

        public int AlbedoTexId   => _albedoTex;
        public int NormalTexId   => _normalTex;
        public int WorldPosTexId => _worldPosTex;
        public int AOTexId       => _aoTex;

        public WorldRenderTarget(int width, int height)
        {
            Resize(width, height);
        }


        public void RenderWorld(World world, Camera camera, float timeOfDay)
        {
            Shader shader = world.WorldShader;
            shader.Use();

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, world.atlasTexture);

            Matrix4 view = camera.GetViewMatrix();
            Matrix4 projection = camera.GetProjectionMatrix();

            shader.SetFloat("timeOfDay", timeOfDay);
            shader.SetMatrix4("view", view);
            shader.SetMatrix4("projection", projection);

            foreach (Chunk chunk in world.Chunks.Values)
            {
                //behind camera
                if (!chunk.noGeometry && Frustum.ChunkInFrustum(camera, new Vector3(chunk.X, chunk.Y, chunk.Z)))
                {
                    Matrix4 model = Matrix4.CreateTranslation(chunk.X * Chunk.Size, chunk.Y * Chunk.Size, chunk.Z * Chunk.Size);
                    shader.SetMatrix4("model", model);
                    if (chunk.mesh != null)
                        chunk.mesh.Draw();
                }
            }
        }


        public void RenderToTexture(World world, Camera camera, float timeOfDay)
        {
            if (_fbo == 0)
                throw new InvalidOperationException("WorldRenderTarget is not initialized. Call Resize() or construct with size first.");

            // Save minimal state (so this method does not break other rendering)
            GL.GetInteger(GetPName.DrawFramebufferBinding, out int prevDrawFbo);

            int[] prevViewport = new int[4];
            GL.GetInteger(GetPName.Viewport, prevViewport);

            bool prevDepthTest = GL.IsEnabled(EnableCap.DepthTest);

            // Bind and set viewport to RT size
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, _fbo);
            GL.Viewport(0, 0, _width, _height);

            GL.Enable(EnableCap.DepthTest);
            GL.DepthMask(true);

            // Clear all color attachments and depth
            GL.ClearColor(0f, 0f, 0f, 0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            RenderWorld(world, camera, timeOfDay);

            // Restore state
            if (!prevDepthTest) GL.Disable(EnableCap.DepthTest);

            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, prevDrawFbo);
            GL.Viewport(prevViewport[0], prevViewport[1], prevViewport[2], prevViewport[3]);
        }

        public void Resize(int width, int height)
        {
            width  = Math.Max(1, width);
            height = Math.Max(1, height);

            if (width == _width && height == _height && _fbo != 0)
                return;

            _width = width;
            _height = height;

            // Preserve bindings to avoid breaking other rendering
            GL.GetInteger(GetPName.FramebufferBinding, out int prevFbo);
            GL.GetInteger(GetPName.TextureBinding2D, out int prevTex);
            GL.GetInteger(GetPName.RenderbufferBinding, out int prevRbo);

            // Delete old
            DeleteTargets();

            // Create textures
            _albedoTex = CreateColorTex(_width, _height,
                PixelInternalFormat.Rgba8, PixelFormat.Rgba, PixelType.UnsignedByte,
                TextureMinFilter.Nearest, TextureMagFilter.Nearest);

            // Normals in half-float to reduce banding; you can store (n*0.5+0.5) or store raw -1..1 (float)
            _normalTex = CreateColorTex(_width, _height,
                PixelInternalFormat.Rgba16f, PixelFormat.Rgba, PixelType.Float,
                TextureMinFilter.Nearest, TextureMagFilter.Nearest);

            // World position typically needs float format
            _worldPosTex = CreateColorTex(_width, _height,
                PixelInternalFormat.Rgba16f, PixelFormat.Rgba, PixelType.Float,
                TextureMinFilter.Nearest, TextureMagFilter.Nearest);

            // AO/misc: RGBA8 for simplicity (you may prefer R8 later)
            _aoTex = CreateColorTex(_width, _height,
                PixelInternalFormat.Rgba16f, PixelFormat.Rgba, PixelType.Float,
                TextureMinFilter.Nearest, TextureMagFilter.Nearest);

            // Depth renderbuffer (not sampleable)
            _depthRbo = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _depthRbo);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent24, _width, _height);

            // Create FBO
            _fbo = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);

            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                TextureTarget.Texture2D, _albedoTex, 0);

            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1,
                TextureTarget.Texture2D, _normalTex, 0);

            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment2,
                TextureTarget.Texture2D, _worldPosTex, 0);

            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment3,
                TextureTarget.Texture2D, _aoTex, 0);

            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer,
                FramebufferAttachment.DepthAttachment,
                RenderbufferTarget.Renderbuffer, _depthRbo);

            // Tell GL we are drawing into multiple color attachments (MRT)
            DrawBuffersEnum[] bufs =
            {
                DrawBuffersEnum.ColorAttachment0,
                DrawBuffersEnum.ColorAttachment1,
                DrawBuffersEnum.ColorAttachment2,
                DrawBuffersEnum.ColorAttachment3
            };
            GL.DrawBuffers(bufs.Length, bufs);

            var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (status != FramebufferErrorCode.FramebufferComplete)
                throw new Exception($"World G-Buffer FBO incomplete: {status}");

            // Restore bindings
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, prevFbo);
            GL.BindTexture(TextureTarget.Texture2D, prevTex);
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, prevRbo);
        }

        public void Dispose()
        {
            DeleteTargets();
        }

        private void DeleteTargets()
        {
            if (_albedoTex != 0) GL.DeleteTexture(_albedoTex);
            if (_normalTex != 0) GL.DeleteTexture(_normalTex);
            if (_worldPosTex != 0) GL.DeleteTexture(_worldPosTex);
            if (_aoTex != 0) GL.DeleteTexture(_aoTex);

            if (_depthRbo != 0) GL.DeleteRenderbuffer(_depthRbo);

            if (_fbo != 0) GL.DeleteFramebuffer(_fbo);

            _albedoTex = 0;
            _normalTex = 0;
            _worldPosTex = 0;
            _aoTex = 0;
            _depthRbo = 0;
            _fbo = 0;
        }

        private static int CreateColorTex(
            int w, int h,
            PixelInternalFormat internalFmt,
            PixelFormat fmt,
            PixelType type,
            TextureMinFilter min = TextureMinFilter.Nearest,
            TextureMagFilter mag = TextureMagFilter.Nearest)
        {
            int tex = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, tex);

            GL.TexImage2D(TextureTarget.Texture2D, 0, internalFmt, w, h, 0, fmt, type, IntPtr.Zero);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)min);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)mag);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

            // Optional but commonly helpful for float textures:
            // GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBaseLevel, 0);
            // GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, 0);

            return tex;
        }
    }
}