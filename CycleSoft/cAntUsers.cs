using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Documents;
using System.Xml;
using System.Xml.XPath;


namespace CycleSoft
{
    public delegate void userUpdateHandler(object sender, userEventArgs e);

    public class userEventArgs : EventArgs
    {
        public string name { get; set; }
        public int instPwr { get; set; }
        public int avgPwr { get; set; }
        public int hr { get; set; }
        public int cad { get; set; }
        public double speed { get; set; }
        public int lastAvgPwr { get; set; }
        public double points { get; set; }
        public int ftp { get; set; }

    }

    public class powerPoints
    {
        public int instPwr;
        public DateTime time;
    }


    public class cAntUsers
    {
        public string firstName { get; set; }
        public string lastName { get; set; }
        public int ftp { get; set; }

        public int instPower { get; protected set; }
        public int avgPower { get; protected set; }
        public int avgPowerTime { get; set; }
        private Queue<powerPoints> qPWRQue;

        private Queue<powerPoints> qPWRSmoothQue;

        private Queue<double> qSPDQue;

        public double speed { get; protected set; }
        public int wheelSize {get; set;}
        public int cad { get; protected set; }
        public int hr { get; protected set; }

        public bool hasPwr { get; private set; }
        public bool hasSpdCad { get; private set; }
        public bool hasHr { get; private set; }

        public int ptrSPwr { get; set; }
        public cSpowerCalcs speedPowerCalcs { get; set; }

        public spdStream activeSpeedStream { get; set; }
        public hbStream activeHrStream { get; set; }
        public powerStream activePwrStream { get; set; }

        public double points {get; set; }

        public event EventHandler<userEventArgs> userUpdateHandler;

        //private Queue<TCXdata> tcxData;

        private cTCXFileGenerator TCXFileHandler;
        private Timer _TCXDataTimer;

        public cAntUsers()
        {
            hasPwr = false;
            hasSpdCad = false;
            hasHr = false;

            speed = -1;
            wheelSize = 2070;
            cad = -1;
            hr = -1;
            instPower = -1;
            avgPower = -1;
            avgPowerTime = 3;

            //
            speedPowerCalcs = new cSpowerCalcs();
            ptrSPwr = -1;

            Timer _timer = new Timer(500); //250
            _timer.Elapsed += new ElapsedEventHandler(_timerElapsed);
            _timer.Enabled = true;

            _TCXDataTimer = new Timer(1000);
            _TCXDataTimer.Elapsed += new ElapsedEventHandler(_addTCXTimer);


            qPWRQue = new Queue<powerPoints>();
            qPWRSmoothQue = new Queue<powerPoints>();
            qSPDQue = new Queue<double>();

            //tcxData = new Queue<TCXdata>();


        }

        private int lastSegment;
        private int[] segAvgPower;  //This should be a queue, probably.

        public void updateWorkoutEvent(object sender, workoutEventArgs e)
        {
            
            if (e.running && !e.paused && _TCXDataTimer.Enabled == false)
            {
                segAvgPower = new int[200];
                string title = "none";
                try
                {
                    cWorkout workout = (cWorkout)sender;
                    title = workout.activeWorkout.title;
                }
                catch
                { title = "idk"; }
                if (TCXFileHandler == null)
                {
                    TCXFileHandler = new cTCXFileGenerator(firstName, lastName, title);
                    TCXDistanceCount = 0;
                }
                _TCXDataTimer.Enabled = true;
            }
            else if (e.paused && _TCXDataTimer.Enabled)
                _TCXDataTimer.Enabled = false;
            else if (e.finished && (_TCXDataTimer.Enabled || e.paused))
            {
                _TCXDataTimer.Enabled = false;
                //writeTCXData(e.workoutCurrentMS/1000);
                TCXFileHandler.closeTCXData();
                TCXFileHandler = null;
            }

            if (e.running && !e.paused)
            {
                if (lastSegment != e.currentSegment)
                {
                    if (lastSegment >= 0 && qPWRQue.Count > 0)
                    {
                        segAvgPower[lastSegment] = (int)qPWRQue.Average(s => s.instPwr);
                    }
                    qPWRQue.Clear();

                }
                lastSegment= e.currentSegment;

            }
            
        }

        private long TCXDistanceCount;
        public void _addTCXTimer(object sender, ElapsedEventArgs e)
        {
            //addTCXData();
            TCXdata TrackPoint = new TCXdata();
            TrackPoint.timeStamp = DateTime.Now;
            TrackPoint.distanceMeters = (double)wheelSize * (double)TCXDistanceCount / 1000;
            TrackPoint.heartRate = hr;
            TrackPoint.cadence = cad;
            TrackPoint.power = instPower;
            TrackPoint.speed = speed * 0.44704; //convert to meters/sec.
            TCXFileHandler.addTCXData(TrackPoint);
        }

        // This data is to update user Window ... should probably get WorkOut Messages in this class. 
        // Should really be doing work here. :(
        void _timerElapsed(object sender, ElapsedEventArgs e)
        {
            if (null != userUpdateHandler)
            {
                userEventArgs uEA = new userEventArgs();
                uEA.avgPwr = avgPower;
                uEA.cad = cad;
                uEA.hr = hr;
                uEA.instPwr = instPower;
                uEA.speed = speed;
                uEA.lastAvgPwr = 0;
                if (lastSegment > 0)
                    uEA.lastAvgPwr = segAvgPower[lastSegment - 1];
                uEA.points = points;

                uEA.ftp = ftp;
                uEA.name = firstName + " " + lastName;

                userUpdateHandler(this, uEA);
            }
        }

        public void updateHrEvent(object sender, EventArgs e)
        {
            hbStream evtFrom = (hbStream)sender;           
            hr = evtFrom.hb;
            hasHr = true;
        }
    
        public void updatePwrEvent(object sender, EventArgs e)
        {
            powerStream evtFrom = (powerStream)sender;
            if (ptrSPwr >= 0)
            {
                hasPwr = false;
                return;
            }

            powerPoints pp = new powerPoints();
            pp.instPwr = evtFrom.powerInst;
            pp.time = DateTime.Now;

            if (avgPowerTime > 0)
            {
                qPWRSmoothQue.Enqueue(pp);
                while (qPWRSmoothQue.Peek().time < DateTime.Now.Subtract(TimeSpan.FromSeconds((double)avgPowerTime)))
                    qPWRSmoothQue.Dequeue();
            }

            if (qPWRQue.Count > 0 && avgPowerTime > 0)
                instPower = (int)qPWRSmoothQue.Average(s => s.instPwr);
            else
                instPower = evtFrom.powerInst;

            if (_TCXDataTimer.Enabled)
            {

                qPWRQue.Enqueue(pp);
                avgPower = 0;
                if (qPWRQue.Count > 0)
                    avgPower = (int)qPWRQue.Average(s => s.instPwr);
            }
            hasPwr = true;
        }



        public void updateSpdCadEvent(object sender, EventArgs e)
        {
            spdStream evtFrom = (spdStream)sender;
            // set the wheelSize from this sender ... 
            evtFrom.wheelSize = wheelSize;
            speed = evtFrom.spdInst;
            cad = evtFrom.cadInst;
            hasSpdCad = true;

            TCXDistanceCount += evtFrom.publicSpdCnt;
            
            if (ptrSPwr >= 0)
            {
                if(speed >=0)
                    qSPDQue.Enqueue(speed);
                if (qSPDQue.Count > 4) qSPDQue.Dequeue();

                hasPwr = false;
                instPower = speedPowerCalcs.getSpower(ptrSPwr, qSPDQue.Average());


                if (_TCXDataTimer.Enabled)
                {

                    powerPoints pp = new powerPoints();
                    pp.instPwr = instPower;
                    pp.time = DateTime.Now;
                    qPWRQue.Enqueue(pp);
                    /*
                     * This was using time filtering, going to try doing this with segment averages
                     * 

                                   while (qPWRQue.Peek().time < DateTime.Now.Subtract(TimeSpan.FromSeconds((double)avgPowerTime)))
                                       qPWRQue.Dequeue();
                   */
                    avgPower = 0;
                    if (qPWRQue.Count > 0)
                        avgPower = (int)qPWRQue.Average(s => s.instPwr);
                }
                return;
            }

        }


    }
}
