using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.XPath;


namespace CycleSoft
{
    class cTCXFileGenerator
    {
        private bool bFileOpen;
        private string fileName;
        private string userPath;
        private Stream tcxOutContents;
        private StreamWriter tcxStreamWrite;
        private double distanceMeters;
        private int totalSeconds;

        public cTCXFileGenerator(string userF, string userL, string workOut)
        {
            distanceMeters = 0;
            totalSeconds = 0; 

            userPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CycleSoft\\TCXFiles\\");
            Directory.CreateDirectory(userPath);
            workOut = Regex.Replace(workOut, "[^a-zA-Z0-9_.]+", "_", RegexOptions.Compiled);
            fileName = userPath + userF + "_" + userL + "_" + "_"+ workOut + "_"+ DateTime.Now.ToString("yyyy-M-d_Hmm") + ".tcx";

            tcxStreamWrite = File.AppendText(fileName);
            tcxStreamWrite.AutoFlush = true;

            tcxStreamWrite.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            tcxStreamWrite.WriteLine("<TrainingCenterDatabase xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xsi:schemaLocation=\"http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2 http://www.garmin.com/xmlschemas/TrainingCenterDatabasev2.xsd\" xmlns:ns5=\"http://www.garmin.com/xmlschemas/ActivityGoals/v1\" xmlns:ns3=\"http://www.garmin.com/xmlschemas/ActivityExtension/v2\" xmlns:ns2=\"http://www.garmin.com/xmlschemas/UserProfile/v2\" xmlns=\"http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:ns4=\"http://www.garmin.com/xmlschemas/ProfileExtension/v1\">");
            tcxStreamWrite.WriteLine("\t<Activities xmlns=\"http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2\">");
            tcxStreamWrite.WriteLine("\t\t<Activity Sport=\"Biking\">");
            tcxStreamWrite.WriteLine("\t\t\t<Id>" + DateTime.Now.ToUniversalTime().ToString("s") + "Z</Id>");
            tcxStreamWrite.WriteLine("\t\t\t<Lap StartTime=\"" + DateTime.Now.ToUniversalTime().ToString("s") + "Z\">");
            tcxStreamWrite.WriteLine("\t\t\t\t<TotalTimeSeconds>XXTTGRHEREXX</TotalTimeSeconds>");
            tcxStreamWrite.WriteLine("\t\t\t\t<DistanceMeters>XXTDGRHEREXX</DistanceMeters>");
//            tcxStreamWrite.WriteLine("\t\t\t\t<DistanceMeters>" + wheelSize * TCXDistanceCount / 1000 + "</DistanceMeters>");
            tcxStreamWrite.WriteLine("\t\t\t\t<Calories>0</Calories>");
            tcxStreamWrite.WriteLine("\t\t\t\t<Intensity>Active</Intensity>");
            tcxStreamWrite.WriteLine("\t\t\t\t<TriggerMethod>Time</TriggerMethod>");
            tcxStreamWrite.WriteLine("\t\t\t\t<Track>");

            // keep file open for writing.
            //tcxStreamWrite.Close();
            //tcxOutContents.Close();
        
        }

        public bool addTCXData(TCXdata point)
        {
            try
            {
                //Stream tcxOutContents = File.Open(fileName, FileMode.CreateNew);
                //StreamWriter tcxStreamWrite = new StreamWriter(tcxOutContents);

                distanceMeters = point.distanceMeters;
                totalSeconds++;

                tcxStreamWrite.WriteLine("\t\t\t\t\t<Trackpoint>");
                tcxStreamWrite.WriteLine("\t\t\t\t\t\t<Time>" + point.timeStamp.ToUniversalTime().ToString("s") + "Z</Time>");
                tcxStreamWrite.WriteLine("\t\t\t\t\t\t<DistanceMeters>" + point.distanceMeters + "</DistanceMeters>");
                tcxStreamWrite.WriteLine("\t\t\t\t\t\t<HeartRateBpm>");
                tcxStreamWrite.WriteLine("\t\t\t\t\t\t\t<Value>" + point.heartRate + "</Value>");
                tcxStreamWrite.WriteLine("\t\t\t\t\t\t</HeartRateBpm>");
                tcxStreamWrite.WriteLine("\t\t\t\t\t\t<Cadence>" + point.cadence + "</Cadence>");
                tcxStreamWrite.WriteLine("\t\t\t\t\t\t<Extensions>");
                tcxStreamWrite.WriteLine("\t\t\t\t\t\t\t<ns3:TPX>");
                tcxStreamWrite.WriteLine("\t\t\t\t\t\t\t\t<ns3:Watts>" + point.power + "</ns3:Watts>");
                tcxStreamWrite.WriteLine("\t\t\t\t\t\t\t\t<ns3:Speed>" + point.speed + "</ns3:Speed>");
                tcxStreamWrite.WriteLine("\t\t\t\t\t\t\t</ns3:TPX>");
                tcxStreamWrite.WriteLine("\t\t\t\t\t\t</Extensions>");
                tcxStreamWrite.WriteLine("\t\t\t\t\t</Trackpoint>");

                //tcxStreamWrite.Close();
                //tcxOutContents.Close();

                return true;
            }
            catch
            {
                return false;
            }

        }

        public bool closeTCXData()
        {
            XmlDocument xml = new XmlDocument();
            try
            {
                //Stream tcxOutContents = File.Open(fileName, FileMode.CreateNew);
                //StreamWriter tcxStreamWrite = new StreamWriter(tcxOutContents);

                tcxStreamWrite.WriteLine("\t\t\t\t</Track>");
                tcxStreamWrite.WriteLine("\t\t\t</Lap>");
                tcxStreamWrite.WriteLine("\t\t</Activity>");
                tcxStreamWrite.WriteLine("\t</Activities>");
                tcxStreamWrite.WriteLine("</TrainingCenterDatabase>");
                tcxStreamWrite.Close();
                // tcxOutContents.Close();

                /* Content Like:
                <TotalTimeSeconds>XXTTGRHEREXX</TotalTimeSeconds>
                <DistanceMeters>XXTDGRHEREXX</DistanceMeters>
                */
                XmlDocument doc = new XmlDocument();
                doc.Load(fileName);
                XmlNodeList xmlnode = doc.GetElementsByTagName("TotalTimeSeconds");
                xmlnode[0].ChildNodes[0].Value = totalSeconds.ToString();
                xmlnode = doc.GetElementsByTagName("DistanceMeters");
                xmlnode[0].ChildNodes[0].Value = distanceMeters.ToString();
                doc.Save(fileName);
                
                return true;
             
            }
            catch
            { return false; }
            
        }

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
}
