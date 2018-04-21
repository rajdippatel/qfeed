using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace IQFEED.Client
{
    class Program
    {
        /// <summary>
        /// I am entry point...
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            // Validate symboles file path
            var symbolFilePath = @"C:\symbols.txt";
            if (!File.Exists(symbolFilePath)) {
                Console.WriteLine("Symbols File Missing :- File Path : " + symbolFilePath);
                Console.ReadLine();
                return;
            }

            var iQConnectFilePath = "IQConnect.exe";
            if (!File.Exists(iQConnectFilePath))
            {
                Console.WriteLine("IQConnect File Missing :- File Path : " + iQConnectFilePath);
                Console.ReadLine();
                return;
            }
            
            try
            {
                // Create input symbole container object.
                var symboleContainer = new SymbolContainer();
                symboleContainer.Load(symbolFilePath);

                // Create client app and start it.
                var clientApp = new ClientApp()
                {
                    SymbolContainer = symboleContainer,
                    IQConnectFilePath = iQConnectFilePath
                };
                clientApp.Start();

                Console.WriteLine("Client App Started");
                // Keep program open until user requests to close it.
                Console.WriteLine("Type '0' to exit");
                string input;
                do
                {
                    input = Console.ReadLine();
                } while (input != "0");
            }
            catch(Exception e)
            {
                Console.WriteLine("Something went wrong :- " + e);
                Console.ReadLine();
            }
        }
    }
}
