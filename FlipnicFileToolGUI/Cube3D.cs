using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Input;
using FlipnicFileToolGUI.Shaders;
using FlipnicFileToolGUI.Textures;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTKAvalonia;
using FlipnicLib;
using System.IO;

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
        private Tim2? _texture;

        private float[] _vertices = [-0.5f, -0.5f, -0.5f, 0.0f, 0.0f, 0.5f, -0.5f, -0.5f, 1.0f, 0.0f, 0.5f, 0.5f, -0.5f, 1.0f, 1.0f, 0.5f, 0.5f, -0.5f, 1.0f, 1.0f, -0.5f, 0.5f, -0.5f, 0.0f, 1.0f, -0.5f, -0.5f, -0.5f, 0.0f, 0.0f, -0.5f, -0.5f, 0.5f, 0.0f, 0.0f, 0.5f, -0.5f, 0.5f, 1.0f, 0.0f, 0.5f, 0.5f, 0.5f, 1.0f, 1.0f, 0.5f, 0.5f, 0.5f, 1.0f, 1.0f, -0.5f, 0.5f, 0.5f, 0.0f, 1.0f, -0.5f, -0.5f, 0.5f, 0.0f, 0.0f, -0.5f, 0.5f, 0.5f, 1.0f, 0.0f, -0.5f, 0.5f, -0.5f, 1.0f, 1.0f, -0.5f, -0.5f, -0.5f, 0.0f, 1.0f, -0.5f, -0.5f, -0.5f, 0.0f, 1.0f, -0.5f, -0.5f, 0.5f, 0.0f, 0.0f, -0.5f, 0.5f, 0.5f, 1.0f, 0.0f, 0.5f, 0.5f, 0.5f, 1.0f, 0.0f, 0.5f, 0.5f, -0.5f, 1.0f, 1.0f, 0.5f, -0.5f, -0.5f, 0.0f, 1.0f, 0.5f, -0.5f, -0.5f, 0.0f, 1.0f, 0.5f, -0.5f, 0.5f, 0.0f, 0.0f, 0.5f, 0.5f, 0.5f, 1.0f, 0.0f, -0.5f, -0.5f, -0.5f, 0.0f, 1.0f, 0.5f, -0.5f, -0.5f, 1.0f, 1.0f, 0.5f, -0.5f, 0.5f, 1.0f, 0.0f, 0.5f, -0.5f, 0.5f, 1.0f, 0.0f, -0.5f, -0.5f, 0.5f, 0.0f, 0.0f, -0.5f, -0.5f, -0.5f, 0.0f, 1.0f, -0.5f, 0.5f, -0.5f, 0.0f, 1.0f, 0.5f, 0.5f, -0.5f, 1.0f, 1.0f, 0.5f, 0.5f, 0.5f, 1.0f, 0.0f, 0.5f, 0.5f, 0.5f, 1.0f, 0.0f, -0.5f, 0.5f, 0.5f, 0.0f, 0.0f, -0.5f, 0.5f, -0.5f, 0.0f, 1.0f];
        private readonly uint[] _indices =
        {
            0, 1, 2, // first triangle
            6, 5, 4, // second triangle
        };

        public CubeRenderingTkOpenGlControl()
        {
            Console.WriteLine("UI: Creating OpenGLControl");

            //Initial camera facing update
            UpdateCameraFront();
        }

        public void SaveAs(string fileName)
        {
            Lp4.ExportObj(_vertices, fileName);
        }

        public string GetVertices()
        {
            StringBuilder sb = new();
            for (int i = 0; i < _vertices.Length; i += 4)
            {
                sb.Append($"X={_vertices[i]},Y={_vertices[i + 1]},Z={_vertices[i + 2]}\n");
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

        //OpenTkInit is called once when the control is created
        protected override void OpenTkInit()
        {
            //Compile shaders
            _shader = new("Shaders/shader.vert", "Shaders/shader.frag");

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
            GL.ClearColor(0.6f, 0.6f, 1f, 1.0f);

            //Bind to the VAO
            GL.BindVertexArray(_vertexArrayObject);

            //Set up the buffer for the triangle
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);

            //Copy triangle vertices to the buffer
            GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * sizeof(float), _vertices, BufferUsageHint.StaticDraw);
            

            //Configure structure of the vertices
            //					  (position parameter in vertex shader, 3 points, data is stored as floats, non-normalized, 5 floats/point, first point at offset 0 in data array)
            GL.VertexAttribPointer(_shader.GetAttribLocation("aPosition"), 3, VertexAttribPointerType.Float, true, 4 * sizeof(float), 0);
            GL.EnableVertexAttribArray(_shader.GetAttribLocation("aPosition"));


            //Configure texture coordinate structure
            var texCoordLocation = _shader.GetAttribLocation("aTexCoord");
            GL.VertexAttribPointer(texCoordLocation, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 4 * sizeof(float));
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
            // model *= Matrix4.CreateRotationX(MathHelper.DegreesToRadians(-30));
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
