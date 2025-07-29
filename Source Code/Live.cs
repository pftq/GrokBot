using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Bot
{
    public class Live
    {
        // configure
        public static bool enableAlerts = false;
        public static string screenshotDir = "live";
        public static string screenshotFilename = "desktop.png";
        public static bool saveScreenshot = true;
        public static int maxClickDelay = 1000;
        public static bool skipDelay = false;
        private static int clickDelay
        {
            get
            {
                if (skipDelay)
                {
                    skipDelay = false;
                    return 1;
                }
                else
                    return new Random().Next(0, maxClickDelay);
            }
        }

        // do not touch
        private static int screenWidth = 1920;
        private static int screenHeight = 1200;
        private static Bitmap screen;

        public static int GetWidth() { return screenWidth; }
        public static int GetHeight() { return screenHeight; }
        public static Bitmap GetScreen() { return screen; }

        private static bool mouseIsDown = false;

        public static Bitmap MakeScreenshot()
        {
            Bitmap image = new Bitmap(screenWidth, screenHeight, PixelFormat.Format32bppArgb);
            using (Graphics gfx = Graphics.FromImage(image))
            {
                gfx.CopyFromScreen(Screen.PrimaryScreen.Bounds.X, Screen.PrimaryScreen.Bounds.Y, 0, 0, new Size(screenWidth, screenHeight), CopyPixelOperation.SourceCopy);
            }
            return image;
        }

        public static void RefreshScreen()
        {
            if (screen != null) screen.Dispose();
            screen = MakeScreenshot();
        }

        public static void LoadScreen()
        {
            screenWidth = Screen.PrimaryScreen.Bounds.Width;
            screenHeight = Screen.PrimaryScreen.Bounds.Height;
            if (screen != null) screen.Dispose();
            screen = MakeScreenshot();
            SaveScreenshot(screen, screenshotFilename);
        }

        public static void SaveScreenshot(Bitmap img, string filename)
        {
            try
            {
                if (saveScreenshot)
                {
                    if (!Directory.Exists(screenshotDir)) Directory.CreateDirectory(screenshotDir);
                    img.Save(screenshotDir + "/" + filename, ImageFormat.Png);
                }
            }
            catch (Exception ex) { Program.WriteLine("Error saving screenshot: " + ex); }
        }

        [DllImport("user32.dll")]
        static extern IntPtr GetDesktopWindow();

        public static float GetDpiScale()
        {
            using (Graphics g = Graphics.FromHwnd(GetDesktopWindow()))
            {
                return g.DpiX / 96f;
            }
        }

        public static void PerformAction(LiveAction action)
        {
            if (action == null) return;

            if (enableAlerts) Alert();

            // Hold down modifiers if specified
            if (action.shiftDown)
            {
                Program.WriteLine("Holding shift...");
                KeyDown(Keys.Shift);
            }
            if (action.altDown)
            {
                Program.WriteLine("Holding alt...");
                KeyDown(Keys.Alt);
            }
            if (action.controlDown)
            {
                Program.WriteLine("Holding control...");
                KeyDown(Keys.ControlKey);
            }

            // Smooth mouse movement
            if (action.mouseX > 0 || action.mouseY > 0)
            {
                Program.WriteLine("Moving mouse to: " + action.mouseX + ", " + action.mouseY + "...");
                float scale = GetDpiScale();
                int logicalX = (int)(action.mouseX / scale);
                int logicalY = (int)(action.mouseY / scale);

                Point current = Cursor.Position;
                int targetX = logicalX;
                int targetY = logicalY;
                LiveControl.SetCursorPosition(targetX, targetY);
                Thread.Sleep(10); // Microdelay
            }

            // Left click
            if (action.leftClick)
            {
                Program.WriteLine("Left-clicking mouse...");
                Thread.Sleep(clickDelay);
                LiveControl.MouseEvent(LiveControl.MouseEventFlags.LeftDown);
                RandomDelay();
                if (!action.holdClick) LiveControl.MouseEvent(LiveControl.MouseEventFlags.LeftUp);
                else mouseIsDown = true;
                Thread.Sleep(10); // Microdelay
            }

            // Right click
            if (action.rightClick)
            {
                Program.WriteLine("Right-clicking mouse...");
                Thread.Sleep(clickDelay);
                LiveControl.MouseEvent(LiveControl.MouseEventFlags.RightDown);
                RandomDelay();
                if(!action.holdClick) LiveControl.MouseEvent(LiveControl.MouseEventFlags.RightUp);
                else mouseIsDown = true;
                Thread.Sleep(10); // Microdelay
            }

            if (mouseIsDown && !action.holdClick)
            {
                LiveControl.MouseEvent(LiveControl.MouseEventFlags.LeftUp);
                LiveControl.MouseEvent(LiveControl.MouseEventFlags.RightUp);
                mouseIsDown = false;
            }

            // Text input
            if (!string.IsNullOrEmpty(action.textInput))
            {
                Program.WriteLine("Typing " + action.textInput + "...");

                Thread.Sleep(clickDelay);
                string specialChars = "~()%^+-{}[]\\";
                foreach (char c in action.textInput)
                {
                    try
                    {
                        string toSend = specialChars.Contains(c) ? "{" + c + "}" : c.ToString();
                        SendKeys.SendWait(toSend);
                        Thread.Sleep(10);
                    }
                    catch { }
                }
                Thread.Sleep(10); // Microdelay
            }

            // Special keys (last after everything else except modifier release)
            if (action.enterKey)
            {
                Program.WriteLine("Pressing Enter...");
                Thread.Sleep(clickDelay);
                KeyDown(Keys.Enter);
                RandomDelay();
                KeyUp(Keys.Enter);
                Thread.Sleep(10); // Microdelay
            }

            if (action.backspaceKey)
            {
                Program.WriteLine("Pressing Backspace...");
                Thread.Sleep(clickDelay);
                KeyDown(Keys.Back);
                RandomDelay();
                KeyUp(Keys.Back);
                Thread.Sleep(10); // Microdelay
            }

            if (action.escapeKey)
            {
                Program.WriteLine("Pressing Escape...");
                Thread.Sleep(clickDelay);
                KeyDown(Keys.Escape);
                RandomDelay();
                KeyUp(Keys.Escape);
                Thread.Sleep(10); // Microdelay
            }

            // Release modifiers
            if (action.shiftDown) KeyUp(Keys.Shift);
            if (action.altDown) KeyUp(Keys.Alt);
            if (action.controlDown) KeyUp(Keys.ControlKey);
            Thread.Sleep(10); // Microdelay
        }

        public static void RandomDelay()
        {
            Thread.Sleep(10 + new Random().Next(0, 40));
        }

        [DllImport("user32.dll")]
        static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, uint dwExtraInfo);

        private const uint KEYEVENTF_KEYDOWN = 0x0000;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        public static void KeyDown(Keys key)
        {
            keybd_event((byte)key, 0, KEYEVENTF_KEYDOWN, 0);
        }

        public static void KeyUp(Keys key)
        {
            keybd_event((byte)key, 0, KEYEVENTF_KEYUP, 0);
        }

        public static void Alert()
        {
            System.Media.SystemSounds.Asterisk.Play();
            Thread.Sleep(200);
        }

    }

    public class LiveAction
    {
        public DateTime timestamp = DateTime.MinValue;
        public int mouseX = 0;
        public int mouseY = 0;
        public bool shiftDown = false;
        public bool altDown = false;
        public bool controlDown = false;
        public bool leftClick = false;
        public bool rightClick = false;
        public bool holdClick = false;
        public string textInput = "";
        public bool requestResponse = false;
        public bool enterKey = false;
        public bool backspaceKey = false;
        public bool escapeKey = false;
    }

    public class LiveActions
    {
        public List<LiveAction> liveActions = new List<LiveAction>();
        public string explanation = "";
        public bool Any()
        {
            return liveActions != null && liveActions.Any();
        }
    }
}