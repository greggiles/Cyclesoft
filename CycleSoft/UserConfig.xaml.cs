using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace ANTSniffer
{
    /// <summary>
    /// Interaction logic for Window2.xaml
    /// </summary>
    public partial class UserConfig : Window
    {

        private cAntUsers targetUser;
        public UserConfig(cAntUsers user)
        {
            InitializeComponent();
            targetUser = user;
            this.Title = targetUser.firstName + " " + targetUser.lastName + " Config";
            textBoxWheelSize.Text = user.wheelSize.ToString();
            textBoxFTP.Text = user.ftp.ToString();
            for (int i = 0; i < targetUser.speedPowerCalcs.l_Trainers.Count; i++)
                comboBox1.Items.Add(targetUser.speedPowerCalcs.l_Trainers[i].name);
            comboBox1.SelectedIndex = targetUser.ptrSPwr;
            textBoxPowerAvgTime.Text = targetUser.avgPowerTime.ToString();

        }

        private void buttonSaveWheelSize_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                targetUser.wheelSize = Convert.ToInt32(textBoxWheelSize.Text);
            }
            catch
            {
                textBoxWheelSize.Text = targetUser.wheelSize.ToString();
            }

        }

        private void comboBox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            targetUser.ptrSPwr = comboBox1.SelectedIndex;
        }

        private void buttonSaveFTP_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                targetUser.ftp = Convert.ToInt32(textBoxFTP.Text);
            }
            catch
            {
                textBoxFTP.Text = targetUser.ftp.ToString();
            } 


        }

        private void buttonSaveAvgFilter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                targetUser.avgPowerTime = Convert.ToInt32(textBoxPowerAvgTime.Text);
            }
            catch
            {
                textBoxPowerAvgTime.Text = targetUser.avgPowerTime.ToString();
            } 

        }

        private Timer _countDownTimer;
        private Timer _calTimer;
        private int CalibrationPoint;

        private double[] speedData;
        private int spdPtr;

        private void btnDoCalibration_Click(object sender, RoutedEventArgs e)
        {
            CalibrationPoint = 0;
            spdPtr = 0;
            _countDownTimer = new Timer(1000);
            _countDownTimer.Elapsed += new ElapsedEventHandler(_countDownTimerElapsed);
            _countDownTimer.Enabled = true;
            _calTimer = new Timer(200);
            _calTimer.Elapsed += new ElapsedEventHandler(_calTimerElapsed);
            _calTimer.Enabled = true;
            speedData = new double[30];

            calInstructions.Content = "Go Fast. Stop Pedaling in "+(25-CalibrationPoint).ToString()+ "sec";
            btnDoCalibration.IsEnabled = false;

        }

        void _calTimerElapsed(object sender, ElapsedEventArgs e)
        {

            this.Dispatcher.Invoke((Action)(() =>
            {
                if (spdPtr < 30 && CalibrationPoint >= 25)
                {
                    speedData[spdPtr] = targetUser.speed;
                    spdPtr++;
                }

                if (CalibrationPoint >= 30)
                    _calTimer.Enabled = false;
                
                else
                    // do something to draw current speed?
                    calTitleBox.Text = "Calibration: (curr speed = " + targetUser.speed + ")";
            }));
        }

        void _countDownTimerElapsed(object sender, ElapsedEventArgs e)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                CalibrationPoint++;
                if (CalibrationPoint < 25)
                {
                    calInstructions.Content = "Go Fast. Stop Pedaling in " + (25 - CalibrationPoint).ToString() + "sec";
                }
                if (CalibrationPoint == 25)
                {
                    calInstructions.Content = "No Pedal " + (30 - CalibrationPoint).ToString() + "sec";
                    speedData[0] = targetUser.speed;
                    spdPtr++;
                }

                if (CalibrationPoint > 25)
                {
                    calInstructions.Content = "No Pedal " + (30 - CalibrationPoint).ToString() + "sec";
                }

                if (CalibrationPoint >= 30)
                {
                    calInstructions.Content = "Done";
                    _countDownTimer.Enabled = false;
                    calTitleBox.Text = "Calibration:";
                    btnDoCalibration.IsEnabled = true;
                    draw_calibration();
                    //graph data
                }
            }));

        }

        private void draw_calibration()
        {

            Point[] currentWorkoutPoints = new Point[62];
            
            currentWorkoutPoints[0] = new Point(0,1);
            for (int i = 0 ; i < 30 ; i++)
            {
                currentWorkoutPoints[i*2+1] = new Point((double)i/30, 1-speedData[i]/30);
                currentWorkoutPoints[i*2+2] = new Point((double)(i+1)/30, 1-speedData[i]/30);
            }
            currentWorkoutPoints[61] = new Point(1,1);


            var figure = new PathFigure
            {
                StartPoint = currentWorkoutPoints[0],
                IsClosed = true
            };

            var segment = new PolyLineSegment(currentWorkoutPoints.Skip(1), true);
            figure.Segments.Add(segment);
            polyline.Figures.Clear();
            polyline.Figures.Add(figure);
        }

        private void btnSaveCalibration_Click(object sender, RoutedEventArgs e)
        {

        }
        

    }
}
