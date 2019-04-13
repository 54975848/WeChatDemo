using System;
using System.Runtime.InteropServices;

namespace WebApi.Util
{
    public class Convert62
    {
        /// <summary>
        /// 16进制转字符串
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        [DllImport("EUtils.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr EStrToHex(string context);

        /// <summary>
        /// 16进制转字符串
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static string eStrToHex(string context)
        {
            IntPtr intptr = EStrToHex(context);
            string str = "" + Marshal.PtrToStringAnsi(intptr).Substring(0, 344);
            return str;
        }
    }
}