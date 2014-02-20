using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CycleSoft
{
    public class spdStream : dataStream
    {
        protected byte SPEEDCAD = 121;  // ANT Channel to use
        const byte pCADTIME = 1;
        const byte pCADCNT = 3;
        const byte pSPDTIME = 5;
        const byte pSPDCNT = 7;


        private UInt16 prevCadTime;
        private UInt16 prevCadCnt;
        private int preCadUpdCnt;
        private UInt16 prevSpdTime;
        private UInt16 prevSpdCnt;
        private int preSpdUpdCnt;

        private UInt16 toZeroCount = 25; // No Updates in 10 cycles, 0 speeds
        
        const int ROLLOVER = 65536;   // Rollover Point - Can go to 65535 in a 16bit uint.
        const int HB_ROLLOVER = 256;   // Rollover Point - HB count goes to 255 in a 8bit uint.
        const int TIMEDIVISOR = 1024; // time intervals are 1/1024 of a second

        public int publicSpdCnt { get; private set; }

        public int wheelSize { get;set; }

        public spdStream(ushort sAddress, byte sType, uint uId) :base (sAddress, sType, uId)
        {
            bIntstatus = false;

            if (SPEEDCAD == sensorType)
            {
                openLog(sensorAddress);
            }

            return;

        }


        public spdStream(byte[] initializeData) : base(initializeData)
        {
            bIntstatus = false;
            if (SPEEDCAD == sensorType)
            {
                openLog(sensorAddress);
                prevCadTime = BitConverter.ToUInt16(initializeData, pCADTIME);
                prevCadCnt = BitConverter.ToUInt16(initializeData, pCADCNT);

                wheelSize = 2070;

                parseData(initializeData);
            }

            return;
        }

        private void openLog(ushort sensorAddress)
        {
            string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CycleSoft\\SensorLogFiles\\");
            Directory.CreateDirectory(logPath);

            String Filename = logPath + sensorAddress.ToString() + "_SpdCad_log.txt";
            log = new StreamWriter(Filename.ToString());
            log.WriteLine(DateTime.Now.ToString("HH:mm:ss") + ": log opened for new stream.");
            log.Write("    Stream Speed and Cadence data for sensor " + sensorAddress.ToString());

            bIntstatus = true;
        }

        public String getStatus()
        {
            StringBuilder response = new StringBuilder();
            response.Append(sensorAddress +"-");
            response.Append("SPD - inst " + spdInst + " - CAD - inst "+  cadInst + " " + prevCadCnt);
            return response.ToString();
        }

        public bool parseData(byte[] inputData)
        {
            stillAlive = true;
            float lastSpd = spdInst;
            int lastCad = cadInst;


            if (sensorAddress == BitConverter.ToUInt16(inputData, pADDRESS) && sensorType == inputData[pTYPE])
            {
                log.WriteLine(DateTime.Now.ToString("HH:mm:ss") + ":SpdCad Data: " + BitConverter.ToString(inputData));
                UInt16 cadTime = BitConverter.ToUInt16(inputData, pCADTIME);
                UInt16 cadCnt = BitConverter.ToUInt16(inputData, pCADCNT);
                UInt16 spdTime = BitConverter.ToUInt16(inputData, pSPDTIME);
                UInt16 spdCnt = BitConverter.ToUInt16(inputData, pSPDCNT);

                if (cadCnt != prevCadCnt)
                {
                    if (cadCnt < prevCadCnt)
                        cadCnt += (UInt16)(ROLLOVER-prevCadCnt);
                    else
                        cadCnt -= prevCadCnt;

                    // at this point, n revolutions in cadCnt
                    if (cadTime < prevCadTime)
                        cadTime += (UInt16)(ROLLOVER-prevCadTime);
                    else
                        cadTime -= prevCadTime;
                    
                    float temp = cadCnt/((float)cadTime/(60*TIMEDIVISOR));
                    cadInst = (int)temp;
                    if (cadInst > 200 || cadInst < 0) cadInst = lastCad;
                    prevCadTime = BitConverter.ToUInt16(inputData, pCADTIME);
                    prevCadCnt = BitConverter.ToUInt16(inputData, pCADCNT);
                    preCadUpdCnt = 0;
                }
                else
                {
                    cadCnt = 0;
                    preCadUpdCnt++;
                    if (preCadUpdCnt>toZeroCount)
                        cadInst = 0;
                }

                if (spdCnt != prevSpdCnt)
                {
                    if (spdCnt < prevSpdCnt)
                        spdCnt += (UInt16)(ROLLOVER - prevSpdCnt);
                    else
                        spdCnt -= prevSpdCnt;

                    // at this point, n revolutions in cadCnt
                    if (spdTime < prevSpdTime)
                        spdTime += (UInt16)(ROLLOVER - prevSpdTime);
                    else
                        spdTime -= prevSpdTime;

                    float rph = spdCnt / ((float)spdTime / (3600 * TIMEDIVISOR));


                    spdInst = (rph * wheelSize / 1609344);
                    if (spdInst > 50 || spdInst < 0) spdInst = lastSpd;

                    prevSpdTime = BitConverter.ToUInt16(inputData, pSPDTIME);
                    prevSpdCnt = BitConverter.ToUInt16(inputData, pSPDCNT);
                    preSpdUpdCnt = 0;
                }
                else
                {
                    spdCnt = 0;
                    preSpdUpdCnt++;
                    if (preSpdUpdCnt > toZeroCount)
                        spdInst = 0;
                }
                currentStatus = getStatus();
                //if (lastSpd != spdInst || lastCad != cadInst)
                {
                    // if statement added to ignore data being sent that caused spikes in the charts.
                    // didn't work
                    //if ((Math.Abs(lastSpd - spdInst) < 10) && (Math.Abs(lastCad - cadInst) < 10))

                    // adding this if condition, trying to kill spikes caused when 
                    // shit happens. like when the system thinks we get SPDCad data
                    // but it is something else, like this:
                    /* 11:06:05:SpdCad Data: 00-6C-A9-5C-DC-00-B0-03-96-80-83-8F-79-01
                     * 11:06:05:SpdCad Data: 00-03-B3-5D-DC-FF-B2-04-96-80-83-8F-79-01
                     * 11:06:06:SpdCad Data: 00-03-B3-5D-DC-FF-B2-04-96-80-83-8F-79-01
                     * 11:06:06:SpdCad Data: 00-03-B3-5D-DC-FF-B2-04-96-80-83-8F-79-01
                     * 11:06:06:SpdCad Data: 00-10-36-FF-FF-75-ED-DD-00-80-83-8F-79-01  ?? what is this all about
                     * 11:06:06:SpdCad Data: 00-03-B3-5D-DC-CC-B5-05-96-80-83-8F-79-01
                     * 11:06:06:SpdCad Data: 00-03-B3-5D-DC-CC-B5-05-96-80-83-8F-79-01
                     * 11:06:07:SpdCad Data: 00-CD-B7-5E-DC-CC-B5-05-96-80-83-8F-79-01
                     * 11:06:07:SpdCad Data: 00-CD-B7-5E-DC-98-B8-06-96-80-83-8F-79-01
                     */

                    if (cadCnt < 5 && spdCnt < 5)
                    {
                        fireUpdateEvent(this, EventArgs.Empty);
                        publicSpdCnt = spdCnt;                      // this is used for TCX export, and I think is where things got ugly.
//                        log.WriteLine(DateTime.Now.ToString("HH:mm:ss") + ":   SpdCad Event Fired with publicSpdCnt: " + publicSpdCnt.ToString());
                    }
                    
                    
                    // wonder if I should include an "else" statement, to write lastSpd && lastCadence to current?
                   /* else
                    {
                        spdInst = lastSpd;
                        cadInst = lastCad;
                    }*/
                }
                return true;
            }
            
            log.WriteLine(DateTime.Now.ToString("HH:mm:ss") + ":!!Unknown Data: " + BitConverter.ToString(inputData));
       
            return false;
            
        }
    }


}
