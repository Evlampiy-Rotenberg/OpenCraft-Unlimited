namespace OpenCraft
{
    public class Chunk
    {
        public Mesh mesh = null!;
        public bool empty = true;

        public int X, Y, Z;
        public const int Size = 32;
        public byte[,,] Blocks { get; private set; }
        private readonly Dictionary<(int,int,int), Chunk> Chunks;
        
        SimpleGridAtlas Atlas;

        public Chunk(int x, int y, int z, SimpleGridAtlas atlas, Dictionary<(int,int,int), Chunk> chunks)
        {
            Atlas = atlas;
            
            X = x;
            Y = y;
            Z = z;

            Chunks = chunks;
            Blocks = new byte[Size, Size, Size];

            Generate();
        }

        private void Generate()
        {
            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    for (int z = 0; z < Size; z++)
                    {
                        int blockX = X*Chunk.Size+x;
                        int blockY = Y*Chunk.Size+y;
                        int blockZ = Z*Chunk.Size+z;


                        byte blockType = WorldGenerator.GenerateBlock(blockX, blockY, blockZ);
                        Blocks[x, y, z] = blockType;

                        if (blockType != 0) empty = false;
                    }
                }
            }

            GenerateMesh();
        }

        public (float[] vertices, float[] uvs, uint[] indices) CalcMesh()
        {
            List<float> vertices = new();
            List<float> uvs = new();
            List<uint> indices = new();

            uint indexOffset = 0;

            for (int x = 0; x < Size; x++)
            for (int y = 0; y < Size; y++)
            for (int z = 0; z < Size; z++)
            {
                byte b = GetBlock(x, y, z);
                if (b == 0) continue;

                var region = Atlas.GetRegion(b);

                if (GetBlock(x + 1, y, z) == 0) AddFace(PosX, x, y, z, region, ref vertices, ref uvs, ref indices, ref indexOffset);
                if (GetBlock(x - 1, y, z) == 0) AddFace(NegX, x, y, z, region, ref vertices, ref uvs, ref indices, ref indexOffset);
                if (GetBlock(x, y + 1, z) == 0) AddFace(PosY, x, y, z, region, ref vertices, ref uvs, ref indices, ref indexOffset);
                if (GetBlock(x, y - 1, z) == 0) AddFace(NegY, x, y, z, region, ref vertices, ref uvs, ref indices, ref indexOffset);
                if (GetBlock(x, y, z + 1) == 0) AddFace(PosZ, x, y, z, region, ref vertices, ref uvs, ref indices, ref indexOffset);
                if (GetBlock(x, y, z - 1) == 0) AddFace(NegZ, x, y, z, region, ref vertices, ref uvs, ref indices, ref indexOffset);
            }

            return (vertices.ToArray(), uvs.ToArray(), indices.ToArray());
        }


        public void GenerateMesh()
        {
            var (v, uv, ind) = CalcMesh();
            
            if (ind.Length == 0){empty = true;}
            else {empty = false;}

            mesh = new Mesh(v, uv, ind);
        }

        static readonly int[][] PosX = {new[]{1,0,0}, new[]{1,1,0}, new[]{1,1,1}, new[]{1,0,1}};
        static readonly int[][] NegX = {new[]{0,0,1}, new[]{0,1,1}, new[]{0,1,0}, new[]{0,0,0}};
        static readonly int[][] PosY = {new[]{0,1,1}, new[]{1,1,1}, new[]{1,1,0}, new[]{0,1,0}};
        static readonly int[][] NegY = {new[]{0,0,0}, new[]{1,0,0}, new[]{1,0,1}, new[]{0,0,1}};
        static readonly int[][] PosZ = {new[]{1,0,1}, new[]{1,1,1}, new[]{0,1,1}, new[]{0,0,1}};
        static readonly int[][] NegZ = {new[]{0,0,0}, new[]{0,1,0}, new[]{1,1,0}, new[]{1,0,0}};

        void AddFace(
            int[][] face, int x, int y, int z,
            SimpleGridAtlas.Region r,
            ref List<float> v, ref List<float> uv, ref List<uint> ind, ref uint indexOffset)
        {
            for (int i = 0; i < 4; i++)
            {
                v.Add(x + face[i][0]);
                v.Add(y + face[i][1]);
                v.Add(z + face[i][2]);
            }

            uv.Add(r.U0); uv.Add(r.V0);
            uv.Add(r.U0); uv.Add(r.V1);
            uv.Add(r.U1); uv.Add(r.V1);
            uv.Add(r.U1); uv.Add(r.V0);

            ind.Add(indexOffset);
            ind.Add(indexOffset + 1);
            ind.Add(indexOffset + 2);

            ind.Add(indexOffset);
            ind.Add(indexOffset + 2);
            ind.Add(indexOffset + 3);

            indexOffset += 4;
        }

        public byte GetBlock(int x, int y, int z)
        {
            if ((uint)x < Size && (uint)y < Size && (uint)z < Size)
                return Blocks[x, y, z];

            int ncx = X;
            int ncy = Y;
            int ncz = Z;

            if (x < 0) { ncx--; x += Size; }
            else if (x >= Size) { ncx++; x -= Size; }

            if (y < 0) { ncy--; y += Size; }
            else if (y >= Size) { ncy++; y -= Size; }

            if (z < 0) { ncz--; z += Size; }
            else if (z >= Size) { ncz++; z -= Size; }

            if (Chunks.TryGetValue((ncx, ncy, ncz), out Chunk? neighbor) && neighbor != null)
                return neighbor.GetBlock(x, y, z);

            int worldX = ncx * Size + x;
            int worldY = ncy * Size + y;
            int worldZ = ncz * Size + z;

            return WorldGenerator.GenerateBlock(worldX, worldY, worldZ);
        }

        public void SetBlock(int x, int y, int z, byte value)
        {
            Blocks[x, y, z] = value;
        }
    }
}
