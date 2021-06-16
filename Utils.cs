using System;

namespace OvergownBot
{
    public class CellFormatException : Exception {}
    
    public class Utils
    {
        public static string R1C1(int x, int y)
        {
            return $"R{1 + x}C{1 + y}";
        }
        
        public static string A1(int x, int y)
        {
            if (x >= 26) throw new InvalidOperationException("Currently columns past Z are not supported for A1 format");
            var cx = (char) ('A' + x);
            return $"{cx}{1+y}";
        }
    }
}