using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
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

namespace ANTSniffer
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //This delegate is for using the dispatcher to avoid conflicts of the feedback thread referencing the UI elements
        delegate void dAppendText(String toAppend);

        StreamWriter log;

        private cAntHandler AntHandler;
        private cStreamHandler StreamHandler;
        private cUserHandler UserHandler;
        private cWorkout WorkoutHandler;
        List<UserWindow> userWindows;
        
        public cDrawingEngine drawingEngine;

        private bool b_hasVid;

        private cWebSocketServer theServer;

        public MainWindow()
        {
            //totally cheating, and making this hard coded for now
            b_hasVid = false;
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

            for(int i=0; i<WorkoutHandler.workOutList.Count; i++)
                cbSelectWorkout.Items.Add(WorkoutHandler.workOutList[i].title);

            WorkoutHandler.workoutEventStartStop += updateWorkoutEvent;

            dataGridPower.DataContext = StreamHandler.pwrStreams;
            dataGridHR.DataContext = StreamHandler.hbStreams;
            dataGridSpdCad.DataContext = StreamHandler.spdStreams;

            dataGridUsers.DataContext = UserHandler.l_Users;

            Timer _timer = new Timer(200);
            _timer.Elapsed += new ElapsedEventHandler(_timerElapsed);
            _timer.Enabled = true;

            if (AntHandler.startUp())
            {
                AntHandler.channelMessageHandler += StreamHandler.handleAntData;
                button_Start.Background = System.Windows.Media.Brushes.Salmon;
                button_Start.Content = "Stop";
                if ((bool)bLogStreams.IsChecked)
                {
                    String Filename = "logfile.txt";
                    log = new StreamWriter(Filename.ToString());
                }
            }
            button3.IsEnabled = false;
            button3.Content = "Select Workout";

        }


        private void button_Start_Click(object sender, RoutedEventArgs e)
        {
            if (button_Start.Content.Equals("Start"))
            {
                if (AntHandler.startUp())
                {
                    AntHandler.channelMessageHandler += StreamHandler.handleAntData;
                    button_Start.Background = System.Windows.Media.Brushes.Salmon;
                    button_Start.Content = "Stop";
                    if ((bool)bLogStreams.IsChecked)
                    {
                        String Filename = "logfile.txt";
                        log = new StreamWriter(Filename.ToString());
                    }
                    return;
                }
            }
            //If we get here it means we failed startup or are stopping the demo
            AntHandler.shutdown();
            AntHandler.channelMessageHandler -= StreamHandler.handleAntData;

            StreamHandler.closeStreams();
            
            button_Start.Background = System.Windows.Media.Brushes.LightGreen;
            button_Start.Content = "Start";

            if (log != null)
            {
                log.Close();
            }
        }

        void initiateUserPointPanel(UserPointPanel upp, int userWin)
        {
            upp.Visibility = Visibility.Visible;
            upp.textBox_FirstUser1.Text = userWindows[userWin].userStreamToClose.firstName;
            upp.textBox_LastUser1.Text = userWindows[userWin].userStreamToClose.lastName;
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

        string updateUserPointPanel(UserPointPanel upp, int userWin)
        {
            string webdata = "<br>"+ userWindows[userWin].userStreamToClose.firstName + " ";
            webdata += userWindows[userWin].userStreamToClose.lastName + " <br>";
            webdata += userWindows[userWin].userStreamToClose.instPower.ToString() + "/";
            webdata += (WorkoutHandler.activeWorkout.segments[WorkoutHandler.activeSegment].effort * userWindows[userWin].userStreamToClose.ftp).ToString();

            webdata += "<br><progress id='power' max='2' value = '";
            webdata += ((double)userWindows[userWin].userStreamToClose.instPower / userWindows[userWin].userStreamToClose.ftp).ToString();
            webdata += "'></progress>";

            
            if (upp.textBox_FirstUser1.Text != userWindows[userWin].userStreamToClose.firstName)
            {
                upp.Visibility = Visibility.Visible;
                upp.textBox_FirstUser1.Text = userWindows[userWin].userStreamToClose.firstName;
                upp.textBox_LastUser1.Text = userWindows[userWin].userStreamToClose.lastName;
            }

            upp.textBox_HRUser1.Text = userWindows[userWin].userStreamToClose.hr.ToString();
            upp.textBox_PwrUser1.Text = userWindows[userWin].userStreamToClose.instPower.ToString();
            upp.textBox_PointsUser1.Text = userWindows[userWin].userStreamToClose.points.ToString();
            upp.textBox_CADUser1.Text = userWindows[userWin].userStreamToClose.cad.ToString();

            Thickness currentMargin = upp.polylineCanvas.Margin;
            if(WorkoutHandler.workOutSeconds>upp.polylineCanvasOuter.ActualWidth/2)
                currentMargin.Left = upp.polylineCanvasOuter.ActualWidth / 2 - WorkoutHandler.workOutSeconds;
            upp.polylineCanvas.Margin = currentMargin;
                            
            upp.polyline.Clear();
            upp.polyline.Figures.Add(userWindows[userWin].polyline.Figures[0]);

            if (userWindows[userWin].pwrline.Figures.Count > upp.pwrline.Figures.Count)
            {
                for (int i = upp.pwrline.Figures.Count; i <= userWindows[userWin].pwrline.Figures.Count; i++)
                {
                    upp.pwrline.Figures.Add(userWindows[userWin].pwrline.Figures[i]);
                }
            }
            
            if (userWindows[userWin].spdline.Figures.Count > upp.spdline.Figures.Count)
            {
                for (int i = upp.spdline.Figures.Count; i <= userWindows[userWin].spdline.Figures.Count; i++)
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

            return webdata;
        }


        void _timerElapsed(object sender, ElapsedEventArgs e)
        {

            this.Dispatcher.Invoke((Action)(() =>
            {

                try
                {
                    dataGridPower.Items.Refresh();
                    dataGridHR.Items.Refresh();
                    dataGridSpdCad.Items.Refresh();
                    dataGridUsers.Items.Refresh();

                    if(WorkoutHandler.bIsFinished)
                        bStartWorkout.Content = "Clear Workout";

                    try
                    {
                        textBoxTotalTime.Text = userWindows[0].textBoxTotalTime.Text;
                        textBoxSegmentTime.Text = userWindows[0].textBoxSegmentTime.Text;

                        string webresponse = userWindows[0].textBoxTotalTime.Text;
                        webresponse += "<br>" + userWindows[0].textBoxSegmentTime.Text;

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
                                webresponse += updateUserPointPanel(userPointPanel1, 0);
                                break;
                            case 2:
                                clrUserPointPanel(userPointPanel4);
                                clrUserPointPanel(userPointPanel3);
                                webresponse += updateUserPointPanel(userPointPanel2, 1);
                                webresponse += updateUserPointPanel(userPointPanel1, 0);
                                break;
                            case 3:
                                clrUserPointPanel(userPointPanel4);
                                webresponse += updateUserPointPanel(userPointPanel3, 2);
                                webresponse += updateUserPointPanel(userPointPanel2, 1);
                                webresponse += updateUserPointPanel(userPointPanel1, 0);
                                break;
                            default:
                                webresponse += updateUserPointPanel(userPointPanel4, 3);
                                webresponse += updateUserPointPanel(userPointPanel3, 2);
                                webresponse += updateUserPointPanel(userPointPanel2, 1);
                                webresponse += updateUserPointPanel(userPointPanel1, 0);
                                break;
                        }
                        theServer.senddata(webresponse);

                    
                    }
                    catch { }

                }
                catch
                { }
            }));
        }

        private void redraw_line(PathGeometry line, List<Point> points)
        {
            var figure = new PathFigure
            {
                StartPoint = points[0],
                IsClosed = false
            };

            var segment = new PolyLineSegment(points, true);
            figure.Segments.Add(segment);
            line.Clear();
            line.Figures.Add(figure);

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

        private void bLogDevice_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void bLogDevice_Unchecked(object sender, RoutedEventArgs e)
        {

        }

        private void bLogStreams_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void bLogStreams_Unchecked(object sender, RoutedEventArgs e)
        {

        }

        private void LaunchUserWindows(object sender, RoutedEventArgs e)
        {

            for (int i = 0; i < dataGridUsers.SelectedItems.Count; i++)
            {
                cAntUsers tempUser = (cAntUsers)dataGridUsers.SelectedItems[i];
                UserWindow childWin = new UserWindow();

                tempUser.userUpdateHandler += childWin.updateEvent;
                WorkoutHandler.workoutEventHandler += childWin.updateWorkoutEvent;
                WorkoutHandler.workoutEventHandler += tempUser.updateWorkoutEvent;


                userWindows.Add(childWin);
                childWin.setTitle(tempUser.firstName + " " + tempUser.lastName, (int)(tempUser.ftp*2));
                childWin.Activate();
                childWin.Closed += new EventHandler(UserWnd_Closed);
                childWin.Show();
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



        private void bStartWorkout_Click_1(object sender, RoutedEventArgs e)
        {

            if (bStartWorkout.Content.Equals("Launch Workout"))
            {
                if (WorkoutHandler.loadWorkout(cbSelectWorkout.SelectedIndex))
                {
                    bStartWorkout.Content = "Start Workout";
                    button3.Content = "Start Workout";
                    button3.IsEnabled = true;

                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        userPointPanel1.polylineCanvas.Width = WorkoutHandler.activeWorkout.length;
                        userPointPanel2.polylineCanvas.Width = WorkoutHandler.activeWorkout.length;
                        userPointPanel3.polylineCanvas.Width = WorkoutHandler.activeWorkout.length;
                        userPointPanel4.polylineCanvas.Width = WorkoutHandler.activeWorkout.length;
                    }));

                }
            }
            else if (bStartWorkout.Content.Equals("Start Workout"))
            {
                if (WorkoutHandler.startWorkout())
                {
                    bStartWorkout.Content = "Pause Workout";
                    button3.Content = "Pause Workout";
                }
            }
            else if (bStartWorkout.Content.Equals("Pause Workout"))
            {
                if (WorkoutHandler.playPauseWorkout())
                {
                    bStartWorkout.Content = "Re-Start Workout";
                    button3.Content = "Re-Start Workout";
                    if(b_hasVid)
                        mediaElement1.Pause();
                }
            }
            else if (bStartWorkout.Content.Equals("Re-Start Workout"))
            {
                if (WorkoutHandler.playPauseWorkout())
                {
                    bStartWorkout.Content = "Pause Workout";
                    button3.Content = "Pause Workout";
                    if (b_hasVid)
                        mediaElement1.Play();
                }
            }
            else if (bStartWorkout.Content.Equals("Clear Workout"))
            {
                if (WorkoutHandler.resetWorkout())
                {
                    bStartWorkout.Content = "Launch Workout";
                    cbSelectWorkout.SelectedIndex = 0;
                    bStartWorkout.IsEnabled = false;
                }
            }

        }

        private void cbSelectWorkout_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (bStartWorkout.Content.Equals("Start Workout"))
            {
                bStartWorkout.Content = "Launch Workout";
                bStartWorkout.IsEnabled = false;
            }

            if (cbSelectWorkout.SelectedIndex >= 0)
                bStartWorkout.IsEnabled = true;

            if (null == WorkoutHandler.workOutList[cbSelectWorkout.SelectedIndex].videopath)
                b_hasVid = false;
            else
            {
                b_hasVid = true;
                var uri = new System.Uri(WorkoutHandler.workOutList[cbSelectWorkout.SelectedIndex].videopath);
                var converted = uri.AbsoluteUri;
                mediaElement1.Source = uri;
            }
            drawLocalChart();

        }

        private void drawLocalChart()
        {
            Point[] currentWorkoutPoints = drawingEngine.getWorkoutPoints(WorkoutHandler.workOutList[cbSelectWorkout.SelectedIndex]);
            var figure = new PathFigure
            {
                StartPoint = currentWorkoutPoints[0],
                IsClosed = true
            };

            polyline.Figures.Clear();

            var segment = new PolyLineSegment(currentWorkoutPoints.Skip(1), true);
            figure.Segments.Add(segment);
            polyline.Figures.Add(figure);

        }

        private void bStartWorkout_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {

        }

        private void slider1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            this.drawingEngine.chartZoom = slider1.Value;
            if (cbSelectWorkout.SelectedIndex >= 0)
                drawLocalChart();

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
                if (e.running)
                {
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        mediaElement1.Play();
                       
                    }));
                }
                else
                {
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        mediaElement1.Pause();

                    }));
                }
            }
        }

        public void button2_Click(object sender, RoutedEventArgs e)
        {
            if (b_hasVid)
                mediaElement1.Play();
        }

        private void button3_Click(object sender, RoutedEventArgs e)
        {
            if (button3.Content.Equals("Pause Workout"))
            {
                if (WorkoutHandler.playPauseWorkout())
                {
                    bStartWorkout.Content = "Re-Start Workout";
                    button3.Content = "Re-Start Workout";
                    if (b_hasVid)
                        mediaElement1.Pause();
                }
            }
            else if (button3.Content.Equals("Re-Start Workout"))
            {
                if (WorkoutHandler.playPauseWorkout())
                {
                    bStartWorkout.Content = "Pause Workout";
                    button3.Content = "Re-Start Workout";
                    if (b_hasVid)
                        mediaElement1.Play();
                }
            }

        }

        private void button4_Click(object sender, RoutedEventArgs e)
        {
            if (b_hasVid)
                mediaElement1.Stop();
            WorkoutHandler.finish();
        }

        private void textBox3_TextChanged(object sender, TextChangedEventArgs e)
        {

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


    }

}
