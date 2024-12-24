using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SnakeGame
{
    class Program
    {
        static void Main(string[] args)
        {
            var gameClient = new GameClient();
            gameClient.Start();
        }

    }


}
