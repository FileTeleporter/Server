using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileteleporterTransfert.Tools
{
    public static class EZConsole
    {

        private static Dictionary<string, Header> senderHeaders = new Dictionary<string, Header>();

        private class Header
        {
            public Header(string headerType, ConsoleColor headerColor, ConsoleColor textColor)
            {
                this.header = headerType;
                this.headerColor = headerColor;
                this.textColor = textColor;
            }
            public string header;
            public ConsoleColor headerColor;
            public ConsoleColor textColor;
        }


        /// <summary>
        /// Write the text to the console
        /// </summary>
        /// <param name="text">The text to write</param>
        public static void WriteLine(string text)
        {
            ThreadManager.ExecuteOnMainThread(() =>
            {
                Console.WriteLine(text);
            });
        }

        /// <summary>
        /// Write the text in color in the console
        /// </summary>
        /// <param name="text">The text to write</param>
        /// <param name="color">The wanted color</param>
        public static void WriteLine(string text, ConsoleColor color)
        {
            ThreadManager.ExecuteOnMainThread(() =>
            {
                ConsoleColor defaultColor = Console.ForegroundColor;
                Console.ForegroundColor = color;
                Console.WriteLine(text);
                Console.ForegroundColor = defaultColor;
            });
        }

        /// <summary>
        /// Write the text to the console with the specified header customization
        /// </summary>
        /// <param name="headerType">a string representing a header type already created</param>
        /// <param name="text">The text to write</param>
        public static void WriteLine(string headerType, string text)
        {
            ThreadManager.ExecuteOnMainThread(() =>
            {
                if (senderHeaders.ContainsKey(headerType))
                {
                    Header header = senderHeaders[headerType];
                    ConsoleColor defaultColor = Console.ForegroundColor;
                    Console.ForegroundColor = header.headerColor;
                    Console.Write(header.header + " ");
                    Console.ForegroundColor = header.textColor;
                    Console.Write(text + Environment.NewLine);
                    Console.ForegroundColor = defaultColor;
                }
                else
                {
                    Console.WriteLine(text);
                }
            });
        }

        /// <summary>
        /// Add an header to customize text when written to the console
        /// </summary>
        /// <param name="headerType">The name of the header you want to use</param>
        /// <param name="headerText">The text that'll be written before the main text</param>
        /// <param name="headerColor">The color of the text before the main text</param>
        /// <param name="textColor">The color of the main text</param>
        public static void AddHeader(string headerType, string headerText, ConsoleColor headerColor, ConsoleColor textColor)
        {
            if (senderHeaders.ContainsKey(headerType))
                senderHeaders[headerType] = new Header(headerText, headerColor, textColor);
            else
                senderHeaders.Add(headerType, new Header(headerText, headerColor, textColor));
        }
    }
}
