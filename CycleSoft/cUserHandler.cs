using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.XPath;


namespace CycleSoft
{
    public class cUserHandler
    {
        public List<cAntUsers> l_Users;
        XmlReader reader;
        private string[] userFileArray;

        public cUserHandler(cStreamHandler StreamHandler)
        {
            l_Users = new List<cAntUsers>();

            userFileArray = Directory.GetFiles(".\\users\\", "*.xml");
            for (int i = 0; i < userFileArray.Length; i++)
            {

                cAntUsers newUser = new cAntUsers();

                Stream userContents = File.Open(userFileArray[i], FileMode.Open);
                reader = XmlReader.Create(userContents);

                while (!reader.EOF)
                {
                    /* Content Like:
                        <User fName = "Greg" lName = "GT" ftp = "245">
	                        <power></power>
	                        <wheelsize>2170</wheelsize>
	                        <speedCad>64522</speedCad>
	                        <heartrate>4030</heartrate>
	                        <virtPower>16</virtPower>
                        </User>
                    */
                    reader.Read();
                    switch (reader.Name)
                    {
                        case "User":
                            if (reader.HasAttributes)
                            {
                                newUser.firstName = reader.GetAttribute("fName");
                                newUser.lastName = reader.GetAttribute("lName");
                                newUser.ftp = Convert.ToInt16(reader.GetAttribute("ftp"));
                            }
                            break;
                        case "power":
                            try
                            {

                                ushort sAddress = (ushort)reader.ReadElementContentAsInt();
                                int pwrStreamIdx = StreamHandler.addStream(sAddress, (byte)sensorTypes.Power);
                                if (pwrStreamIdx >= 0)
                                {
                                    StreamHandler.pwrStreams[pwrStreamIdx].updateEvent += newUser.updatePwrEvent;
                                    newUser.activePwrStream = StreamHandler.pwrStreams[pwrStreamIdx];
                                }
                            }
                            catch
                            { }
                            break;
                        case "speedCad":
                            try
                            {

                                ushort sAddress = (ushort)reader.ReadElementContentAsInt();
                                int spdStreamIdx = StreamHandler.addStream(sAddress, (byte)sensorTypes.SpeedCadence);
                                if (spdStreamIdx >= 0)
                                {
                                    StreamHandler.spdStreams[spdStreamIdx].updateEvent += newUser.updateSpdCadEvent;
                                    newUser.activeSpeedStream = StreamHandler.spdStreams[spdStreamIdx];
                                }
                            }
                            catch
                            { }
                            break;
                        case "heartrate":
                            try
                            {

                                ushort sAddress = (ushort)reader.ReadElementContentAsInt();
                                int hbStreamIdx = StreamHandler.addStream(sAddress, (byte)sensorTypes.HeartRate);
                                if (hbStreamIdx >= 0)
                                {
                                    StreamHandler.hbStreams[hbStreamIdx].updateEvent += newUser.updateHrEvent;
                                    newUser.activeHrStream = StreamHandler.hbStreams[hbStreamIdx];
                                }
                            }
                            catch
                            { }
                            break;
                        case "wheelsize":
                            try
                            {
                                newUser.wheelSize = reader.ReadElementContentAsInt();
                            }
                            catch
                            { newUser.wheelSize = 2100; }
                            break;
                        case "virtPower":
                            try
                            {
                                newUser.ptrSPwr = reader.ReadElementContentAsInt();
                            }
                            catch
                            { newUser.ptrSPwr = -1; }
                            break;
                        default:
                            break;
                    }
                }
                l_Users.Add(newUser);
                reader.Close();
                userContents.Close();
            }


        }

        public void saveUser(cAntUsers user)
        {
            /* Content Like:
                <User fName = "Greg" lName = "GT" ftp = "245">
                    <power></power>
                    <wheelsize>2170</wheelsize>
                    <speedCad>64522</speedCad>
                    <heartrate>4030</heartrate>
                    <virtPower>16</virtPower>
                </User>
            */
            Stream userOutContents = File.Open("users\\" + user.firstName +"_"+user.lastName+"_"+user.ptrSPwr.ToString()+".xml", FileMode.OpenOrCreate);
            StreamWriter userStreamWrite = new StreamWriter(userOutContents);
            userStreamWrite.WriteLine("<User fName = \"" + user.firstName + "\" lName = \"" + user.lastName + "\" ftp = \"" + user.ftp.ToString() + "\">");
            if(user.activePwrStream != null)
                userStreamWrite.WriteLine("\t<power>" + user.activePwrStream.sensorAddress.ToString() + "</power>");
            userStreamWrite.WriteLine("\t<wheelsize>" + user.wheelSize.ToString() + "</wheelsize>");
            if(user.activeSpeedStream != null)
                userStreamWrite.WriteLine("\t<speedCad>" + user.activeSpeedStream.sensorAddress.ToString() + "</speedCad>");
            if(user.activeHrStream != null)
                userStreamWrite.WriteLine("\t<heartrate>" + user.activeHrStream.sensorAddress.ToString()+ "</heartrate>");
            userStreamWrite.WriteLine("\t<virtPower>" + user.ptrSPwr.ToString() + "</virtPower>");
            userStreamWrite.WriteLine("</User>");

            userStreamWrite.Close();
            userOutContents.Close();

            
        }


        // Adds Users to the User List, not neccesarily opened to a Window, however.
        // this is called from the "Add User" Button
        public bool addUser(MainWindow sender, cStreamHandler StreamHandler)
        {
            cAntUsers newUser = new cAntUsers();
            newUser.firstName = sender.textBoxFirstName.Text;
            newUser.lastName = sender.textBoxLastName.Text;

            try { newUser.ftp = Convert.ToInt32(sender.textBoxFTP.Text); }
            catch { newUser.ftp = 200; }

            if (sender.dataGridSpdCad.SelectedIndex >= 0)
            {
//                StreamHandler.spdStreams[sender.dataGridSpdCad.SelectedIndex].updateEvent += new UpdateHandler(newUser.updateSpdCadEvent);
                StreamHandler.spdStreams[sender.dataGridSpdCad.SelectedIndex].updateEvent += newUser.updateSpdCadEvent;
                newUser.activeSpeedStream = StreamHandler.spdStreams[sender.dataGridSpdCad.SelectedIndex];
            }
            if (sender.dataGridPower.SelectedIndex >= 0)
            {
                StreamHandler.pwrStreams[sender.dataGridPower.SelectedIndex].updateEvent += new UpdateHandler(newUser.updatePwrEvent);
                newUser.activePwrStream = StreamHandler.pwrStreams[sender.dataGridPower.SelectedIndex];
            }
            if (sender.dataGridHR.SelectedIndex >= 0)
            {
                StreamHandler.hbStreams[sender.dataGridHR.SelectedIndex].updateEvent += new UpdateHandler(newUser.updateHrEvent);
                newUser.activeHrStream = StreamHandler.hbStreams[sender.dataGridHR.SelectedIndex];
            }

            if ((sender.dataGridSpdCad.SelectedIndex >= 0) || (sender.dataGridPower.SelectedIndex >= 0) || (sender.dataGridHR.SelectedIndex >= 0))
            {
                l_Users.Add(newUser);
                return true;
            }
            return false;
        }

        public bool removeUser(int toRemove)
        {
            if (toRemove >= 0 && toRemove < l_Users.Count)
            {
                if (l_Users[toRemove].activeHrStream != null)
                    l_Users[toRemove].activeHrStream.updateEvent -= l_Users[toRemove].updateHrEvent;
                if (l_Users[toRemove].activePwrStream != null)
                    l_Users[toRemove].activePwrStream.updateEvent -= l_Users[toRemove].updatePwrEvent;
                if (l_Users[toRemove].activeSpeedStream != null)
                    l_Users[toRemove].activeSpeedStream.updateEvent -= l_Users[toRemove].updateSpdCadEvent;

                l_Users.RemoveAt(toRemove);
                return true;
            }
            return false;
        }
    
    }
}
