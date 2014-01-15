using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Fleck;

namespace CycleSoft
{
    class cWebSocketServer
    {
        private WebSocketServer server;
        private List<IWebSocketConnection> allSockets;

        public cWebSocketServer()
        {   
            FleckLog.Level = LogLevel.Debug;
            // var allSockets = new List<IWebSocketConnection>();
            allSockets = new List<IWebSocketConnection>();
            //var server = new WebSocketServer("ws://localhost:8181");
            server = new WebSocketServer("ws://localhost:8181");
            server.Start(socket =>
                {
                    socket.OnOpen = () =>
                        {
                            //Console.WriteLine("Open!");
                            allSockets.Add(socket);
                        };
                    socket.OnClose = () =>
                        {
                            // Console.WriteLine("Close!");
                            allSockets.Remove(socket);
                        };
                    socket.OnMessage = message =>
                        {
                            //Console.WriteLine(message);
                            // allSockets.ToList().ForEach(s => s.Send("Echo: " + message));
                        };
                });

        }
//            var input = Console.ReadLine();
/*            var input = "";
            while (input != "exit")
            {
                foreach (var socket in allSockets.ToList())
                {
                    socket.Send(input);
                }
                // input = Console.ReadLine();
            }
            */
        public void senddata(String sendme)
        {
                foreach (var socket in allSockets.ToList())
                {
                    socket.Send(sendme);
                }
            
        }
    }

}
