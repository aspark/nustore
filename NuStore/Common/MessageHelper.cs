using System;
using System.Collections.Generic;
using System.Text;

namespace NuStore.Common
{
    class MessageHelper
    {
        private static readonly ConsoleColor _originForegroundColor = Console.ForegroundColor;

        static MessageHelper()
        {

        }

        public static void Warning(string msg, params object[] args)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(msg, args);
            Console.ForegroundColor = _originForegroundColor;
        }

        public static void Error(string msg, params object[] args)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(msg, args);
            Console.ForegroundColor = _originForegroundColor;
        }

        internal static void Info(string msg, params object[] args)
        {
            Console.WriteLine(msg, args);
        }
    }
}
