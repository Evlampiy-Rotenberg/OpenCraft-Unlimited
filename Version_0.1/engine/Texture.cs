using OpenTK.Graphics.OpenGL4;
using StbImageSharp;
using System;
using System.IO;

namespace OpenCraft
{
    public class Texture : IDisposable
    {
        public int ID { get; private set; }

        public Texture(
            string path,
            TextureUnit unit = TextureUnit.Texture0,
            TextureWrapMode wrap = TextureWrapMode.Repeat,
            TextureMinFilter minFilter = TextureMinFilter.Nearest,
            TextureMagFilter magFilter = TextureMagFilter.Nearest)
        {
            ID = GL.GenTexture();
            Bind(unit);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)wrap);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)wrap);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)minFilter);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)magFilter);

            StbImage.stbi_set_flip_vertically_on_load(1);

            using var stream = File.OpenRead(path);
            ImageResult img = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

            GL.TexImage2D(
                TextureTarget.Texture2D,
                level: 0,
                internalformat: PixelInternalFormat.Rgba,
                width: img.Width,
                height: img.Height,
                border: 0,
                format: PixelFormat.Rgba,
                type: PixelType.UnsignedByte,
                pixels: img.Data);

            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        public void Bind(TextureUnit unit = TextureUnit.Texture0)
        {
            GL.ActiveTexture(unit);
            GL.BindTexture(TextureTarget.Texture2D, ID);
        }

        public void Dispose()
        {
            if (ID != 0)
            {
                GL.DeleteTexture(ID);
                ID = 0;
            }
        }
    }
}
