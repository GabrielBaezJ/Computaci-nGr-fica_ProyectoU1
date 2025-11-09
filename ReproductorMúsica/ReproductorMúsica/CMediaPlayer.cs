using System;
using System.Collections.Generic;
using System.Drawing;
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

        public CMediaPlayer(WindowsMediaPlayer mediaPlayer, Timer timer, PictureBox canvas)
        {
            this.player = mediaPlayer;
            this.animationTimer = timer;
            this.canvas = canvas;

            if (this.animationTimer != null)
                this.animationTimer.Tick += Timer_Tick;
        }

        public void LoadTrack(string path, int styleIndex)
        {
            try
            {
                player.URL = path;
            }
            catch { }
            SetStyle(styleIndex);
            offset = 0;
            // Reiniciar líneas al parar
            linesToDraw = 0;
        }

        public void SetStyle(int styleIndex)
        {
            style = styleIndex;
            switch (style)
            {
                case 0:
                    pen = new Pen(Color.Cyan, 2);
                    break;
                case 1:
                    pen = new Pen(Color.MediumVioletRed, 2);
                    break;
                case 2:
                    pen = new Pen(Color.OrangeRed, 2);
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
        }

        public void Pause()
        {
            try { player.controls.pause(); } catch { }
            try { animationTimer?.Stop(); } catch { }
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
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (canvas == null) return;
            if (canvas.Width == 0 || canvas.Height == 0) return;

            Bitmap bmp = new Bitmap(canvas.Width, canvas.Height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Black);

                int centerX = canvas.Width / 2;
                int centerY = canvas.Height / 2;
                int radius = Math.Min(canvas.Width, canvas.Height) / 2 - 10;

                switch (style)
                {
                    case 0:
                        DrawLines(g, centerX, centerY, radius);
                        break;
                    case 1:
                        DrawSinusoidalBars(g, centerX, (centerY + 290), radius);
                        break;
                    case 2:
                        if (linesToDraw < totalLines)
                            linesToDraw++; // aumenta 1 línea por tick

                        DrawAnimatedRadialLines(g, centerX, centerY, radius, linesToDraw);
                        break;
                }
            }

            try { canvas.Image?.Dispose(); } catch { }
            canvas.Image = bmp;

            offset += 3;
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
            int barCount = 60;
            float barWidth = (float)canvas.Width / barCount;

            for (int i = 0; i < barCount; i++)
            {
                float x = i * barWidth;
                double angle = (i * 2 * Math.PI / barCount) + offset * 0.05;
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
    }
}
