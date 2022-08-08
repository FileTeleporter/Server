using System;
using System.Collections.Generic;
using System.Text;

class Constants
{
    public const int TICKS_PER_SEC = 30; // How many ticks per second
    public const float MS_PER_TICK = 1000f / TICKS_PER_SEC; // How many milliseconds per tick
    public const int BUFFER_FOR_FILE = 1048576;
    public const int SEND_FILE_PORT = 60589;
    public const int DATA_BUFFER_SIZE = 4096;
    public const int DISCOVERY_PORT = 56237;
    public const int MAIN_PORT = 26950;
}
