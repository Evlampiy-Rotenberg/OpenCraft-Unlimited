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

namespace OpenCraft
{
	internal class Camera
	{
		private float screenWidth;
		private float screenHeight;
		private float Sensitivity = 0.35f;

		public Vector3 Position;

		public Vector3 front;
		public Vector3 right;
		public Vector3 up;
		
		public float pitch = 0.0f;
		public float yaw = -90.0f;

		private bool firstMove = true;
		public Vector2 lastPos;

		float DefaultFOV;
		float FOV;

		public void ResetMouse()
		{
		    firstMove = true;
		}

		public Camera(Vector3 position, float width, float height, float fov = 90f)
		{
			Position = position;
			screenWidth = width;
			screenHeight = height;
			DefaultFOV = fov;
			FOV = fov;
			
		}

		public void Resize(float newWidth, float newHeight)
		{
			screenWidth = newWidth;
			screenHeight = newHeight;
		}

		public Matrix4 GetViewMatrix()
		{
			return Matrix4.LookAt(Position, Position + front, up);
		}

		public Matrix4 GetProjectionMatrix()
		{
			return Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(FOV), screenWidth/screenHeight, 0.1f, 5000.0f);
		}

		public void UpdateVectors()
		{
			if (pitch > 89.9f) {pitch = 89.9f;}
			if (pitch < -89.9f)	{pitch = -89.9f;}

			front.X = MathF.Cos(MathHelper.DegreesToRadians(pitch)) * MathF.Cos(MathHelper.DegreesToRadians(yaw));
			front.Y = MathF.Sin(MathHelper.DegreesToRadians(pitch));
			front.Z = MathF.Cos(MathHelper.DegreesToRadians(pitch)) * MathF.Sin(MathHelper.DegreesToRadians(yaw));

			front = Vector3.Normalize(front);

			right = Vector3.Normalize(Vector3.Cross(front, Vector3.UnitY));
			up = Vector3.Normalize(Vector3.Cross(right, front));
		}

		public void InputController(KeyboardState input, MouseState mouse, FrameEventArgs e)
		{
			float sensitivity;
			if (input.IsKeyDown(Keys.C))
			{
				FOV = DefaultFOV / 4.5f;
				sensitivity = Sensitivity / 4.5f;
			}
			else
			{
				FOV = DefaultFOV;
				sensitivity = Sensitivity;
			}

			if (firstMove)
			{
				lastPos = new Vector2(mouse.X, mouse.Y);
				firstMove = false;
			}
			else
			{
				var deltaX = mouse.X - lastPos.X;
				var deltaY = mouse.Y - lastPos.Y;
				lastPos = new Vector2(mouse.X, mouse.Y);

				yaw += deltaX * sensitivity;
				pitch -= deltaY * sensitivity;
			}
		}

		public void Update(KeyboardState input, MouseState mouse, FrameEventArgs e)
		{
			InputController(input, mouse, e);
			UpdateVectors();
		}
	}
}