using OpenTK.Mathematics;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OpenCraft
{
    class ChunkGenerator()
    {
        const int LoadDistance = 24;
        const int UnloadDistance = 32;
        const int CreatePerFrame = 2;

        static List<(int dx, int dy, int dz)> defaultOffsets = GenerateOffsetsByLoadDistance(LoadDistance);

        static List<(int dx, int dy, int dz)> GenerateOffsetsByLoadDistance(int LoadDistance)
        {
            int maxD2 = 3 * LoadDistance * LoadDistance;
            var buckets = new List<(int dx, int dy, int dz)>[maxD2 + 1];

            for (int dx = -LoadDistance; dx <= LoadDistance; dx++)
            for (int dy = -LoadDistance; dy <= LoadDistance; dy++)
            for (int dz = -LoadDistance; dz <= LoadDistance; dz++)
            {
                int d2 = dx*dx + dy*dy + dz*dz;
                (buckets[d2] ??= new List<(int, int, int)>()).Add((dx, dy, dz));
            }

            var result = new List<(int dx, int dy, int dz)>((2*LoadDistance + 1) * (2*LoadDistance + 1) * (2*LoadDistance + 1));
            for (int d2 = 0; d2 <= maxD2; d2++)
                if (buckets[d2] != null)
                    result.AddRange(buckets[d2]);

            return result;
        }


        public static void ApplyMeshInMainTHread(Chunk[] chunks, World world)
        {
            for (int i = 0; i < chunks.Length; i++)
            {
                var chunk = chunks[i];
                var key = (chunk.X, chunk.Y, chunk.Z);

                if (!world.Chunks.ContainsKey(key))
                {
                    if (world.Chunks.TryAdd(key, chunk))
                    {
                        chunk.ApplyMesh();
                        chunk.AttachWorld(world.Chunks);
                    }
                }
            }
        }


        public static Chunk[] GenerateChunks(Camera camera, World world)
        {
            int cx = (int)MathF.Floor(camera.Position.X / Chunk.Size);
            int cy = (int)MathF.Floor(camera.Position.Y / Chunk.Size);
            int cz = (int)MathF.Floor(camera.Position.Z / Chunk.Size);

            var toCreate = new List<(int x, int y, int z)>(CreatePerFrame);

            foreach (var (dx, dy, dz) in defaultOffsets)
            {
                float d = new Vector3(dx, dy, dz).Length;
                if (d < LoadDistance)
                {
                    var key = (cx + dx, cy + dy, cz + dz);
                    if (!world.Chunks.ContainsKey(key) && Frustum.ChunkInFrustum(camera, key))
                    //if (!world.Chunks.ContainsKey(key))
                    {
                        toCreate.Add(key);
                        if (toCreate.Count >= CreatePerFrame) break;
                    }
                }
            }

            var prepared = new Chunk[toCreate.Count];

            Parallel.For(0, toCreate.Count, i =>
            {
                var key = toCreate[i];
                var chunk = new Chunk(key.x, key.y, key.z, world.Atlas);
                chunk.GenerateMesh();
                prepared[i] = chunk;
            });

            RemoveFarthestChunk(world, camera.Position/Chunk.Size);
            return prepared;
        }


        private static void RemoveFarthestChunk(World world, Vector3 cameraChunkPos)
        {
            if (world.Chunks.Count == 0) return;

            foreach (var key in world.Chunks.Keys)
            {
                float dx = (float)key.Item1 - cameraChunkPos.X;
                float dy = (float)key.Item2 - cameraChunkPos.Y;
                float dz = (float)key.Item3 - cameraChunkPos.Z;

                float dist = new Vector3(dx, dy, dz).Length;

                if (dist > UnloadDistance)
                {
                    world.Chunks[key].mesh.Dispose();
                    world.Chunks.TryRemove(key, out _);
                }
            }
        }
    }
}