namespace OpenCraft
{
    public static class TestModel
    {
        public static Mesh CreateCube()
        {
            float[] vertices =
            {
                // Front (+Z)
                -0.5f, -0.5f,  0.5f,
                 0.5f, -0.5f,  0.5f,
                 0.5f,  0.5f,  0.5f,
                -0.5f,  0.5f,  0.5f,

                // Back (-Z)
                 0.5f, -0.5f, -0.5f,
                -0.5f, -0.5f, -0.5f,
                -0.5f,  0.5f, -0.5f,
                 0.5f,  0.5f, -0.5f,

                // Left (-X)
                -0.5f, -0.5f, -0.5f,
                -0.5f, -0.5f,  0.5f,
                -0.5f,  0.5f,  0.5f,
                -0.5f,  0.5f, -0.5f,

                // Right (+X)
                 0.5f, -0.5f,  0.5f,
                 0.5f, -0.5f, -0.5f,
                 0.5f,  0.5f, -0.5f,
                 0.5f,  0.5f,  0.5f,

                // Top (+Y)
                -0.5f,  0.5f,  0.5f,
                 0.5f,  0.5f,  0.5f,
                 0.5f,  0.5f, -0.5f,
                -0.5f,  0.5f, -0.5f,

                // Bottom (-Y)
                -0.5f, -0.5f, -0.5f,
                 0.5f, -0.5f, -0.5f,
                 0.5f, -0.5f,  0.5f,
                -0.5f, -0.5f,  0.5f
            };

            // Same UV for each face: (0,0) (1,0) (1,1) (0,1)
            float[] texCoords =
            {
                // Front
                0f, 0f,
                1f, 0f,
                1f, 1f,
                0f, 1f,

                // Back
                0f, 0f,
                1f, 0f,
                1f, 1f,
                0f, 1f,

                // Left
                0f, 0f,
                1f, 0f,
                1f, 1f,
                0f, 1f,

                // Right
                0f, 0f,
                1f, 0f,
                1f, 1f,
                0f, 1f,

                // Top
                0f, 0f,
                1f, 0f,
                1f, 1f,
                0f, 1f,

                // Bottom
                0f, 0f,
                1f, 0f,
                1f, 1f,
                0f, 1f
            };

            uint[] indices =
            {
                0, 1, 2,  2, 3, 0,       // Front
                4, 5, 6,  6, 7, 4,       // Back
                8, 9,10, 10,11, 8,       // Left
               12,13,14, 14,15,12,       // Right
               16,17,18, 18,19,16,       // Top
               20,21,22, 22,23,20        // Bottom
            };

            return new Mesh(vertices, texCoords, indices);
        }
    }
}
