using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace IQFEED.Client
{
    /// <summary>
    /// Main class for feed communication.
    /// </summary>
    class ClientApp
    {
        #region Fields && Properties

        /// <summary>
        /// Holds IQConnect related configurations
        /// </summary>
        public IQConnectConfig ConnectConfig { get; set; } = new IQConnectConfig();

        /// <summary>
        /// File path for IOConnect exe
        /// </summary>
        public string IQConnectFilePath { get; set; }

        /// <summary>
        /// Holds symbol related data.
        /// </summary>
        public SymbolContainer SymbolContainer { get; set; }


        /// <summary>
        /// Holds tcp client object
        /// </summary>
        private TcpClient tcpClient;
        /// <summary>
        /// Network stream over connected socket
        /// </summary>
        private NetworkStream networkStream;
        /// <summary>
        /// Reader stream to read data from socket
        /// </summary>
        private StreamReader reader;
        /// <summary>
        /// Writer stream to write data to socket
        /// </summary>
        private StreamWriter writer;
        /// <summary>
        /// Watch variable to continue read/write operations
        /// </summary>
        private volatile bool isRunning;

        #endregion

        /// <summary>
        /// Begin read data communication work.
        /// </summary>
        public void Start()
        {
            StartIQConnectProcess();
            isRunning = true;
            new Thread(Run) { IsBackground = true, Name = "Communicator Thread" }.Start();
        }

        public void Stop()
        {
            isRunning = false;
        }

        /// <summary>
        /// Start IQConnect Process using supplied connect configurations.
        /// </summary>
        private void StartIQConnectProcess()
        {
            System.Diagnostics.Process.Start(IQConnectFilePath, ConnectConfig.StartArguments);
        }

        /// <summary>
        /// Runs actual data communication logic
        /// </summary>
        private void Run()
        {
            try
            {
                // Create default tcp client.
                using (tcpClient = new TcpClient())
                {
                    // Establishes connection to server.
                    ConnectToServer();

                    // Create network stream over connected socket.
                    using (networkStream = tcpClient.GetStream())
                    {
                        reader = new StreamReader(networkStream, Encoding.ASCII);
                        writer = new StreamWriter(networkStream, Encoding.ASCII) { AutoFlush = true };
                        
                        ConfirmEstablishedConnection();
                        SendRequests();
                        ReadResponse();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Something wrong happened :- Exception : {0}", e);
            }
        }

        /// <summary>
        /// Establishes connection to server.
        /// </summary>
        private void ConnectToServer()
        {
            // Create server end point from configuration.
            var serverEndPoint = new IPEndPoint(ConnectConfig.ServerAddress, ConnectConfig.ServerPort);

            Console.WriteLine("Connection Started");
            // Connect client socket to server.
            tcpClient.Connect(serverEndPoint);
            Console.WriteLine("Connection Established");
        }

        /// <summary>
        /// Confirms already connected connection by checking "SERVER CONNECTED" message.
        /// </summary>
        private void ConfirmEstablishedConnection()
        {
            // Continue until it is running or server sent CONNECTED message.
            while(isRunning)
            {
                // Read data from underline socket
                var dataLine = reader.ReadLine();
                // if EOF reached throw exception.
                if (dataLine == null)
                    throw new EndOfStreamException();
                Console.WriteLine("Read Data : " + dataLine);
                if(dataLine.Contains("SERVER CONNECTED"))
                {
                    Console.WriteLine("Connection Confirmed");
                    break;
                }
            }
        }

        /// <summary>
        /// Sends requests with protocol, update fields and symbols.
        /// </summary>
        private void SendRequests()
        {
            // Send protocol request
            SendRequest("S,SET PROTOCOL,5.2\r\n");
            
            // Send update fields request
            SendRequest("S,SELECT UPDATE FIELDS,Symbol,Most Recent Trade,Most Recent Trade Time\r\n");
            
            // Send symbols request
            foreach (var symbol in SymbolContainer.Symbols.Keys)
            {
                SendRequest(string.Format("t{0}\r\n", symbol));
            }
        }

        /// <summary>
        /// Sends request for given command 
        /// </summary>
        /// <param name="requestCommand">Request command</param>
        private void SendRequest(string requestCommand)
        {
            writer.Write(requestCommand);
            Console.WriteLine(string.Format("Request Successfully Sent:\r\n{0}", requestCommand));
        }


        /// <summary>
        /// Reads response until user requests to stop
        /// </summary>
        private void ReadResponse()
        {
            // Keep it running until user says to stop.
            while(isRunning)
            {
                var responseLine = reader.ReadLine();
                // If server closed connection it will receive null. In that case stop it.
                if (responseLine == null)
                    break;

                // Process single response line.
                ProcessResponse(responseLine);
            }
        }

        /// <summary>
        /// Processes single response line.
        /// </summary>
        /// <param name="responseLine">Response line.</param>
        private void ProcessResponse(string responseLine)
        {
            //FORMAT OF responseLine: "Q/S, SYMBOL, LAST TRADE PRICE, TIME,\n"
            Console.WriteLine(responseLine);
            var responseFields = responseLine.Split(',');
            // Check
            if (responseFields.Length < 4)
                return;

            if (responseFields[0] == "Q" || responseFields[0] == "P")
            {
                // Validate whether recived response is for our request.
                if(SymbolContainer.Symbols.TryGetValue(responseFields[1], out Symbol symbol))
                {
                    // Parse current price and update to symbol.
                    symbol.CurrentPrice = double.Parse(responseFields[2].Trim());
                    // Parse last trade time and update to symbol.
                    symbol.LastTradeTime = TimeSpan.Parse(responseFields[3].Trim());
                }
            }
        }
    }
}
