using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ANTSniffer
{

        
    public class powerStream : dataStream
    {

        
        
        const byte PWR = 11;        // Device number    

        const byte pPWREvtCnt = 2;
        const byte pACCPWR = 5;
        const byte pPWR = 7;

        private UInt16 pwrEventCnt;

        public powerStream(ushort sAddress, byte sType, uint uId) :base (sAddress, sType, uId)
        {
            bIntstatus = false;

            if (PWR == sensorType)
            {
                openLog(sensorAddress);
            }

            return;

        }

        public powerStream(byte[] initializeData) :base (initializeData)
        {
            bIntstatus = false;

            if (PWR == sensorType)
            {
                openLog(sensorAddress); 
                parseData(initializeData);
            }

            return;
        }

        private void openLog(ushort sensorAddress)
        {
            String Filename = sensorAddress.ToString() + "_Power_log.txt";
            log = new StreamWriter(Filename.ToString());
            log.WriteLine(DateTime.Now.ToString("HH:mm:ss") + ": log opened for new stream.");
            log.Write("    Stream Power data for sensor " + sensorAddress.ToString());
            bIntstatus = true;
        }

        public String getStatus()
        {
            StringBuilder response = new StringBuilder();
            response.Append(sensorAddress +"-");
            response.Append("PWR - inst " + powerInst);
            return response.ToString();
        }

        public bool parseData(byte[] inputData)
        {
            stillAlive = true;
            int prvPwr = powerInst;

            if (sensorAddress == BitConverter.ToUInt16(inputData, pADDRESS) && sensorType == inputData[pTYPE])
            {
                log.WriteLine(DateTime.Now.ToString("HH:mm:ss") + ":PWR Data: " + BitConverter.ToString(inputData));
                if (0x10 == inputData[pPAGE])
                {
                    powerInst = BitConverter.ToUInt16(inputData, pPWR);
                            
                    if(inputData[pPWREvtCnt] != pwrEventCnt)
                    {
                        pwrEventCnt = inputData[pPWREvtCnt];
                    }
                }
                currentStatus = getStatus();
                //if (prvPwr != powerInst)
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
