using OpenTK.Mathematics;

namespace OpenCraft
{
    class GameObject
    {
        public Vector3 Position;
        public Vector3 Size;
        public Vector3 Velocity;

        public Dictionary<(int, int, int), Chunk> World = new();

        public GameObject(Vector3 startPosition, Vector3 objectSize)
        {
            Position = startPosition;
            Size = objectSize;
            Velocity = Vector3.Zero;
        }

        public void SetWorld(Dictionary<(int, int, int), Chunk> world)
        {
        	World = world;
        }


		public void Update(float dt)
		{
		    Velocity += new Vector3(0f, -35f, 0f)*dt;
		    Velocity *= new Vector3(MathF.Pow(0.9f, dt*100), MathF.Pow(0.99f, dt*100), MathF.Pow(0.9f, dt*100));

		    Position.X += Velocity.X * dt;
		    ResolveAxisCollisions(axis: 0);

		    Position.Y += Velocity.Y * dt;
		    ResolveAxisCollisions(axis: 1);

		    Position.Z += Velocity.Z * dt;
		    ResolveAxisCollisions(axis: 2);
		}


		private void ResolveAxisCollisions(int axis)
		{
		    List<Vector3i> hits = CheckCollision();
		    if (hits.Count == 0)
		        return;

		    float v = axis == 0 ? Velocity.X : axis == 1 ? Velocity.Y : Velocity.Z;
		    if (v == 0f)
		        return;

		    int chosenCoord = v > 0f ? int.MaxValue : int.MinValue;

		    foreach (var b in hits)
		    {
		        int c = axis == 0 ? b.X : axis == 1 ? b.Y : b.Z;
		        if (v > 0f) {if (c < chosenCoord) chosenCoord = c;}
		        else {if (c > chosenCoord) chosenCoord = c;}
		    }

		    if (axis == 0)
		    {
		        if (v > 0f)
		            Position.X = chosenCoord - Size.X;
		        else
		            Position.X = chosenCoord + 1f;

		        Velocity.X = 0f;
		    }
		    else if (axis == 1)
		    {
		        if (v > 0f)
		            Position.Y = chosenCoord - Size.Y;
		        else
		            Position.Y = chosenCoord + 1f;

		        Velocity.Y = 0f;
		    }
		    else
		    {
		        if (v > 0f)
		            Position.Z = chosenCoord - Size.Z;
		        else
		            Position.Z = chosenCoord + 1f;

		        Velocity.Z = 0f;
		    }
		}

		public List<Vector3i> CheckCollision()
		{
		    int S = Chunk.Size;

		    int minX = (int)MathF.Floor(Position.X);
		    int minY = (int)MathF.Floor(Position.Y);
		    int minZ = (int)MathF.Floor(Position.Z);

		    int maxX = (int)MathF.Ceiling(Position.X + Size.X) - 1;
		    int maxY = (int)MathF.Ceiling(Position.Y + Size.Y) - 1;
		    int maxZ = (int)MathF.Ceiling(Position.Z + Size.Z) - 1;

		    List<Vector3i> touchedBlocks = new List<Vector3i>();

		    for (int x = minX; x <= maxX; x++)
		    for (int y = minY; y <= maxY; y++)
		    for (int z = minZ; z <= maxZ; z++)
		    {
		        int chunkX = (int)MathF.Floor(x / (float)S);
		        int chunkY = (int)MathF.Floor(y / (float)S);
		        int chunkZ = (int)MathF.Floor(z / (float)S);

		        int localX = x - chunkX * S;
		        int localY = y - chunkY * S;
		        int localZ = z - chunkZ * S;

		        if (!World.TryGetValue((chunkX, chunkY, chunkZ), out Chunk? chunk))
		            continue;

		        byte block = chunk.Blocks[localX, localY, localZ];
		        if (block == 0)
		            continue;

		        touchedBlocks.Add(new Vector3i(x, y, z));
		    }
		    return touchedBlocks;
		}
    }
}
