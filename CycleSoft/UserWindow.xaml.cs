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
        private double ftpx2;

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

        List<Point> spdData;
        List<Point> hrData;
        List<Point> cadData;
        List<Point> pwrData;
        List<Point> avgpwrData;

        public PointCollection pTarget;


        cDrawingEngine dwgEngine;
        
        public void updateWorkoutEvent(object sender, workoutEventArgs e)
        {
            workoutStreamToClose = (cWorkout)sender;
            workoutStatus = e;

            if (workoutStatus.running) bWorkoutRunning = true;
        }


        public void updateEvent(object sender, userEventArgs e)
        {
            userStatus = e;

            hb = e.hr;
            spdInst = e.speed;
            cadInst = e.cad;
            ftpx2 = e.ftp * 2;
            powerMax = (e.ftp + (dwgEngine.chartZoom * e.ftp / 2));

            userStreamToClose = (cAntUsers)sender;

            this.Dispatcher.BeginInvoke((Action)(() =>
            {             
                textBoxHr.Text = e.hr.ToString();

                textBoxMPH.Text = e.speed.ToString("F1");
                textBoxCad.Text = e.cad.ToString();

                textBoxPwr.Text = e.instPwr.ToString();
                powerInst = e.instPwr;
                textPwrAvg.Text = e.avgPwr.ToString();
                textPwrAvgLast.Text = e.lastAvgPwr.ToString();

                textBoxPoints.Text = e.points.ToString();
                try
                {
                    labelPwr50.Content = e.ftp.ToString();
                    labelPwrMax.Content = ((int)(powerMax)).ToString();
                    labelPwr75.Content = ((int)(3 * powerMax / 4)).ToString();
                    labelPwr25.Content = ((int)(powerMax / 4)).ToString();
                }
                catch { }
            }));


            // Draw Instant Power Bar
            var xpoints = new Point[4];
            double xx = (double)e.instPwr / ftpx2;
            if (xx > 1)  xx = 1;

            xpoints[0] = new Point(0.0, 0.02);
            xpoints[1] = new Point(0.0, 0.98);
            xpoints[2] = new Point(xx, 0.98);
            xpoints[3] = new Point(xx, 0.02);


            //Draw Avg Power Bar
            // Changing this to CAD bar on 1/6/2014
            // x = (double)e.avgPwr / powerMax;
            double x = (double)cadInst / 150;
            if (x > 1) x = 1;
            var pointsCad = new Point[4];
            pointsCad[0] = new Point(0.0, 0.02);
            pointsCad[1] = new Point(0.0, 0.98);
            pointsCad[2] = new Point(x, 0.98);
            pointsCad[3] = new Point(x, 0.02);


            //work on target Power Line

            double y = 0;
            double ymin = 0;
            double ymax = 1;

            double target = 0;

            int cadColor = 0;
            int pwrColor = 0;


            if (bWorkoutRunning)
            {
                target = activeWorkout.segments[workoutStatus.currentSegment].effort;
                if (workoutStatus.alternateTarget > 0) target = workoutStatus.alternateTarget;
                y = target / 2;
                ymin = y - activeWorkout.segments[workoutStatus.currentSegment].ptsMinus / 2;
                if (ymin < 0) ymin = 0;
                ymax = y + activeWorkout.segments[workoutStatus.currentSegment].ptsPlus / 2;
                if (ymax > 1) ymax = 1;

                // send more Points to User. Note THIS REALLY DOESN"T BELONG HERE? :(
                if (e.instPwr >= target * e.ftp && e.instPwr <= (target+activeWorkout.segments[workoutStatus.currentSegment].ptsPlus)*e.ftp )
                {
                    if (!workoutStatus.paused)
                        userStreamToClose.points += 1;
                    pwrColor = 1;
                }
                else if (e.instPwr < target * e.ftp && e.instPwr >= (target-activeWorkout.segments[workoutStatus.currentSegment].ptsMinus)*e.ftp )
                {
                    if (!workoutStatus.paused)                       
                        userStreamToClose.points += .5;
                    pwrColor = 2;
                }
            }
            else
            {
                y = 1 - (double)e.ftp / ftpx2;
            }

            var pointsPT = new Point[10];
            pointsPT[0] = new Point(y, 0.05);
            pointsPT[1] = new Point(y, 0.4);
            pointsPT[2] = new Point(ymin, 0.5);
            pointsPT[3] = new Point(y, 0.6);
            pointsPT[4] = new Point(y, .95);
            pointsPT[5] = new Point(y + .01, .95);
            pointsPT[6] = new Point(y + .01, 0.6);
            pointsPT[7] = new Point(ymax, 0.5);
            pointsPT[8] = new Point(y + .01, 0.4);
            pointsPT[9] = new Point(y + .01, .05);


            //work on target Cad Line

            y = 0;
            if (bWorkoutRunning)
            {
                y = (double)activeWorkout.segments[workoutStatus.currentSegment].cadTarget / 150;
                ymin = y - (double)activeWorkout.segments[workoutStatus.currentSegment].ptsCadMinus / 150;
                if (ymin < 0) ymin = 0;
                ymax = y + (double)activeWorkout.segments[workoutStatus.currentSegment].ptsCadPlus / 150;
                if (ymax > 1) ymax = 1;

                if (cadInst >= activeWorkout.segments[workoutStatus.currentSegment].cadTarget)
                {
                    if (cadInst - activeWorkout.segments[workoutStatus.currentSegment].cadTarget <=
                        activeWorkout.segments[workoutStatus.currentSegment].ptsCadPlus)
                    {
                        if (!workoutStatus.paused)
                            userStreamToClose.points += 1;
                        cadColor = 1;
                    }
                }
                else if (activeWorkout.segments[workoutStatus.currentSegment].cadTarget - cadInst <=
                    activeWorkout.segments[workoutStatus.currentSegment].ptsCadMinus)
                {
                    if (!workoutStatus.paused)
                        userStreamToClose.points += .5;
                    cadColor = 2;
                }
            }
            else
            {
                y = .5;
                ymin = 0;
                ymax = 1;
            }


            var pointsCT = new Point[10];
            pointsCT[0] = new Point(y, 0.05);
            pointsCT[1] = new Point(y, 0.4);
            pointsCT[2] = new Point(ymin, 0.5);
            pointsCT[3] = new Point(y, 0.6);
            pointsCT[4] = new Point(y, .95);
            pointsCT[5] = new Point(y + .01, .95);
            pointsCT[6] = new Point(y + .01, 0.6);
            pointsCT[7] = new Point(ymax, 0.5);
            pointsCT[8] = new Point(y + .01, 0.4);
            pointsCT[9] = new Point(y + .01, .05);


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

                y = ((double)hb / 200);
                if (y > 1) y = 1;
                Point hr = new Point(x, 1 - y);
                hrData.Add(hr);

                y = ((double)cadInst / 200);
                if (y > 1) y = 1;
                Point cad = new Point(x, 1 - y);
                cadData.Add(cad);

                y = (double)powerInst / (powerMax);
                if (y > 1) y = 1;
                Point pwr = new Point(x, 1 - y);
                pwrData.Add(pwr);
            }

            //TimeSpan duration = new TimeSpan(0, 0, 0, (int)((workoutStatus.workoutTotalMS - workoutStatus.workoutCurrentMS) / 1000));
            // Show time going up for total Workout Time
            TimeSpan duration = new TimeSpan(0, 0, 0, (int)((workoutStatus.workoutCurrentMS) / 1000));
            TimeSpan durationSeg = new TimeSpan(0, 0, 0, (int)((999 + workoutStatus.segmentTotalMS - workoutStatus.segmentCurrentMS) / 1000));

            this.Dispatcher.BeginInvoke((Action)(() =>
            {
                textBlockWorkout.Text = workoutStatus.message;
                if (workoutStreamToClose != null)
                {
                    if (workoutStatus.starting || activeWorkout != workoutStreamToClose.activeWorkout)
                    {
                        load_Workout(workoutStreamToClose.activeWorkout);
                        userStreamToClose.points = 0;
                    }
                }

                if (workoutStatus.finished)
                {
                    bWorkoutRunning = false;
                    textBlockWorkout.Text = "FINISHED";
                }

                textBoxTotalTime.Text = duration.ToString(@"h\:mm\:ss");
                textBoxSegmentTime.Text = durationSeg.ToString(@"h\:mm\:ss");

                if (workoutStatus.paused && !workoutStatus.finished)
                {
                    bWorkoutRunning = false;
                    textBlockWorkout.Text = "PAUSED";
                }

                if (textBlockWorkout.Text == "" && activeWorkout != null)
                    textBlockWorkout.Text = activeWorkout.segments[workoutStatus.currentSegment].segmentName;


                if (roundRobin == 0) extend_line(spdline, (spdData));
                if (roundRobin == 1) extend_line(hrline, hrData);
                if (roundRobin == 2) extend_line(cadline, cadData);
                if (roundRobin == 3) extend_line(pwrline, dwgEngine.scaleLine(pwrData));
                
                if(++roundRobin > 3) roundRobin = 0;
                

                if (pwrColor == 1) powerMeterCanvas.Background = new SolidColorBrush(Colors.LightGreen);
                else if (pwrColor == 2) powerMeterCanvas.Background = new SolidColorBrush(Colors.Yellow);
                else powerMeterCanvas.Background = new SolidColorBrush(Colors.Black);

                if (cadColor == 1) cadMeterCanvas.Background = new SolidColorBrush(Colors.LightGreen);
                else if (cadColor == 2) cadMeterCanvas.Background = new SolidColorBrush(Colors.Yellow);
                else cadMeterCanvas.Background = new SolidColorBrush(Colors.Black);

                PathFigure figureInstPower = new PathFigure
                {
                    StartPoint = xpoints[0],
                    IsClosed = true
                };

                var segment = new PolyLineSegment(xpoints.Skip(1), true);
                figureInstPower.Segments.Add(segment);
                polylinePwr.Figures.Clear();
                polylinePwr.Figures.Add(figureInstPower);

                PathFigure figureAvgPower = new PathFigure
                {
                    StartPoint = pointsCad[0],
                    IsClosed = true
                };


                segment = new PolyLineSegment(pointsCad.Skip(1), true);
                figureAvgPower.Segments.Add(segment);
                polylineAvgPwr.Figures.Clear();
                polylineAvgPwr.Figures.Add(figureAvgPower);

                PathFigure figurePowerMeter = new PathFigure
                {
                    StartPoint = pointsPT[0],
                    IsClosed = true
                };

                segment = new PolyLineSegment(pointsPT.Skip(1), true);
                figurePowerMeter.Segments.Add(segment);
                polylinePwrTarget.Figures.Clear();
                polylinePwrTarget.Figures.Add(figurePowerMeter);

                PathFigure figureCadMeter = new PathFigure
                {
                    StartPoint = pointsCT[0],
                    IsClosed = true
                };

                segment = new PolyLineSegment(pointsCT.Skip(1), true);
                figureCadMeter.Segments.Add(segment);
                polylineCadTarget.Figures.Clear();
                polylineCadTarget.Figures.Add(figureCadMeter);
            }));


            

                    
        }


        private int roundRobin;
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

            pTarget = new PointCollection(10);
            Point pt01 = new Point(.01, .01);
            Point pt02 = new Point(.06, .01);
            Point pt03 = new Point(.07, .06);
            Point pt04 = new Point(.04, .06);
            pTarget.Add(pt01);
            pTarget.Add(pt02);
            pTarget.Add(pt03);
            pTarget.Add(pt04);


              workoutStatus = new workoutEventArgs();

            roundRobin = 0;

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
            if (points.Count < 5) return;
            var figure = new PathFigure
            {
                StartPoint = points[points.Count-5],
                IsClosed = false
            };

            var segment = new PolyLineSegment(points.Skip(points.Count-5), true);

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
