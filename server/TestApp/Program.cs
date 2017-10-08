using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: TestApp projectDirectoryPath");
                return;
            }
            var engine = NemerleServer.NemerleEngine.LoadFromWorkspace(args[0]);
            Console.WriteLine("Nemerle Engine is up.");
        }
    }
}
