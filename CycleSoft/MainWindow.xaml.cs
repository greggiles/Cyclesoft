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

            for (int i = 0; i < WorkoutHandler.workOutList.Count; i++)
            {
                cbSelectWorkout.Items.Add(WorkoutHandler.workOutList[i].title);
                cbSelectEditWorkout.Items.Add(WorkoutHandler.workOutList[i].title);
            }

            WorkoutHandler.workoutEventStartStop += updateWorkoutEvent;

            dataGridPower.DataContext = StreamHandler.pwrStreams;
            dataGridHR.DataContext = StreamHandler.hbStreams;
            dataGridSpdCad.DataContext = StreamHandler.spdStreams;

            dataGridUsers.DataContext = UserHandler.l_Users;

            Timer _timer = new Timer(500);
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

        string updateUserPointPanel(UserPointPanel upp, int userWin)
        {
            string webdata = "<br>"+ userWindows[userWin].userStreamToClose.firstName + " ";
            webdata += userWindows[userWin].userStreamToClose.lastName + " <br>";
            webdata += userWindows[userWin].userStreamToClose.instPower.ToString() + "/";
            webdata += (WorkoutHandler.activeWorkout.segments[WorkoutHandler.activeSegment].effort * userWindows[userWin].userStreamToClose.ftp).ToString();

            webdata += "<br><progress id='power' max='2' value = '";
            webdata += ((double)userWindows[userWin].userStreamToClose.instPower / userWindows[userWin].userStreamToClose.ftp).ToString();
            webdata += "'></progress>";

            upp.Visibility = Visibility.Visible;
            upp.textBox_FirstUser1.Text = userWindows[userWin].userStreamToClose.firstName;
            upp.textBox_LastUser1.Text = userWindows[userWin].userStreamToClose.lastName;

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
                    try
                    {
                        string webresponse;
                        if (userWindows.Count > 0)
                        {
                            textBoxTotalTime.Text = userWindows[0].textBoxTotalTime.Text;
                            textBoxSegmentTime.Text = userWindows[0].textBoxSegmentTime.Text;

                            webresponse = userWindows[0].textBoxTotalTime.Text;
                            webresponse += "<br>" + userWindows[0].textBoxSegmentTime.Text;
                        }
                        else
                        {
                            textBoxTotalTime.Text = "??";
                            textBoxSegmentTime.Text = "??";

                            webresponse = "No Users Loaded<br>No Workout?<br>";

                        }

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

        private void buttonDeleteUsers_Click(object sender, RoutedEventArgs e)
        {
            if (dataGridUsers.SelectedIndex >= 0)
            {
                UserHandler.deleteUser(dataGridUsers.SelectedIndex);
                dataGridUsers.Items.Refresh();
            }

        }

        
        private void bStartWorkout_Click(object sender, RoutedEventArgs e)
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
                    if(b_hasVid)
                        mediaElement1.Pause();
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
                        mediaElement1.Play();
                }
            }

        }


        private void bEndWorkout_Click(object sender, RoutedEventArgs e)
        {
            cbSelectWorkout.IsEnabled = true;
            cbSelectWorkout.SelectedIndex = -1;

            WorkoutHandler.finish();
            WorkoutHandler.resetWorkout();
            
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

}
