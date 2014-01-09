using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Timers;
using System.Windows.Documents;
using System.Xml;
using System.Xml.XPath;


namespace ANTSniffer
{
    public delegate void userUpdateHandler(object sender, userEventArgs e);

    public class userEventArgs : EventArgs
    {
        public int instPwr { get; set; }
        public int avgPwr { get; set; }
        public int hr { get; set; }
        public int cad { get; set; }
        public double speed { get; set; }
        public int lastAvgPwr { get; set; }
    }

    public class powerPoints
    {
        public int instPwr;
        public DateTime time;
    }

    public class TCXdata
    {
        // Stores Trackpoint Data, like:
        /*  
          <Trackpoint>
            <Time>2013-11-20T02:32:47Z</Time>
            <DistanceMeters>9859.3</DistanceMeters>
            <HeartRateBpm>
              <Value>140</Value>
            </HeartRateBpm>
            <Cadence>87</Cadence>
            <Extensions>
              <ns3:TPX>
                <ns3:Watts>175</ns3:Watts>
                <ns3:Speed>6.88138888888889</ns3:Speed>
              </ns3:TPX>
            </Extensions>
          </Trackpoint>
        */
        public DateTime timeStamp;
        public double distanceMeters;
        public int heartRate;
        public int cadence;
        public int power;
        public double speed;

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

        private Queue<TCXdata> tcxData;

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

            Timer _timer = new Timer(1000); //250
            _timer.Elapsed += new ElapsedEventHandler(_timerElapsed);
            _timer.Enabled = true;

            _TCXDataTimer = new Timer(1000);
            _TCXDataTimer.Elapsed += new ElapsedEventHandler(_addTCXTimer);

            
            qPWRQue = new Queue<powerPoints>();
            qSPDQue = new Queue<double>();

            tcxData = new Queue<TCXdata>();


        }

        private int lastSegment;
        private int[] segAvgPower;  //This should be a queue, probably.

        public void updateWorkoutEvent(object sender, workoutEventArgs e)
        {
            if (e.running && !e.paused && _TCXDataTimer.Enabled == false)
            {
                segAvgPower = new int[200];
                clrTCXData();
                _TCXDataTimer.Enabled = true;
            }
            else if (e.paused && _TCXDataTimer.Enabled)
                _TCXDataTimer.Enabled = false;
            else if (e.finished && (_TCXDataTimer.Enabled || e.paused))
            {
                _TCXDataTimer.Enabled = false;
                writeTCXData(e.workoutCurrentMS/1000);
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
        public void clrTCXData()
        {
            tcxData.Clear();
            TCXDistanceCount = 0;
        }
        public void _addTCXTimer(object sender, ElapsedEventArgs e)
        {
            addTCXData();
        }
        // Add data to a queue to be written to TCX export File
        // ...
        public void addTCXData()
        {   //        public DateTime timeStamp;
            //        public double distanceMeters;
            //        public int heartRate;
            //        public int cadence;
            //        public int power;
            //        public double speed;
            TCXdata newTrackPoint = new TCXdata();
            newTrackPoint.timeStamp = DateTime.Now;
            newTrackPoint.distanceMeters = (double)wheelSize * (double)TCXDistanceCount / 1000;
            newTrackPoint.heartRate = hr;
            newTrackPoint.cadence = cad;
            newTrackPoint.power = instPower;
            newTrackPoint.speed = speed * 0.44704; //convert to meters/sec.

            tcxData.Enqueue(newTrackPoint);
        }

        public void writeTCXData(long seconds)
        {   //        public DateTime timeStamp;

            TCXdata point;
            try {
                point = tcxData.Peek();
            }
            catch
            {
                return;
            }
            Stream tcxOutContents = File.Open(firstName+"_"+lastName+"_"+point.timeStamp.ToString("yyyyddmm")+".tcx", FileMode.CreateNew);
            StreamWriter tcxStreamWrite = new StreamWriter(tcxOutContents);
            tcxStreamWrite.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            tcxStreamWrite.WriteLine("<TrainingCenterDatabase xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xsi:schemaLocation=\"http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2 http://www.garmin.com/xmlschemas/TrainingCenterDatabasev2.xsd\" xmlns:ns5=\"http://www.garmin.com/xmlschemas/ActivityGoals/v1\" xmlns:ns3=\"http://www.garmin.com/xmlschemas/ActivityExtension/v2\" xmlns:ns2=\"http://www.garmin.com/xmlschemas/UserProfile/v2\" xmlns=\"http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:ns4=\"http://www.garmin.com/xmlschemas/ProfileExtension/v1\">");
            tcxStreamWrite.WriteLine("\t<Activities xmlns=\"http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2\">");
            tcxStreamWrite.WriteLine("\t\t<Activity Sport=\"Biking\">");
            tcxStreamWrite.WriteLine("\t\t\t<Id>" + point.timeStamp.ToUniversalTime().ToString("s") + "Z</Id>");
            tcxStreamWrite.WriteLine("\t\t\t<Lap StartTime=\""+point.timeStamp.ToUniversalTime().ToString("s")+"Z\">");
            tcxStreamWrite.WriteLine("\t\t\t\t<TotalTimeSeconds>"+seconds+"</TotalTimeSeconds>");
            tcxStreamWrite.WriteLine("\t\t\t\t<DistanceMeters>" + wheelSize * TCXDistanceCount / 1000 + "</DistanceMeters>");
            tcxStreamWrite.WriteLine("\t\t\t\t<Calories>10</Calories>");
            tcxStreamWrite.WriteLine("\t\t\t\t<Intensity>Active</Intensity>");
            tcxStreamWrite.WriteLine("\t\t\t\t<TriggerMethod>Time</TriggerMethod>");
            tcxStreamWrite.WriteLine("\t\t\t\t<Track>");

            while (tcxData.Count > 0)
            {
                point = tcxData.Dequeue();
                tcxStreamWrite.WriteLine("\t\t\t\t\t<Trackpoint>");
                tcxStreamWrite.WriteLine("\t\t\t\t\t\t<Time>" + point.timeStamp.ToUniversalTime().ToString("s") + "Z</Time>");
                tcxStreamWrite.WriteLine("\t\t\t\t\t\t<DistanceMeters>"+point.distanceMeters+"</DistanceMeters>");
                tcxStreamWrite.WriteLine("\t\t\t\t\t\t<HeartRateBpm>");
                tcxStreamWrite.WriteLine("\t\t\t\t\t\t\t<Value>"+point.heartRate+"</Value>");
                tcxStreamWrite.WriteLine("\t\t\t\t\t\t</HeartRateBpm>");
                tcxStreamWrite.WriteLine("\t\t\t\t\t\t<Cadence>"+point.cadence+"</Cadence>");
                tcxStreamWrite.WriteLine("\t\t\t\t\t\t<Extensions>");
                tcxStreamWrite.WriteLine("\t\t\t\t\t\t\t<ns3:TPX>");
                tcxStreamWrite.WriteLine("\t\t\t\t\t\t\t\t<ns3:Watts>"+point.power+"</ns3:Watts>");
                tcxStreamWrite.WriteLine("\t\t\t\t\t\t\t\t<ns3:Speed>"+point.speed+"</ns3:Speed>");
                tcxStreamWrite.WriteLine("\t\t\t\t\t\t\t</ns3:TPX>");
                tcxStreamWrite.WriteLine("\t\t\t\t\t\t</Extensions>");
                tcxStreamWrite.WriteLine("\t\t\t\t\t</Trackpoint>");
            }

            tcxStreamWrite.WriteLine("\t\t\t\t</Track>");
            tcxStreamWrite.WriteLine("\t\t\t</Lap>");
            tcxStreamWrite.WriteLine("\t\t</Activity>");
            tcxStreamWrite.WriteLine("\t</Activities>");
            tcxStreamWrite.WriteLine("</TrainingCenterDatabase>");

            tcxStreamWrite.Close();
            tcxOutContents.Close();
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

            instPower = evtFrom.powerInst;

            if (_TCXDataTimer.Enabled)
            {

                powerPoints pp = new powerPoints();
                pp.instPwr = evtFrom.powerInst;
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
