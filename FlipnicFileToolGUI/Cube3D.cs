using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Avalonia;
using Avalonia.Input;
using FlipnicFileToolGUI.Shaders;
using FlipnicFileToolGUI.Textures;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTKAvalonia;
using System.IO;
using FlipnicLib;
using FlipnicLib.Formats;

namespace FlipnicFileToolGUI
{
    public class CubeRenderingTkOpenGlControl : BaseTkOpenGlControl
    {
        private UiOpenGlShader? _shader;
        private int _vertexBufferObject;
        private int _vertexArrayObject;
        private int _elementBufferObject;
        private OpenGlTexture _brickTexture;

        private Vector3 _cameraPosition = new(0, 2, 2);
        private Vector3 _cameraFront;
        private Vector3 _up = Vector3.UnitY;
        private float _fov = 45;
        private double _pitch = -40;
        private double _yaw = 90f;
        private float _modelRotationDegrees = 0f;
        private bool _isDragging;
        private Point _lastPos;

        private const float Speed = 0.015f;
        private object? _texture;

        private float[] _vertices = [];
        private readonly uint[] _indices =
        {
            0, 1, 3, // first triangle
            1, 2, 3, // second triangle
        };

        public CubeRenderingTkOpenGlControl()
        {
            if (Debugger.IsAttached) Console.WriteLine("UI: Creating OpenGLControl");

            //Initial camera facing update
            UpdateCameraFront();
        }

        public void SaveAs(string fileName)
        {
            StaticUtils.ExportObj(fileName, _vertices, _texture);
        }

        public string GetVertices()
        {
            StringBuilder sb = new();
            for (int i = 0; i < _vertices.Length; i += 4)
            {
                try
                {
                    sb.Append($"X={_vertices[i]},Y={_vertices[i + 1]},Z={_vertices[i + 2]}\n");
                }
                catch
                {
                    sb.Append($"---\n");
                }
            }

            return sb.Length > 0 ? sb.ToString()[..(sb.Length - 1)] : "";
        }


        public void ImportLP4(Lp4 lp4)
        {
            lp4.Read();
            _texture = null;
            if (lp4.Texture != null)
            {
                _texture = lp4.Texture;
            }
            OpenTkInit();
            GL.ClearColor(0.6f, 0.6f, 1f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            _vertices = lp4.GetVerticies();
            
            GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
        }

        public void ImportICO(SaveIcon saveIcon)
        {
            _texture = saveIcon.Texture;
            OpenTkInit();
            GL.ClearColor(0.6f, 0.6f, 1f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            List<float> vertices = new();
            foreach (var vertex in saveIcon.Vertices)
            {
                vertices.Add(vertex.TextureX / 4096f);
                vertices.Add(-vertex.TextureY / 4096f);
                vertices.Add(-vertex.CoordX / 4096f);
                vertices.Add(-vertex.CoordY / 4096f);
                vertices.Add(vertex.CoordZ / 4096f);
            }
            _vertices = vertices.ToArray();
            GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
        }

        public string GetInfo(bool more = false)
        {
            if (!more)
            {
                return "Num. of vertices: " + _vertices.Length + "\n" + "Field of view: " + _fov + "\nSpeed: " + Speed;
            }
            else
            {
                return "CameraX: " + _cameraPosition.X + "\nCameraY: " + _cameraPosition.Y + "\nCameraZ: " + _cameraPosition.Z;
            }
        }

        private void DefaultShaders()
        {
            DeleteShadersIfExist();
            Directory.CreateDirectory(Path.GetTempPath() + "Shaders");
            const string frag = """
                                #version 330 core
                                out vec4 outputColor;

                                in vec2 texCoord;

                                uniform sampler2D texture0;

                                void main()
                                {
                                    outputColor = texture(texture0, texCoord);
                                } 
                                """;
            const string vert = """
                                #version 330 core
                                in vec3 aPosition;
                                in vec2 aTexCoord;

                                out vec2 texCoord;

                                uniform mat4 model;
                                uniform mat4 view;
                                uniform mat4 projection;

                                void main()
                                {
                                    texCoord = aTexCoord;
                                    gl_Position = vec4(aPosition, 1.0) * model * view * projection;
                                }
                                """;
            var str = File.OpenWrite(Path.GetTempPath() + "Shaders/shader.vert");
            var buffer = Encoding.UTF8.GetBytes(vert);
            str.Write(buffer, 0, buffer.Length);
            str.Close();
            str = File.OpenWrite(Path.GetTempPath() + "Shaders/shader.frag");
            buffer = Encoding.UTF8.GetBytes(frag);
            str.Write(buffer, 0, buffer.Length);
            str.Close();
        }

        private void DeleteShadersIfExist()
        {
            var pfx = Path.GetTempPath() + "Shaders/";
            if (Directory.Exists(pfx))
            {
                Directory.Delete(pfx, true);
            }
        }
        
        //OpenTkInit is called once when the control is created
        protected override void OpenTkInit()
        {
            DefaultShaders();
            //Compile shaders
            _shader = new(Path.GetTempPath() + "Shaders/shader.vert", Path.GetTempPath() + "Shaders/shader.frag");
            DeleteShadersIfExist();
            //Load textures
            _brickTexture = new();
            _brickTexture.Use();
            _brickTexture.LoadFromFile(_texture);

            //Set textures in shaders
            _shader.Use();
            _shader.SetInt("texture0", 2);

            //Create vertex and buffer objects
            _vertexArrayObject = GL.GenVertexArray();
            _vertexBufferObject = GL.GenBuffer();
            

            //Set bg colour to a dark forest green
            GL.ClearColor(0.6f, 0.5f, 0.9f, 1.0f);

            //Bind to the VAO
            GL.BindVertexArray(_vertexArrayObject);
            GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);
            //Set up the buffer for the triangle
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);

            //Copy triangle vertices to the buffer
            GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * (OperatingSystem.IsMacOS() ? 4 : 1) * sizeof(float), _vertices, BufferUsageHint.StaticDraw);


            //Configure structure of the vertices
            //					  (position parameter in vertex shader, 3 points, data is stored as floats, non-normalized, 5 floats/point, first point at offset 0 in data array)
            GL.VertexAttribPointer(_shader.GetAttribLocation("aPosition"), 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 2 * sizeof(float));
            GL.EnableVertexAttribArray(_shader.GetAttribLocation("aPosition"));


            
            //Configure texture coordinate structure
            var texCoordLocation = _shader.GetAttribLocation("aTexCoord");
            GL.VertexAttribPointer(texCoordLocation, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
            GL.EnableVertexAttribArray(texCoordLocation);

            
            //Set up the EBO
            _elementBufferObject = GL.GenBuffer();

            //Set up its buffer
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _elementBufferObject);
            //Copy data to the buffer
            GL.BufferData(BufferTarget.ElementArrayBuffer, _indices.Length * sizeof(uint), _indices, BufferUsageHint.StaticDraw);

        }

        //OpenTkRender is called once a frame. The aspect ratio and keyboard state are configured prior to this being called.
        protected override void OpenTkRender()
        {
            GL.Enable(EnableCap.DepthTest);

            //Clear the previous frame
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            //Update camera pos etc
            DoUpdate();

            //Render the object(s)
            DoRender();

            //Clean up the opengl state back to how we got it
            GL.Disable(EnableCap.DepthTest);
        }

        //OpenTkTeardown is called when the control is being destroyed
        protected override void OpenTkTeardown()
        {
            //Bind ArrayBuffer to null so we get an error if any more draw operations go through (helps with debugging)
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            //And ElementArrayBuffer
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

            //Delete our VBO and EBO
            GL.DeleteBuffer(_vertexBufferObject);
            GL.DeleteBuffer(_elementBufferObject);
            GL.DeleteVertexArray(_vertexArrayObject);

            //Clean up shaders and textures
            _shader?.Dispose();
            GL.UseProgram(0);
            _brickTexture.Dispose();
        }

        //Demonstrating use of the Avalonia keyboard state provided by OpenTKAvalonia to control the camera 
        private void DoUpdate()
        {
            var effectiveSpeed = Speed;

            if (KeyboardState.IsKeyDown(Key.LeftCtrl))
            {
                effectiveSpeed *= 10;
            }

            if (KeyboardState.IsKeyDown(Key.W))
            {
                _cameraPosition += _cameraFront * effectiveSpeed; //Forward 
            }

            if (KeyboardState.IsKeyDown(Key.S))
            {
                _cameraPosition -= _cameraFront * effectiveSpeed; //Backwards
            }

            if (KeyboardState.IsKeyDown(Key.A))
            {
                _cameraPosition -= Vector3.Normalize(Vector3.Cross(_cameraFront, _up)) * effectiveSpeed; //Left
            }

            if (KeyboardState.IsKeyDown(Key.D))
            {
                _cameraPosition += Vector3.Normalize(Vector3.Cross(_cameraFront, _up)) * effectiveSpeed; //Right
            }

            if (KeyboardState.IsKeyDown(Key.LeftShift))
            {
                //Note this is subtracting up, because..? I think avalonia renders the scene upside down.
                _cameraPosition -= _up * effectiveSpeed; //Up 
            }

            if (KeyboardState.IsKeyDown(Key.Space))
            {
                _cameraPosition += _up * effectiveSpeed; //Down
            }
        }

        private void DoRender()
        {
            //Bind shaders and textures
            _shader!.Use();
            _brickTexture.Use(TextureUnit.Texture2);

            //3d projection matrices
            var model = Matrix4.CreateRotationY(MathHelper.DegreesToRadians(_modelRotationDegrees));
            var view = Matrix4.LookAt(_cameraPosition, _cameraPosition + _cameraFront, _up);
            var projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(_fov), (float)(Bounds.Width / Bounds.Height), 0.1f, 100.0f);

            _shader.SetMatrix4("model", model);
            _shader.SetMatrix4("view", view);
            _shader.SetMatrix4("projection", projection);

            //Load configuration from the VAO
            GL.BindVertexArray(_vertexArrayObject);

            //Draw buffer - a cube
            GL.DrawArrays(PrimitiveType.Triangles, 0, _vertices.Length);
        }

        //The following four methods show how to use the Avalonia events for pointer and scroll input to allow moving the camera by clicking-and-dragging and scrolling to zoom
        //It would appear pointer capture doesn't work, at least not as I would expect it to, which is unfortunate
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            _isDragging = true;
            e.Pointer.Capture(this);
            _lastPos = e.GetPosition(null);
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            _isDragging = false;
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            if (!_isDragging)
                return;

            //Work out the change in position
            var pos = e.GetPosition(null);

            var deltaX = pos.X - _lastPos.X;
            var deltaY = pos.Y - _lastPos.Y;
            _lastPos = pos;

            const float sensitivity = 0.05f;

            //Yaw is a function of the change in X
            _yaw -= deltaX * sensitivity;

            //Clamp pitch
            if (_pitch > 89.0f)
            {
                _pitch = -89.0f;
            }
            else if (_pitch < -89.0f)
            {
                _pitch = 89.0f;
            }
            else
            {
                //Pitch is a function of the change in Y
                _pitch += -deltaY * sensitivity;
            }

            //Recalculate the camera front vector
            UpdateCameraFront();
        }

        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            var scrollDelta = e.Delta.Y; //negative is out, positive is in
            _fov -= (float)scrollDelta; //therefore we subtract, because zooming in should decrease the fov
            if (_fov < 1)
            {
                _fov = 1;
            }
            else if (_fov > 180)
            {
                _fov = 180;
            }
        }
        private void UpdateCameraFront()
        {
            _cameraFront.X = (float)Math.Cos(MathHelper.DegreesToRadians(_pitch)) * (float)Math.Cos(MathHelper.DegreesToRadians(_yaw));
            _cameraFront.Y = (float)Math.Sin(MathHelper.DegreesToRadians(_pitch));
            _cameraFront.Z = -(float)Math.Cos(MathHelper.DegreesToRadians(_pitch)) * (float)Math.Sin(MathHelper.DegreesToRadians(_yaw));
            _cameraFront = Vector3.Normalize(_cameraFront);
        }
    }
}
