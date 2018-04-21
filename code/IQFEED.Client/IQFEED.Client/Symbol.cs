using System;

namespace IQFEED.Client
{
    /// <summary>
    /// Data structure to hold data related to single symbol.
    /// </summary>
    class Symbol
    {
        public string Name { get; private set; }
        public double CurrentPrice { get; set; }
        public int TotalVolume { get; set; }
        public TimeSpan LastTradeTime { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name"></param>
        public Symbol(string name)
        {
            this.Name = name;
        }
    }
}
