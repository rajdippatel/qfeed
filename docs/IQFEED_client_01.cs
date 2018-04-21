using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace IQFEED_client_01
{
    class client_01
    {
        // global variables for network communication with IQFEED socket 
        static AsyncCallback my_callback_delegate;
        static Socket my_socket;
        static byte[] my_socket_buffer = new byte[8096];  // we create the socket buffer global for performance
        static string left_over_message = "";  // stores unprocessed data between reads from the socket
        static bool need_to_call_beginReceive = true;  // flag for tracking when a call to BeginReceive needs called

        //global variables for other things
        static string symbol_filename = @"C:\symbols.txt";
        static data_holder DATA; //object that will hold the data for ALL symbols and the list of symbols


        static void Main(string[] args)
        {
            Console.WriteLine("in client program");
            DATA = new data_holder(symbol_filename); //initialize object that will hold the processed data

            //initialize connection with IQServers
            string customer_product_id = "enter your ID here";
            string LoginID = "enter your login here";
            string Password = "enter your password here";
            string product_version = "0.1";
            string sArguments = string.Format("‑product {0} ‑version {1} ‑login {2} ‑password {3} ‑autoconnect ‑savelogininfo", customer_product_id, product_version, LoginID, Password);
            System.Diagnostics.Process.Start("IQConnect.exe", sArguments);

            // ===============  create the socket, connect to IQFEED, and wait for data ==================
            my_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress ipLocalhost = IPAddress.Parse("127.0.0.1");
            int iPort = 5009; //5009 for IQFEED LEVEL1
            IPEndPoint ipendLocalhost = new IPEndPoint(ipLocalhost, iPort);

            try
            {
                my_socket.Connect(ipendLocalhost); //connect to LEVEL1 Server
                confirm_connected();
                send_requests();
                WaitForData();  //start the cycle of waiting for data and receiving data
            }
            catch (SocketException ex)
            {
                Console.WriteLine("ERROR  {0}", ex.Message);
                Console.ReadLine();
            }

            // keep program open by waiting to readline.  When 0 is entered, program will shut down
            Console.WriteLine("Type '0' to exit");
            string input = Console.ReadLine();
            while (input != "0")
            {
                input = Console.ReadLine();
            }
            my_socket.Close();
        }

        //CODE in confirm_connected is essentially copied from IQFEED Sample programs, with only sublte changes
        private static void confirm_connected()
        {
            Console.WriteLine("waiting to get connection confirmation");

            // Need to read off that socket until we get a message indicating that IQFeed is connected to the server
            byte[] szSocketBuffer = new byte[Int16.MaxValue];
            // since we are using a blocking socket, a Receive value of zero indicates that the socket has been closed.
            int iBytesReceived = 0;
            bool bShutDown = false;
            while (!bShutDown && (iBytesReceived = my_socket.Receive(szSocketBuffer)) > 0)
            {
                // with this connection, we aren't worried about efficiency of data processing
                // since there isn't going to be a lot of data delivered to the socket.  As a result
                // we just read the data off the socket and display it to the console and then process it.
                string sData = Encoding.ASCII.GetString(szSocketBuffer, 0, iBytesReceived);
                Console.WriteLine(sData);

                if (sData.Contains("SERVER CONNECTED"))
                {
                    Console.WriteLine("IQFeed is connected to the server.");
                    bShutDown = true;
                }
            }
        }

        private static void send_requests()
        {
            //send protocol
            string set_protocol = "S,SET PROTOCOL,5.2\r\n";
            SendRequestToIQFeed(set_protocol);

            //update fields
            string update_fields_request = "S,SELECT UPDATE FIELDS,Symbol,Most Recent Trade,Most Recent Trade Time\r\n";
            SendRequestToIQFeed(update_fields_request);

            //request symbols
            foreach (string sym in DATA.symbols)
            {
                string watch_request = string.Format("t{0}\r\n", sym); //Begins watching a symbol for Level 1 updates.
                SendRequestToIQFeed(watch_request);
            }

        }

        //CODE in SendRequestToIQFeed is copied from IQFEED Sample programs, just modified to work for console instead of form
        private static void SendRequestToIQFeed(string sCommand)
        {
            // and we send it to the feed via the socket
            byte[] szCommand = new byte[sCommand.Length];
            szCommand = Encoding.ASCII.GetBytes(sCommand);
            int iBytesToSend = szCommand.Length;
            try
            {
                int iBytesSent = my_socket.Send(szCommand, iBytesToSend, SocketFlags.None);
                if (iBytesSent != iBytesToSend)
                {
                    Console.WriteLine(String.Format("Error Sending Request:\r\n{0}", sCommand));
                    Console.ReadLine();
                }
                else
                {
                    Console.WriteLine(String.Format("Request successfully sent:\r\n{0}", sCommand));
                }
            }
            catch (SocketException ex)
            {
                // handle socket errors
                Console.WriteLine(String.Format("Socket Error Sending Request:\r\n{0}\r\n{1}", sCommand, ex.Message));
                Console.ReadLine();
            }
        }

        private static void WaitForData()
        {
            // make sure we have a callback created
            if (my_callback_delegate == null)
            {
                my_callback_delegate = new AsyncCallback(OnReceive);
            }

            // send the notification to the socket.  It is very important that we don't call Begin Reveive more than once per call
            // to EndReceive.  As a result, we set a flag to ignore multiple calls.
            if (need_to_call_beginReceive)
            {
                need_to_call_beginReceive = false;
                // we pass in the sSocketName in the state parameter so that we can verify the socket data we receive is the data we are looking for
                my_socket.BeginReceive(my_socket_buffer, 0, my_socket_buffer.Length, SocketFlags.None, my_callback_delegate, null);
            }
        }

        //SINCE handling of messages with be most CPU intensive, it is important that I have OnReceive optimized as much as possible.
        //I have already improved upon IQFEED's sample application, but wondering if there is a better way???
        private static void OnReceive(IAsyncResult asyn)
        {
            int iReceivedBytes = 0;
            iReceivedBytes = my_socket.EndReceive(asyn);
            // set our flag back to true so we can call begin receive again
            need_to_call_beginReceive = true;

            string sData = Encoding.ASCII.GetString(my_socket_buffer, 0, iReceivedBytes);

            string this_line;
            string final_char = sData.Substring(sData.Length - 1); // if it does NOT equal '\n' then the last record received is not complete
            using (StringReader reader = new StringReader(left_over_message + sData))
            {
                left_over_message = "";
                while ((this_line = reader.ReadLine()) != null)
                {
                    //check to see if there is more data.  Peek returns -1 when no more charecters to be read
                    if (reader.Peek() != -1)
                    {
                        ProcessMessage(this_line);
                    }
                    //otherwise, we are in last line, and need to determine if it is complete or not
                    else
                    {
                        if (final_char == "\n")
                        {
                            ProcessMessage(this_line);
                        }
                        else
                        {
                            left_over_message = this_line;
                        }
                    }
                }

                // call wait for data to notify the socket that we are ready to receive another callback
                WaitForData();
            }
        }

        //NOT SURE IF THERE IS ANYTHING MORE EFFICIENT THAN THE FOLLOWING FUNCTION
        private static void ProcessMessage(string myline)
        {
            //FORMAT OF myline: "Q/S, SYMBOL, LAST TRADE PRICE, TIME,\n"
            Console.WriteLine(myline);
            string[] fields = myline.Split(',');
            if (fields[0] == "Q" || fields[0] == "P")
            {
                int this_index = DATA.symbol_indexer[fields[1]]; //get index for the symbol
                DATA.CURRENT_PRICE[this_index] = Double.Parse(fields[2]);
                DATA.LAST_TRADE_TIME[this_index] = TimeSpan.Parse(fields[3]);
                Console.WriteLine(string.Format("{0}, {1}", DATA.LAST_TRADE_TIME[0], DATA.LAST_TRADE_TIME[1]));
            }

        }
    }

    class data_holder
    {
        // member variables
        public string[] symbols;
        public double[] CURRENT_PRICE;
        public int[] TOTAL_VOLUME;
        public TimeSpan[] LAST_TRADE_TIME;
        public Dictionary<string, int> symbol_indexer;

        //contructor
        public data_holder(string filename)
        {
            symbols = File.ReadAllLines(filename);
            int symbol_count = symbols.Length;

            CURRENT_PRICE = new double[symbol_count];
            TOTAL_VOLUME = new int[symbol_count];
            LAST_TRADE_TIME = new TimeSpan[symbol_count];
            symbol_indexer = new Dictionary<string, int>();
            for (int i = 0; i < symbol_count; i++)
            {
                symbol_indexer.Add(symbols[i], i);
            }
        }

    }
}
