using System.Collections.Generic;
using System.IO;

namespace IQFEED.Client
{
    class SymbolContainer
    {
        public Dictionary<string, Symbol> Symbols = new Dictionary<string, Symbol>();

        /// <summary>
        /// Loads symbols file into memory
        /// </summary>
        /// <param name="filename">File Name or Path</param>
        public void Load(string filename)
        {
            // Clears existing symboles, if any.
            Symbols.Clear();
            foreach (var line in File.ReadAllLines(filename))
            {
                Symbols[line] = new Symbol(line);
            }            
        }
    }
}
