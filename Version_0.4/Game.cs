using StbImageSharp;

using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

using OpenTK.Windowing.Common.Input;

namespace OpenCraft
{
    internal partial class Game: GameWindow
    {
        private string version = "v0.4a";

        int Width, Height;
        private bool mouseCaptured = false;

        static World GameWorld = null!;
        static Camera MainCamera = null!;
        static GameObject Player = null!;
        private VoxelRaycaster _raycaster = null!;

        private WorldRenderTarget _worldRt = null!;

        static byte selectedBlock = 6;

        private double fpsTime;
        private int fpsFrames;
        private int fps;
        private int fpsLimit;

        float globalTime = 0f;
        float timeOfDay = 0f;
        float dayDuration = 60f * 8f;

        string worldFolder = "saves/world";

        public Game(int width, int height, int fps) : base(GameWindowSettings.Default, new NativeWindowSettings {NumberOfSamples = 4})
            {
                Width = width;
                Height = height;
                fpsLimit = fps;

                using var s = File.OpenRead("textures/cobblestone.png");
                ImageResult img = ImageResult.FromStream(s, ColorComponents.RedGreenBlueAlpha);

                this.Icon = new WindowIcon(new OpenTK.Windowing.Common.Input.Image(img.Width, img.Height, img.Data));
                Title = $"OpenCraft {version}";

                CenterWindow(new Vector2i(width, height));
            }


        protected override void OnLoad()
        {
            base.OnLoad();

            UpdateFrequency = fpsLimit;

            Player = new GameObject(new Vector3(0, 8, 0), new Vector3(0.8f, 1.8f, 0.8f));
            MainCamera = new Camera(new Vector3(16, 30, 60), new Vector2(-20f, -90f), Width, Height, 90f);
            

            GameWorld = new World(new Shader("World.vert", "World.frag"), -1);

            _raycaster = new VoxelRaycaster(GameWorld.Chunks);
            _worldRt = new WorldRenderTarget(Width, Height);
            Compositor.Load();

            Player.SetWorld(GameWorld);
        
            Background.Load(Width, Height);

            Crosshair.LoadCrosshair();

            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(TriangleFace.Back);
            GL.Enable(EnableCap.Multisample);

            chunkTask = Task.Run(() => ChunkGenerator.GenerateChunks(MainCamera, GameWorld));

        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            fpsTime += args.Time;
            fpsFrames++;

            if (fpsTime >= 1.0)
            {
                fps = fpsFrames;
                fpsFrames = 0;
                fpsTime -= 1.0;

                Title = $"OpenCraft {version} | FPS: {fps}";
            }

            GL.ClearColor(0.53f, 0.81f, 0.92f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Background.RenderToTexture(MainCamera.pitch);
            _worldRt.RenderToTexture(GameWorld, MainCamera, timeOfDay);


            Compositor.DrawToScreen(
                fullscreenVao: Background._vao,
                bgTexId: Background.TextureId,
                albedoTexId: _worldRt.AlbedoTexId,
                normalTexId: _worldRt.NormalTexId,
                worldPosTexId: _worldRt.WorldPosTexId,
                aoTexId: _worldRt.AOTexId,
                screenWidth: Size.X,
                screenHeight: Size.Y,
                timeOfDay: timeOfDay
            );
    

            Crosshair.DrawCrosshair(MainCamera.screenWidth, MainCamera.screenHeight);

            SwapBuffers();
        }


        Task<Chunk[]>? chunkTask = null;

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            MouseState mouse = MouseState;
            KeyboardState input = KeyboardState;

            float dt = Math.Min((float)args.Time, 0.1f);
            globalTime += dt;
            timeOfDay = (globalTime % dayDuration) / dayDuration;

            if (chunkTask != null && chunkTask.IsCompleted)
            {
                Chunk[] newChunks = chunkTask.Result;
                ChunkGenerator.ApplyMeshInMainTHread(newChunks, GameWorld);
                chunkTask = Task.Run(() => ChunkGenerator.GenerateChunks(MainCamera, GameWorld));

            }

            if (mouseCaptured)
            {
                if (gameMode == 0) {PlayerControllerUpdate0(input, dt);}
                else {PlayerControllerUpdate1(input, dt);}
            }

            Player.Update(dt);

            if (input.IsKeyDown(Keys.LeftShift))
                MainCamera.Position = Player.Position + new Vector3(0.4f, 1.2f, 0.4f);
            else
                MainCamera.Position = Player.Position + new Vector3(0.4f, 1.5f, 0.4f);


            var origin = MainCamera.Position;
            var dir = MainCamera.front.Normalized();
            float reach = 8f;

            if (_raycaster.RaycastBlock(origin, dir, reach, out var hit, out var normal))
            {
                if (MouseState.IsButtonPressed(MouseButton.Left))
                    if (mouseCaptured) GameWorld.SetBlockWorld(hit, 0);

                if (MouseState.IsButtonPressed(MouseButton.Right))
                {
                    var placePos = hit + normal;
                    var touched = Player.GetBlocksPos();

                    if (!touched.Contains(placePos))
                        if (mouseCaptured) GameWorld.SetBlockWorld(placePos, selectedBlock);
                }
                if (input.IsKeyPressed(Keys.Q)) {Console.WriteLine($"Block position: {hit + normal}");}
            }

            if (input.IsKeyPressed(Keys.F))
            {
                WorldStorage.SaveSeed($"{worldFolder}/seed.txt", WorldGenerator.Seed);
                WorldStorage.SaveChanges($"{worldFolder}/chunks.bin", GameWorld.Changes);

                Console.WriteLine("Seed saved");
            }

            if (input.IsKeyPressed(Keys.L))
            {
                chunkTask = null;
                GameWorld = new World(new Shader("World.vert", "World.frag"), WorldStorage.LoadSeed($"{worldFolder}/seed.txt"));
                chunkTask = Task.Run(() => ChunkGenerator.GenerateChunks(MainCamera, GameWorld));

                _raycaster = new VoxelRaycaster(GameWorld.Chunks);
                Player.SetWorld(GameWorld);
                Player.Position = new Vector3(0, 8, 0);

                GameWorld.Changes = WorldStorage.LoadChanges($"{worldFolder}/chunks.bin");
                Console.WriteLine($"Loaded chunks: {GameWorld.Changes.Count}");
                WorldStorage.ApplyChangesToWorld(GameWorld, GameWorld.Changes);
            }

            if (input.IsKeyPressed(Keys.R))
            {
                chunkTask = null;
                GameWorld = new World(new Shader("World.vert", "World.frag"), -1);
                chunkTask = Task.Run(() => ChunkGenerator.GenerateChunks(MainCamera, GameWorld));


                _raycaster = new VoxelRaycaster(GameWorld.Chunks);
                Player.SetWorld(GameWorld);
                Player.Position = new Vector3(0, 8, 0);
            }

            if (KeyboardState.IsKeyPressed(Keys.O))
                gameMode = 1;
            if (KeyboardState.IsKeyPressed(Keys.P))
                gameMode = 0;

            if (KeyboardState.IsKeyPressed(Keys.Escape))
                ReleaseMouse();
            if (MouseState.IsButtonPressed(MouseButton.Left))
                CaptureMouse();

            if (mouseCaptured)
                MainCamera.Update(input, mouse, dt);
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            Width = e.Width;
            Height = e.Height;

            MainCamera?.Resize(Width, Height);
            _worldRt?.Resize(Width, Height);

            GL.Viewport(0, 0, e.Width, e.Height);
        }

        protected override void OnUnload()
        {
            base.OnUnload();
            GameWorld.Dispose();
        }

        private void CaptureMouse()
        {
            mouseCaptured = true;
            CursorState = CursorState.Grabbed;
            MainCamera.ResetMouse();
        }

        private void ReleaseMouse()
        {
            mouseCaptured = false;
            CursorState = CursorState.Normal;
        }
    }
}