using System;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Timers;

namespace CycleSoft
{

    // A delegate type for hooking up change notifications.
    public delegate void TimeoutHandler(object sender, EventArgs e);
    public delegate void UpdateHandler(object sender, EventArgs e);

    public enum sensorTypes 
    {
        Power = 0x0b, 
        HeartRate = 120,
        SpeedCadence = 121
    }

    public class dataStream
    {
        // An event that clients can use to be notified whenever the
        // elements of the list change.
        public event TimeoutHandler timeoutEvent;
        public event UpdateHandler updateEvent;

        protected void fireUpdateEvent(object sender, EventArgs e)
        {
            if (updateEvent != null)
            {
                updateEvent(this, EventArgs.Empty);
                bUsed = true;
                bAlive = true;
            }
            else
            {
                bUsed = false;
                bAlive = true;
            }
        }


        void _timerElapsed(object sender, ElapsedEventArgs e)
        {
//            OnChanged(EventArgs.Empty);
            if (!stillAlive)
            {
                bAlive = false;
                // only send timeout if no user windows are subscribed.
                // hold out hope that the sensor will come back!!
                if (timeoutEvent != null && updateEvent == null)
                    timeoutEvent(this, EventArgs.Empty);
            }
            else
                bAlive = true;

            stillAlive = false;
        }

        protected byte pPAGE = 1;
        protected byte pUID = 9;
        protected byte pADDRESS = 10;
        protected byte pTYPE = 12;
       
        protected bool bIntstatus = false;
        public UInt16 sensorAddress { get; private set; }
        public byte sensorType { get; private set; }

        public string currentStatus { get; set; }

        public UInt32 uniqueID { get; set; }


        private Queue<int> qPWRQue;
        private UInt16 pwrEventCnt;

        private UInt16 cadTime;
        private UInt16 cadTimePrev;
        private UInt16 cadCnt;
        private UInt16 cadCntPrev;
        private UInt16 cad;

        private UInt16 spdTime;
        private UInt16 spdTimePrev;
        private UInt16 spdCnt;
        private UInt16 spdCntPrev;
        private UInt16 spd;

        protected bool stillAlive;
        protected StreamWriter log;

        public float spdInst { get; protected set; }
        public int cadInst { get; protected set; }
        public UInt16 hb { get; protected set; }
        public UInt16 powerInst { get; protected set; }

        public bool bAlive { get; protected set; }
        public bool bUsed { get; protected set; }
            

        public dataStream(ushort sAddress, byte sType, uint uId)
        {
            bIntstatus = false;
            sensorAddress = sAddress;
            sensorType = sType;
            uniqueID = uId;
            
            setup();

            return;
        }

        public dataStream(byte[] inputData)
        {
            bIntstatus = false;
            sensorAddress = BitConverter.ToUInt16(inputData, pADDRESS);
            sensorType = inputData[pTYPE];
            uniqueID = BitConverter.ToUInt32(inputData, pUID);

            setup();

            return;
        }

        private void setup()
        {
            currentStatus = "Starting up!";

            Timer _timer = new Timer(3000);
            _timer.Elapsed += new ElapsedEventHandler(_timerElapsed);
            _timer.Enabled = true;

            spdInst = 0;
            cadInst = 0;
            hb = 0;
            powerInst = 0;

            bUsed = false;
            bAlive = false;

            return;
        }

        public void closeStream()
        {
            try
            {
                log.Close();
            }
            catch
            { }

        }

/*        public UInt32 getID()
        {
            // when the main Program gets a new sting, this is used to determine 
            return uniqueID;
        }
 */
    }



}
