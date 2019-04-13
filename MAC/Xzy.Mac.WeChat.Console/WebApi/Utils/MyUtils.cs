using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WebApi.Utils
{
    public static class MyUtils
    {
        public static bool IsFileInUse(string fileName)
        {
            bool inUse = true;
            FileStream fs = null;
            try
            {

                fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);

                inUse = false;
            }
            catch (Exception ex)
            {
                try
                {
                    Process[] pcs = Process.GetProcesses();
                    foreach (Process p in pcs)
                    {
                        if (p.MainModule.FileName == fileName)
                        {
                            p.Kill();
                        }
                    }
                }
                catch (Exception e) { }
            }
            finally
            {
                if (fs != null)

                    fs.Close();
            }
            return inUse;//true表示正在使用,false没有使用  
        }


        public static Bitmap Base64StringToImage(string basestr)
        {
            Bitmap bitmap = null;
            try
            {
                String inputStr = basestr;
                byte[] arr = Convert.FromBase64String(inputStr);
                MemoryStream ms = new MemoryStream(arr);
                Bitmap bmp = new Bitmap(ms);
                ms.Close();
                bitmap = bmp;
                //MessageBox.Show("转换成功！");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Base64StringToImage 转换失败\nException：" + ex.Message);
            }

            return bitmap;
        }

        public static string ImageToBase64String(this Image image)
        {
            return Convert.ToBase64String(image.ImageToBytes());
        }

        /// <summary>
        /// 分析 url 字符串中的参数信息
        /// </summary>
        /// <param name="url">输入的 URL</param>
        /// <param name="baseUrl">输出 URL 的基础部分</param>
        /// <param name="nvc">输出分析后得到的 (参数名,参数值) 的集合</param>
        public static void ParseUrl(string url, out string baseUrl,out NameValueCollection nvc)
        {
            if (url == null) {
                throw new ArgumentNullException("url");
            }
            nvc = new NameValueCollection();
            baseUrl = "";
            if (url == "")
                return;
            int questionMarkIndex = url.IndexOf('?');
            if (questionMarkIndex == -1) {
                baseUrl = url;
                return;
            }
            baseUrl = url.Substring(0, questionMarkIndex);
            if (questionMarkIndex == url.Length - 1) {
                baseUrl = url;
                return;
            }
            string ps = url.Substring(questionMarkIndex + 1);
            //开始分析参数对
            Regex re = new Regex(@"(^|&)?(\w+)=([^&]+)(&|$)?", RegexOptions.Compiled);
            MatchCollection mc = re.Matches(ps);
            foreach (Match m in mc) {
                nvc.Add(m.Result("$2").ToLower(), m.Result("$3"));
            }
        }
    }
}
