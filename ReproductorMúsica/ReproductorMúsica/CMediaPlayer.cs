using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WMPLib;

namespace ReproductorMúsica
{
    // Media player helper for artwork/visualization and basic control wrappers
    internal class CMediaPlayer
    {
        private WindowsMediaPlayer player;
        private Timer animationTimer;
        private int progress;
        private PictureBox canvas;
        private Pen pen = new Pen(Color.MediumVioletRed, 2);
        private int offset = 0;
        private int style = 0;
        private int linesToDraw = 0;
        private const int totalLines = 36;

        // New fields for spectrum simulation and smoothing
        private int barCount = 64;
        private float[] spectrum;
        private float[] spectrumSmooth;
        private Random rng = new Random();
        private float spectrumSmoothing = 0.6f; // 0..1, bigger = smoother

        // Audio analyzer (real FFT)
        private AudioAnalyzer analyzer;

        // Theme colors (can be switched at runtime)
        private Color themeBarStart = Color.FromArgb(255, 128, 255, 0);
        private Color themeBarEnd = Color.FromArgb(255, 0, 200, 80);
        private Color themeCircleInner = Color.Magenta;
        private Color themeCircleOuter = Color.Cyan;

        // Auto-randomization and beat detection
        private bool autoRandomize = true;
        private bool styleLocked = false; // when true, do not change style during playback
        private float prevAvg = 0f;
        private float beatPulse = 0f; // decaying pulse applied when a beat is detected
        private int previousStyle = -1; // remember last chosen style to avoid repeats

        // Particle system for beat effects
        private class Particle
        {
            public float X, Y;
            public float VX, VY;
            public float Radius;
            public int Alpha;
            public Color Color;
        }
        private List<Particle> particles = new List<Particle>();
        private int maxParticles = 220;

        // Center jitter and low-band energy
        private float centerOffsetX = 0f;
        private float centerOffsetY = 0f;
        private float lowEnergy = 0f;
        private int beatCooldown = 0;

        public string GetCurrentStyleName()
        {
            switch (style)
            {
                case 0: return "Barras";
                case 1: return "Círculos";
                case 2: return "Polígonos";
                default: return "Desconocido";
            }
        }

        public CMediaPlayer(WindowsMediaPlayer mediaPlayer, Timer timer, PictureBox canvas, AudioAnalyzer analyzer = null)
        {
            this.player = mediaPlayer;
            this.animationTimer = timer;
            this.canvas = canvas;
            this.analyzer = analyzer;

            spectrum = new float[barCount];
            spectrumSmooth = new float[barCount];

            if (this.animationTimer != null)
                this.animationTimer.Tick += Timer_Tick;
        }

        // Simple theme setter: 0 = neon, 1 = cool, 2 = warm
        public void SetTheme(int themeIndex)
        {
            switch (themeIndex)
            {
                case 0: // neon
                    themeBarStart = Color.FromArgb(255, 128, 255, 0);
                    themeBarEnd = Color.FromArgb(255, 0, 200, 80);
                    themeCircleInner = Color.Magenta;
                    themeCircleOuter = Color.Cyan;
                    break;
                case 1: // cool blues
                    themeBarStart = Color.FromArgb(255, 100, 200, 255);
                    themeBarEnd = Color.FromArgb(255, 0, 120, 220);
                    themeCircleInner = Color.FromArgb(255, 120, 180, 255);
                    themeCircleOuter = Color.FromArgb(255, 0, 120, 200);
                    break;
                case 2: // warm
                    themeBarStart = Color.FromArgb(255, 255, 160, 80);
                    themeBarEnd = Color.FromArgb(255, 200, 60, 20);
                    themeCircleInner = Color.FromArgb(255, 255, 120, 180);
                    themeCircleOuter = Color.FromArgb(255, 200, 80, 40);
                    break;
                default:
                    SetTheme(0);
                    break;
            }
        }

        public void LoadTrack(string path, int styleIndex)
        {
            try
            {
                player.URL = path;
            }
            catch { }

            // If caller passed -1 or out of range, choose a random style for this track
            if (styleIndex < 0 || styleIndex > 2)
            {
                int newStyle = previousStyle;
                int attempts = 0;
                while (newStyle == previousStyle && attempts < 10)
                {
                    newStyle = rng.Next(0, 3); // 0=barras,1=circulos,2=poligonos
                    attempts++;
                }
                style = newStyle;
                previousStyle = style;
            }
            else
            {
                style = styleIndex;
                previousStyle = styleIndex;
            }

            styleLocked = true; // lock the chosen style so only that style is drawn for the whole track

            // Apply style colors
            SetStyle(style);

            offset = 0;
            // Reiniciar líneas al parar
            linesToDraw = 0;

            // reset beat/state
            prevAvg = 0f;
            beatPulse = 0f;
            particles.Clear();
            centerOffsetX = centerOffsetY = 0f;
        }

        public void SetStyle(int styleIndex)
        {
            style = styleIndex;
            switch (style)
            {
                case 0:
                    pen = new Pen(Color.FromArgb(255, 0, 255, 128), 2); // vibrant cyan-green
                    break;
                case 1:
                    pen = new Pen(Color.FromArgb(255, 255, 0, 255), 2); // magenta
                    break;
                case 2:
                    pen = new Pen(Color.FromArgb(255, 255, 128, 0), 2); // orange
                    break;
                default:
                    pen = new Pen(Color.MediumVioletRed, 2);
                    break;
            }
        }

        public void Play()
        {
            try { player.controls.play(); } catch { }
            try { animationTimer?.Start(); } catch { }
            try { analyzer?.Start(); } catch { }
        }

        public void Pause()
        {
            try { player.controls.pause(); } catch { }
            try { animationTimer?.Stop(); } catch { }
            try { analyzer?.Stop(); } catch { }
        }

        public void Stop()
        {
            try { player.controls.stop(); } catch { }
            try { animationTimer?.Stop(); } catch { }
            try
            {
                canvas.Image?.Dispose();
            }
            catch { }
            canvas.Image = null;
            progress = 0;
            offset = 0;
            // Reiniciar líneas al parar en el diseño 3.
            linesToDraw = 0;

            // reset spectrum
            for (int i = 0; i < spectrum.Length; i++) spectrum[i] = spectrumSmooth[i] = 0f;

            // reset beat/state
            prevAvg = 0f;
            beatPulse = 0f;

            // unlock style so next load can randomize again
            styleLocked = false;

            try { analyzer?.Stop(); } catch { }
            particles.Clear();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (canvas == null) return;
            if (canvas.Width == 0 || canvas.Height == 0) return;

            UpdateSpectrum();

            Bitmap bmp = new Bitmap(canvas.Width, canvas.Height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Black);

                int centerX = canvas.Width / 2;
                int centerY = canvas.Height / 2;
                int radius = Math.Min(canvas.Width, canvas.Height) / 2 - 10;

                switch (style)
                {
                    // Bars: only draw bars at bottom, no radial spokes
                    case 0:
                        DrawBars(g, centerX, centerY, radius);
                        break;

                    // Circles: draw only circles visualization
                    case 1:
                        DrawInfiniteCircles(g, centerX + centerOffsetX, centerY + centerOffsetY, radius);
                        break;

                    // Polygons: animated polygons (optionally radial accent)
                    case 2:
                        if (linesToDraw < totalLines)
                            linesToDraw++; // aumenta 1 línea por tick

                        DrawAnimatedPolygons(g, centerX, centerY, radius, linesToDraw);
                        break;
                    default:
                        DrawBars(g, centerX, centerY, radius);
                        break;
                }

                // draw particles on top
                UpdateAndDrawParticles(g);

                // debug overlay
                DrawDebugOverlay(g);
            }

            try { canvas.Image?.Dispose(); } catch { }
            canvas.Image = bmp;

            offset += 3;

            // decay beat pulse
            beatPulse *= 0.92f;
            if (beatPulse < 0.01f) beatPulse = 0f;

            if (beatCooldown > 0) beatCooldown--;
        }

        private void UpdateSpectrum()
        {
            // Use real analyzer if available
            if (analyzer != null)
            {
                try
                {
                    var real = analyzer.GetSpectrum(barCount);
                    for (int i = 0; i < barCount && i < real.Length; i++)
                    {
                        spectrum[i] = real[i];
                        spectrumSmooth[i] = spectrumSmooth[i] * spectrumSmoothing + spectrum[i] * (1f - spectrumSmoothing);
                    }
                }
                catch { /* fallback to simulation below */ }
            }

            // Fallback simulation if analyzer not available or failed
            double position = 0.0;
            try { position = player.controls.currentPosition; } catch { position = 0.0; }

            int seed = (int)(DateTime.Now.Ticks & 0xFFFFFF);
            var localRng = new Random(seed ^ offset);

            for (int i = 0; i < barCount; i++)
            {
                double t = position * 2.0 + i * 0.13 + offset * 0.01 + localRng.NextDouble() * 0.5;
                float value = (float)((Math.Abs(Math.Sin(t + i)) * 0.8) + (localRng.NextDouble() * 0.2));

                float falloff = 1.0f - (i / (float)barCount);
                value *= (0.3f + 0.7f * falloff);

                // only set if analyzer not used
                if (analyzer == null)
                {
                    spectrum[i] = value;
                    spectrumSmooth[i] = spectrumSmooth[i] * spectrumSmoothing + spectrum[i] * (1f - spectrumSmoothing);
                }
            }

            // compute average amplitude and detect beats
            float avg = 0f;
            for (int i = 0; i < spectrumSmooth.Length; i++) avg += spectrumSmooth[i];
            avg /= spectrumSmooth.Length;

            // compute low-band energy for center and color
            int lowBands = Math.Max(2, barCount / 12);
            float lowSum = 0f;
            for (int i = 0; i < lowBands; i++) lowSum += spectrumSmooth[i];
            lowEnergy = lowSum / lowBands; // 0..1 roughly

            // center jitter based on low energy
            centerOffsetX = (float)(Math.Sin(offset * 0.02) * lowEnergy * 28.0);
            centerOffsetY = (float)(Math.Cos(offset * 0.017) * lowEnergy * 18.0);

            float delta = avg - prevAvg;

            // if amplitude rises quickly, treat as a beat and create a pulse
            if (delta > 0.06f) // lower threshold because real audio can be subtler
            {
                beatPulse = Math.Min(2.0f, beatPulse + delta * 8f);

                // spawn particles on beat, with cooldown
                if (beatCooldown == 0)
                {
                    int cx = canvas != null ? canvas.Width / 2 : 0;
                    int cy = canvas != null ? canvas.Height / 2 : 0;
                    SpawnParticles(cx + centerOffsetX, cy + centerOffsetY, Math.Min(1.0f, delta * 8f + lowEnergy));
                    beatCooldown = 6; // a few frames cooldown
                }
            }

            prevAvg = avg;
        }

        private void SpawnParticles(float cx, float cy, float intensity)
        {
            int count = 6 + rng.Next(6);
            for (int i = 0; i < count; i++)
            {
                if (particles.Count >= maxParticles) break;
                double ang = rng.NextDouble() * Math.PI * 2.0;
                float speed = (float)(2.0 + rng.NextDouble() * 6.0 * intensity);
                Particle p = new Particle();
                p.X = cx + (float)(Math.Cos(ang) * rng.NextDouble() * 8f);
                p.Y = cy + (float)(Math.Sin(ang) * rng.NextDouble() * 8f);
                p.VX = (float)(Math.Cos(ang) * speed);
                p.VY = (float)(Math.Sin(ang) * speed);
                p.Radius = (float)(2.0 + rng.NextDouble() * 4.0 + intensity * 4.0f);
                p.Alpha = 200;
                Color c = BlendColors(themeCircleInner, themeCircleOuter, (float)rng.NextDouble());
                p.Color = c;
                particles.Add(p);
            }
        }

        private void UpdateAndDrawParticles(Graphics g)
        {
            if (particles.Count == 0) return;
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                var p = particles[i];
                // update
                p.X += p.VX;
                p.Y += p.VY;
                p.VX *= 0.98f;
                p.VY *= 0.98f;
                p.Radius *= 1.02f;
                p.Alpha -= 8;

                // draw
                try
                {
                    int a = ClampAlpha(p.Alpha);
                    using (SolidBrush br = new SolidBrush(Color.FromArgb(a, p.Color.R, p.Color.G, p.Color.B)))
                    {
                        float rr = Math.Max(1f, p.Radius);
                        g.FillEllipse(br, p.X - rr, p.Y - rr, rr * 2, rr * 2);
                    }
                }
                catch { }

                if (p.Alpha <= 0 || p.Radius > Math.Max(canvas.Width, canvas.Height))
                {
                    particles.RemoveAt(i);
                }
            }
        }

        private void DrawBars(Graphics g, int cx, int cy, int r)
        {
            int w = canvas.Width;
            int h = canvas.Height;

            // local vibrant palette for bars
            Color localStart = themeBarStart;
            Color localEnd = themeBarEnd;

            int count = Math.Min(barCount, spectrumSmooth.Length);
            if (count <= 0) return;
            float barWidth = (float)w / (count);

            for (int i = 0; i < count; i++)
            {
                float x = i * barWidth;
                float amp = spectrumSmooth[i];

                // amplify transiently on beat
                float ampBoost = 1f + beatPulse * (0.25f + (1f - i / (float)count) * 0.75f);
                float barHeight = amp * h * 0.45f * ampBoost; // reduce to bottom half

                // avoid zero-height rectangles which cause GDI+ exceptions
                if (!(barHeight > 1f))
                {
                    // still draw a minimal cap so the bar area is visible
                    float minCapH = Math.Min(2f, h * 0.02f);
                    RectangleF capRect = new RectangleF(x + 1, h - minCapH, Math.Max(1f, barWidth - 2), minCapH);
                    try { g.FillRectangle(Brushes.Transparent, capRect); } catch { }
                    continue;
                }

                RectangleF rect = new RectangleF(x + 1, h - barHeight, barWidth - 2, barHeight);

                try
                {
                    using (LinearGradientBrush brush = new LinearGradientBrush(rect, localStart, localEnd, LinearGradientMode.Vertical))
                    {
                        ColorBlend cb = new ColorBlend(3);
                        cb.Colors = new Color[] { Color.FromArgb(220, localStart.R, localStart.G, localStart.B), localStart, localEnd };
                        cb.Positions = new float[] { 0f, 0.5f, 1f };
                        brush.InterpolationColors = cb;
                        g.FillRectangle(brush, rect);
                    }
                }
                catch { }

                using (Brush cap = new SolidBrush(Color.FromArgb(240, 255, 255, 255)))
                {
                    float capH = Math.Min(10, barHeight);
                    if (capH > 0.5f)
                    {
                        try { g.FillRectangle(cap, x + 1, h - barHeight - capH, Math.Max(1f, barWidth - 2), capH); } catch { }
                    }
                }
            }
        }

        private int ClampAlpha(int a)
        {
            if (a < 0) return 0;
            if (a > 255) return 255;
            return a;
        }

        private void DrawInfiniteCircles(Graphics g, float cxF, float cyF, int r)
        {
            float avg = 0f;
            for (int i = 0; i < spectrumSmooth.Length; i++) avg += spectrumSmooth[i];
            avg /= spectrumSmooth.Length;

            float cx = cxF;
            float cy = cyF;

            // local circle palette
            Color innerCol = themeCircleInner;
            Color outerCol = themeCircleOuter;

            // amplify with beat
            float pulse = 1f + beatPulse * 1.2f;

            // increase overall scale a bit so center is larger
            float centerScale = 1.25f;

            // Main center rings (reduced rings for cleaner look)
            int rings = 34;
            float baseRadius = r * 0.16f * pulse * centerScale; // increased base radius

            // Draw a glowing core (filled concentric rings) to make center more visible
            try
            {
                // core color based on lowEnergy
                Color coreColor = BlendColors(innerCol, outerCol, Math.Max(0f, Math.Min(1f, lowEnergy)));
                float coreBase = baseRadius * 0.28f + avg * r * 0.18f + beatPulse * 18f;
                for (int gstep = 5; gstep >= 0; gstep--)
                {
                    int alpha = ClampAlpha((int)(200 * (1.0f - gstep / 6.0f)));
                    float rr = coreBase * (1.0f + gstep * 0.45f);
                    if (rr > 0.5f)
                    {
                        try
                        {
                            using (SolidBrush br = new SolidBrush(Color.FromArgb(alpha, coreColor.R, coreColor.G, coreColor.B)))
                            {
                                g.FillEllipse(br, cx - rr, cy - rr, rr * 2, rr * 2);
                            }
                        }
                        catch { }
                    }
                }

                // stronger, larger central spark/star
                int starLines = 40;
                using (Pen starPen = new Pen(Color.FromArgb(240, innerCol.R, innerCol.G, innerCol.B), Math.Max(1f, 2f + beatPulse * 3f)))
                {
                    for (int i = 0; i < starLines; i++)
                    {
                        double ang = (i * 2 * Math.PI / starLines) + offset * 0.04;
                        float len = coreBase * 0.6f + avg * r * 0.45f * pulse + beatPulse * 12f;
                        if (len > 0.5f)
                        {
                            float x = cx + (float)(Math.Cos(ang) * len);
                            float y = cy + (float)(Math.Sin(ang) * len);
                            try { g.DrawLine(starPen, cx, cy, x, y); } catch { }
                        }
                    }
                }
            }
            catch { }

            for (int i = 0; i < rings; i++)
            {
                float t = (i / (float)rings);
                float radius = baseRadius + t * (r * 0.9f) + (float)(Math.Sin((offset + i * 8) * 0.07) * avg * 28 * pulse);

                int alpha = (int)(Math.Max(0, 200 - i * (180 / rings)));
                alpha = ClampAlpha(alpha);
                Color c = BlendColors(innerCol, outerCol, t);
                if (radius > 0.5f)
                {
                    try
                    {
                        using (Pen p = new Pen(Color.FromArgb(alpha, c.R, c.G, c.B), Math.Max(1f, 3f - t * 2.6f)))
                        {
                            g.DrawEllipse(p, cx - radius, cy - radius, radius * 2, radius * 2);
                        }
                    }
                    catch { }
                }

                // stronger glow on even rings to visualize beats
                if (i % 5 == 0)
                {
                    int glowAlpha = ClampAlpha((int)(20 + beatPulse * 90));
                    using (SolidBrush glow = new SolidBrush(Color.FromArgb(glowAlpha, c.R, c.G, c.B)))
                    {
                        if (radius > 0.5f) try { g.FillEllipse(glow, cx - radius - 4, cy - radius - 4, (radius + 4) * 2, (radius + 4) * 2); } catch { }
                    }
                }
            }

            // Side ring sets to the left and right which react strongly to beatPulse
            int sideRings = 18;
            float horizontalOffset = r * 0.85f; // distance from center to side centers
            int leftCx = (int)(cx - horizontalOffset);
            int rightCx = (int)(cx + horizontalOffset);

            for (int side = 0; side < 2; side++)
            {
                int scx = side == 0 ? leftCx : rightCx;
                // choose a slightly different color blend so side rings stand out
                Color sideA = side == 0 ? BlendColors(innerCol, Color.FromArgb(255, 0, 200, 255), 0.3f) : BlendColors(outerCol, Color.FromArgb(255, 255, 100, 50), 0.3f);
                Color sideB = side == 0 ? BlendColors(outerCol, Color.FromArgb(255, 0, 255, 150), 0.3f) : BlendColors(innerCol, Color.FromArgb(255, 255, 200, 100), 0.3f);

                for (int i = 0; i < sideRings; i++)
                {
                    float t = (i / (float)sideRings);
                    // side radii are smaller but expand on beats
                    float radius = baseRadius * 0.6f + t * (r * 0.45f) + (float)(Math.Sin((offset + i * 6 + side * 10) * 0.08) * avg * 14 * (1f + beatPulse));

                    int alpha = (int)(Math.Max(0, 160 - i * (140 / sideRings)));
                    // make side rings more visible on beat
                    int beatAlphaBoost = (int)(Math.Min(120, beatPulse * 120));
                    int drawAlpha = ClampAlpha(Math.Min(255, alpha + beatAlphaBoost));
                    Color c = BlendColors(sideA, sideB, t);
                    using (Pen p = new Pen(Color.FromArgb(drawAlpha, c.R, c.G, c.B), Math.Max(1f, 2f - t * 1.8f)))
                    {
                        g.DrawEllipse(p, scx - radius, cy - radius, radius * 2, radius * 2);
                    }

                    // small radial spikes connecting side rings to center to show rhythm
                    if (i % 6 == 0)
                    {
                        double ang = (side == 0 ? -Math.PI / 2 : -Math.PI / 2) + offset * 0.01 * (side == 0 ? 1 : -1);
                        float len = radius * 0.3f + beatPulse * 10f;
                        float sx = scx + (float)(Math.Cos(ang) * (radius - len));
                        float sy = cy + (float)(Math.Sin(ang) * (radius - len));
                        float ex = scx + (float)(Math.Cos(ang) * (radius + len));
                        float ey = cy + (float)(Math.Sin(ang) * (radius + len));
                        int spikeAlpha = ClampAlpha((int)(80 + beatPulse * 120));
                        using (Pen spike = new Pen(Color.FromArgb(spikeAlpha, c.R, c.G, c.B), 1f))
                        {
                            g.DrawLine(spike, sx, sy, ex, ey);
                        }
                    }
                }
            }

            // central radial sparks
            int sparks = 32;
            for (int i = 0; i < sparks; i++)
            {
                double ang = (i * 2 * Math.PI / sparks) + offset * 0.03 * pulse;
                float len = baseRadius * 0.5f + avg * r * 0.8f * (float)Math.Abs(Math.Cos(i + offset * 0.03)) * pulse;
                float x = cx + (float)(Math.Cos(ang) * len);
                float y = cy + (float)(Math.Sin(ang) * len);
                using (Pen p = new Pen(Color.FromArgb(200, 255, 100, 220), 1 + (float)(Math.Abs(Math.Sin(offset * 0.05)) * 1.8f)))
                {
                    g.DrawLine(p, cx, cy, x, y);
                }
            }
        }

        private void DrawAnimatedPolygons(Graphics g, int cx, int cy, int r, int linesToDraw)
        {
            int layers = 6;
            int sidesBase = 5 + (linesToDraw % 6); // vary sides with linesToDraw

            float avg = 0f;
            for (int i = 0; i < spectrumSmooth.Length; i++) avg += spectrumSmooth[i];
            avg /= spectrumSmooth.Length;

            float pulse = 1f + beatPulse * 1.0f;

            for (int layer = 0; layer < layers; layer++)
            {
                int sides = sidesBase + layer;
                float radiusLayer = r * (0.12f + layer / (float)layers * 0.8f) * pulse;
                PointF[] pts = new PointF[sides];

                for (int s = 0; s < sides; s++)
                {
                    double ang = (s * 2.0 * Math.PI / sides) + offset * 0.02 + layer * 0.12;
                    int band = (s * spectrumSmooth.Length) / sides;
                    float mod = 1.0f + spectrumSmooth[band] * (0.4f + layer * 0.12f) * (1f + beatPulse);
                    float rx = radiusLayer * mod * (float)Math.Cos(ang);
                    float ry = radiusLayer * mod * (float)Math.Sin(ang);
                    pts[s] = new PointF(cx + rx, cy + ry);
                }

                using (GraphicsPath path = new GraphicsPath())
                {
                    path.AddPolygon(pts);
                    int alpha = Math.Max(30, 220 - layer * 30);
                    alpha = ClampAlpha(alpha);

                    // vibrant color per layer
                    Color layerColor = ColorFromHue((layer * 45 + (int)(offset * 0.5)) % 360);
                    using (SolidBrush br = new SolidBrush(Color.FromArgb(alpha, layerColor)))
                    {
                        g.FillPath(br, path);
                    }

                    using (Pen p = new Pen(Color.FromArgb(230, 255, 255, 255), 1f))
                    {
                        g.DrawPath(p, path);
                    }
                }
            }
        }

        private Color BlendColors(Color a, Color b, float t)
        {
            t = Math.Max(0f, Math.Min(1f, t));
            return Color.FromArgb(
                (int)(a.A + (b.A - a.A) * t),
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }

        private Color ColorFromHue(int hue)
        {
            // simple HSL->RGB conversion for vibrant colors
            float h = (hue % 360) / 360f;
            float s = 0.9f;
            float l = 0.55f;

            float r = 0, g = 0, b = 0;
            if (s == 0)
            {
                r = g = b = l;
            }
            else
            {
                Func<float, float, float, float> hue2rgb = (pa, pb, tc) =>
                {
                    if (tc < 0) tc += 1;
                    if (tc > 1) tc -= 1;
                    if (tc < 1f / 6f) return pa + (pb - pa) * 6f * tc;
                    if (tc < 1f / 2f) return pb;
                    if (tc < 2f / 3f) return pa + (pb - pa) * (2f / 3f - tc) * 6f;
                    return pa;
                };

                float qq = l < 0.5f ? l * (1 + s) : l + s - l * s;
                float p2 = 2 * l - qq;
                r = hue2rgb(p2, qq, h + 1f / 3f);
                g = hue2rgb(p2, qq, h);
                b = hue2rgb(p2, qq, h - 1f / 3f);
            }

            return Color.FromArgb(255, (int)(r * 255), (int)(g * 255), (int)(b * 255));
        }

        private void DrawLines(Graphics g, int cx, int cy, int r)
        {
            for (int angle = 0; angle < 360; angle += 15)
            {
                double rad = (angle + offset) * Math.PI / 180;
                int x = cx + (int)(r * Math.Cos(rad));
                int y = cy + (int)(r * Math.Sin(rad));
                g.DrawLine(pen, cx, cy, x, y);
            }
        }

        private void DrawSinusoidalBars(Graphics g, int cx, int cy, int r)
        {
            int barCountLocal = 60;
            float barWidth = (float)canvas.Width / barCountLocal;

            for (int i = 0; i < barCountLocal; i++)
            {
                float x = i * barWidth;
                double angle = (i * 2 * Math.PI / barCountLocal) + offset * 0.05;
                float amplitude = (float)(Math.Cos(angle) * r * 1);
                float y1 = cy;
                float y2 = cy - Math.Abs(amplitude);

                g.DrawLine(pen, x, y1, x, y2);
            }
        }

        private void DrawAnimatedRadialLines(Graphics g, int cx, int cy, int r, int linesToDraw)
        {
            int total = 36;

            for (int i = 0; i < linesToDraw && i < total; i++)
            {
                double t = (i * 2 * Math.PI / total) + offset * Math.PI / 180.0;

                double x = r * Math.Pow(Math.Cos(t), 3);
                double y = r * Math.Pow(Math.Sin(t), 3);

                g.DrawLine(Pens.MediumPurple, cx, cy, cx + (int)x, cy + (int)y);
            }
        }

        // Helper wrappers for player state
        public void SetVolume(int vol)
        {
            try { player.settings.volume = Math.Max(0, Math.Min(100, vol)); } catch { }
        }

        public void Seek(double seconds)
        {
            try { player.controls.currentPosition = seconds; } catch { }
        }

        public double GetDuration()
        {
            try
            {
                var m = player.currentMedia;
                if (m != null) return m.duration;
            }
            catch { }
            return double.NaN;
        }

        public double GetPosition()
        {
            try { return player.controls.currentPosition; } catch { return 0; }
        }

        public void SetLoop(bool loop)
        {
            try { player.settings.setMode("loop", loop); } catch { }
        }

        public void Forward(double seconds)
        {
            try { Seek(GetPosition() + seconds); } catch { }
        }

        public void Backward(double seconds)
        {
            try { Seek(Math.Max(0.0, GetPosition() - seconds)); } catch { }
        }

        private void DrawDebugOverlay(Graphics g)
        {
            try
            {
                string status = GetCurrentStyleName() + (analyzer != null && analyzer.IsAvailable ? " (Analyzer)" : " (Simulado)");
                using (Font f = new Font("Segoe UI", 12, FontStyle.Bold))
                using (SolidBrush br = new SolidBrush(Color.FromArgb(220, 255, 255, 255)))
                {
                    g.DrawString(status, f, br, 10, 10);
                }
            }
            catch { }
        }
    }
}
