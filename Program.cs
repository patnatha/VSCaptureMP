/*
 * This file is part of VitalSignsCaptureMP v1.011.
 * Copyright (C) 2017-22 John George K., xeonfusion@users.sourceforge.net

    VitalSignsCaptureMP is free software: you can redistribute it and/or modify
    it under the terms of the GNU Lesser General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    VitalSignsCaptureMP is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Lesser General Public License for more details.

    You should have received a copy of the GNU Lesser General Public License
    along with VitalSignsCaptureMP.  If not, see <http://www.gnu.org/licenses/>.*/


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.IO.Ports;
using System.Threading;

namespace VSCaptureMP
{
    class Program
    {
        public static string redcapUrl;
        public static string redcapToken;
        public static string redcapOR;

        static void Main(string[] args)
        {
            Console.WriteLine("VitalSignsCaptureMP (C)2017-22 John George K.");
            Console.WriteLine("For command line usage: -help");
            Console.WriteLine();

            var parser = new CommandLineParser();
            parser.Parse(args);

            if (parser.Arguments.ContainsKey("help"))
            {
                Console.WriteLine("VSCaptureMP.exe -mode[number] -port [portname] -interval [number]");
                Console.WriteLine(" -waveset[number] -export[number] -url [name] -scale[number]");
                Console.WriteLine("-mode <Set connection mode LAN>");
                Console.WriteLine("-port <Set IP address>");
                Console.WriteLine("-interval <Set numeric transmission interval option>");
                Console.WriteLine("-waveset <Set waveform request priority option>");
                Console.WriteLine("-export <Set data export CSV or RedCap option>");
                Console.WriteLine("-scale <Set waveform data scale or calibrate option>");
                Console.WriteLine("Nathan's Version");
                Console.WriteLine();
                return;
            }

            string sConnectset;
            if (parser.Arguments.ContainsKey("mode"))
            {
                sConnectset = parser.Arguments["mode"][0];
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("1. Connect via LAN port");
                Console.WriteLine();
                Console.Write("Choose connection mode (1):");

                sConnectset = Console.ReadLine();
            }

            int nConnectset = 1;
            if (sConnectset != "") nConnectset = Convert.ToInt32(sConnectset);

            if (nConnectset == 1) ConnectviaLAN(args);

            if (false)
            {
                ConsoleKeyInfo cki;

                do
                {
                    cki = Console.ReadKey(true);

                }
                while (cki.Key != ConsoleKey.Escape);
            } else
            {
                while (true)
                {
                    //Infinte blocking
                    Thread.Sleep(10);
                }
            }
        }

        public class UdpState
        {
            //Udp client
            public MPudpclient udpClient;
            //RemoteIP
            public IPEndPoint remoteIP;
        }

        private static ManualResetEvent receiveDone = new ManualResetEvent(false);

        public static void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the state object and the client   
                // from the asynchronous state object.  
                UdpState state = (UdpState)ar.AsyncState;

                MPudpclient Client = state.udpClient;

                //string path = Path.Combine(Directory.GetCurrentDirectory(), "MPrawoutput.txt");

                // Read data from the remote device.  
                byte[] readbuffer = Client.EndReceive(ar, ref state.remoteIP);
                int bytesRead = readbuffer.GetLength(0);

                if (bytesRead > 0)
                {
                    // There might be more data, so store the data received so far.  
                    //Client.ByteArrayToFile(path, readbuffer, bytesRead);

                    Client.ReadData(readbuffer);

                    //  Get the rest of the data.  
                    Client.BeginReceive(new AsyncCallback(ReceiveCallback), state);
                }
                else
                {
                    // All the data has arrived; put it in response.  
                    // Signal that all bytes have been received.  
                    receiveDone.Set();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public static void ReadData(object sender, byte[] readbuffer)
        {
            (sender as MPudpclient).ReadData(readbuffer);
        }

        public static void WaitForSeconds(int nsec)
        {
            DateTime dt = DateTime.Now;
            DateTime dt2 = dt.AddSeconds(nsec);
            do
            {
                dt = DateTime.Now;
            }
            while (dt2 > dt);
        }

        public static void ConnectviaLAN(string[] args)
        {
            var parser = new CommandLineParser();
            parser.Parse(args);

            string sIntervalset;
            if (parser.Arguments.ContainsKey("interval"))
            {
                sIntervalset = parser.Arguments["interval"][0];
            }
            else
            {
                Console.WriteLine("You may connect an Ethernet cable to the Philips Intellivue monitor LAN port");
                Console.WriteLine("Note the IP address from the Network Status menu in the monitor");

                Console.WriteLine();
                Console.WriteLine("Numeric Data Transmission sets:");
                Console.WriteLine("1. 1 second (Real time)");
                Console.WriteLine("2. 12 second (Averaged)");
                Console.WriteLine("3. 1 minute (Averaged)");
                Console.WriteLine("4. 5 minute (Averaged)");
                Console.WriteLine("5. Single poll");

                Console.WriteLine();
                Console.Write("Choose Data Transmission interval (1-5):");

                sIntervalset = Console.ReadLine();
            }

            int[] setarray = { 1000, 12000, 60000, 300000, 0 }; //milliseconds
            short nIntervalset = 2;
            int nInterval = 12000;
            if (sIntervalset != "") nIntervalset = Convert.ToInt16(sIntervalset);
            if (nIntervalset > 0 && nIntervalset < 6) nInterval = setarray[nIntervalset - 1];

            string sDataExportset;
            if (parser.Arguments.ContainsKey("export"))
            {
                sDataExportset = parser.Arguments["export"][0];
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("Data export options:");
                Console.WriteLine("1. Export as CSV files");
                Console.WriteLine("2. Export to RedCap");
                Console.WriteLine();
                Console.Write("Choose data export option (1-2):");

                sDataExportset = Console.ReadLine();

            }

            int nDataExportset = 1;
            if (sDataExportset != "") nDataExportset = Convert.ToInt32(sDataExportset);
            if (nDataExportset == 2)
            {
                string[] lines = File.ReadAllLines("token.auth");
                for (int i = 0; i < lines.Length; i++)
                {
                    if(i == 0)
                    {
                        redcapOR = lines[i].TrimEnd();
                    }
                    else if(i == 1)
                    {
                        redcapUrl = lines[i].TrimEnd();
                    }
                    else if(i == 2)
                    {
                        redcapToken = lines[i].TrimEnd();
                    } else
                    {
                        break;
                    }
                }
            }

            int nCSVset = 3;

            string sWaveformSet;
            if (parser.Arguments.ContainsKey("waveset"))
            {
                sWaveformSet = parser.Arguments["waveset"][0];
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("Waveform data export options:");
                Console.WriteLine("0. None");
                Console.WriteLine("1. ECG I, II, III");
                Console.WriteLine("2. ECG II, ABP, ART IBP, PLETH, CVP, RESP");
                Console.WriteLine("3. ECG AVR, ECG AVL, ECG AVF");
                Console.WriteLine("4. ECG V1, ECG V2, ECG V3");
                Console.WriteLine("5. ECG V4, ECG V5, ECG V6");
                Console.WriteLine("6. EEG1, EEG2, EEG3, EEG4");
                Console.WriteLine("7. ABP, ART IBP");
                Console.WriteLine("8. Compound ECG, PLETH, ABP, ART IBP, CVP, CO2");
                Console.WriteLine("9. ECG II, ART IBP, ICP, ICP2, CVP, TEMP BLOOD");
                Console.WriteLine("10. Vuelink/IntelliBridge Wave1, Wave2, Wave3, Wave4");
                Console.WriteLine("11. All");

                Console.WriteLine();
                Console.WriteLine("Selecting all waves can lead to data loss due to bandwidth issues");
                Console.Write("Choose Waveform data export priority option (0-11):");

                sWaveformSet = Console.ReadLine();

            }

            short nWaveformSet = 0;
            if (sWaveformSet != "") nWaveformSet = Convert.ToInt16(sWaveformSet);

            string sWavescaleSet;
            if (parser.Arguments.ContainsKey("scale"))
            {
                sWavescaleSet = parser.Arguments["scale"][0];
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("Waveform data export scale and calibrate options:");
                Console.WriteLine("1. Export scaled values");
                Console.WriteLine("2. Export calibrated values");
                Console.WriteLine();
                Console.Write("Choose Waveform data export scale option (1-2):");

                sWavescaleSet = Console.ReadLine();

            }

            short nWavescaleSet = 1;
            if (sWavescaleSet != "") nWavescaleSet = Convert.ToInt16(sWavescaleSet);

            // Create a new UdpClient object with default settings.
            MPudpclient _MPudpclient = MPudpclient.getInstance;

            string IPAddressRemote;
            if (parser.Arguments.ContainsKey("port"))
            {
                IPAddressRemote = parser.Arguments["port"][0];
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("Enter the target IP address of the monitor assigned by DHCP:");

                IPAddressRemote = Console.ReadLine();
            }

            Console.WriteLine("Connecting to {0}...", IPAddressRemote);
            Console.WriteLine();
            Console.WriteLine("Requesting Transmission set {0} from monitor", nIntervalset);
            Console.WriteLine();
            Console.WriteLine("Data will be written to CSV file MPDataExport.csv in same folder");
            //Console.WriteLine();
            //Console.WriteLine("Press Escape button to Stop");

            if (nDataExportset > 0 && nDataExportset < 4) _MPudpclient.m_dataexportset = nDataExportset;

            if (nCSVset > 0 && nCSVset < 4) _MPudpclient.m_csvexportset = nCSVset;

            if (nWavescaleSet == 1) _MPudpclient.m_calibratewavevalues = false;
            if (nWavescaleSet == 2) _MPudpclient.m_calibratewavevalues = true;

            if (IPAddressRemote != "")
            {
                //Default Philips monitor port is 24105
                _MPudpclient.m_remoteIPtarget = new IPEndPoint(IPAddress.Parse(IPAddressRemote), 24105);
                _MPudpclient.m_RCUrl = redcapUrl;
                _MPudpclient.m_RCToken = redcapToken;
                _MPudpclient.m_RCWhichOR = redcapOR;

                try
                {
                    _MPudpclient.Connect(_MPudpclient.m_remoteIPtarget);

                    UdpState state = new UdpState();
                    state.udpClient = _MPudpclient;
                    state.remoteIP = _MPudpclient.m_remoteIPtarget;

                    //Send AssociationRequest message
                    _MPudpclient.SendWaveAssociationRequest();

                    //string path = Path.Combine(Directory.GetCurrentDirectory(), "MPrawoutput.txt");

                    //Receive AssociationResult message and MDSCreateEventReport message
                    byte[] readassocbuffer = _MPudpclient.Receive(ref _MPudpclient.m_remoteIPtarget);
                    //_MPudpclient.ByteArrayToFile(path, readassocbuffer, readassocbuffer.GetLength(0));


                    byte[] readmdsconnectbuffer = _MPudpclient.Receive(ref _MPudpclient.m_remoteIPtarget);
                    //_MPudpclient.ByteArrayToFile(path, readmdsconnectbuffer, readmdsconnectbuffer.GetLength(0));

                    //Send MDSCreateEventResult message
                    _MPudpclient.ProcessPacket(readmdsconnectbuffer);

                    //Send Extended PollData Requests cycled every second
                    Task.Run(() => _MPudpclient.SendCycledExtendedPollDataRequest(nInterval));

                    if (nWaveformSet != 0)
                    {
                        _MPudpclient.GetRTSAPriorityListRequest();
                        if (nWaveformSet != 11)
                        {
                            _MPudpclient.SetRTSAPriorityList(nWaveformSet);
                        }

                        Task.Run(() => _MPudpclient.SendCycledExtendedPollWaveDataRequest(nInterval));
                    }

                    //Recheck MDS Attributes
                    Task.Run(() => _MPudpclient.RecheckMDSAttributes(nInterval));

                    //Keep Connection Alive
                    Task.Run(() => _MPudpclient.KeepConnectionAlive(nInterval));

                    //Receive PollDataResponse message
                    _MPudpclient.BeginReceive(new AsyncCallback(ReceiveCallback), state);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error opening/writing to UDP port :: " + ex.Message, "Error!");
                }
            }
            else
            {
                Console.WriteLine("Invalid IP Address");
            }
        }

        static void p_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {

            ReadData(sender);

        }

        public static void ReadData(object sender)
        {
            try
            {
                (sender as MPSerialPort).ReadBuffer();

            }
            catch (TimeoutException) { }
        }

        public static void WaitForMilliSeconds(int nmillisec)
        {
            DateTime dt = DateTime.Now;
            DateTime dt2 = dt.AddMilliseconds(nmillisec);
            do
            {
                dt = DateTime.Now;
            }
            while (dt2 > dt);

        }

        public class CommandLineParser
        {
            public CommandLineParser()
            {
                Arguments = new Dictionary<string, string[]>();
            }

            public IDictionary<string, string[]> Arguments { get; private set; }

            public void Parse(string[] args)
            {
                string currentName = "";
                var values = new List<string>();
                foreach (string arg in args)
                {
                    if (arg.StartsWith("-", StringComparison.InvariantCulture))
                    {
                        if (currentName != "" && values.Count != 0)
                            Arguments[currentName] = values.ToArray();

                        else
                        {
                            values.Add("");
                            Arguments[currentName] = values.ToArray();
                        }
                        values.Clear();
                        currentName = arg.Substring(1);
                    }
                    else if (currentName == "")
                        Arguments[arg] = new string[0];
                    else
                        values.Add(arg);
                }

                if (currentName != "")
                    Arguments[currentName] = values.ToArray();
            }

            public bool Contains(string name)
            {
                return Arguments.ContainsKey(name);
            }
        }
    }

}
