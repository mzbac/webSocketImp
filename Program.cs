using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ConsoleApplication1
{
    class Program
    {   
        static void Main(string[] args)
        {
            WebSocketServer server = new WebSocketServer("127.0.0.1", 80);
            Console.ReadKey();
        }
    }
}
