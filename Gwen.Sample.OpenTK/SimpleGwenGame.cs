using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using OpenTK;
using OpenTK.Input;
using OpenTK.Graphics.OpenGL;
using System.Drawing;
using Gwen.Control;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Gwen.Sample.OpenTK;

/// <summary>
/// Demonstrates the GameWindow class.
/// </summary>
public class SimpleWindow : GameWindow
{
    private Gwen.Input.OpenTK input;
    private Gwen.Renderer.OpenTK renderer;
    private Gwen.Skin.Base skin;
    private Gwen.Control.Canvas canvas;
    private UnitTest.UnitTest test;

    const int fps_frames = 50;
    private readonly List<long> ftime;
    private readonly Stopwatch stopwatch;
    private long lastTime;
    private bool altDown = false;

    public SimpleWindow()
        : base(new GameWindowSettings(), new NativeWindowSettings{ClientSize = new Vector2i(1024, 768)})
    {
        KeyDown += Keyboard_KeyDown;
        KeyUp += Keyboard_KeyUp;

        MouseDown += Mouse_ButtonDown;
        MouseUp += Mouse_ButtonUp;
        MouseMove += Mouse_Move;
        MouseWheel += Mouse_Wheel;

        ftime = new List<long>(fps_frames);
        stopwatch = new Stopwatch();
    }

    public override void Dispose()
    {
        canvas.Dispose();
        skin.Dispose();
        renderer.Dispose();
        base.Dispose();
    }

    /// <summary>
    /// Occurs when a key is pressed.
    /// </summary>
    /// <param name="e">The key that was pressed.</param>
    void Keyboard_KeyDown(KeyboardKeyEventArgs e)
    {
        if (e.Key == Keys.Escape)
            Close();
        else if (e.Key == Keys.LeftAlt)
            altDown = true;
        else if (altDown && e.Key == Keys.Enter)
            if (WindowState == WindowState.Fullscreen)
                WindowState = WindowState.Normal;
            else
                WindowState = WindowState.Fullscreen;

        input.ProcessKeyDown(e);
    }

    void Keyboard_KeyUp(KeyboardKeyEventArgs e)
    {
        altDown = false;
        input.ProcessKeyUp(e);
    }

    void Mouse_ButtonDown(MouseButtonEventArgs args)
    {
        input.ProcessMouseMessage(args);
    }

    void Mouse_ButtonUp(MouseButtonEventArgs args)
    {
        input.ProcessMouseMessage(args);
    }

    void Mouse_Move(MouseMoveEventArgs args)
    {
        input.ProcessMouseMessage(args);
    }

    void Mouse_Wheel(MouseWheelEventArgs args)
    {
        input.ProcessMouseMessage(args);
    }

    /// <summary>
    /// Setup OpenGL and load resources here.
    /// </summary>
    protected override void OnLoad()
    {
        GL.ClearColor(Color.MidnightBlue);

        renderer = new Gwen.Renderer.OpenTK(this);
        skin = new Gwen.Skin.TexturedBase(renderer, "DefaultSkin.png");
        //skin = new Gwen.Skin.Simple(renderer);
        //skin.DefaultFont = new Font(renderer, "Courier", 10);
        canvas = new Canvas(skin);

        input = new Input.OpenTK(this);
        input.Initialize(canvas);

        canvas.SetSize(ClientSize.X, ClientSize.Y);
        canvas.ShouldDrawBackground = true;
        canvas.BackgroundColor = Color.FromArgb(255, 150, 170, 170);
        //canvas.KeyboardInputEnabled = true;

        test = new UnitTest.UnitTest(canvas);

        stopwatch.Restart();
        lastTime = 0;
    }

    /// <summary>
    /// Respond to resize events here.
    /// </summary>
    /// <param name="e">Contains information on the new GameWindow size.</param>
    /// <remarks>There is no need to call the base implementation.</remarks>
    protected override void OnResize(ResizeEventArgs e)
    {
        GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
        GL.MatrixMode(MatrixMode.Projection);
        GL.LoadIdentity();
        GL.Ortho(0, ClientSize.X, ClientSize.Y, 0, -1, 1);

        canvas.SetSize(ClientSize.X, ClientSize.Y);
    }

    /// <summary>
    /// Add your game logic here.
    /// </summary>
    /// <param name="e">Contains timing information.</param>
    /// <remarks>There is no need to call the base implementation.</remarks>
    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        if (ftime.Count == fps_frames)
            ftime.RemoveAt(0);

        ftime.Add(stopwatch.ElapsedMilliseconds - lastTime);
        lastTime = stopwatch.ElapsedMilliseconds;

        if (stopwatch.ElapsedMilliseconds > 1000)
        {
            test.Note = String.Format("String Cache size: {0} Draw Calls: {1} Vertex Count: {2}", renderer.TextCacheSize, renderer.DrawCallCount, renderer.VertexCount);
            test.Fps = 1000f * ftime.Count / ftime.Sum();
            stopwatch.Restart();

            if (renderer.TextCacheSize > 1000) // each cached string is an allocated texture, flush the cache once in a while in your real project
                renderer.FlushTextCache();
        }
    }

    /// <summary>
    /// Add your game rendering code here.
    /// </summary>
    /// <param name="e">Contains timing information.</param>
    /// <remarks>There is no need to call the base implementation.</remarks>
    protected override void OnRenderFrame(FrameEventArgs e)
    {
        GL.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);
        canvas.RenderCanvas();

        SwapBuffers();
    }

    /// <summary>
    /// Entry point of this example.
    /// </summary>
    [STAThread]
    public static void Main() {
        using var example = new SimpleWindow();
        example.Title = "Gwen-DotNet OpenTK test";
        example.VSync = VSyncMode.Off; // to measure performance
        example.Run();
        //example.TargetRenderFrequency = 60;
    }
}