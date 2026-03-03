using System;
using System.Collections.Generic;
using System.IO;
using FlipnicLib;
using FlipnicLib.Formats;
using OpenTK.Graphics.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace FlipnicFileToolGUI.Textures
{
    public sealed class OpenGlTexture : IDisposable
    {
        private readonly int _handle;
        private bool _disposedValue;
        private static Random r = new();

        public OpenGlTexture()
        {
            _handle = GL.GenTexture();
        }

        private void LoadFromImgData(Image<Rgba32> image)
        {
            
        }

        public void LoadFromFile(object? texture)
        {
            Image<Rgba32>? image = null;
            try
            {
                image = texture switch
                {
                    Tim tx2 => Image.Load<Rgba32>(new BitmapTools { Icon = tx2 }.ToMemoryStream()),
                    MemoryStream stream => Image.Load<Rgba32>(stream.ToArray()),
                    byte[] ba => Image.Load<Rgba32>(ba),
                    _ => image
                };
            }
            catch
            {
                StaticUtils.DecodeColors("~-C\rError~--: Unable to load texture!\n");
            }

            byte[] rndCol = [(byte)r.Next(0, 255), (byte)r.Next(0, 255), (byte)r.Next(0, 255)];
            List<byte> pixels = [];
            if (image != null)
            {

                //ImageSharp counts (0, 0) as top-left, OpenGL wants it to be bottom-left. fix.
                image.Mutate(x => x.Flip(FlipMode.Vertical));

                //Convert ImageSharp's format into a byte array, so we can use it with OpenGL.
                pixels = new List<byte>(4 * image.Width * image.Height);

                for (var y = 0; y < image.Height; y++)
                {
                    var row = image.Frames[0].PixelBuffer.DangerousGetRowSpan(y);

                    for (var x = 0; x < image.Width; x++)
                    {
                        pixels.Add(row[x].R);
                        pixels.Add(row[x].G);
                        pixels.Add(row[x].B);
                        pixels.Add(row[x].A);
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

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, (image != null) ? image.Width : 63, (image != null) ? image.Height : 63, 0, PixelFormat.Rgba, PixelType.UnsignedByte, pixels.ToArray());
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        }

        public void Use(TextureUnit unit = TextureUnit.Texture0)
        {
            GL.ActiveTexture(unit);
            GL.BindTexture(TextureTarget.Texture2D, _handle);
        }

        ~OpenGlTexture()
        {
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
