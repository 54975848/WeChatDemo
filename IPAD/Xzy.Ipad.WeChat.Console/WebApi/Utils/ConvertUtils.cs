using System;
using System.Drawing;
using System.IO;

namespace WebApi.Utils
{
    public static class ConvertUtils
    {
        public static Bitmap GetImageFromBase64(string base64string)
        {
            byte[] b = Convert.FromBase64String(base64string);
            MemoryStream ms = new MemoryStream(b);
            Bitmap bitmap = new Bitmap(ms);
            return bitmap;
        }
    }
}