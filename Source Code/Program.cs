using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;

namespace Bot
{
    public class Program
    {
        public static string logfile = "Log.txt";

        // Import the SetWindowPos function from user32.dll
        [DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        // Import console font functions from kernel32.dll
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetCurrentConsoleFontEx(IntPtr hConsoleOutput, bool bMaximumWindow, ref CONSOLE_FONT_INFOEX lpConsoleCurrentFontEx);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GetCurrentConsoleFontEx(IntPtr hConsoleOutput, bool bMaximumWindow, ref CONSOLE_FONT_INFOEX lpConsoleCurrentFontEx);

        // Constants for SetWindowPos
        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        const uint SWP_NOMOVE = 0x0002;
        const uint SWP_NOSIZE = 0x0001;
        const int STD_OUTPUT_HANDLE = -11;

        // Structure for console font information
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct CONSOLE_FONT_INFOEX
        {
            public uint cbSize;
            public uint nFont;
            public COORD dwFontSize;
            public int FontFamily;
            public int FontWeight;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string FaceName;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct COORD
        {
            public short X;
            public short Y;
        }

        [DllImport("shcore.dll")]
        static extern int SetProcessDpiAwareness(int value);

        [STAThread]
        public static void Main(string[] args = null)
        {
            try
            {
                IntPtr consoleHandle = GetConsoleWindow();
                MoveWindow(consoleHandle, 0, 0, Screen.PrimaryScreen.Bounds.Width / 4, Screen.PrimaryScreen.Bounds.Height / 4, true);

                // Set the console window to be always on top
                SetWindowPos(consoleHandle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);

                // Set console font size
                SetConsoleFont(24); // Set font size to 16 (adjustable)

                SetProcessDpiAwareness(2); // 2 = PROCESS_PER_MONITOR_DPI_AWARE

                new Player().Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Program Exception: " + ex);
                Console.Read();
            }
        }

        public static void SetConsoleFont(int fontSize)
        {
            IntPtr consoleOutput = GetStdHandle(STD_OUTPUT_HANDLE);
            if (consoleOutput == IntPtr.Zero)
            {
                WriteLine("Failed to get console handle.", true);
                return;
            }

            CONSOLE_FONT_INFOEX fontInfo = new CONSOLE_FONT_INFOEX();
            fontInfo.cbSize = (uint)Marshal.SizeOf(fontInfo);
            fontInfo.dwFontSize.X = 0; // Let system choose width based on height
            fontInfo.dwFontSize.Y = (short)fontSize;
            fontInfo.FontFamily = 0x30; // FF_MODERN
            fontInfo.FontWeight = 400; // Normal weight
            fontInfo.FaceName = "Consolas"; // Common monospaced font for consoles

            bool result = SetCurrentConsoleFontEx(consoleOutput, false, ref fontInfo);
            if (!result)
            {
                WriteLine("Failed to set console font: " + Marshal.GetLastWin32Error(), true);
            }
        }

        public static bool WriteLine(string s, bool showOnScreen = true)
        {
            if (showOnScreen) Console.WriteLine(s);
            try
            {
                if (showOnScreen)
                    File.AppendAllLines(logfile, new string[] { DateTime.Now + ": " + s });
            }
            catch (Exception e)
            {
                Console.WriteLine("Log failed: " + e);
            }
            return true;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);
    }
}