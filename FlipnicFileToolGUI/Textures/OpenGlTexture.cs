using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using FlipnicLib;
using FlipnicLib.Formats;
using OpenTK.Graphics.OpenGL;

namespace FlipnicFileToolGUI.Textures
{
    public sealed class OpenGlTexture : IDisposable
    {
        private readonly int _handle = GL.GenTexture();
        private bool _disposedValue;

        public static void LoadFromFile(object? texture)
        {
            Image? image = null;
            try
            {
                image = texture switch
                {
                    Tim tx2 => Image.FromStream(new BitmapTools { Icon = tx2 }.ToMemoryStream()),
                    MemoryStream stream => Image.FromStream(stream),
                    byte[] ba => Image.FromStream(new MemoryStream(ba)),
                    _ => image
                };
            }
            catch
            {
                StaticUtils.DecodeColors("~-C\rError~--: Unable to load texture!\n");
            }

            List<byte> pixels;
            if (image != null)
            {

                //Convert System.Drawing format into a byte array, so we can use it with OpenGL.
                var bmp = new Bitmap(image);
                pixels = new List<byte>(4 * image.Width * image.Height);
                
                //System.Drawing counts (0, 0) as top-left, OpenGL wants it to be bottom-left, so we read rows backwards
                for (var y = image.Height - 1; y >= 0; y--)
                {
                    for (var x = 0; x < image.Width; x++)
                    {
                        pixels.Add(bmp.GetPixel(x, y).R);
                        pixels.Add(bmp.GetPixel(x, y).G);
                        pixels.Add(bmp.GetPixel(x, y).B);
                        pixels.Add(bmp.GetPixel(x, y).A);
                    }
                }
            } else // no texture provided, so we'll just render a magenta/black checkerboard pattern
            {
                var duplicate = 7;
                pixels = new(4 * 9 * duplicate * duplicate * 9);
                var black = false;

                for (var y = 0; y < 9; y++)
                {
                    for (var t = 0; t < duplicate; t++)
                    {
                        for (var x = 0; x < 9; x++)
                        {
                            for (var w = 0; w < duplicate; w++)
                            {
                                if (!black)
                                {
                                    pixels.Add(255);
                                    pixels.Add(0);
                                    pixels.Add(255);
                                    pixels.Add(255);
                                }
                                else
                                {
                                    pixels.Add(0);
                                    pixels.Add(0);
                                    pixels.Add(0);
                                    pixels.Add(255);
                                }
                            }
                            black = !black;
                        }
                    }
                }
            }

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, image?.Width ?? 63, image?.Height ?? 63, 0, PixelFormat.Rgba, PixelType.UnsignedByte, pixels.ToArray());
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        }

        public void Use(TextureUnit unit = TextureUnit.Texture0)
        {
            GL.ActiveTexture(unit);
            GL.BindTexture(TextureTarget.Texture2D, _handle);
        }

        ~OpenGlTexture()
        {
            if (OperatingSystem.IsWindows())
            {
                return;
            }
            GL.DeleteTexture(_handle);
        }

        public void Dispose()
        {
            if (_disposedValue)
            {
                return;
            }

            GL.DeleteTexture(_handle);

            _disposedValue = true;

            GC.SuppressFinalize(this);
        }
    }
}
