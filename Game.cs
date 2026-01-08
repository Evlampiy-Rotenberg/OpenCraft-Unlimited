using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

using StbImageSharp;

namespace OpenCraft
{
    internal class Game: GameWindow
    {
        Shader shader = null!;
        Camera camera = null!;
        Dictionary<(int, int, int), Chunk> World = new();
        private VoxelRaycaster _raycaster = null!;

        int atlasTexture;
        SimpleGridAtlas atlas = null!;

        GameObject player = new GameObject(new Vector3(0, 50+2, 0), new Vector3(0.8f, 1.8f, 0.8f));
        private SolidBoxRenderer _box = null!;
        
        private bool mouseCaptured = false;
        int width, height;

        private double fpsTime;
        private int fpsFrames;
        private int fps;
        private int seed;

        Random random = new Random();
        
        private readonly Random rng = new Random();

        public Game(int width, int height, int fps): base(GameWindowSettings.Default, NativeWindowSettings.Default)
        {
            this.width = width;
            this.height = height;

            UpdateFrequency = fps;
            CenterWindow(new Vector2i(width, height));
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            this.width = e.Width;
            this.height = e.Height;

            if (camera != null) {camera.Resize((float)e.Width, (float)e.Height);}
            GL.Viewport(0, 0, e.Width, e.Height);
        }

        protected override void OnLoad()
        {
            base.OnLoad();
            seed = random.Next(0, 1024);
            Console.WriteLine($"Seed: {seed}");

            player.SetWorld(World);
            _raycaster = new VoxelRaycaster(World);

            shader = new Shader("Default.vert", "Default.frag");

            atlas = new SimpleGridAtlas(8, 8, 32, 32);

            atlas.Add("textures/red_wool.png",    id: 1);
            atlas.Add("textures/dirt.png",        id: 2);
            atlas.Add("textures/moss_block.png",  id: 3);
            atlas.Add("textures/sand.png",        id: 4);
            atlas.Add("textures/stone.png",       id: 5);
            atlas.Add("textures/cobblestone.png", id: 6);

            atlasTexture = atlas.UploadToGpu();

            LoadBackground();

            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(TriangleFace.Back);

            _box = new SolidBoxRenderer();
            _box.Load("GameObject.vert", "GameObject.frag");

            camera = new Camera(new Vector3(0f, 0f, 0f), width, height);
            camera.UpdateVectors();
        }


        protected override void OnUnload()
        {
            base.OnUnload();

            shader.Dispose();
            _box?.Dispose();
            foreach (Chunk chunk in World.Values) {chunk.mesh.Dispose();}
            UnloadBackground();
        }


        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            fpsTime += args.Time;
            fpsFrames++;

            //Console.WriteLine(camera.Position);

            if (fpsTime >= 1.0)
            {
                fps = fpsFrames;
                fpsFrames = 0;
                fpsTime -= 1.0;

                Title = $"FPS: {fps}";
            }

            GL.ClearColor(0.53f, 0.81f, 0.92f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            RenderBackground();

            shader.Use();

            Matrix4 view = camera.GetViewMatrix();
            Matrix4 projection = camera.GetProjectionMatrix();
            shader.SetMatrix4("view", view);
            shader.SetMatrix4("projection", projection);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, atlasTexture);
            shader.SetInt("texture0", 0);

            //int totalChunks = 0;
            foreach (Chunk chunk in World.Values)
            {
                if (!chunk.empty)
                {
                    Matrix4 model = Matrix4.CreateTranslation(chunk.X * Chunk.Size, chunk.Y * Chunk.Size, chunk.Z * Chunk.Size);
                    shader.SetMatrix4("model", model);
                    chunk.mesh.Draw();
                    //totalChunks++;
                }
            }
            Context.SwapBuffers();
        }

        const int distance = 16;
        const int MaxChunks = 100000;
        const int CreatePerFrame = 1;

        List<(int dx, int dy, int dz)> defaultOffsets = GenerateOffsetsByDistance(distance);

        static List<(int dx, int dy, int dz)> GenerateOffsetsByDistance(int distance)
        {
            int maxD2 = 3 * distance * distance;
            var buckets = new List<(int dx, int dy, int dz)>[maxD2 + 1];

            for (int dx = -distance; dx <= distance; dx++)
            for (int dy = -distance; dy <= distance; dy++)
            for (int dz = -distance; dz <= distance; dz++)
            {
                int d2 = dx*dx + dy*dy + dz*dz;
                (buckets[d2] ??= new List<(int, int, int)>()).Add((dx, dy, dz));
            }

            var result = new List<(int dx, int dy, int dz)>((2*distance + 1) * (2*distance + 1) * (2*distance + 1));
            for (int d2 = 0; d2 <= maxD2; d2++)
                if (buckets[d2] != null)
                    result.AddRange(buckets[d2]);

            return result;
        }

        float playerSpeed = 50.0f;
        float jumpPower = 15.0f;
        void UpdatePlayer(KeyboardState input, FrameEventArgs e)
        {
            Vector3 forwardFlat = new Vector3(camera.front.X, 0f, camera.front.Z);

            float speed_x = 1.0f;

            if (input.IsKeyDown(Keys.LeftControl))
                speed_x = 2.0f;

            if (input.IsKeyDown(Keys.LeftShift))
                speed_x = 0.3f;

            if (forwardFlat.LengthSquared > 0f)
                forwardFlat = Vector3.Normalize(forwardFlat);

            if (input.IsKeyDown(Keys.W))
                player.Velocity += playerSpeed * speed_x * forwardFlat * (float)e.Time;

            if (input.IsKeyDown(Keys.S))
                player.Velocity += -playerSpeed * speed_x * forwardFlat * (float)e.Time;

            if (input.IsKeyDown(Keys.A))
                player.Velocity += -playerSpeed * speed_x * camera.right * (float)e.Time;

            if (input.IsKeyDown(Keys.D))
                player.Velocity += playerSpeed * speed_x * camera.right * (float)e.Time;


            //if (input.IsKeyPressed(Keys.Space))
            if (input.IsKeyDown(Keys.Space))
            {
                if (player.Velocity.Y == 0)
                {
                    player.Velocity.Y += jumpPower;
                    if (input.IsKeyDown(Keys.W) && input.IsKeyDown(Keys.LeftShift))
                    {
                        player.Velocity += 30f * forwardFlat;
                    }
                }
            }

            player.Update((float)e.Time);
            camera.Position = player.Position + new Vector3(0.4f, 1.5f, 0.4f);
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            MouseState mouse = MouseState;
            KeyboardState input = KeyboardState;

            base.OnUpdateFrame(args);

            var origin = camera.Position;
            var dir = camera.front.Normalized();
            float reach = 6f;

            if (_raycaster.RaycastBlock(origin, dir, reach, out var hit, out var normal))
            {
                // Видалити блок
                if (MouseState.IsButtonPressed(MouseButton.Left))
                    _raycaster.SetBlockWorld(hit, 0);

                // Поставити блок у сусідній блок по нормалі
                if (MouseState.IsButtonPressed(MouseButton.Right))
                    _raycaster.SetBlockWorld(hit + normal, 6);
            }

            if (input.IsKeyPressed(Keys.E))
            {
                Console.WriteLine(camera.Position);
            }

            if (KeyboardState.IsKeyPressed(Keys.Escape))
                ReleaseMouse();

            if (mouseCaptured)
                camera.Update(input, mouse, args);

            UpdatePlayer(input, args);

            
            int cx = (int)Math.Floor(camera.Position.X / (float)Chunk.Size);
            int cy = (int)Math.Floor(camera.Position.Y / (float)Chunk.Size);
            int cz = (int)Math.Floor(camera.Position.Z / (float)Chunk.Size);

            int chunksGenerated = 0;
            foreach (var (dx, dy, dz) in defaultOffsets)
            {
                int x = cx + dx;
                int y = cy + dy;
                int z = cz + dz;

                var key = (x, y, z);

                if (!World.ContainsKey(key))
                {
                    World[key] = new Chunk(key.x, key.y, key.z, atlas, World, seed);
                    chunksGenerated++;
                }
                if (chunksGenerated >= CreatePerFrame) break;
            }
            while (World.Count > MaxChunks) {RemoveFarthestChunk(cx, cy, cz);} 
        }


        private void RemoveFarthestChunk(int cx, int cy, int cz)
        {
            if (World.Count == 0) return;

            (int, int, int) farKey = default;
            long bestDist2 = long.MinValue;
            bool hasKey = false;

            foreach (var k in World.Keys)
            {
                long dx = (long)k.Item1 - cx;
                long dy = (long)k.Item2 - cy;
                long dz = (long)k.Item3 - cz;

                long dist2 = dx * dx + dy * dy + dz * dz;

                if (!hasKey || dist2 > bestDist2)
                {
                    bestDist2 = dist2;
                    farKey = k;
                    hasKey = true;
                }
            }

            if (!hasKey) return;

            World[farKey].mesh.Dispose();
            World.Remove(farKey);
        }

        private void CaptureMouse()
        {
            mouseCaptured = true;
            CursorState = CursorState.Grabbed;
            camera.ResetMouse();
        }

        private void ReleaseMouse()
        {
            mouseCaptured = false;
            CursorState = CursorState.Normal;
        }

        protected override void OnMouseDown(OpenTK.Windowing.Common.MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            if (!mouseCaptured && e.Button == MouseButton.Left)
                CaptureMouse();
        }





        private int _bgVao;
        private int _bgVbo;
        private int _bgEbo;
        private Shader _bgShader = null!;

        private void LoadBackground()
        {
            // 4 вершини: (pos.xy, uv.xy)
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

            // Шейдер для градієнтного фону
            _bgShader = new Shader("Background.vert", "Background.frag");
        }

        private void RenderBackground()
        {
            GL.Disable(EnableCap.DepthTest);

            _bgShader.Use();

            
            float pitch01 = camera.pitch / 90f; // -1 .. +1
            _bgShader.SetFloat("uPitch", pitch01);


            _bgShader.SetVector3("uTopColor",    new Vector3(0.20f, 0.30f, 0.60f));
            _bgShader.SetVector3("uBottomColor", new Vector3(0.90f, 0.80f, 0.70f));

            GL.BindVertexArray(_bgVao);
            GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);

            GL.Enable(EnableCap.DepthTest);
        }

        private void UnloadBackground()
        {
            if (_bgEbo != 0) GL.DeleteBuffer(_bgEbo);
            if (_bgVbo != 0) GL.DeleteBuffer(_bgVbo);
            if (_bgVao != 0) GL.DeleteVertexArray(_bgVao);

            _bgShader?.Dispose();
        }













    }
}
