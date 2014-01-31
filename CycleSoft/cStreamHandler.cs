using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace CycleSoft
{
    public class cStreamHandler
    {
        static readonly int pUID = 9;
        static readonly int pTYPE = 12;

        public ObservableCollection<powerStream> pwrStreams;
        public ObservableCollection<hbStream> hbStreams;
        public ObservableCollection<spdStream> spdStreams;

        public static object _pwrsyncLock = new object();
        public static object _hbsyncLock = new object();
        public static object _spdsyncLock = new object();

        public cStreamHandler()
        {
            pwrStreams = new ObservableCollection<powerStream>();
            hbStreams = new ObservableCollection<hbStream>();
            spdStreams = new ObservableCollection<spdStream>();

            BindingOperations.EnableCollectionSynchronization(pwrStreams, _pwrsyncLock);
            BindingOperations.EnableCollectionSynchronization(hbStreams, _hbsyncLock);
            BindingOperations.EnableCollectionSynchronization(spdStreams, _spdsyncLock);
        }

        public void closeStreams()
        {
            foreach (powerStream ds in pwrStreams)
            {
                ds.closeStream();
            }
            foreach (hbStream ds in hbStreams)
            {
                ds.closeStream();
            }
            foreach (spdStream ds in spdStreams)
            {
                ds.closeStream();
            }

            int idx = pwrStreams.Count();
            while (idx > 0)
            {
                idx--;
                pwrStreams.RemoveAt(idx);
            }
            idx = hbStreams.Count();
            while (idx > 0)
            {
                idx--;
                hbStreams.RemoveAt(idx);
            }
            idx = spdStreams.Count();
            while (idx > 0)
            {
                idx--;
                spdStreams.RemoveAt(idx);
            }
        }

        public int addStream(ushort sAddress, byte type)
        {
            //called from user Handler to add a Stream from XML file definition
            byte[] address = BitConverter.GetBytes(sAddress);
            byte[] uIdBytes = new byte[4];
            uIdBytes[0] = 128; //80h
            uIdBytes[1] = address[0]; //80h
            uIdBytes[2] = address[1]; //80h
            uIdBytes[3] = type; //0Bh for Power Sensor

            uint uId = BitConverter.ToUInt32(uIdBytes, 0);


            switch (type)
            {
                case (byte)sensorTypes.Power: // power
                    for (int i = 0 ; i < pwrStreams.Count ; i++)
                    {
                        if (pwrStreams[i].uniqueID == uId) return i;
                    }
                    powerStream ds = new powerStream(sAddress, (byte)sensorTypes.Power, uId);
                    pwrStreams.Add(ds);
                    ds.timeoutEvent += new TimeoutHandler(heartBeatLost);
                    return pwrStreams.Count - 1;

                case (byte)sensorTypes.HeartRate:
                    for (int i = 0; i < hbStreams.Count; i++ )
                    {
                        if (hbStreams[i].uniqueID == uId) return i;
                    }
                    hbStream hbds = new hbStream(sAddress, (byte)sensorTypes.HeartRate, uId);
                    hbStreams.Add(hbds);
                    hbds.timeoutEvent += new TimeoutHandler(heartBeatLost);
                    return hbStreams.Count - 1;

                case (byte)sensorTypes.SpeedCadence:
                    for (int i = 0; i < spdStreams.Count; i++ )
                    {
                        if (spdStreams[i].uniqueID == uId) return i;
                    }
                    spdStream spdds = new spdStream(sAddress, (byte)sensorTypes.SpeedCadence, uId);
                    spdStreams.Add(spdds);
                    spdds.timeoutEvent += new TimeoutHandler(heartBeatLost);
                    return spdStreams.Count - 1;

                default:
                    return -1;
            }


        }

        public void handleAntData(object sender, antEventArgs data)
        {
            try
            {
                UInt32 id = BitConverter.ToUInt32(data.data, pUID);

                switch (data.data[pTYPE])
                {
                    case (byte)sensorTypes.Power:
                        //                    pwrStreams.Any( 
                        foreach (powerStream ds in pwrStreams)
                        {
                            if (ds.uniqueID == id)
                            {
                                ds.parseData(data.data);
                                id = 0;
                                break;
                            }
                        }
                        if (id != 0)
                        {
                            powerStream ds = new powerStream(data.data);
                            //App.Current.Dispatcher.Invoke((Action)(() =>
                            //{
                                pwrStreams.Add(ds);
                            //}));
                            ds.timeoutEvent += new TimeoutHandler(heartBeatLost);
                        }

                        break;
                    case (byte)sensorTypes.HeartRate:
                        foreach (hbStream ds in hbStreams)
                        {
                            if (ds.uniqueID == id)
                            {
                                ds.parseData(data.data);
                                id = 0;
                                break;
                            }
                        }
                        if (id != 0)
                        {
                            hbStream ds = new hbStream(data.data);
                            //App.Current.Dispatcher.Invoke((Action)(() =>
                            //{
                                hbStreams.Add(ds);
                            //}));
                            ds.timeoutEvent += new TimeoutHandler(heartBeatLost);
                        }
                        break;
                    case (byte)sensorTypes.SpeedCadence:
                        foreach (spdStream ds in spdStreams)
                        {
                            if (ds.uniqueID == id)
                            {
                                ds.parseData(data.data);
                                id = 0;
                                break;
                            }
                        }
                        if (id != 0)
                        {
                            spdStream ds = new spdStream(data.data);
                            //App.Current.Dispatcher.Invoke((Action)(() =>
                            //{
                                spdStreams.Add(ds);
                            //}));
                            ds.timeoutEvent += new TimeoutHandler(heartBeatLost);
                        }
                        break;
                }
            }
            catch
            { }

        }

        void heartBeatLost(object sender, EventArgs e)
        {
         
            dataStream received = (dataStream)sender;

            switch (received.sensorType)
            {
                case (byte)sensorTypes.HeartRate:
                    for (int i = hbStreams.Count - 1; i >= 0; i--)
                    {
                        if (hbStreams[i].uniqueID == received.uniqueID)
                        {
                            hbStreams[i].closeStream();
                            hbStreams[i].timeoutEvent -= this.heartBeatLost;
                            hbStreams.RemoveAt(i);
                        }
                        break;
                    }
                    break;
                case (byte)sensorTypes.Power:
                    for (int i = pwrStreams.Count - 1; i >= 0; i--)
                    {
                        if (pwrStreams[i].uniqueID == received.uniqueID)
                        {
                            pwrStreams[i].closeStream();
                            pwrStreams[i].timeoutEvent -= this.heartBeatLost;
                            pwrStreams.RemoveAt(i);
                        }
                        break;
                    }
                    break;
                case (byte)sensorTypes.SpeedCadence:
                    for (int i = spdStreams.Count - 1; i >= 0; i--)
                    {
                        if (spdStreams[i].uniqueID == received.uniqueID)
                        {
                            spdStreams[i].closeStream();
                            spdStreams[i].timeoutEvent -= this.heartBeatLost;
                            spdStreams.RemoveAt(i);
                        }
                        break;
                    }
                    break;
                default:
                    break;
            }
 
        }

   
    }
}
