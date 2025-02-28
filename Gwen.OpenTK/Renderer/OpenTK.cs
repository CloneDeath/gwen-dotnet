using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.Desktop;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using Color = System.Drawing.Color;
using Image = SixLabors.ImageSharp.Image;
using PixelFormat = SixLabors.ImageSharp.PixelFormats;
using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;

namespace Gwen.Renderer
{
    public class OpenTK : Base
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Vertex
        {
            public short x, y;
            public float u, v;
            public byte r, g, b, a;
        }

        private const int MaxVerts = 1024;
        private Color m_Color;
        private int m_VertNum;
        private readonly Vertex[] m_Vertices;
        private readonly int m_VertexSize;

        private readonly Dictionary<Tuple<String, Font>, TextRenderer> m_StringCache;
        private int m_DrawCallCount;
        private bool m_ClipEnabled;
        private bool m_TextureEnabled;
        private static int m_LastTextureID;

        private bool m_WasBlendEnabled, m_WasTexture2DEnabled, m_WasDepthTestEnabled;
        private int m_PrevBlendSrc, m_PrevBlendDst, m_PrevAlphaFunc;
        private float m_PrevAlphaRef;
        private readonly GameWindow m_Parent;
        private readonly bool m_RestoreRenderState;

        public OpenTK(GameWindow parent, bool restoreRenderState = true) {
            m_Vertices = new Vertex[MaxVerts];
            m_VertexSize = Marshal.SizeOf(m_Vertices[0]);
            m_StringCache = new Dictionary<Tuple<String, Font>, TextRenderer>();
            m_Parent = parent;
            m_RestoreRenderState = restoreRenderState;
        }

        public override void Dispose()
        {
            FlushTextCache();
            base.Dispose();
        }

        public override void Begin()
        {
            if (m_RestoreRenderState)
            {
                // Get previous parameter values before changing them.
                GL.GetInteger(GetPName.BlendSrc, out m_PrevBlendSrc);
                GL.GetInteger(GetPName.BlendDst, out m_PrevBlendDst);
                GL.GetInteger(GetPName.AlphaTestFunc, out m_PrevAlphaFunc);
                GL.GetFloat(GetPName.AlphaTestRef, out m_PrevAlphaRef);

                m_WasBlendEnabled = GL.IsEnabled(EnableCap.Blend);
                m_WasTexture2DEnabled = GL.IsEnabled(EnableCap.Texture2D);
                m_WasDepthTestEnabled = GL.IsEnabled(EnableCap.DepthTest);
            }

            // Set default values and enable/disable caps.
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.AlphaFunc(AlphaFunction.Greater, 1.0f);
            GL.Enable(EnableCap.Blend);
            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.Texture2D);

            m_VertNum = 0;
            m_DrawCallCount = 0;
            m_ClipEnabled = false;
            m_TextureEnabled = false;
            m_LastTextureID = -1;

            GL.EnableClientState(ArrayCap.VertexArray);
            GL.EnableClientState(ArrayCap.ColorArray);
            GL.EnableClientState(ArrayCap.TextureCoordArray);
        }

        public override void End()
        {
            Flush();

            if (m_RestoreRenderState)
            {
                GL.BindTexture(TextureTarget.Texture2D, 0);
                m_LastTextureID = 0;

                // Restore the previous parameter values.
                GL.BlendFunc((BlendingFactor)m_PrevBlendSrc, (BlendingFactor)m_PrevBlendDst);
                GL.AlphaFunc((AlphaFunction)m_PrevAlphaFunc, m_PrevAlphaRef);

                if (!m_WasBlendEnabled)
                    GL.Disable(EnableCap.Blend);

                if (m_WasTexture2DEnabled && !m_TextureEnabled)
                    GL.Enable(EnableCap.Texture2D);

                if (m_WasDepthTestEnabled)
                    GL.Enable(EnableCap.DepthTest);
            }

            GL.DisableClientState(ArrayCap.VertexArray);
            GL.DisableClientState(ArrayCap.ColorArray);
            GL.DisableClientState(ArrayCap.TextureCoordArray);
        }

        /// <summary>
        /// Returns number of cached strings in the text cache.
        /// </summary>
        public int TextCacheSize { get { return m_StringCache.Count; } }

        public int DrawCallCount { get { return m_DrawCallCount; } }

        public int VertexCount { get { return m_VertNum; } }
        /// <summary>
        /// Clears the text rendering cache. Make sure to call this if cached strings size becomes too big (check TextCacheSize).
        /// </summary>
        public void FlushTextCache()
        {
            // todo: some auto-expiring cache? based on number of elements or age
            foreach (var textRenderer in m_StringCache.Values)
            {
                textRenderer.Dispose();
            }
            m_StringCache.Clear();
        }

        private void Flush()
        {
            if (m_VertNum == 0) return;

            GL.VertexPointer(2, VertexPointerType.Short, m_VertexSize, m_Vertices.SelectMany(v => new[]{v.x, v.y}).ToArray());
            GL.ColorPointer(4, ColorPointerType.UnsignedByte, m_VertexSize, m_Vertices.SelectMany(v => new[]{v.r, v.g, v.b, v.a}).ToArray());
            GL.TexCoordPointer(2, TexCoordPointerType.Float, m_VertexSize, m_Vertices.SelectMany(v => new[]{v.u, v.v}).ToArray());

            GL.DrawArrays(PrimitiveType.Quads, 0, m_VertNum);

            m_DrawCallCount++;
            m_VertNum = 0;
        }

        public override void DrawFilledRect(Rectangle rect)
        {
            if (m_TextureEnabled)
            {
                Flush();
                GL.Disable(EnableCap.Texture2D);
                m_TextureEnabled = false;
            }

            rect = Translate(rect);

            DrawRect(rect);
        }

        public override Color DrawColor
        {
            get { return m_Color; }
            set
            {
                m_Color = value;
            }
        }

        public override void StartClip()
        {
            m_ClipEnabled = true;
        }

        public override void EndClip()
        {
            m_ClipEnabled = false;
        }

        public override void DrawTexturedRect(Texture t, Rectangle rect, float u1 = 0, float v1 = 0, float u2 = 1, float v2 = 1)
        {
            // Missing image, not loaded properly?
            if (null == t.RendererData)
            {
                DrawMissingImage(rect);
                return;
            }

            int tex = (int)t.RendererData;
            rect = Translate(rect);

            bool differentTexture = (tex != m_LastTextureID);
            if (!m_TextureEnabled || differentTexture)
            {
                Flush();
            }

            if (!m_TextureEnabled)
            {
                GL.Enable(EnableCap.Texture2D);
                m_TextureEnabled = true;
            }

            if (differentTexture)
            {
                GL.BindTexture(TextureTarget.Texture2D, tex);
                m_LastTextureID = tex;
            }

            DrawRect(rect, u1, v1, u2, v2);
        }

        private void DrawRect(Rectangle rect, float u1 = 0, float v1 = 0, float u2 = 1, float v2 = 1)
        {
            if (m_VertNum + 4 >= MaxVerts)
            {
                Flush();
            }

            if (m_ClipEnabled)
            {
                // cpu scissors test

                if (rect.Y < ClipRegion.Y)
                {
                    int oldHeight = rect.Height;
                    int delta = ClipRegion.Y - rect.Y;
                    rect.Y = ClipRegion.Y;
                    rect.Height -= delta;

                    if (rect.Height <= 0)
                    {
                        return;
                    }

                    float dv = (float)delta / (float)oldHeight;

                    v1 += dv * (v2 - v1);
                }

                if ((rect.Y + rect.Height) > (ClipRegion.Y + ClipRegion.Height))
                {
                    int oldHeight = rect.Height;
                    int delta = (rect.Y + rect.Height) - (ClipRegion.Y + ClipRegion.Height);

                    rect.Height -= delta;

                    if (rect.Height <= 0)
                    {
                        return;
                    }

                    float dv = (float)delta / (float)oldHeight;

                    v2 -= dv * (v2 - v1);
                }

                if (rect.X < ClipRegion.X)
                {
                    int oldWidth = rect.Width;
                    int delta = ClipRegion.X - rect.X;
                    rect.X = ClipRegion.X;
                    rect.Width -= delta;

                    if (rect.Width <= 0)
                    {
                        return;
                    }

                    float du = (float)delta / (float)oldWidth;

                    u1 += du * (u2 - u1);
                }

                if ((rect.X + rect.Width) > (ClipRegion.X + ClipRegion.Width))
                {
                    int oldWidth = rect.Width;
                    int delta = (rect.X + rect.Width) - (ClipRegion.X + ClipRegion.Width);

                    rect.Width -= delta;

                    if (rect.Width <= 0)
                    {
                        return;
                    }

                    float du = (float)delta / (float)oldWidth;

                    u2 -= du * (u2 - u1);
                }
            }

            int vertexIndex = m_VertNum;
            m_Vertices[vertexIndex].x = (short)rect.X;
            m_Vertices[vertexIndex].y = (short)rect.Y;
            m_Vertices[vertexIndex].u = u1;
            m_Vertices[vertexIndex].v = v1;
            m_Vertices[vertexIndex].r = m_Color.R;
            m_Vertices[vertexIndex].g = m_Color.G;
            m_Vertices[vertexIndex].b = m_Color.B;
            m_Vertices[vertexIndex].a = m_Color.A;

            vertexIndex++;
            m_Vertices[vertexIndex].x = (short)(rect.X + rect.Width);
            m_Vertices[vertexIndex].y = (short)rect.Y;
            m_Vertices[vertexIndex].u = u2;
            m_Vertices[vertexIndex].v = v1;
            m_Vertices[vertexIndex].r = m_Color.R;
            m_Vertices[vertexIndex].g = m_Color.G;
            m_Vertices[vertexIndex].b = m_Color.B;
            m_Vertices[vertexIndex].a = m_Color.A;

            vertexIndex++;
            m_Vertices[vertexIndex].x = (short)(rect.X + rect.Width);
            m_Vertices[vertexIndex].y = (short)(rect.Y + rect.Height);
            m_Vertices[vertexIndex].u = u2;
            m_Vertices[vertexIndex].v = v2;
            m_Vertices[vertexIndex].r = m_Color.R;
            m_Vertices[vertexIndex].g = m_Color.G;
            m_Vertices[vertexIndex].b = m_Color.B;
            m_Vertices[vertexIndex].a = m_Color.A;

            vertexIndex++;
            m_Vertices[vertexIndex].x = (short)rect.X;
            m_Vertices[vertexIndex].y = (short)(rect.Y + rect.Height);
            m_Vertices[vertexIndex].u = u1;
            m_Vertices[vertexIndex].v = v2;
            m_Vertices[vertexIndex].r = m_Color.R;
            m_Vertices[vertexIndex].g = m_Color.G;
            m_Vertices[vertexIndex].b = m_Color.B;
            m_Vertices[vertexIndex].a = m_Color.A;

            m_VertNum += 4;
        }

        public override bool LoadFont(Font font)
        {
            Debug.Print($"LoadFont {font.FaceName}");
            font.RealSize = font.Size * Scale;

            var sysFont = new SixLabors.Fonts.Font(SystemFonts.TryGet(font.FaceName, out var f) ? f : throw new Exception(), font.Size);
            font.RendererData = sysFont;
            return true;
        }

        public override void FreeFont(Font font)
        {
            Debug.Print($"FreeFont {font.FaceName}");
            if (font.RendererData == null)
                return;

            Debug.Print($"FreeFont {font.FaceName} - actual free");
            if (font.RendererData is not SixLabors.Fonts.Font sysFont)
                throw new InvalidOperationException("Freeing empty font");

            font.RendererData = null;
        }

        public override Point MeasureText(Font font, string text)
        {
            //Debug.Print(String.Format("MeasureText '{0}'", text));
            SixLabors.Fonts.Font sysFont = font.RendererData as SixLabors.Fonts.Font;

            if (sysFont == null || Math.Abs(font.RealSize - font.Size * Scale) > 2)
            {
                FreeFont(font);
                LoadFont(font);
                sysFont = font.RendererData as SixLabors.Fonts.Font;
            }

            var key = new Tuple<String, Font>(text, font);

            if (m_StringCache.ContainsKey(key))
            {
                var tex = m_StringCache[key].Texture;
                return new Point(tex.Width, tex.Height);
            }

            //Spaces are not being picked up, let's just use .'s.
            // TODO: Test out tabs/spaces with new TextMeasurer...
            var TabSize = TextMeasurer.MeasureSize("....", new TextOptions(sysFont));

            var size = TextMeasurer.MeasureSize(text, new TextOptions(sysFont) { TabWidth = TabSize.Width });

            return new Point((int)Math.Round(size.Width), (int)Math.Round(size.Height));
        }

        public override void RenderText(Font font, Point position, string text)
        {
            //Debug.Print(String.Format("RenderText {0}", font.FaceName));

            // The DrawString(...) below will bind a new texture
            // so make sure everything is rendered!
            Flush();

            SixLabors.Fonts.Font? sysFont = font.RendererData as SixLabors.Fonts.Font;

            if (sysFont == null || Math.Abs(font.RealSize - font.Size * Scale) > 2)
            {
                FreeFont(font);
                LoadFont(font);
                sysFont = font.RendererData as SixLabors.Fonts.Font;
            }

            var key = new Tuple<String, Font>(text, font);

            if (!m_StringCache.ContainsKey(key))
            {
                // not cached - create text renderer
                Debug.Print($"RenderText: caching \"{text}\", {font.FaceName}");

                Point size = MeasureText(font, text);
                TextRenderer tr = new TextRenderer(size.X, size.Y, this);
                tr.DrawString(text, sysFont, new SolidBrush(SixLabors.ImageSharp.Color.White), PointF.Empty); // renders string on the texture

                DrawTexturedRect(tr.Texture, new Rectangle(position.X, position.Y, tr.Texture.Width, tr.Texture.Height));

                m_StringCache[key] = tr;
            }
            else
            {
                TextRenderer tr = m_StringCache[key];
                DrawTexturedRect(tr.Texture, new Rectangle(position.X, position.Y, tr.Texture.Width, tr.Texture.Height));
            }
        }

        internal static void LoadTextureInternal(Texture t, Image bmp)
        {
            // Create the opengl texture
            GL.GenTextures(1, out int glTex);

            GL.BindTexture(TextureTarget.Texture2D, glTex);
            m_LastTextureID = glTex;

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

            // Sort out our GWEN texture
            t.RendererData = glTex;
            t.Width = bmp.Width;
            t.Height = bmp.Height;

            var img = bmp.CloneAs<PixelFormat.Rgba32>();

            var pixelBytes = new byte[img.Width * img.Height * Unsafe.SizeOf<PixelFormat.Rgba32>()];
            img.CopyPixelDataTo(pixelBytes);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, t.Width, t.Height, 0, global::OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, pixelBytes);
        }

        public override void LoadTexture(Texture t)
        {
            Image bmp;
            try
            {
                bmp = Image.Load(t.Name);
            }
            catch (Exception)
            {
                t.Failed = true;
                return;
            }

            LoadTextureInternal(t, bmp);
            bmp.Dispose();
        }

        public override void LoadTextureStream(Texture t, Stream data)
        {
            Image bmp;
            try
            {
                bmp = Image.Load(data);
            }
            catch (Exception)
            {
                t.Failed = true;
                return;
            }

            LoadTextureInternal(t, bmp);
            bmp.Dispose();
        }

        public override void LoadTextureRaw(Texture t, byte[] pixelData)
        {
            Image<PixelFormat.Rgba32> bmp;
            try {
                bmp = Image.Load<PixelFormat.Rgba32>(pixelData);
            }
            catch (Exception)
            {
                t.Failed = true;
                return;
            }

            // Create the opengl texture
            GL.GenTextures(1, out int glTex);

            GL.BindTexture(TextureTarget.Texture2D, glTex);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

            // Sort out our GWEN texture
            t.RendererData = glTex;

            var pixelBytes = new byte[bmp.Width * bmp.Height * Unsafe.SizeOf<PixelFormat.Rgba32>()];
            bmp.CopyPixelDataTo(pixelBytes);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, t.Width, t.Height, 0, global::OpenTK.Graphics.OpenGL.PixelFormat.Rgba, PixelType.UnsignedByte, pixelBytes);
            bmp.Dispose();

            GL.BindTexture(TextureTarget.Texture2D, m_LastTextureID);
        }

        public override void FreeTexture(Texture t)
        {
            if (t.RendererData == null)
                return;
            int tex = (int)t.RendererData;
            if (tex == 0)
                return;
            GL.DeleteTextures(1, ref tex);
            t.RendererData = null;
        }

        public override Color PixelColor(Texture texture, uint x, uint y, Color defaultColor)
        {
            if (texture.RendererData == null)
                return defaultColor;

            int tex = (int)texture.RendererData;
            if (tex == 0)
                return defaultColor;

            GL.BindTexture(TextureTarget.Texture2D, tex);
            m_LastTextureID = tex;

            long offset = 4 * (x + y * texture.Width);
            byte[] data = new byte[4 * texture.Width * texture.Height];

            GL.GetTexImage(TextureTarget.Texture2D, 0, global::OpenTK.Graphics.OpenGL.PixelFormat.Rgba, PixelType.UnsignedByte, data);
            var pixel = Color.FromArgb(data[offset + 3], data[offset + 0], data[offset + 1], data[offset + 2]);

            return pixel;
        }

        public override void SetCursor(Cursor cursor) {
            m_Parent.Cursor = cursor switch {
                Cursor.Beam => MouseCursor.IBeam,
                Cursor.Normal => MouseCursor.Default,
                Cursor.SizeNS => MouseCursor.ResizeNS,
                Cursor.SizeWE => MouseCursor.ResizeEW,
                Cursor.SizeNWSE => MouseCursor.ResizeNWSE,
                Cursor.SizeNESW => MouseCursor.ResizeNESW,
                Cursor.SizeAll => MouseCursor.ResizeAll,
                Cursor.No => MouseCursor.NotAllowed,
                Cursor.Wait => MouseCursor.Crosshair,
                Cursor.Finger => MouseCursor.PointingHand,
                _ => throw new ArgumentOutOfRangeException(nameof(cursor), cursor, null)
            };
        }
    }
}
