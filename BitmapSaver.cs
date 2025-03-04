using System;
using System.Drawing.Imaging;
using System.Drawing;

namespace GK_Projekt2
{
    public static class BitmapSaver
    {
        public static void SaveJpeg(Bitmap bitmap, string filePath, long compressionLevel)
        {
            if (compressionLevel < 0 || compressionLevel > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(compressionLevel), "Poziom kompresji musi wynosić między 1 a 100!");
            }

            ImageCodecInfo jpegCodec = GetEncoderInfo("image/jpeg");
            if (jpegCodec == null)
            {
                throw new InvalidOperationException("Nie udało się załadować kodeka JPEG");
            }

            EncoderParameters encoderParameters = new EncoderParameters(1);
            encoderParameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, compressionLevel);

            bitmap.Save(filePath, jpegCodec, encoderParameters);
        }

        private static ImageCodecInfo GetEncoderInfo(string mimeType)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.MimeType.Equals(mimeType, StringComparison.OrdinalIgnoreCase))
                {
                    return codec;
                }
            }
            return null;
        }
    }
}
