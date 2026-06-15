using System;
using System.Drawing;

namespace RemoteControl.Client
{
    partial class ScreenUtil
    {
        private static bool IsMostlyBlackFrame(Bitmap image)
        {
            if (image == null || image.Width <= 0 || image.Height <= 0)
            {
                return true;
            }

            int samplesX = Math.Min(16, image.Width);
            int samplesY = Math.Min(16, image.Height);
            int total = 0;
            int dark = 0;
            int maxIntensity = 0;

            for (int y = 0; y < samplesY; y++)
            {
                int py = samplesY == 1 ? image.Height / 2 : (image.Height - 1) * y / (samplesY - 1);
                for (int x = 0; x < samplesX; x++)
                {
                    int px = samplesX == 1 ? image.Width / 2 : (image.Width - 1) * x / (samplesX - 1);
                    Color color = image.GetPixel(px, py);
                    int intensity = (color.R + color.G + color.B) / 3;
                    if (intensity <= 12)
                    {
                        dark++;
                    }

                    if (intensity > maxIntensity)
                    {
                        maxIntensity = intensity;
                    }

                    total++;
                }
            }

            return total > 0 && dark >= total * 98 / 100 && maxIntensity <= 30;
        }

        private static Bitmap CreateDiagnosticImage(Rectangle bounds, Exception ex)
        {
            int width = Math.Max(640, bounds.Width);
            int height = Math.Max(360, bounds.Height);
            Bitmap bitmap = new Bitmap(width, height);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            using (Brush background = new SolidBrush(Color.FromArgb(36, 48, 64)))
            using (Brush textBrush = new SolidBrush(Color.White))
            using (Font titleFont = new Font("Arial", 18F, FontStyle.Bold))
            using (Font bodyFont = new Font("Arial", 11F, FontStyle.Regular))
            {
                graphics.FillRectangle(background, 0, 0, width, height);
                graphics.DrawString("Screen capture compatibility fallback", titleFont, textBrush, new PointF(32, 32));
                graphics.DrawString("The current desktop returned only black frames from all GDI capture modes.", bodyFont, textBrush, new PointF(32, 76));
                graphics.DrawString("Check the display driver, RDP session, locked desktop, or GPU acceleration settings.", bodyFont, textBrush, new PointF(32, 104));
                if (ex != null && !string.IsNullOrEmpty(ex.Message))
                {
                    graphics.DrawString("Last error: " + ex.Message, bodyFont, textBrush, new RectangleF(32, 140, width - 64, height - 172));
                }
            }

            return bitmap;
        }
    }
}
