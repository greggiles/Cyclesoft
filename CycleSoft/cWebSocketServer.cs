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

    public class remoteEventArgs : EventArgs
    {
        public bool pauseWorkout { get; set; } // used only in userWindow
        public bool muteWorkout { get; set; } // used only in userWindow
        public bool playPauseSpotify { get; set; } // used only in userWindow
        public bool prevSpotify { get; set; } // used only in userWindow
        public bool nextSpotify { get; set; } // used only in userWindow
        public bool muteSpotify { get; set; } // used only in userWindow
    }

    class cWebSocketServer
    {
        private WebSocketServer server;
        private List<IWebSocketConnection> allSockets;
        public int clientCount { get; private set; }
        public event EventHandler<remoteEventArgs> remoteEventStartStop;

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
                                remoteEventArgs e = new remoteEventArgs();
                                if (message.Contains("playSpotify"))
                                    e.playPauseSpotify = true;
                                if (message.Contains("nextSpotify"))
                                    e.nextSpotify = true;
                                if (message.Contains("pauseWorkout"))
                                    e.pauseWorkout = true;
                                if (message.Contains("muteWorkout"))
                                    e.muteWorkout = true;

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
