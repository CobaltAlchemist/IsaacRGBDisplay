using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

namespace k95TestSite
{
    class IsaacInterface
    {
        //We only want to read data from isaac
        const int PROCESS_WM_READ = 0x0010;

        //need a couple kernel32.dll functions
        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out]byte[] lpBuffer, int dwSize, out int lpNumberOfBytesRead);

        //some predetermined offsets in the game
        IntPtr MaxHeartsOffset = (IntPtr)0x1b30;
        IntPtr RedHeartsOffset = (IntPtr)0x1b34;
        IntPtr EternalHeartsOffset = (IntPtr)0x1b38;
        IntPtr SoulHeartsOffset = (IntPtr)0x1b3c;
        IntPtr BlackHeartsOffset = (IntPtr)0x1b40;
        //the offset to the player and the process's handle
        IntPtr FinalOffset;
        IntPtr processHandle;

        public IsaacInterface()
        {
            //establish some new variables
            bool success = false;
            Process IsaacAfterbirth;
            IntPtr baseAddress = (IntPtr)0x0;
            //keep trying to connect to the binding of isaac until it works
            while (!success)
            {
                try
                {
                    IsaacAfterbirth = Process.GetProcessesByName("isaac-ng")[0];
                    processHandle = OpenProcess(PROCESS_WM_READ, false, IsaacAfterbirth.Id);
                    baseAddress = IsaacAfterbirth.MainModule.BaseAddress;
                    success = true;
                }
                catch (Exception e)
                {
                    Thread.Sleep(1000);
                }
            }
            //Make a buffer for 4 bytes, the size of our pointers
            byte[] buffer = new byte[4];
            int bytesRead = 0;
            //first offset
            ReadProcessMemory(processHandle, IntPtr.Add(baseAddress, 0x2E4634), buffer, buffer.Length, out bytesRead); //read the memory
            Debug.WriteLine(BitConverter.ToString(buffer.Reverse().ToArray())); //write it for debugging purposes
            IntPtr InterOffset = (IntPtr)Convert.ToInt32(BitConverter.ToString(buffer.Reverse().ToArray(), 0).Replace("-", ""), 16); //set the intermediary pointer
            Debug.WriteLine("Converted Address to " + InterOffset); //write it for debugging purposes again

            //second offset
            ReadProcessMemory(processHandle, IntPtr.Add(InterOffset, 0xB7D8), buffer, buffer.Length, out bytesRead);
            Debug.WriteLine(BitConverter.ToString(buffer.Reverse().ToArray()));
            InterOffset = (IntPtr)Convert.ToInt32(BitConverter.ToString(buffer.Reverse().ToArray(), 0).Replace("-", ""), 16);
            Debug.WriteLine("Converted Address to " + InterOffset);

            //final offset
            ReadProcessMemory(processHandle, IntPtr.Add(InterOffset, 0x0), buffer, buffer.Length, out bytesRead);
            Debug.WriteLine(BitConverter.ToString(buffer.Reverse().ToArray()));
            FinalOffset = (IntPtr)Convert.ToInt32(BitConverter.ToString(buffer.Reverse().ToArray(), 0).Replace("-", ""), 16);
            Debug.WriteLine("Converted Final Address to " + FinalOffset);
        }
        /*
        *MaxHearts
        *RedHearts
        *EternalHearts
        *SoulHearts
        *BlackHearts
        */
        public bool AnalyzeData(int MH, int RH, int EH, int SH, string BH)
        {
            //we dont care about the data if there's nothing keeping our character alive
            if (MH == 0 && SH == 0)
                return false;
            //we dont want our red hearts to be greater than our max hearts
            if (RH > MH)
                return false;
            //there's no way to have over 24 life of just max hearts and soul hearts
            if (MH + SH > 24)
                return false;
            //after that gauntlet, we know our data is valid
            return true;
        }

        public byte[][] GetData()
        {
            //just packaging up the data
            byte[][] toSend = new byte[5][];
            byte[] buffer = ReadBulkData(MaxHeartsOffset);
            toSend[0] = buffer.Take(4).ToArray();
            toSend[1] = buffer.Skip(4).Take(4).ToArray();
            toSend[2] = buffer.Skip(8).Take(4).ToArray();
            toSend[3] = buffer.Skip(12).Take(4).ToArray();
            toSend[4] = buffer.Skip(16).Take(4).ToArray();
            return toSend;
        }

        //Unused so far, just ignore this for now
        private byte[] ReadData(IntPtr offset)
        {
            byte[] buffer = new byte[4];
            int bytesRead = 0;
            ReadProcessMemory(processHandle, IntPtr.Add(FinalOffset, (int)offset), buffer, buffer.Length, out bytesRead);
            return buffer;
        }

        private byte[] ReadBulkData(IntPtr offset)
        {
            //just need 20 bytes of data and send it to be packaged by GetData()
            byte[] buffer = new byte[20];
            int bytesRead = 0;
            ReadProcessMemory(processHandle, IntPtr.Add(FinalOffset, (int)offset), buffer, buffer.Length, out bytesRead);
            return buffer;
        }
    }
}
