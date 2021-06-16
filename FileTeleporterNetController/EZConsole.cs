/*
 * Copyright (C) 2021  Jolan Aklin And Yohan Zbinden

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileTeleporterNetController.Tools
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
            Console.WriteLine(text);
        }

        /// <summary>
        /// Write the text in color in the console
        /// </summary>
        /// <param name="text">The text to write</param>
        /// <param name="color">The wanted color</param>
        public static void WriteLine(string text, ConsoleColor color)
        {
            ConsoleColor defaultColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ForegroundColor = defaultColor;
        }

        /// <summary>
        /// Write the text to the console with the specified header customization
        /// </summary>
        /// <param name="headerType">a string representing a header type already created</param>
        /// <param name="text">The text to write</param>
        public static void WriteLine(string headerType, string text)
        {
            Console.SetCursorPosition(0, Console.CursorTop);
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
        }

        public static void Write(string text, ConsoleColor color)
        {
            ConsoleColor defaultColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(text);
            Console.ForegroundColor = defaultColor;
        }

        public static void Write(string headerType, string text)
        {
            if (senderHeaders.ContainsKey(headerType))
            {
                Header header = senderHeaders[headerType];
                ConsoleColor defaultColor = Console.ForegroundColor;
                Console.ForegroundColor = header.headerColor;
                Console.Write(header.header + " ");
                Console.ForegroundColor = header.textColor;
                Console.Write(text);
                Console.ForegroundColor = defaultColor;
            }
            else
            {
                Console.WriteLine(text);
            }
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
