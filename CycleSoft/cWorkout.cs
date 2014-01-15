using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Timers;
using System.Windows.Documents;
using System.Xml;
using System.Xml.XPath;


namespace CycleSoft
{
    public delegate void workoutEventHandler(object sender, EventArgs e);
    public delegate void workoutEventStartStop(object sender, EventArgs e);

    /* Ok ... need to update this
     * need to have handles to:
     * 
     * LOAD
     * START
     * STOP
     * PAUSE
     * END
     * UPDATE
     * 
     * 
     */

    public class workoutEventArgs : EventArgs
    {
        public string message { get; set; } // used only in userWindow
        public bool starting { get; set; }  // used only in userWindow to load Workout and clear Points
        public bool running { get; set; }   // used in User for TCXFile, and Window to set Status
        public bool paused { get; set; }
        public bool finished { get; set; }
        public bool clear { get; set; }                 // not used
        public long workoutTotalMS { get; set; }            // needed
        public long workoutCurrentMS { get; set; }          // needed
        public long segmentTotalMS { get; set; }            // needed
        public long segmentCurrentMS { get; set; }          // needed
        public double segmentCurrentTarget { get; set; }    // needed
        public double pointsPlus { get; set; }
        public double pointsMinus { get; set; }
        public double segmentCadTarget { get; set; }    // needed
        public double pointsCadPlus { get; set; }
        public double pointsCadMinus { get; set; }
        public int currentSegment { get; set; }             // needed? Used in UserWindow for Points
        public segmentDef currentSegmentDef { get; set; }
        /*      public string segmentName;
                public string type;
                public double effort;
                public double effortFinish;
                public double ptsPlus;
                public double ptsMinus;
                public int cadTarget;
                public int ptsCadPlus;
                public int ptsCadMinus;
                public long length;
                public long underTime;
                public long overTime;
         */

    }

    public class workoutStatusArgs : EventArgs
    {
        public bool running { get; set; }
        public bool finished { get; set; }
    }





    public class cWorkout
    {
        XmlReader reader; 
        private string[] workoutFileArray;
        public List<workoutDef> workOutList;
        public bool workoutRunning { get; private set; }

        public event EventHandler<workoutEventArgs> workoutEventHandler;
        public event EventHandler<workoutStatusArgs> workoutEventStartStop;

        Timer _countDownTimer;
        Timer _updateTimer;

        Stopwatch workoutTime;
        long msTimeForNextSegment;

        int workoutCountDown; // used for counting down for ends of segments, etc ...
        public int activeSegment { get; private set; }

        public workoutDef activeWorkout;
        private bool bIsPaused;
        public bool bIsFinished { get; private set; }
        public bool bIsRunning {get; private set;}

        public int workOutSeconds { get; private set; }

        private string workoutPath;

        public cWorkout()
        {
            // Read working Directory for files named "workoutX.xml"
            // Create Workouts from them.

            workOutList = new List<workoutDef>();
            workoutDef newWorkout = new workoutDef();
            newWorkout.segments = new List<segmentDef>();
            segmentDef newSegment = new segmentDef();

            bIsPaused = false;
            bIsFinished = false;

            bIsRunning = false;
            workOutSeconds = 0;

            workoutPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CycleSoft\\Workouts\\");
            Directory.CreateDirectory(workoutPath);
            workoutFileArray = Directory.GetFiles(workoutPath, "*.xml");

            for (int i = 0; i < workoutFileArray.Length; i++)
            {
                Stream workOutContents = File.Open(workoutFileArray[i],FileMode.Open);
                reader = XmlReader.Create(workOutContents);
                long totalLength = 0;
                while (!reader.EOF)
                {
                    /* Content Like:
                     * <Workout Title = "Test Workout 4 Seg">
	                        <segment type = "Ramp" id = "WarmUp" length = "30">
		                        <effort start = ".50" finish = ".7" plus = ".15" minus = ".02"/>
		                        <cad target = "0" plus = "200" minus = "0"/>
	                        </segment>
	                        <segment type = "SteadyState" id = "SteadyState" length = "60">
		                        <effort start = ".90" plus = ".15" minus = ".02"/>
		                        <cad target = "90" plus = "20" minus = "0"/>
	                        </segment>
	                        <segment type = "OverUnder" id = "OverUnder" length = "120">
		                        <effort start = ".95" finish = "1.10" plus = ".05" min = ".01" undertime = "20" overtime = "10" />		
		                        <cad target = "0" plus = "200" minus = "0"/>
	                        </segment>
	                        <segment id = "CoolDown" type = "ramp" length = "30">
		                        <effort start = ".55" plus = ".15" minus = ".02" finish = ".4"/>
		                        <cad target = "0" plus = "200" minus = "0"/>
	                        </segment>
                        </Workout>
                     */
                    reader.Read();
                    switch (reader.Name)
                    {
                        case "Workout":
                            if (reader.HasAttributes)
                            {
                                newWorkout.title = reader.GetAttribute("Title");
                                newWorkout.videopath = reader.GetAttribute("Video");
                            }
                            break;
                        case "segment":
                            newSegment.segmentName = reader.GetAttribute("id");
                            newSegment.type = reader.GetAttribute("type");
                            newSegment.length = Convert.ToInt64(reader.GetAttribute("length"));
                            totalLength += newSegment.length;
                            break;
                        case "effort":
                            newSegment.effort = Convert.ToDouble(reader.GetAttribute("start"));
                            newSegment.effortFinish = Convert.ToDouble(reader.GetAttribute("finish"));
                            newSegment.ptsPlus = Convert.ToDouble(reader.GetAttribute("plus"));
                            newSegment.ptsMinus = Convert.ToDouble(reader.GetAttribute("minus"));
                            newSegment.underTime = Convert.ToInt64(reader.GetAttribute("undertime"));
                            newSegment.overTime = Convert.ToInt64(reader.GetAttribute("overtime"));
                            break;
                        case "cad":
                            newSegment.cadTarget = Convert.ToInt32(reader.GetAttribute("target"));
                            newSegment.ptsCadPlus = Convert.ToInt32(reader.GetAttribute("plus"));
                            newSegment.ptsCadMinus = Convert.ToInt32(reader.GetAttribute("minus"));
                            newWorkout.segments.Add(newSegment);
                            newSegment = new segmentDef();
                            break;
                        default:
                            break;
                    }
                }
                newWorkout.length = totalLength;
                workOutList.Add(newWorkout);
                newWorkout = new workoutDef();
                newWorkout.segments = new List<segmentDef>();
                reader.Close();
                workOutContents.Close();
            }
             

        }



        public bool loadWorkout(int workoutToStart)
        {
            if (workoutRunning) return false;
            if (workoutToStart >= workOutList.Count) return false;
            if (workoutEventHandler==null) return false;

            activeSegment = -1;
            activeWorkout = workOutList[workoutToStart];
            msTimeForNextSegment = activeWorkout.segments[0].length*1000;

            workoutEventArgs wEA = getDefaultEventData();
            wEA.message = "Loading Workout " + activeWorkout.title;
            wEA.segmentCurrentTarget = activeWorkout.segments[0].effort;
            wEA.segmentCadTarget = activeWorkout.segments[0].cadTarget;
            wEA.segmentTotalMS = activeWorkout.segments[0].length*1000;
            wEA.workoutTotalMS = activeWorkout.length*1000;
            wEA.starting = true;
            wEA.currentSegmentDef = activeWorkout.segments[0];
            workoutEventHandler(this, wEA);
            workOutSeconds = 0;
            return true;
        }
        

        public bool startWorkout()
        {
            if (workoutEventHandler == null) return false;

            forceFinish = false;

            workoutCountDown = 10;

            workoutEventArgs wEA = getDefaultEventData();
            wEA.message = "Starting " + activeWorkout.title + " in " + workoutCountDown.ToString() + " Seconds";
            wEA.segmentCurrentTarget = activeWorkout.segments[0].effort;
            wEA.pointsPlus = activeWorkout.segments[0].ptsPlus;
            wEA.pointsMinus = activeWorkout.segments[0].ptsMinus;

            wEA.segmentCadTarget = activeWorkout.segments[0].cadTarget;
            wEA.pointsCadPlus = activeWorkout.segments[0].ptsCadPlus;
            wEA.pointsCadMinus = activeWorkout.segments[0].ptsCadMinus;

            wEA.segmentTotalMS = activeWorkout.segments[0].length * 1000;
            wEA.workoutCurrentMS = 0;
            wEA.workoutTotalMS = activeWorkout.length*1000;

            wEA.currentSegmentDef = activeWorkout.segments[0];

            workoutEventHandler(this, wEA);

            _countDownTimer = new Timer(1000);
            _countDownTimer.Elapsed += new ElapsedEventHandler(_countDownTimerElapsed);
            _countDownTimer.Enabled = true;

            return true;
        }

        public bool playPauseWorkout()
        {
            if (workoutCountDown > 0)
            {
                if (_countDownTimer.Enabled) _countDownTimer.Enabled = false;
                else _countDownTimer.Enabled = true;
                return true;
            }
            if (workoutTime == null) return false;
            if (workoutTime.ElapsedMilliseconds <= 0) return false;

            if (!bIsPaused)
            {
                bIsPaused = true;
                workoutTime.Stop();
                if (null != workoutEventStartStop)
                {
                    workoutStatusArgs e = new workoutStatusArgs();
                    e.running = false;
                    workoutEventStartStop(this, e);
                }
                bIsRunning = false;
            }
            else
            {
                bIsPaused = false;
                workoutTime.Start();
                if (null != workoutEventStartStop)
                {
                    workoutStatusArgs e = new workoutStatusArgs();
                    e.running = true;
                    workoutEventStartStop(this, e);
                }
                bIsRunning = true;
            }
            return true;
        }

        public bool resetWorkout()
        {

            bIsPaused=false;
            bIsFinished = false;
            bIsRunning = false;
            return true;
        }

        void _countDownTimerElapsed(object sender, ElapsedEventArgs e)
        {
            workoutCountDown--;

            workoutEventArgs wEA = getDefaultEventData();
            if (workoutCountDown > 0)
            {
                wEA.message = "Starting "+activeWorkout.title + " in " + workoutCountDown.ToString() + " Seconds";
                wEA.segmentCurrentTarget = activeWorkout.segments[0].effort;
                wEA.pointsPlus = activeWorkout.segments[0].ptsPlus;
                wEA.pointsMinus = activeWorkout.segments[0].ptsMinus;
                wEA.segmentCadTarget = activeWorkout.segments[0].cadTarget;
                wEA.pointsCadPlus = activeWorkout.segments[0].ptsCadPlus;
                wEA.pointsCadMinus = activeWorkout.segments[0].ptsCadMinus;
                wEA.segmentTotalMS = activeWorkout.segments[0].length * 1000;
                wEA.currentSegmentDef = activeWorkout.segments[0];
                wEA.workoutTotalMS = activeWorkout.length*1000;
            }
            else
            {
                _countDownTimer.Stop();
                activeSegment = 0;
                wEA.message = "GGGGOOOOOO!!!";
                wEA.segmentCurrentTarget = activeWorkout.segments[0].effort;
                wEA.pointsPlus = activeWorkout.segments[0].ptsPlus;
                wEA.pointsMinus = activeWorkout.segments[0].ptsMinus;
                wEA.segmentCadTarget = activeWorkout.segments[0].cadTarget;
                wEA.pointsCadPlus = activeWorkout.segments[0].ptsCadPlus;
                wEA.pointsCadMinus = activeWorkout.segments[0].ptsCadMinus;
                wEA.segmentTotalMS = activeWorkout.segments[0].length * 1000;
                wEA.currentSegmentDef = activeWorkout.segments[0];
                wEA.workoutTotalMS = activeWorkout.length*1000;

                workoutTime = new Stopwatch();
                workoutTime.Start();
                if (null != workoutEventStartStop) 
                {
                    workoutStatusArgs wSA = new workoutStatusArgs();
                    wSA.running = true;
                    workoutEventStartStop(this, wSA);
                }
                bIsRunning = true;
                _updateTimer = new Timer(100);
                _updateTimer.Elapsed += new ElapsedEventHandler(_updateTimerElapsed);
                _updateTimer.Enabled = true;

            }
            if (null != workoutEventHandler)
                workoutEventHandler(this, wEA);

        }
        private bool forceFinish;
        public void finish()
        {
            //Force finish
            forceFinish = true;

        }

        void _updateTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (workoutEventHandler == null) return;

            workOutSeconds = (int)(workoutTime.ElapsedMilliseconds / 1000);

            workoutEventArgs wEA = getDefaultEventData();

            if (workoutTime.ElapsedMilliseconds > msTimeForNextSegment && 
                workoutTime.ElapsedMilliseconds/1000 < activeWorkout.length &&
                activeSegment < activeWorkout.segments.Count-1)
            {
                //New Segment!!
                activeSegment++;
                msTimeForNextSegment += activeWorkout.segments[activeSegment].length * 1000;
                wEA.message = activeWorkout.segments[activeSegment].segmentName;
            }
            if (workoutTime.ElapsedMilliseconds / 1000 < activeWorkout.length && !forceFinish)
            {
                long segTimeLeft = (msTimeForNextSegment - workoutTime.ElapsedMilliseconds);
                //active workout ... send a normal update
                if (segTimeLeft < 1000)
                    wEA.message = "1s to Go!";
                else if (segTimeLeft < 2000)
                    wEA.message = "2s to Go!";
                else if (segTimeLeft < 3000)
                    wEA.message = "3s to Go!";

                wEA.workoutCurrentMS = workoutTime.ElapsedMilliseconds;
                wEA.workoutTotalMS = activeWorkout.length*1000;
                wEA.segmentCurrentMS = activeWorkout.segments[activeSegment].length*1000 - (msTimeForNextSegment - workoutTime.ElapsedMilliseconds);
                wEA.segmentTotalMS = activeWorkout.segments[activeSegment].length*1000;

                wEA.pointsPlus = activeWorkout.segments[activeSegment].ptsPlus;
                wEA.pointsMinus = activeWorkout.segments[activeSegment].ptsMinus;
                wEA.pointsCadPlus = activeWorkout.segments[activeSegment].ptsCadPlus;
                wEA.pointsCadMinus = activeWorkout.segments[activeSegment].ptsCadMinus;
                wEA.currentSegmentDef = activeWorkout.segments[activeSegment];
                switch (activeWorkout.segments[activeSegment].type)
                {
                    case "steady":
                        wEA.segmentCurrentTarget = activeWorkout.segments[activeSegment].effort;
                        wEA.segmentCadTarget = activeWorkout.segments[activeSegment].cadTarget;

                        break;
                    case "ramp":
                        wEA.segmentCurrentTarget = activeWorkout.segments[activeSegment].effort +
                            ((double)wEA.segmentCurrentMS / wEA.segmentTotalMS)*(activeWorkout.segments[activeSegment].effortFinish - activeWorkout.segments[activeSegment].effort);
                        wEA.segmentCadTarget = activeWorkout.segments[activeSegment].cadTarget;
                        break;
                    case "overunder":
                        long timeLeft = (msTimeForNextSegment - workoutTime.ElapsedMilliseconds)/1000;
                        while (timeLeft > 0)
                        {
                            if (timeLeft <= activeWorkout.segments[activeSegment].overTime)
                            {
                                wEA.segmentCurrentTarget = activeWorkout.segments[activeSegment].effortFinish;
                                timeLeft = 0;
                            }
                            else timeLeft -= activeWorkout.segments[activeSegment].overTime;

                            if (timeLeft > 0 && timeLeft <= activeWorkout.segments[activeSegment].underTime)
                            {
                                wEA.segmentCurrentTarget = activeWorkout.segments[activeSegment].effort;
                                timeLeft = 0;
                            }
                            else timeLeft -= activeWorkout.segments[activeSegment].underTime;
                        }
                        wEA.segmentCadTarget = activeWorkout.segments[activeSegment].cadTarget;
                        break;
                    default:
                        wEA.segmentCurrentTarget = activeWorkout.segments[activeSegment].effort;
                        wEA.segmentCadTarget = activeWorkout.segments[activeSegment].cadTarget;
                        break;

                }    
                            
            }
            else
            {
                // Workout is over!!
                if (!forceFinish)
                    bIsFinished = true;
                forceFinish = false;

                workoutTime.Stop();
                if (null != workoutEventStartStop)
                {
                    workoutStatusArgs wSA = new workoutStatusArgs();
                    wSA.running = false;
                    workoutEventStartStop(this, wSA);
                }

                bIsRunning = false;
                wEA.message = "Done!!!";
                wEA.finished = true;
                _updateTimer.Stop();
            }

            if (workoutEventHandler != null) 
                workoutEventHandler(this, wEA);

        }
 
        workoutEventArgs getDefaultEventData()
        {
            workoutEventArgs wEA = new workoutEventArgs();
            wEA.clear = false;
            wEA.finished = bIsFinished;
            wEA.message = "";
            wEA.paused = bIsPaused;
            wEA.segmentCurrentMS = 0;
            wEA.segmentCurrentTarget = 0;
            wEA.segmentCadTarget = 80;
            wEA.segmentTotalMS = 0;
            wEA.starting = false;
            wEA.workoutCurrentMS = 0;
            wEA.workoutTotalMS = 0;
            wEA.currentSegment = activeSegment;
            wEA.running = bIsRunning;
            return wEA;
        }
    }



    public class workoutDef
    {
        public string title;
        public string videopath;
        public long length;

        public List<segmentDef> segments;
    }

    public class segmentDef
    {
        public string segmentName;
        public string type;
        public double effort;
        public double effortFinish;
        public double ptsPlus;
        public double ptsMinus;
        public int cadTarget;
        public int ptsCadPlus;
        public int ptsCadMinus;
        public long length;
        public long underTime;
        public long overTime;
    }

    enum segmentTypes
    {
        SteadyState,
        Ramp,
        OverUnder
    }

    public static class cogganZones
    {
        public static readonly double L1_ActiveRecovery = .55;
        public static readonly double L2_Endurance = .65;
        public static readonly double L3_Tempo = .83;
        public static readonly double L4_Threshold = .95;
        public static readonly double L5_Aerobic = 1.13;
        public static readonly double L6_Anerobic = 1.25;
        public static readonly double L7_Allout = 1.40;
    }

    public static class rpeZones
    {
        public static readonly double RPE_30 = .5;
        public static readonly double RPE_35 = .56;
        public static readonly double RPE_40 = .63;
        public static readonly double RPE_45 = .69;
        public static readonly double RPE_50 = .76;
        public static readonly double RPE_55 = .82;
        public static readonly double RPE_60 = .89;
        public static readonly double RPE_65 = .95;
        public static readonly double RPE_70 = 1.01;
        public static readonly double RPE_75 = 1.08;
        public static readonly double RPE_80 = 1.14;
        public static readonly double RPE_85 = 1.21;
        public static readonly double RPE_90 = 1.27;
        public static readonly double RPE_95 = 1.34;
        public static readonly double RPE_100 = 1.40;
    }


}
