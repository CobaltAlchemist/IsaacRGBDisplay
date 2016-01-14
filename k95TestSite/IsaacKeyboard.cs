using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using CUE.NET;
using CUE.NET.Devices;
using CUE.NET.Devices.Keyboard;
using CUE.NET.Devices.Keyboard.Enums;
using System.Diagnostics;

namespace k95TestSite
{
    class IsaacKeyboard
    {
        private static CorsairKeyboard keyBoard;
        int MaxRedHearts;
        int RedHearts;
        int EternalHearts;
        int SoulHearts;
        string BlackHearts;
        CorsairKeyboardKeyId[] Keys = new CorsairKeyboardKeyId[12];

        private bool paused = false;
        public bool Pause = false;
        public bool isNull = false;

        public IsaacKeyboard()
        {
            try
            {
                //initialize CueSDK and create a corsair keyboard from it
                CueSDK.Initialize();
                Debug.WriteLine("CueSDK Initialized - " + CueSDK.LoadedArchitecture);
                keyBoard = CueSDK.KeyboardSDK;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Error: " + e);
                isNull = true;
            }
            //register every F key except F12
            for (int i = 1; i < 12; i++)
            {
                Keys[i-1] = (CorsairKeyboardKeyId)(i + 1);
                Debug.WriteLine("-{0} Added-", Keys[i]);
            }
            //Who made F12 equal to 73??
            Keys[11] = CorsairKeyboardKeyId.F12;
            Debug.WriteLine("-{0} Added-", Keys[11]);

            //I'm keeping this here for future reference
            //keyBoard['A'].Led.Color = Color.Red;
            //keyBoard[CorsairKeyboardKeyId.Home].Led.Color = Color.White;
            //keyBoard.Update();
            //keyBoard.UpdateMode = CUE.NET.Devices.Generic.Enums.UpdateMode.Continuous;
            //keyBoard.UpdateFrequency = 1f / 20f;
        }

        public void Tick()
        {
            //set all keys to black
            ResetKeys();
            try
            {
                //if our secondary thread hasn't paused this...
                if (!Pause)
                {
                    //for each red heart container, set the light depending on if it's at full or half
                    for (int i = 0; i < MaxRedHearts / 2; i++)
                    {
                        if (RedHearts - 1 > i * 2)
                            keyBoard[Keys[i]].Led.Color =  Color.Red;
                        else
                            if (RedHearts == (i * 2) + 1)
                            keyBoard[Keys[i]].Led.Color = Color.FromArgb(128, 0, 0);
                    }
                    //for each slot other than max hearts, make sure there's no soul hearts or black hearts there
                    for (int i = 0; i < 11 - (MaxRedHearts / 2); i++)
                    {
                        if (SoulHearts - 1 > i * 2)
                            keyBoard[Keys[i + MaxRedHearts / 2]].Led.Color = 
                                BlackHearts[i] == '1' ?
                                Color.Purple : Color.LightBlue;
                        else
                            if (SoulHearts == (i * 2) + 1)
                            keyBoard[Keys[i + MaxRedHearts / 2]].Led.Color = 
                                BlackHearts[i] == '1' ?
                                Color.FromArgb(40, 0, 40) : Color.FromArgb(255, 86, 108, 115);
                    }
                    //ezpz check that eternal heart and do the thing
                    if (EternalHearts == 1)
                    {
                        int location = Math.Min(RedHearts + 1, MaxRedHearts);
                        keyBoard[Keys[location / 2]].Led.Color = Color.White;
                    }
                }
            }
            catch
            (Exception e)
            {
                Debug.WriteLine("God damnit - " + e);
                ResetKeys();
            }

            //finally, update the keyboard
            keyBoard.Update();
        }

        private void ResetKeys()
        {
            //this just sets all keys to black
            for (int i = 0; i < 12; i++)
            {
                keyBoard[Keys[i]].Led.Color = Color.Black;
            }
        }

        public void InputData(int MRH, int RH, int EH, int SH, string BH)
        {
            //this takes the data from the second thread and puts our information into this class.... I should be doing this in c++
            MaxRedHearts = MRH; RedHearts = RH; EternalHearts = EH; SoulHearts = SH; BlackHearts = BH;
        }
    }
}
