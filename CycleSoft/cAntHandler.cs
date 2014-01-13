using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using ANT_Managed_Library;      //Reference the ANT_Managed_Library namespace to make the code easier and more readable 


namespace CycleSoft
{

    public delegate void statusMsgHandler(object sender, EventArgs e);
    public delegate void deviceMsgHandler(object sender, EventArgs e);
    public delegate void channelMsgHandler(object sender, EventArgs e);

    public class antEventArgs : EventArgs
    {
        public string message { get; set; }
        public byte[] data { get; set; }
    }

    public class cAntHandler
    {
        static readonly ushort USER_DEVICENUM = 0;        // Device number    
        static readonly byte USER_DEVICETYPE = 0;          // Device type
        static readonly byte USER_TRANSTYPE = 0;           // Transmission type
        static readonly byte USER_RADIOFREQ = 57;          // RF Frequency + 2400 MHz
        static readonly ushort USER_CHANNELPERIOD = 8192;  // Channel Period (8192/32768)s period = 4Hz
        static readonly byte[] USER_NETWORK_KEY = { 0xB9, 0xA5, 0x21, 0xFB, 0xBD, 0x72, 0xC3, 0x45 };

        private ANT_Device device0;

        public event EventHandler<antEventArgs> channelMessageHandler;


        public cAntHandler()
        {

        }
        //Creates the ANTDevice instances and calls the setupAndOpen routine according to the selected demo mode
        public bool startUp()
        {
            //The managed library will throw ANTExceptions on errors
            //We run this in a try catch because we want to print any errors instead of crash            
            try
            {
                //Regardless of selection we need to connect to the first device
                //The library has an automatic constructor to automatically connect to the first available device
                //You can still manually choose which device to connect to by using the parameter constructor,
                // ie: ANTDeviceInstance = new ANTDevice(0, 57600)
                device0 = new ANT_Device();
                //device0 = new ANT_Device(0, 57600);
                
                //First we want to setup the response functions so we can see the feedback as we setup
                //To do this, the device and each channel have response events which are fired when feedback
                //is received from the device, including command acknowledgements and transmission events.
                device0.deviceResponse += new ANT_Device.dDeviceResponseHandler(device0_deviceResponse);
                device0.getChannel(0).channelResponse += new dChannelResponseHandler(d0channel0_channelResponse);

//                textBox_device0.Text = "Device 0 Connected" + Environment.NewLine;
//                textBox_Display.Text = "Starting Mode: d0 only - Slave Scan" + Environment.NewLine;

                setupAndOpenScan(device0, ANT_ReferenceLibrary.ChannelType.BASE_Slave_Receive_0x00);

            }
            catch (Exception ex)
            {
//                textBox_Display.AppendText("Error: " + ex.Message + Environment.NewLine);
//                if (device0 == null)    //We print another message if we didn't connect to any device to be a little more helpful
//                    textBox_Display.AppendText("Could not connect to any devices, ensure an ANT device is connected to your system and try again." + Environment.NewLine);
//                textBox_Display.AppendText(Environment.NewLine);
                return false;
            }
            return true;
        }

        public void shutdown()
        {
            //If you need to manually disconnect from a ANTDevice call the shutdownDeviceInstance method,
            //It releases all the resources, the device, and nullifies the object reference
            ANT_Device.shutdownDeviceInstance(ref device0);

        }

        private void setupAndOpenScan(ANT_Device deviceToSetup, ANT_ReferenceLibrary.ChannelType channelType)
        {
            //We try-catch and forward exceptions to the calling function to handle and pass the errors to the user
            try
            {

                if (!deviceToSetup.setNetworkKey(0, USER_NETWORK_KEY, 500))
//                    threadSafePrintLine("Network Key set to ... well, you know,  on net 0.", textBox_Display);
//                else
                    throw new Exception("Channel assignment operation failed.");


                //To access an ANTChannel on a paticular device we need to get the channel from the device
                //Once again, this ensures you have a valid object associated with a real-world ANTChannel
                //ie: You can only get channels that actually exist
                ANT_Channel channel0 = deviceToSetup.getChannel(0);

                //Almost all functions in the library have two overloads, one with a response wait time and one without
                //If you give a wait time, you can check the return value for success or failure of the command, however
                //the wait time version is blocking. 500ms is usually a safe value to ensure you wait long enough for any response.
                //But with no wait time, the command is simply sent and you have to monitor the device response for success or failure.

                //To setup channels for communication there are three mandatory operations assign, setID, and Open
                //Various other settings such as message period and network key affect communication 
                //between two channels as well, see the documentation for further details on these functions.

                //So, first we assign the channel, we have already been passed the channelType which is an enum that has various flags
                //If we were doing something more advanced we could use a bitwise or ie:base|adv1|adv2 here too
                //We also use net 0 which has the public network key by default
                if (!channel0.assignChannel(channelType, 0, 500))
//                    threadSafePrintLine("Ch assigned to " + channelType + " on net 0.", textBox_Display);
//                else
                    throw new Exception("Channel assignment operation failed.");

                //Next we have to set the channel id. Slaves will only communicate with a master device that 
                //has the same id unless one or more of the id parameters are set to a wild card 0. If wild cards are included
                //the slave will search until it finds a broadcast that matches all the non-wild card parameters in the id.
                //For now we pick an arbitrary id so that we can ensure we match between the two devices.
                //The pairing bit ensures on a search that you only pair with devices that also are requesting 
                //pairing, but we don't need it here so we set it to false
                if (!channel0.setChannelID(USER_DEVICENUM, false, USER_DEVICETYPE, USER_TRANSTYPE, 500))
//                    threadSafePrintLine("Set channel ID to " + USER_DEVICENUM + " " + USER_DEVICETYPE + " " + USER_TRANSTYPE, textBox_Display);
//                else
                    throw new Exception("Set Channel ID operation failed.");

                //Setting the channel period isn't mandatory, but we set it slower than the default period so messages aren't coming so fast
                //The period parameter is divided by 32768 to set the period of a message in seconds. So here, 16384/32768 = 1/2 sec/msg = 2Hz
                if (!channel0.setChannelPeriod(USER_CHANNELPERIOD, 500))
//                    threadSafePrintLine("Message Period set to " + USER_CHANNELPERIOD + "/32768 seconds per message", textBox_Display);
//                else
                    throw new Exception("Set Channel Period Op Faildd.");

                if (!channel0.setChannelFreq(USER_RADIOFREQ, 500))
//                    threadSafePrintLine("Message Radio Freq set to +" + USER_RADIOFREQ, textBox_Display);
//                else
                    throw new Exception("Set Radio Freq failed.");

                if (!deviceToSetup.enableRxExtendedMessages(true, 500))
//                    threadSafePrintLine("Requesting Extended Messages", textBox_Display);
//                else
                    throw new Exception("Extenned Message Request Failed.");



                //Now we open the channel
                if (!deviceToSetup.openRxScanMode(500))
//                    threadSafePrintLine("Opened Device in Scan mode" + Environment.NewLine, textBox_Display);
//                else
                    throw new Exception("Channel Open operation failed.");
            }
            catch (Exception ex)
            {
                throw new Exception("Setup and Open Failed. " + ex.Message + Environment.NewLine);
            }
        }

        //Print the device response to the textbox
        void device0_deviceResponse(ANT_Response response)
        {
//            threadSafePrintLine(decodeDeviceFeedback(response), textBox_device0);
        }

        //Print the channel response to the textbox
        void d0channel0_channelResponse(ANT_Response response)
        {
            //threadSafePrintLine(decodeChannelFeedback(response), textBox_device0);
            antEventArgs aEA = new antEventArgs();
            aEA.data = response.messageContents;

            if (null != channelMessageHandler)
                channelMessageHandler(this, aEA);
            

        }

/*        void threadSafePrintLine(String stringToPrint, TextBox boxToPrintTo)
        {
            //We need to put this on the dispatcher because sometimes it is called by the feedback thread
            //If you set the priority to 'background' then it never interferes with the UI interaction if you have a high message rate (we don't have to worry about it in the demo)
            boxToPrintTo.Dispatcher.BeginInvoke(new dAppendText(boxToPrintTo.AppendText),
                System.Windows.Threading.DispatcherPriority.Background, stringToPrint + Environment.NewLine);
        }
*/
        //This function decodes the message code into human readable form, shows the error value on failures, and also shows the raw message contents
        String decodeDeviceFeedback(ANT_Response response)
        {
            string toDisplay = "Device: ";

            //The ANTReferenceLibrary contains all the message and event codes in user-friendly enums
            //This allows for more readable code and easy conversion to human readable strings for displays

            // So, for the device response we first check if it is an event, then we check if it failed, 
            // and display the failure if that is the case. "Events" use message code 0x40.
            if (response.responseID == (byte)ANT_ReferenceLibrary.ANTMessageID.RESPONSE_EVENT_0x40)
            {
                //We cast the byte to its messageID string and add the channel number byte associated with the message
                toDisplay += (ANT_ReferenceLibrary.ANTMessageID)response.messageContents[1] + ", Ch:" + response.messageContents[0];
                //Check if the eventID shows an error, if it does, show the error message
                if ((ANT_ReferenceLibrary.ANTEventID)response.messageContents[2] != ANT_ReferenceLibrary.ANTEventID.RESPONSE_NO_ERROR_0x00)
                    toDisplay += Environment.NewLine + ((ANT_ReferenceLibrary.ANTEventID)response.messageContents[2]).ToString();
            }
            else   //If the message is not an event, we just show the messageID
                toDisplay += ((ANT_ReferenceLibrary.ANTMessageID)response.responseID).ToString();

            //Finally we display the raw byte contents of the response, converting it to hex
            toDisplay += Environment.NewLine + "::" + Convert.ToString(response.responseID, 16) + ", " + BitConverter.ToString(response.messageContents) + Environment.NewLine;
            return toDisplay;
        }

        String decodeChannelFeedback(ANT_Response response)
        {

            StringBuilder stringToPrint;    //We use a stringbuilder for speed and better memory usage, but, it doesn't really matter for the demo.
            stringToPrint = new StringBuilder("Channel: ", 100); //Begin the string and allocate some more space

            //In the channel feedback we will get either RESPONSE_EVENTs or receive events,
            //If it is a response event we display what the event was and the error code if it failed.
            //Mostly, these response_events will all be broadcast events from a Master channel.     
            if (response.responseID == (byte)ANT_ReferenceLibrary.ANTMessageID.RESPONSE_EVENT_0x40)
                stringToPrint.AppendLine(((ANT_ReferenceLibrary.ANTEventID)response.messageContents[2]).ToString());
            else   //This is a receive event, so display the ID
                stringToPrint.AppendLine("Received " + ((ANT_ReferenceLibrary.ANTMessageID)response.responseID).ToString());

            //Always print the raw contents in hex, with leading '::' for easy visibility/parsing
            //If this is a receive event it will contain the payload of the message
            stringToPrint.Append("  :: ");
            stringToPrint.Append(Convert.ToString(response.responseID, 16));
            stringToPrint.Append(", ");
            stringToPrint.Append(BitConverter.ToString(response.messageContents) + Environment.NewLine);

            if (response.responseID == (byte)ANT_ReferenceLibrary.ANTMessageID.BROADCAST_DATA_0x4E)
            {
//                if (log != null)
//                    log.WriteLine(BitConverter.ToString(response.messageContents));
            }

/*
*/
            return stringToPrint.ToString();
        }

    }
}
