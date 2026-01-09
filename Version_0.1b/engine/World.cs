using System;
using System.IO;
using System.Text.Json;
using System.Numerics;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;


namespace OpenCraft
{
    public class World : IDisposable
    {

        Shader WorldShader = null!;
        static Random random = new Random();
        public Dictionary<(int, int, int), Chunk> Chunks = new();

        int atlasTexture;
        public SimpleGridAtlas Atlas = null!;

        public World(Shader worldShader)
        {  
            WorldShader = worldShader;

            string path = "config/worldgen.json";

            int seed;
            float frequency;
            int heightScale;
            int border;
            float mPower;
            float vPower;
            float vScale;
            (int blockId, int min, int max)[] layers;

            int randSeed = random.Next(0, 65536);

            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                JsonElement root = JsonDocument.Parse(json).RootElement;

                seed        = root.GetProperty("seed").GetInt32();
                frequency   = root.GetProperty("frequency").GetSingle();
                heightScale = root.GetProperty("heightScale").GetInt32();
                border      = root.GetProperty("border").GetInt32();
                mPower      = root.GetProperty("mPower").GetSingle();
                vPower      = root.GetProperty("vPower").GetSingle();
                vScale      = root.GetProperty("vScale").GetSingle();

                if (seed == -1)
                    seed = randSeed;

                var layersJson = root.GetProperty("layers");
                layers = new (int blockId, int min, int max)[layersJson.GetArrayLength()];

                int i = 0;
                foreach (JsonElement el in layersJson.EnumerateArray())
                {
                    int blockId = el.GetProperty("blockId").GetInt32();
                    int min     = el.GetProperty("min").GetInt32();
                    int max     = el.GetProperty("max").GetInt32();

                    layers[i++] = (blockId, min, max);
                }
            }
            else
            {
                seed        = randSeed;
                frequency   = 0.0003f;
                heightScale = 512;
                border      = 8;
                mPower      = 3f;
                vPower      = 3f;
                vScale      = 0.25f;

                layers = new (int blockId, int min, int max)[]
                {
                    (4, -9999, -20),
                    (3, -21,   50),
                    (2,  51,   120),
                    (5,  121,  9999)
                };
            }

            WorldGenerator.Init(seed, frequency, heightScale, border, mPower, vPower, vScale, layers);
            Console.WriteLine($"Seed: {123}");

            Atlas = new SimpleGridAtlas(8, 8, 32, 32);

            Atlas.Add("textures/red_wool.png",    id: 1);
            Atlas.Add("textures/dirt.png",        id: 2);
            Atlas.Add("textures/moss_block.png",  id: 3);
            Atlas.Add("textures/sand.png",        id: 4);
            Atlas.Add("textures/stone.png",       id: 5);
            Atlas.Add("textures/cobblestone.png", id: 6);

            atlasTexture = Atlas.UploadToGpu();
        }

        public void Dispose()
        {
            WorldShader.Dispose();
            foreach (Chunk chunk in Chunks.Values) {chunk.mesh.Dispose();}
        }

        public void Draw(Camera camera)
        {
            WorldShader.Use();

            Matrix4 view = camera.GetViewMatrix();
            Matrix4 projection = camera.GetProjectionMatrix();

            WorldShader.SetMatrix4("view", view);
            WorldShader.SetMatrix4("projection", projection);

            foreach (Chunk chunk in Chunks.Values)
            {
                if (!chunk.empty)
                {
                    Matrix4 model = Matrix4.CreateTranslation(chunk.X * Chunk.Size, chunk.Y * Chunk.Size, chunk.Z * Chunk.Size);
                    WorldShader.SetMatrix4("model", model);
                    chunk.mesh.Draw();
                }
            }
        }
    }
}