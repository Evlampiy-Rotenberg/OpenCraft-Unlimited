using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace OpenCraft
{
    internal partial class Game: GameWindow
    {
        int gameMode = 0;

        static void PlayerControllerUpdate0(KeyboardState input, float dt)
        {
            Vector3 forwardFlat = new Vector3(MainCamera.front.X, 0f, MainCamera.front.Z);

            float speed_x = 1.0f;
            float playerSpeed = 50f;
            float jumpPower = 12f;

            if (input.IsKeyDown(Keys.LeftControl))
                speed_x = 2.2f;

            if (input.IsKeyDown(Keys.LeftShift))
                speed_x = 0.3f;

            if (forwardFlat.LengthSquared > 0f)
                forwardFlat = Vector3.Normalize(forwardFlat);

            if (input.IsKeyDown(Keys.W))
                Player.Velocity += playerSpeed * speed_x * forwardFlat * dt;

            if (input.IsKeyDown(Keys.S))
                Player.Velocity += -playerSpeed * speed_x * forwardFlat * dt;

            if (input.IsKeyDown(Keys.A))
                Player.Velocity += -playerSpeed * speed_x * MainCamera.right * dt;

            if (input.IsKeyDown(Keys.D))
                Player.Velocity += playerSpeed * speed_x * MainCamera.right * dt;

            if (input.IsKeyDown(Keys.Space))
            {
                Vector3 probePos = new Vector3(Player.Position.X, Player.Position.Y-0.1f, Player.Position.Z);
                Vector3 probeSize = new Vector3(Player.Size.X, 0.1f, Player.Size.Z);

                if (Player.CheckCollision(probePos, probeSize).Count > 0 && Player.Velocity.Y == 0f)
                {
                    Player.Velocity.Y += jumpPower;
                    if (input.IsKeyDown(Keys.W) && input.IsKeyDown(Keys.LeftShift))
                    {
                        Player.Velocity += 40f * forwardFlat;
                    }
                }
            }
            updateInventory(input);
        }


        static void PlayerControllerUpdate1(KeyboardState input, float dt)
        {
            Vector3 forwardFlat = new Vector3(MainCamera.front.X, 0f, MainCamera.front.Z);

            float speed_x = 2.0f;
            float playerSpeed = 50f;

            if (input.IsKeyDown(Keys.LeftControl))
                speed_x = 8.0f;

            if (forwardFlat.LengthSquared > 0f)
                forwardFlat = Vector3.Normalize(forwardFlat);

            if (input.IsKeyDown(Keys.W))
                Player.Velocity += playerSpeed * speed_x * forwardFlat * dt;

            if (input.IsKeyDown(Keys.S))
                Player.Velocity += -playerSpeed * speed_x * forwardFlat * dt;

            if (input.IsKeyDown(Keys.A))
                Player.Velocity += -playerSpeed * speed_x * MainCamera.right * dt;

            if (input.IsKeyDown(Keys.D))
                Player.Velocity += playerSpeed * speed_x * MainCamera.right * dt;

            if (input.IsKeyDown(Keys.Space))
            {
                Player.Velocity.Y += playerSpeed * speed_x * dt;
            }

            if (input.IsKeyDown(Keys.LeftShift))
            {
                Player.Velocity.Y -= playerSpeed * speed_x * dt;
            }

            updateInventory(input);
        }

        static void updateInventory(KeyboardState input)
        {
            if (input.IsKeyPressed(Keys.D1)) selectedBlock = 1;
            if (input.IsKeyPressed(Keys.D2)) selectedBlock = 2;
            if (input.IsKeyPressed(Keys.D3)) selectedBlock = 3;
            if (input.IsKeyPressed(Keys.D4)) selectedBlock = 4;
            if (input.IsKeyPressed(Keys.D5)) selectedBlock = 5;
            if (input.IsKeyPressed(Keys.D6)) selectedBlock = 6;
            if (input.IsKeyPressed(Keys.D7)) selectedBlock = 7;
            if (input.IsKeyPressed(Keys.D8)) selectedBlock = 8;
            if (input.IsKeyPressed(Keys.D9)) selectedBlock = 9;
        }
    }
}