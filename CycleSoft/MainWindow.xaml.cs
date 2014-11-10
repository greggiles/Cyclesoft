using System;
using System.Configuration;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
//using System.Drawing;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.XPath;
using Newtonsoft.Json;
using System.Net;
using System.Net.Sockets;
//using System.Runtime.InteropServices;
//using Bend.Util;
using System.Threading;
using SpotifyAPI;


namespace CycleSoft
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //This delegate is for using the dispatcher to avoid conflicts of the feedback thread referencing the UI elements
        delegate void dAppendText(String toAppend);

        private cAntHandler AntHandler;
        private cStreamHandler StreamHandler;
        private cUserHandler UserHandler;
        private cWorkout WorkoutHandler;
        List<UserWindow> userWindows;
        
        public cDrawingEngine drawingEngine;

        private bool b_hasVid;
        private bool b_vidPlaying;

        private MyHttpServer webServer;
        private Thread webThread;
        private ThreadStart webListen;
        private cWebSocketServer theServer;
        private int clientCount;

        private static readonly Lazy<SpotifyApi> SpotifyClient = new Lazy<SpotifyApi>(() =>
        {
            var api = new SpotifyApi();
            api.Initialize();
            Thread.Sleep(300);
            return api;
        });


        public class JsonData
        {
            public workoutEventArgs wEA { get; set; }
            public List<userEventArgs> uEAs { get; set; }
        }

/*
 *      [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll", SetLastError = true)]  
        static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);          
        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")]
        public static extern IntPtr PostMessage(IntPtr hWnd, uint Msg, Int32 wParam, Int32 lParam);
        */

// removed for internal web service
//        Process webProcess;

        const uint WM_KEYDOWN = 0x100;
        const uint WM_SYSCOMMAND = 0x018;
        const uint SC_CLOSE = 0x053;

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            // This doesn't work, becuase the process was launched with Elevated Privledges

            /*
            if (webProcess != null)
            {
                IntPtr h = webProcess.MainWindowHandle;
                IntPtr editHandle = FindWindowEx(h, IntPtr.Zero, "EDIT", null);
                PostMessage(editHandle, WM_KEYDOWN, 'Q', 0);
            }
            */
            webServer.close();
            AntHandler.shutdown();
            AntHandler.channelMessageHandler -= StreamHandler.handleAntData;
            StreamHandler.closeStreams();

            
            Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;

            Application.Current.Shutdown();
            //ok, this isn't pretty, but ...
            //webThread.Abort();

        }
        public MainWindow()
        {

            //totally cheating, and making this hard coded for now
            b_hasVid = false;
            b_vidPlaying = false;
            // video file is linked from xaml file, anyways.

            drawingEngine = new cDrawingEngine();
            drawingEngine.chartZoom = 1.6;

            InitializeComponent();

            slider1.Value = 1.6;

            AntHandler = new cAntHandler();
            StreamHandler = new cStreamHandler();
            UserHandler = new cUserHandler(StreamHandler);
            userWindows = new List<UserWindow>();
            WorkoutHandler = new cWorkout();

            theServer = new cWebSocketServer();
            webServer = new MyHttpServer(8080);
            webListen = new ThreadStart(webServer.listen);
            webThread = new Thread(webListen);
            webThread.Start();


            clientCount = 0;

            // For IIS Express, this updates the applicationhost.config file, so that we know we run 
            // from the same directory the program is running. What a PIA
/*
            string IISExpressFile = "C:\\Program Files\\IIS Express\\iisexpress.exe";
            if (!System.IO.File.Exists(IISExpressFile))
            {
                AutoClosingMessageBox.Show("To Use web features, install IISExpress from Microsoft", "Webservice Error", 3000);
            }
            else
            {
                string filename = "applicationhost.config";
                if (System.IO.File.Exists(filename))
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(filename);
                    XmlAttribute path = (XmlAttribute)doc.SelectSingleNode("//configuration/system.applicationHost/sites/site/application/virtualDirectory/@physicalPath");
                    //path.InnerText = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location).Replace(@"\", @"\\") + "\\\\web";
                    path.InnerText = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + "\\web";
                    doc.Save(filename);
                }

                List<string> knownAddresses = new List<string>();
                IPHostEntry host;
                host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (IPAddress ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        knownAddresses.Add("http://" + ip.ToString() + ":8080/");
                    }
                }
                knownAddresses.Add("http://localhost" + ":8080/");

                StringBuilder Message = new StringBuilder("Now Accepting Web Connections at:\n");
                foreach (string url in knownAddresses) Message.AppendLine(url);



                // Create Rules and Open Firewalls and Start IIS Express
                ProcessStartInfo p1 = new ProcessStartInfo("LaunchWeb.bat", System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location).Replace(@"\", @"/"));
                p1.UseShellExecute = true;
                p1.WindowStyle = ProcessWindowStyle.Minimized;
                p1.Verb = "runas";
                try
                {
                    webProcess = Process.Start(p1);
                    AutoClosingMessageBox.Show(Message.ToString(), "Web Server Running", 2500);
                }
                catch
                { MessageBox.Show("Failed to start WebProcess"); }
            }
*/            

            for (int i = 0; i < WorkoutHandler.workOutList.Count; i++)
            {
                TimeSpan woTime = new TimeSpan(0, 0, (int)WorkoutHandler.workOutList[i].length);
                cbSelectWorkout.Items.Add(WorkoutHandler.workOutList[i].title + " " + woTime.ToString(@"h\:mm\:ss"));
                cbSelectEditWorkout.Items.Add(WorkoutHandler.workOutList[i].title);
            }

            WorkoutHandler.workoutEventStartStop += updateWorkoutEvent;
            theServer.remoteEventStartStop += remotePlayPauseEvent;
            

            dataGridPower.DataContext = StreamHandler.pwrStreams;
            dataGridHR.DataContext = StreamHandler.hbStreams;
            dataGridSpdCad.DataContext = StreamHandler.spdStreams;

            dataGridUsers.DataContext = UserHandler.l_Users;

            System.Timers.Timer _timer = new System.Timers.Timer(500);
            _timer.Elapsed += new ElapsedEventHandler(_timerElapsed);
            _timer.Enabled = true;


            lbl_ANTStatus.Content = "ANT Device not Found, installing or disabling Garmin Agent, then press \"Start\"";

            if (AntHandler.startUp())
            {
                AntHandler.channelMessageHandler += StreamHandler.handleAntData;
                button_Start.Background = System.Windows.Media.Brushes.Salmon;
                button_Start.Content = "ANT ENABLED";
                lbl_ANTStatus.Content = "Press \"ANT ENABLED\" button to stop";
            }
            bStartWorkout.IsEnabled = false;
            bStartWorkout2.IsEnabled = false;
            bEndWorkout.IsEnabled = false;
            bEndWorkout2.IsEnabled = false;
            bStartWorkout.Content = "Select Workout";
            bStartWorkout2.Content = "Select Workout";
            bEndWorkout.Content = "End Workout";
            bEndWorkout2.Content = "End Workout";

        }


        private void button_Start_Click(object sender, RoutedEventArgs e)
        {
            if (button_Start.Content.Equals("Start"))
            {
                if (AntHandler.startUp())
                {
                    AntHandler.channelMessageHandler += StreamHandler.handleAntData;
                    button_Start.Background = System.Windows.Media.Brushes.Salmon;
                    button_Start.Content = "ANT ENABLED";
                    lbl_ANTStatus.Content = "Press \"ANT ENABLED\" button to stop";

                    return;
                }
            }
            //If we get here it means we failed startup or are stopping the demo
            AntHandler.shutdown();
            AntHandler.channelMessageHandler -= StreamHandler.handleAntData;

            StreamHandler.closeStreams();
            
            button_Start.Background = System.Windows.Media.Brushes.LightGreen;
            button_Start.Content = "Start";
            lbl_ANTStatus.Content = "ANT Device not Found, installing or disabling Garmin Agent, then press \"Start\"";

        }

        void clrUserPointPanel(UserPointPanel upp)
        {
            upp.Visibility = Visibility.Hidden;
            upp.textBox_FirstUser1.Text = "";
            upp.textBox_LastUser1.Text = "";
            upp.textBox_HRUser1.Text = "";
            upp.textBox_PwrUser1.Text = "";
            upp.textBox_PointsUser1.Text = "";
            upp.textBox_CADUser1.Text = "";
        }

        void updateUserPointPanel(UserPointPanel upp, int userWin)
        {
            upp.Visibility = Visibility.Visible;
            upp.textBox_FirstUser1.Text = userWindows[userWin].userStreamToClose.firstName;
            upp.textBox_LastUser1.Text = userWindows[userWin].userStreamToClose.lastName;

            upp.textBox_HRUser1.Text = userWindows[userWin].userStreamToClose.hr.ToString();
            upp.textBox_PwrUser1.Text = userWindows[userWin].userStreamToClose.instPower.ToString();
            upp.textBox_PointsUser1.Text = Math.Truncate(userWindows[userWin].userStreamToClose.points).ToString();
            upp.textBox_CADUser1.Text = userWindows[userWin].userStreamToClose.cad.ToString();

            Thickness currentMargin = upp.polylineCanvas.Margin;
            if(WorkoutHandler.workOutSeconds>upp.polylineCanvasOuter.ActualWidth/2)
                currentMargin.Left = upp.polylineCanvasOuter.ActualWidth / 2 - WorkoutHandler.workOutSeconds;
            upp.polylineCanvas.Margin = currentMargin;
                            
            upp.polyline.Clear();
            if (userWindows[userWin].polyline.Figures.Count > 0)
                upp.polyline.Figures.Add(userWindows[userWin].polyline.Figures[0]);

            if (userWindows[userWin].pwrline.Figures.Count > upp.pwrline.Figures.Count)
            {
                for (int i = upp.pwrline.Figures.Count; i < userWindows[userWin].pwrline.Figures.Count; i++)
                {
                    upp.pwrline.Figures.Add(userWindows[userWin].pwrline.Figures[i]);
                }
            }
            
            if (userWindows[userWin].spdline.Figures.Count > upp.spdline.Figures.Count)
            {
                for (int i = upp.spdline.Figures.Count; i < userWindows[userWin].spdline.Figures.Count; i++)
                {
                    upp.spdline.Figures.Add(userWindows[userWin].spdline.Figures[i]);
                }
            }
            upp.polylinePwr.Clear();
            upp.polylineAvgPwr.Clear();
            upp.polylinePwrTarget.Clear();
            upp.polylineCadTarget.Clear();
            upp.polylinePwr.Figures.Add(userWindows[userWin].polylinePwr.Figures[0]);
            upp.polylineAvgPwr.Figures.Add(userWindows[userWin].polylineAvgPwr.Figures[0]);
            upp.polylinePwrTarget.Figures.Add(userWindows[userWin].polylinePwrTarget.Figures[0]);
            upp.polylineCadTarget.Figures.Add(userWindows[userWin].polylineCadTarget.Figures[0]);

            upp.powerMeterCanvas.Background = userWindows[userWin].powerMeterCanvas.Background;
            upp.cadMeterCanvas.Background = userWindows[userWin].cadMeterCanvas.Background;

        }


        void _timerElapsed(object sender, ElapsedEventArgs e)
        {

            // moved out of Invoke? 2014-01-29
            if (WorkoutHandler.bIsFinished)
            {
                bStartWorkout.IsEnabled = false;
                bStartWorkout2.IsEnabled = false;
                bEndWorkout.IsEnabled = false;
                bEndWorkout2.IsEnabled = false;
                bStartWorkout.Content = "Select Workout";
                bStartWorkout2.Content = "Select Workout";
                bEndWorkout.Content = "End Workout";
                bEndWorkout2.Content = "End Workout";
            }

            this.Dispatcher.BeginInvoke((Action)(() =>
            {

                try
                {
                    dataGridPower.Items.Refresh();
                    dataGridHR.Items.Refresh();
                    dataGridSpdCad.Items.Refresh();
                    dataGridUsers.Items.Refresh();
                }
                catch
                {}

                try
                {
                    if (userWindows.Count > 0)
                    {
                        textBoxTotalTime.Text = userWindows[0].textBoxTotalTime.Text;
                        textBoxSegmentTime.Text = userWindows[0].textBoxSegmentTime.Text;

                    }
                    else
                    {
                        textBoxTotalTime.Text = "??";
                        textBoxSegmentTime.Text = "??";
                    }
                }
                catch 
                {}

                try
                {
                    switch (userWindows.Count)
                    {
                        case 0:
                            clrUserPointPanel(userPointPanel4);
                            clrUserPointPanel(userPointPanel3);
                            clrUserPointPanel(userPointPanel2);
                            clrUserPointPanel(userPointPanel1);
                            break;
                        case 1:
                            clrUserPointPanel(userPointPanel4);
                            clrUserPointPanel(userPointPanel3);
                            clrUserPointPanel(userPointPanel2);
                            updateUserPointPanel(userPointPanel1, 0);
                            break;
                        case 2:
                            clrUserPointPanel(userPointPanel4);
                            clrUserPointPanel(userPointPanel3);
                            updateUserPointPanel(userPointPanel2, 1);
                            updateUserPointPanel(userPointPanel1, 0);
                            break;
                        case 3:
                            clrUserPointPanel(userPointPanel4);
                            updateUserPointPanel(userPointPanel3, 2);
                            updateUserPointPanel(userPointPanel2, 1);
                            updateUserPointPanel(userPointPanel1, 0);
                            break;
                        default:
                            updateUserPointPanel(userPointPanel4, 3);
                            updateUserPointPanel(userPointPanel3, 2);
                            updateUserPointPanel(userPointPanel2, 1);
                            updateUserPointPanel(userPointPanel1, 0);
                            break;
                    }
                }                 
                catch
                {}


            }));


            try
            {
                //theServer.senddata(webresponse);

                JsonData toSend = new JsonData();
                toSend.uEAs = new List<userEventArgs>();

                if (userWindows.Count > 0 || WorkoutHandler.bIsRunning)
                {
                    foreach (UserWindow uw in userWindows)
                    {
                        //workoutEventArgs wEa = new workoutEventArgs();
                        //wEa = uw.workoutStatus;
                        toSend.wEA = uw.workoutStatus;
                        userEventArgs uEa = new userEventArgs();
                        uEa = uw.userStatus;
                        toSend.uEAs.Add(uEa);
                    }
                    var json = JsonConvert.SerializeObject(toSend);
                    /*                            var json = JsonConvert.SerializeObject(toSend,
                                                    new JsonSerializerSettings() { Formatting = Newtonsoft.Json.Formatting.None });
                    */
                    int newCount = theServer.senddata(json);
                    if (newCount != clientCount)
                    {
                        clientCount = newCount;
                        if (clientCount > 0)
                        {
                            json = JsonConvert.SerializeObject(WorkoutHandler.activeWorkout);
                            /*                                    json = JsonConvert.SerializeObject(WorkoutHandler.activeWorkout,
                                                                    new JsonSerializerSettings() { Formatting = Newtonsoft.Json.Formatting.None });
                            */
                            theServer.senddata(json);
                        }
                    }
                }

            }
            catch { }
        
        
        
        }


        void UserWnd_Closed(object sender, EventArgs e)
        {
            UserWindow lw = (UserWindow)sender;
            for (int i = userWindows.Count - 1; i >= 0; i--)
            {
                if (lw == userWindows[i])
                {
                    // find and get rid of event Handlers
                    // !!! need to do this.
                    userWindows.RemoveAt(i);
                    break;
                }
            }

        }

        private void LaunchUserWindows(object sender, RoutedEventArgs e)
        {

            for (int i = 0; i < dataGridUsers.SelectedItems.Count; i++)
            {
                if (i < UserHandler.l_Users.Count)
                {
                    cAntUsers tempUser = (cAntUsers)dataGridUsers.SelectedItems[i];
                    UserWindow childWin = new UserWindow();

                    tempUser.userUpdateHandler += childWin.updateEvent;
                    // this should go away. Everything that happens below should probably go to 
                    // the tempUser update, and the above event should pass whatever is required.
                    WorkoutHandler.workoutEventHandler += childWin.updateWorkoutEvent;
                    // The below should handle most of the above.
                    WorkoutHandler.workoutEventHandler += tempUser.updateWorkoutEvent;


                    userWindows.Add(childWin);
                    childWin.setTitle(tempUser.firstName + " " + tempUser.lastName, (int)(tempUser.ftp * 2));
                    childWin.Activate();
                    childWin.Closed += new EventHandler(UserWnd_Closed);
                    childWin.Show();
                }
            }

        }

        private void buttonAddUser_Click(object sender, RoutedEventArgs e)
        {
            UserHandler.addUser(this, StreamHandler);
            dataGridUsers.Items.Refresh();
        }

        private void buttonRemoveUsers_Click(object sender, RoutedEventArgs e)
        {
            if (dataGridUsers.SelectedIndex >= 0)
            {
                UserHandler.removeUser(dataGridUsers.SelectedIndex);
                dataGridUsers.Items.Refresh();
            }
        }

        private void buttonDeleteUsers_Click(object sender, RoutedEventArgs e)
        {
            if (dataGridUsers.SelectedIndex >= 0)
            {
                UserHandler.deleteUser(dataGridUsers.SelectedIndex);
                dataGridUsers.Items.Refresh();
            }

        }

        public void remotePlayPauseEvent(object sender, remoteEventArgs e)
        {
            if (e.pauseWorkout)
                this.Dispatcher.BeginInvoke((Action)(() =>
                {
                    playPause();
                }
                ));

            if (e.nextSpotify)
                SpotifyClient.Value.Next();

            if (e.playPauseSpotify)
                SpotifyClient.Value.PlayPause();

        }

        
        private void bStartWorkout_Click(object sender, RoutedEventArgs e)
        {
            playPause();
        }

        private void playPause()
        {

            if (bStartWorkout.Content.Equals("Start Workout"))
            {
                if (WorkoutHandler.startWorkout())
                {
                    cbSelectWorkout.IsEnabled = false;
                    bStartWorkout.Content = "Pause Workout";
                    bStartWorkout2.Content = "Pause Workout";
                    bEndWorkout.IsEnabled = false;
                    bEndWorkout2.IsEnabled = false;
                }
            }
            else if (bStartWorkout.Content.Equals("Pause Workout"))
            {
                if (WorkoutHandler.playPauseWorkout())
                {
                    bStartWorkout.Content = "Re-Start Workout";
                    bStartWorkout2.Content = "Re-Start Workout";
                    bEndWorkout.IsEnabled = true;
                    bEndWorkout2.IsEnabled = true;
                    if (b_hasVid)
                    {
                        b_vidPlaying = false;
                        mediaElement1.Pause();
                    }
                }
            }
            else if (bStartWorkout.Content.Equals("Re-Start Workout"))
            {
                if (WorkoutHandler.playPauseWorkout())
                {
                    bStartWorkout.Content = "Pause Workout";
                    bStartWorkout2.Content = "Pause Workout";
                    bEndWorkout.IsEnabled = false;
                    bEndWorkout2.IsEnabled = false;
                    if (b_hasVid)
                    {
                        mediaElement1.Play();
                        b_vidPlaying = true;
                    }
                }
            }

        }

        private void bEndWorkout_Click(object sender, RoutedEventArgs e)
        {
            cbSelectWorkout.IsEnabled = true;
            cbSelectWorkout.SelectedIndex = -1;

            WorkoutHandler.finish();
            //WorkoutHandler.resetWorkout();
            
            bStartWorkout.IsEnabled = false;
            bStartWorkout2.IsEnabled = false;
            bEndWorkout.IsEnabled = false;
            bEndWorkout2.IsEnabled = false;
            bStartWorkout.Content = "Select Workout";
            bStartWorkout2.Content = "Select Workout";
            bEndWorkout.Content = "End Workout";
            bEndWorkout2.Content = "End Workout";
        }

        private void cbSelectEditWorkout_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbSelectEditWorkout.SelectedIndex >= 0)
            {
                drawLocalChart(cbSelectEditWorkout.SelectedIndex, polyline1);
            }
        }
        private void cbSelectWorkout_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbSelectWorkout.SelectedIndex >= 0)
            {
                if (WorkoutHandler.loadWorkout(cbSelectWorkout.SelectedIndex))
                {
                    bStartWorkout.IsEnabled = true;
                    bStartWorkout2.IsEnabled = true;
                    bStartWorkout.Content = "Start Workout";
                    bStartWorkout2.Content = "Start Workout";
                    WorkoutHandler.resetWorkout();

                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        userPointPanel1.polylineCanvas.Width = WorkoutHandler.activeWorkout.length;
                        userPointPanel2.polylineCanvas.Width = WorkoutHandler.activeWorkout.length;
                        userPointPanel3.polylineCanvas.Width = WorkoutHandler.activeWorkout.length;
                        userPointPanel4.polylineCanvas.Width = WorkoutHandler.activeWorkout.length;

                        userPointPanel1.pwrline.Clear();
                        userPointPanel2.pwrline.Clear();
                        userPointPanel3.pwrline.Clear();
                        userPointPanel4.pwrline.Clear();

                        userPointPanel1.spdline.Clear();
                        userPointPanel2.spdline.Clear();
                        userPointPanel3.spdline.Clear();
                        userPointPanel4.spdline.Clear();


                    }));

                    if (null == WorkoutHandler.workOutList[cbSelectWorkout.SelectedIndex].videopath)
                        b_hasVid = false;
                    else
                    {
                        b_hasVid = true;
                        var uri = new System.Uri(WorkoutHandler.workOutList[cbSelectWorkout.SelectedIndex].videopath);
                        var converted = uri.AbsoluteUri;
                        mediaElement1.Source = uri;
                    }
                    drawLocalChart(cbSelectWorkout.SelectedIndex, polyline);

                    var json = JsonConvert.SerializeObject(WorkoutHandler.activeWorkout);
/*                    var json = JsonConvert.SerializeObject(WorkoutHandler.activeWorkout,
                        new JsonSerializerSettings() { Formatting = Newtonsoft.Json.Formatting.None });
*/
                    theServer.senddata(json);
                }
            }
        }
        private void drawLocalChart(int Selected, PathGeometry target)
        {
            if (Selected < 0 || Selected >= WorkoutHandler.workOutList.Count)
                return;
            Point[] currentWorkoutPoints = drawingEngine.getWorkoutPoints(WorkoutHandler.workOutList[Selected]);
            var figure = new PathFigure
            {
                StartPoint = currentWorkoutPoints[0],
                IsClosed = true
            };

            target.Figures.Clear();

            var segment = new PolyLineSegment(currentWorkoutPoints.Skip(1), true);
            figure.Segments.Add(segment);
            target.Figures.Add(figure);

        }

        private void bStartWorkout_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {

        }

        private void slider1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            this.drawingEngine.chartZoom = slider1.Value;
            if (cbSelectWorkout.SelectedIndex >= 0)
                drawLocalChart(cbSelectWorkout.SelectedIndex, polyline);

            try
            {
                userPointPanel1.pwrline.Clear();
                userPointPanel2.pwrline.Clear();
                userPointPanel3.pwrline.Clear();
                userPointPanel4.pwrline.Clear();

                userPointPanel1.spdline.Clear();
                userPointPanel2.spdline.Clear();
                userPointPanel3.spdline.Clear();
                userPointPanel4.spdline.Clear();
            }
            catch { }


            if (userWindows != null)
            {
                foreach (UserWindow uw in userWindows)
                    uw.changeZoom(slider1.Value);
            }
        }

        public void updateWorkoutEvent(object sender, workoutStatusArgs e)
        {
            if (b_hasVid)
            {
                if (e.running && !b_vidPlaying)
                {
                    b_vidPlaying = true;
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        mediaElement1.Play();
                       
                    }));
                }
                else if (!e.running && b_vidPlaying)
                {
                    b_vidPlaying = false;
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        mediaElement1.Pause();

                    }));
                }
            }
        }


        private void mediaElement1_MediaOpened(object sender, RoutedEventArgs e)
        {

        }

        private void buttonSaveUser_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < dataGridUsers.SelectedItems.Count; i++)
            {
                cAntUsers tempUser = (cAntUsers)dataGridUsers.SelectedItems[i];
                UserHandler.saveUser(tempUser);
            }

        }

        private void checkBox1_Checked(object sender, RoutedEventArgs e)
        {
            if (b_hasVid)
                mediaElement1.IsMuted = true;
        }

        private void checkBox1_Unchecked(object sender, RoutedEventArgs e)
        {
            if (b_hasVid)
                mediaElement1.IsMuted = false;

        }

        private void textBoxFirstName_GotFocus(object sender, RoutedEventArgs e)
        {
            textBoxFirstName.SelectAll();
        }

        private void textBoxLastName_GotFocus(object sender, RoutedEventArgs e)
        {
            textBoxLastName.SelectAll();
        }

        private void textBoxFTP_GotFocus(object sender, RoutedEventArgs e)
        {
            textBoxFTP.SelectAll();
        }

        private void buttonModUser_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Not Done Yet. Intent to go to User Config Tab Automatically with selections made, and button changed to \"mod\" user."); 
            
        }


    }

    public class AutoClosingMessageBox
    {
        System.Threading.Timer _timeoutTimer;
        string _caption;
        AutoClosingMessageBox(string text, string caption, int timeout)
        {
            _caption = caption;
            _timeoutTimer = new System.Threading.Timer(OnTimerElapsed,
                null, timeout, System.Threading.Timeout.Infinite);
            MessageBox.Show(text, caption);
        }
        public static void Show(string text, string caption, int timeout)
        {
            new AutoClosingMessageBox(text, caption, timeout);
        }
        void OnTimerElapsed(object state)
        {
            IntPtr mbWnd = FindWindow(null, _caption);
            if (mbWnd != IntPtr.Zero)
                SendMessage(mbWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            _timeoutTimer.Dispose();
        }
        const int WM_CLOSE = 0x0010;
        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);
    }

}
