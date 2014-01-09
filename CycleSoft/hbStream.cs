using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ANTSniffer
{
    public class hbStream : dataStream
    {
        private byte HR = 120;        // Device number    

        const byte pHBTIME = 5;
        const byte pHBCNT = 7;
        const byte pHBCALC = 8;

        private UInt16 hbTime;
        private UInt16 hbTimePrev;
        private UInt16 hbCnt;
        private UInt16 hbCntPrev;



        private UInt16 lasthbEventCnt;
        private int messageCnt;

        public hbStream(ushort sAddress, byte sType, uint uId) :base (sAddress, sType, uId)
        {
            bIntstatus = false;

            if (HR == sensorType)
            {
                openLog(sensorAddress);
            }

            return;

        }

        public hbStream(byte[] initializeData) : base (initializeData)
        {
            bIntstatus = false;
            if (HR == sensorType)
            {
                openLog(sensorAddress);
                messageCnt = 0;

                parseData(initializeData);
            }

            return;
        }

        private void openLog(ushort sensorAddress)
        {
            
            String Filename = sensorAddress.ToString() + "_HeartRate_log.txt";
            log = new StreamWriter(Filename.ToString());
            log.WriteLine(DateTime.Now.ToString("HH:mm:ss") + ": log opened for new stream.");
            log.Write("    Stream HeartRate data for sensor " + sensorAddress.ToString());
            bIntstatus = true;

        }

        public String getStatus()
        {
            StringBuilder response = new StringBuilder();
            response.Append(sensorAddress +"-");
            response.Append("HR - inst " + hb );
            return response.ToString();
        }

        public bool parseData(byte[] inputData)
        {
            stillAlive = true;
            int lastHb = hb;

            if (sensorAddress == BitConverter.ToUInt16(inputData, pADDRESS) && sensorType == inputData[pTYPE])
            {
                log.WriteLine(DateTime.Now.ToString("HH:mm:ss") + ":HeartRate Data: " + BitConverter.ToString(inputData));
                if (hbCntPrev != inputData[pHBCNT])
                {
                    hbCntPrev = inputData[pHBCALC];
                    hb = inputData[pHBCALC];
                    messageCnt = 0;
                }
                else
                    messageCnt++;

                if (messageCnt > 40)    //if we go 10 seconds without a HB change, we are in trouble!
                    hb = 0;

                currentStatus = getStatus();
                //if (hb != lastHb)
                {
                    fireUpdateEvent(this, EventArgs.Empty);
                }
                return true;
            }
            
            log.WriteLine(DateTime.Now.ToString("HH:mm:ss") + ":!!Unknown Data: " + BitConverter.ToString(inputData));
       
            return false;
            
        }
    }


}
