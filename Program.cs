using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Net;

namespace resultproject
{
    class Program
    {
        private static string LoadKey(string pathToFile)
        {
            string key;
            using (StreamReader sr = new StreamReader(pathToFile))
                key = sr.ReadLine().Replace(" ", string.Empty);

            return key;
        }
        static void Main(string[] args)
        {
            string pathToFileKey = "config/bot_key";
            string accessToken = LoadKey(pathToFileKey);

            var proxy = new WebProxy("51.91.212.159:3128"); // 51.158.111.229:8811 | 51.158.186.141:8080 | 51.91.212.159:3128

            TelegramBot tb = new TelegramBot(accessToken, proxy);
            tb.Run();
        }
    }
}
