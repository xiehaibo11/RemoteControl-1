using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;

namespace RemoteControl.Protocals
{
    public class ResponseBase
    {
        public bool Result = true;
        public string Message = "成功";
        public string Detail = "";

        protected Image ByteArray2Image(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                return null;
            }

            using (MemoryStream ms = new MemoryStream(data))
            using (Image image = Image.FromStream(ms))
            {
                return new Bitmap(image);
            }
        }

        protected byte[] Image2ByteArray(Image image, ImageFormat imageFormat)
        {
            using (var ms = new MemoryStream())
            {
                image.Save(ms, imageFormat);
                return ms.ToArray();
            }
        }

        protected byte[] Image2JpegByteArray(Image image, long quality)
        {
            if (quality < 20) quality = 20;
            if (quality > 95) quality = 95;

            ImageCodecInfo codec = null;
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
            foreach (ImageCodecInfo item in codecs)
            {
                if (item.FormatID == ImageFormat.Jpeg.Guid)
                {
                    codec = item;
                    break;
                }
            }

            using (var ms = new MemoryStream())
            {
                if (codec == null)
                {
                    image.Save(ms, ImageFormat.Jpeg);
                }
                else
                {
                    using (var parameters = new EncoderParameters(1))
                    {
                        parameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
                        image.Save(ms, codec, parameters);
                    }
                }
                return ms.ToArray();
            }
        }
    }
}
