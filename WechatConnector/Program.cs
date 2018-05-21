using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WechatConnector
{
    public class Program
    {
        public static void Main(string[] args)
        {
#if DEBUG
            PrintSplash();
#endif
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .Build();

        private static void PrintSplash()
        {
            try
            {
                Console.BackgroundColor = ConsoleColor.Green;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("".PadRight(Console.WindowWidth - 1));
                Console.WriteLine(" WeChat Connector".PadRight(Console.WindowWidth - 1));
                Console.WriteLine("".PadRight(Console.WindowWidth - 1));
                Console.WriteLine();
                Console.ResetColor();
            }catch(System.IO.IOException ex)
            {
                //Do nothing - we're not in a console app, are we?
            }
        }
    }
}
