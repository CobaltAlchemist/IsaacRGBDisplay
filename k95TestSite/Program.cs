using System;
using System.Linq;
using System.Drawing;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Windows.Input;
using CUE.NET;
using CUE.NET.Devices;
using CUE.NET.Devices.Keyboard;
using CUE.NET.Devices.Keyboard.Enums;
using System.Collections;

namespace k95TestSite
{
    class Program
    {
        //volatile so that both threads can use them
        static volatile int[] hearts;
        static volatile string blackHearts;
        static volatile bool connected;
        const int Freq = 200;
        static void Main(string[] args)
        {
            hearts = new int[4];
            blackHearts = "NaN";
            connected = false;
            //Make a new thread to handle all the back end operations.
            Thread opThread = new Thread(new ThreadStart(() =>
            {
                //establish a new connection to the keyboard (in my case the k95) and check if it's null
                IsaacKeyboard k95 = new IsaacKeyboard();
                if (!k95.isNull)
                {
                    //establish a connection to BoI:A and start the main loop
                    IsaacInterface GameInterface = new IsaacInterface();
                    while (true)
                    {
                        //make sure the process still exists otherwise why bother with this thread
                        if (Process.GetProcessesByName("isaac-ng")[0] != null)
                        {
                            //Setting up all the intermediate data
                            byte[][] data = GameInterface.GetData();
                            int maxH, redH, soulH, eterH;
                            string blackH = "";
                            //Inputting the data into the main variables
                            maxH = BitConverter.ToInt32(data[0], 0);
                            redH = BitConverter.ToInt32(data[1], 0);
                            eterH = BitConverter.ToInt32(data[2], 0);
                            soulH = BitConverter.ToInt32(data[3], 0);
                            for (int i = 3; i >= 0; i--)
                            {
                                //7 will look like 000001110000000etc so you have to reverse the order you read it...
                                blackH += Convert.ToString(data[4][i], 2).PadLeft(8, '0');
                            }
                            //...and reverse the whole thing to get 111000000000etc
                            blackH = new string(blackH.Reverse().ToArray());
                            hearts[0] = maxH; hearts[1] = redH; hearts[2] = eterH; hearts[3] = soulH;
                            blackHearts = blackH;
                            //calculate a bool to make sure the data is usable
                            bool validData = GameInterface.AnalyzeData(maxH, redH, eterH, soulH, blackH);
                            if (validData)
                            {
                                //it's valid, so we're connected and we should input the data
                                connected = true;
                                k95.InputData(maxH, redH, eterH, soulH, blackH);
                            }
                            else
                            {
                                //it's broken so it's not connected, lets slow down this thread, and try to establish a new connection
                                connected = false;
                                Thread.Sleep(2000);
                                GameInterface = new IsaacInterface();
                            }
                            //the keyboard class will be paused if it's not valid data, but still tick so it can do some cleanup
                            k95.Pause = !validData;
                            k95.Tick();
                        }
                        
                        Thread.Sleep(Freq);
                    }
                }
           }));
            opThread.Start();
            //simple thing to check for a quit
            bool end = false;
            while(!end)
            {
                Console.Clear();
                Console.WriteLine("-------------------------------------");
                Console.WriteLine("----- Corsair k95 Isaac Rebirth -----");
                if (connected)
                    Console.WriteLine("--------------Connected--------------");
                else
                    Console.WriteLine("------------Not Connected------------");
                Console.WriteLine("Max Hearts:     {0}", hearts[0]);
                Console.WriteLine("Red Hearts:     {0}", hearts[1]);
                Console.WriteLine("Eternal Hearts: {0}", hearts[2]);
                Console.WriteLine("Soul Hearts:    {0}", hearts[3]);
                Console.WriteLine("Black Hearts:   {0}", blackHearts);
                Console.WriteLine("Press ESC to exit");
                //if the thread died somehow, we might as well end this one too
                if (!opThread.IsAlive)
                { break; }
                //if the user presses a key, lets find out which one it is!a
                if(Console.KeyAvailable)
                {
                    ConsoleKeyInfo key = Console.ReadKey(true);
                    switch (key.Key)
                    {
                        case ConsoleKey.Escape:
                            end = true;
                            break;
                        default:
                            break;
                    }
                }
                //no need to hog cpu time
                Thread.Sleep(Freq);
            }
            //if the thread is still alive, then the keyboard might have lost connection
            //***More debugging needed***
            if (!opThread.IsAlive)
            {
                Console.WriteLine("Error Occured, Check Keyboard");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
            //close the other thread and make a nice message
            opThread.Abort();
            Console.Clear();
            Console.WriteLine("-------------------------------------");
            Console.WriteLine("----- Corsair k95 Isaac Rebirth -----");
            Console.WriteLine("-------------------------------------");
            Console.WriteLine("Thanks for playing!");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();

        }
    }
}
