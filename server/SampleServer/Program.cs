using LanguageServer.Infrastructure.JsonDotNet;
using LanguageServer.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleServer
{
    class Program
    {
        static void Main(string[] args)
        {
            var exeDirectory = Path.GetDirectoryName(typeof(Program).Assembly.Location);
            var logFile = Path.Combine(exeDirectory, "LogFile.txt");

            Console.OutputEncoding = Encoding.UTF8;
            var app = new App(Console.OpenStandardInput(), Console.OpenStandardOutput(), x =>
            {
                File.AppendAllText(logFile, x);
            });
            Logger.Instance.Attach(app);
            try
            {
                app.Listen().Wait();
            }
            catch (AggregateException ex)
            {
                Console.Error.WriteLine(ex.InnerExceptions[0]);
                Environment.Exit(-1);
            }
        }
    }
}
