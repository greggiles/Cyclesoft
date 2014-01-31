using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
//using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CycleSoft
{
    /// <summary>
    /// Interaction logic for UserWindow.xaml
    /// </summary>
    /// 
    public partial class UserWindow : Window
    {
        public UInt32 uniqueIDSpd { get; protected set; }
        public UInt32 uniqueIDPwr { get; protected set; }
        public UInt32 uniqueIDHr { get; protected set; }

        private double spdInst;
        private int cadInst;
        private int hb;
        private int powerInst;
        private double powerMax;

        private workoutDef activeWorkout;

        public cAntUsers userStreamToClose { get; protected set; }
        private cWorkout workoutStreamToClose;
        public workoutEventArgs workoutStatus {  get; protected set; }
        public userEventArgs userStatus {  get; protected set; }


        private bool bWorkoutRunning;

        // The activeWorkout points ... to quickly draw the target
        // power line, find the 0->1 "x" coordinate before and after normalized
        // time ... average the y from each of these points.
        Point[] currentWorkoutPoints;

        cDrawingEngine dwgEngine;
        
        public void updateWorkoutEvent(object sender, workoutEventArgs e)
        {
            this.Dispatcher.BeginInvoke((Action)(() => {
                try
                {
                    workoutStreamToClose = (cWorkout)sender;
                    workoutStatus = e;

                    if (workoutStatus.running) bWorkoutRunning = true;

                    textBlockWorkout.Text = e.message;

                    if (workoutStatus.starting || activeWorkout != workoutStreamToClose.activeWorkout)
                    {
                        load_Workout(workoutStreamToClose.activeWorkout);
                        userStreamToClose.points = 0;
                    }

                    if (workoutStatus.finished)
                    {
                        bWorkoutRunning = false;
                        textBlockWorkout.Text = "FINISHED";
                    }

                    //TimeSpan duration = new TimeSpan(0, 0, 0, (int)((workoutStatus.workoutTotalMS - workoutStatus.workoutCurrentMS) / 1000));
                    // Show time going up for total Workout Time
                    TimeSpan duration = new TimeSpan(0, 0, 0, (int)((workoutStatus.workoutCurrentMS) / 1000));
                    textBoxTotalTime.Text = duration.ToString(@"h\:mm\:ss");

                    duration = new TimeSpan(0, 0, 0, (int)((999 + workoutStatus.segmentTotalMS - workoutStatus.segmentCurrentMS) / 1000));
                    textBoxSegmentTime.Text = duration.ToString(@"h\:mm\:ss");

                    if (workoutStatus.paused && !workoutStatus.finished)
                    {
                        bWorkoutRunning = false;
                        textBlockWorkout.Text = "PAUSED";
                    }
                }
                catch { };
            }));


        }


        public void updateEvent(object sender, userEventArgs e)
        {
            userStatus = e;
            this.Dispatcher.BeginInvoke((Action)(() => {

                userStreamToClose = (cAntUsers)sender;
             
                textBoxHr.Text = e.hr.ToString();
                hb = e.hr;

                textBoxMPH.Text = e.speed.ToString("F1");
                textBoxCad.Text = e.cad.ToString();
                spdInst = e.speed;
                cadInst = e.cad;

                textBoxPwr.Text = e.instPwr.ToString();
                powerInst = e.instPwr;
                textPwrAvg.Text = e.avgPwr.ToString();
                textPwrAvgLast.Text = e.lastAvgPwr.ToString();

                textBoxPoints.Text = e.points.ToString();

                try
                {
                    labelPwr50.Content = e.ftp.ToString();
                    powerMax = (e.ftp + (dwgEngine.chartZoom * e.ftp / 2));
                    labelPwrMax.Content = ((int)(powerMax)).ToString();
                    labelPwr75.Content = ((int)(3 * powerMax / 4)).ToString();
                    labelPwr25.Content = ((int)(powerMax / 4)).ToString();
                }
                catch { }


                // Draw Instant Power Bar
                var points = new Point[4];
                double x = (double)e.instPwr / powerMax;
                if (x > 1)  x = 1;

                points[0] = new Point(0.0, 0.02);
                points[1] = new Point(0.0, 0.98);
                points[2] = new Point(x, 0.98);
                points[3] = new Point(x, 0.02);

                var figure = new PathFigure
                {
                    StartPoint = points[0],
                    IsClosed = true
                };

                var segment = new PolyLineSegment(points.Skip(1), true);
                figure.Segments.Add(segment);
                polylinePwr.Figures.Clear();
                polylinePwr.Figures.Add(figure);

                //Draw Avg Power Bar
                // Changing this to CAD bar on 1/6/2014
                // x = (double)e.avgPwr / powerMax;
                x = (double)cadInst / 150;
                if (x > 1) x = 1;
                points[0] = new Point(0.0, 0.02);
                points[1] = new Point(0.0, 0.98);
                points[2] = new Point(x, 0.98);
                points[3] = new Point(x, 0.02);

                figure = new PathFigure
                {
                    StartPoint = points[0],
                    IsClosed = true
                };

                segment = new PolyLineSegment(points.Skip(1), true);
                figure.Segments.Add(segment);
                polylineAvgPwr.Figures.Clear();
                polylineAvgPwr.Figures.Add(figure);

                //work on target Power Line

                double y = 0;
                double ymin = 0;
                double ymax = 1;

                double target = 0;

                if (bWorkoutRunning)
                {
                    target = activeWorkout.segments[workoutStatus.currentSegment].effort;
                    if (workoutStatus.alternateTarget > 0) target = workoutStatus.alternateTarget;
                    y = target / 2;
                    ymin = y - activeWorkout.segments[workoutStatus.currentSegment].ptsMinus / 2;
                    if (ymin < 0) ymin = 0;
                    ymax = y + activeWorkout.segments[workoutStatus.currentSegment].ptsPlus / 2;
                    if (ymax > 1) ymax = 1;
                }
                else
                {
                    y = 1 - (double)e.ftp / powerMax;
                }

                points = new Point[10];
                points[0] = new Point(y, 0.05);
                points[1] = new Point(y, 0.4);
                points[2] = new Point(ymin, 0.5);
                points[3] = new Point(y, 0.6);
                points[4] = new Point(y, .95);
                points[5] = new Point(y+.01, .95);
                points[6] = new Point(y+.01, 0.6);
                points[7] = new Point(ymax, 0.5);
                points[8] = new Point(y + .01, 0.4);
                points[9] = new Point(y + .01, .05);


                figure = new PathFigure
                {
                    StartPoint = points[0],
                    IsClosed = true
                };

                segment = new PolyLineSegment(points.Skip(1), true);
                figure.Segments.Add(segment);
                polylinePwrTarget.Figures.Clear();
                polylinePwrTarget.Figures.Add(figure);

                //work on target Cad Line

                y = 0;
                if (bWorkoutRunning)
                {
                    y = (double)activeWorkout.segments[workoutStatus.currentSegment].cadTarget / 150;
                    ymin = y - (double)activeWorkout.segments[workoutStatus.currentSegment].ptsCadMinus / 150;
                    if (ymin < 0) ymin = 0;
                    ymax = y + (double)activeWorkout.segments[workoutStatus.currentSegment].ptsCadPlus / 150;
                    if (ymax > 1) ymax = 1;
                }
                else
                {
                    y = .5;
                    ymin = 0;
                    ymax = 1;
                }

                points[0] = new Point(y, 0.05);
                points[1] = new Point(y, 0.4);
                points[2] = new Point(ymin, 0.5);
                points[3] = new Point(y, 0.6);
                points[4] = new Point(y, .95);
                points[5] = new Point(y + .01, .95);
                points[6] = new Point(y + .01, 0.6);
                points[7] = new Point(ymax, 0.5);
                points[8] = new Point(y + .01, 0.4);
                points[9] = new Point(y + .01, .05);


                figure = new PathFigure
                {
                    StartPoint = points[0],
                    IsClosed = true
                };

                segment = new PolyLineSegment(points.Skip(1), true);
                figure.Segments.Add(segment);
                polylineCadTarget.Figures.Clear();
                polylineCadTarget.Figures.Add(figure);
                
                // 
                //ReDraw HR, Cadence, Speed and Power Lines                
                //

                if (bWorkoutRunning && !workoutStatus.paused)
                {

                    x = (double)workoutStatus.workoutCurrentMS / workoutStatus.workoutTotalMS;
                    y = spdInst / 40;
                    if (y > 1) y = 1;
                    Point spd = new Point(x, 1 - y);
                    spdData.Add(spd);
                    extend_line(spdline, (spdData));

                    y = ((double)hb / 200);
                    if (y > 1) y = 1;
                    Point hr = new Point(x, 1 - y);
                    hrData.Add(hr);
                    extend_line(hrline, hrData);

                    y = ((double)cadInst / 200);
                    if (y > 1) y = 1;
                    Point cad = new Point(x, 1 - y);
                    cadData.Add(cad);
                    extend_line(cadline, cadData);
                    
                    y = (double)powerInst / (powerMax);
                    if (y > 1) y = 1;
                    Point pwr = new Point(x, 1 - y);
                    pwrData.Add(pwr);
                    extend_line(pwrline, dwgEngine.scaleLine(pwrData));

                    // send more Points to User. Note THIS REALLY DOESN"T BELONG HERE? :(
                    double powerdiff = 2 * (y - target / 2);
                    if (powerdiff < 0 && -powerdiff <= activeWorkout.segments[workoutStatus.currentSegment].ptsMinus)
                    {
                        userStreamToClose.points += .5;
                        powerMeterCanvas.Background = new SolidColorBrush(Colors.Yellow);
                    }
                    else if (powerdiff > 0 && powerdiff <= activeWorkout.segments[workoutStatus.currentSegment].ptsPlus)
                    {
                        userStreamToClose.points += 1;
                        powerMeterCanvas.Background = new SolidColorBrush(Colors.LightGreen);
                    }
                    else
                        powerMeterCanvas.Background = new SolidColorBrush(Colors.Black);

                    if (cadInst >= activeWorkout.segments[workoutStatus.currentSegment].cadTarget)
                    {
                        if (cadInst - activeWorkout.segments[workoutStatus.currentSegment].cadTarget <= 
                            activeWorkout.segments[workoutStatus.currentSegment].ptsCadPlus)
                        {
                            userStreamToClose.points += 1;
                            cadMeterCanvas.Background = new SolidColorBrush(Colors.LightGreen);
                        }
                    }
                    else if (activeWorkout.segments[workoutStatus.currentSegment].cadTarget - cadInst <= 
                        activeWorkout.segments[workoutStatus.currentSegment].ptsCadMinus)
                    {
                        userStreamToClose.points += .5;
                        cadMeterCanvas.Background = new SolidColorBrush(Colors.Yellow);
                    }
                    else
                        cadMeterCanvas.Background = new SolidColorBrush(Colors.Black);


                }

          }));
                    
        }

        List<Point> spdData;
        List<Point> hrData;
        List<Point> cadData;
        List<Point> pwrData;
        List<Point> avgpwrData;

        public UserWindow()
        {
            InitializeComponent();
            uniqueIDSpd = 0;
            uniqueIDPwr = 0;
            uniqueIDHr = 0;

            spdData = new List<Point>();
            cadData = new List<Point>();
            hrData = new List<Point>();
            pwrData = new List<Point>();
            avgpwrData = new List<Point>();

            workoutStatus = new workoutEventArgs();

            powerMax = 400;
            labelPwrMax.Content = "400";

            dwgEngine = new cDrawingEngine();

        }

        public void setTitle(string name, int userMaxPower)
        {
            this.Title = name;            
        }

        public void changeZoom(double zoomValue)
        {
            dwgEngine.chartZoom = zoomValue;

            if (activeWorkout != null)
            {
                draw_workout();
            }
            if ((pwrData!= null) && pwrData.Count > 0)
                redraw_line(pwrline, dwgEngine.scaleLine(pwrData));
            if ((spdData != null) && spdData.Count > 0)
                redraw_line(spdline, dwgEngine.scaleLine(spdData));
        }

        private void draw_workout()
        {

            currentWorkoutPoints = dwgEngine.getWorkoutPoints(activeWorkout);
            var figure = new PathFigure
            {
                StartPoint = currentWorkoutPoints[0],
                IsClosed = true
            };

            var segment = new PolyLineSegment(currentWorkoutPoints.Skip(1), true);
            figure.Segments.Add(segment);
            polyline.Figures.Clear();
            polyline.Figures.Add(figure);

            List<Point> midline = new List<Point>();
            Point pt = new Point();
            pt.X = 0;
            pt.Y = .5;
            midline.Add(pt);
            pt = new Point();
            pt.X = 1;
            pt.Y = .5;
            midline.Add(pt);

            redraw_line(cadmidline, midline);
            redraw_line(pwrmidline, midline);

            midline.Clear();

            pt = new Point();
            pt.X = 0;
            pt.Y = .75;
            midline.Add(pt);

            pt = new Point();
            pt.X = 1;
            pt.Y = .75;
            midline.Add(pt);

            redraw_line(pwr75line, midline);

            midline.Clear();

            pt = new Point();
            pt.X = 0;
            pt.Y = .25;
            midline.Add(pt);

            pt = new Point();
            pt.X = 1;
            pt.Y = .25;
            midline.Add(pt);

            redraw_line(pwr25line, midline);

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

        private void extend_line(PathGeometry line, List<Point> points)
        {
            if (points.Count < 2) return;
            var figure = new PathFigure
            {
                StartPoint = points[points.Count-2],
                IsClosed = false
            };

            var segment = new LineSegment();
            segment.Point = points[points.Count - 1];
            figure.Segments.Add(segment);
            //line.Clear();
            line.Figures.Add(figure);

        }


        public void load_Workout(workoutDef workout)
        {
            activeWorkout = workout;
            draw_workout();

            spdline.Clear();
            cadline.Clear();
            hrline.Clear();
            pwrline.Clear();

            spdData.Clear();
            cadData.Clear();
            hrData.Clear();
            pwrData.Clear();



        }
        
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (null != userStreamToClose) userStreamToClose.userUpdateHandler -= this.updateEvent;
            if (null != workoutStreamToClose) workoutStreamToClose.workoutEventHandler -= this.updateWorkoutEvent;
        }

        private void butonConfigUser_Click(object sender, RoutedEventArgs e)
        {
            UserConfig childWin = new UserConfig(userStreamToClose);
            childWin.Activate();
            childWin.Show();

        }




    }
}
