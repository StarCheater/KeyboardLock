using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace KeyboardLock
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }


        public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private ProcessModule objCurrentModule;
        private LowLevelKeyboardProc objKeyboardProcess;
        private IntPtr ptrHook;
        private KeysConverter kc = new KeysConverter();
        private const string passph = "DOST4321";
        private readonly char[] pass = new char[passph.Length];

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string name);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int id, LowLevelKeyboardProc callback, IntPtr hMod,
            uint dwThreadId);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CallNextHookEx(IntPtr hook, int nCode, IntPtr wp, IntPtr lp);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool UnhookWindowsHookEx(IntPtr hook);
        
        public enum MapType : uint
        {
            MAPVK_VK_TO_VSC = 0x0,
            MAPVK_VSC_TO_VK = 0x1,
            MAPVK_VK_TO_CHAR = 0x2,
            MAPVK_VSC_TO_VK_EX = 0x3,
        }

        [DllImport("user32.dll")]
        public static extern int ToUnicode(
            uint wVirtKey,
            uint wScanCode,
            byte[] lpKeyState,
            [Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 4)]
            StringBuilder pwszBuff,
            int cchBuff,
            uint wFlags);

        [DllImport("user32.dll")]
        public static extern bool GetKeyboardState(byte[] lpKeyState);

        [DllImport("user32.dll")]
        public static extern uint MapVirtualKey(uint uCode, MapType uMapType);
       

        public static char GetCharFromKey(Keys key)
        {
            char ch = ' ';
            int virtualKey = (int)(key);
            byte[] keyboardState = new byte[256];
            GetKeyboardState(keyboardState);

            uint scanCode = MapVirtualKey((uint)virtualKey, MapType.MAPVK_VK_TO_VSC);
            StringBuilder stringBuilder = new StringBuilder(2);

            int result = ToUnicode((uint)virtualKey, scanCode, keyboardState, stringBuilder, stringBuilder.Capacity, 0);
            switch (result)
            {
                case -1:
                    break;
                case 0:
                    break;
                case 1:
                    {
                        ch = stringBuilder[0];
                        break;
                    }
                default:
                    {
                        ch = stringBuilder[0];
                        break;
                    }
            }
            return ch;
        }

        

        [StructLayout(LayoutKind.Sequential)]
        public struct KBDLLHOOKSTRUCT
        {
            public readonly Keys key;
            public readonly int scanCode;
            public readonly int flags;
            public readonly int time;
            public readonly IntPtr extra;
        }

        public static bool CheckKeys(Keys check, IEnumerable<Keys> keys)
        {
            foreach (var key in keys)
            {
                if (key == check)
                {
                    return true;
                }
            }

            return false;
        }

        private IntPtr captureKey(int nCode, IntPtr wp, IntPtr lp)
        {
            if (nCode >= 0)
            {
                var objKeyInfo = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lp, typeof(KBDLLHOOKSTRUCT));
                label1.Text = kc.ConvertToString(objKeyInfo.key);
                
                label3.Text = GetCharFromKey(objKeyInfo.key).ToString();
                
                if (objKeyInfo.flags >= 128) //Клавиша отпущенна
                {
                    
                    for (int i = 0; i < pass.Length - 1; i++) pass[i] = pass[i + 1];

                    string sre = kc.ConvertToString(objKeyInfo.key);
                    pass[pass.Length - 1] = ((sre.Length == 1)) ? sre[0] : ' '; /* : ((sre.Length == 2) && (sre[0] == 'D')) ? sre[1]//*/
                    label2.Text = "|"+new string(pass)+"|";
                }

                //if (!(CheckKeys(objKeyInfo.key, new[] { Keys.D0, Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6, Keys.D7, Keys.D8, Keys.D9, Keys.Delete, Keys.Back, Keys.Left, Keys.Right })))
                
                return checkBox1.Checked ? (IntPtr) 1 : CallNextHookEx(ptrHook, nCode, wp, lp);
            }
            return CallNextHookEx(ptrHook, nCode, wp, lp);
        }

        private void KeyboardOn()
        {
            if (ptrHook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(ptrHook);
                ptrHook = IntPtr.Zero;
            }
        }

        private void KeyboardOff()
        {
            if (ptrHook == IntPtr.Zero)
            {
              
                ptrHook = SetWindowsHookEx(13, objKeyboardProcess, GetModuleHandle(objCurrentModule.ModuleName), 0);

            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            objCurrentModule = Process.GetCurrentProcess().MainModule;
            objKeyboardProcess = captureKey;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            KeyboardOff();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            KeyboardOn();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            KeyboardOn();
        }
    }
}
