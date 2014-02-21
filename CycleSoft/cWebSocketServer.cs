using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Fleck;

namespace CycleSoft
{
    public delegate void remoteEventStartStop(object sender, EventArgs e);

    class cWebSocketServer
    {
        private WebSocketServer server;
        private List<IWebSocketConnection> allSockets;
        public int clientCount { get; private set; }
        public event EventHandler<EventArgs> remoteEventStartStop;

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
                            if (null != remoteEventStartStop)
                            {
                                EventArgs e = new EventArgs();
                                remoteEventStartStop(this, e);
                            }
                            
                         /*   var assembly = Assembly.GetExecutingAssembly();
                            var resourceName = "CycleSoft.client.html";
                            string[] result = assembly.GetManifestResourceNames();

                            Stream stream = assembly.GetManifestResourceStream(resourceName);
                            StreamReader reader = new StreamReader(stream);

                            socket.Send(reader.ReadToEnd());
                          */
                            // return test;
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
        public int senddata(String sendme)
        {
                foreach (var socket in allSockets.ToList())
                {
                    socket.Send(sendme);
                }
                return allSockets.Count;
        }
    }

}
