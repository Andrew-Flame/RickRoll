using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading;
using System.Drawing;
using System.Media;
using System.IO;
using System;
using System.Reflection;
using RickRoll.Properties;
using System.IO.Compression;

namespace RickRoll
{
    class Program
    {
        static int width, height;
        static IntPtr handle = GetConsoleWindow();
        static string gradient = "  .^>]|X@";  //Gradient scale: left - dark; right - light
        static int gradient_len = gradient.Length - 1;
        static char[][] images;

        [DllImport("kernel32.dll")]
        static extern IntPtr GetStdHandle(int handle);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetConsoleDisplayMode(IntPtr ConsoleHandle, uint Flags, IntPtr NewScreenBufferDimensions);
        [DllImport("kernel32.dll")]
        static public extern IntPtr GetConsoleWindow();
        [DllImport("user32.dll")]
        static public extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        static void Main()
        {
            ShowWindow(handle, 0);  //Hide the console
            string path = UnzipFrames();

            /* Setting up the console */
            IntPtr hConsole = GetStdHandle(-11);
            SetConsoleDisplayMode(hConsole, 1, IntPtr.Zero);
            Console.SetBufferSize(Console.WindowWidth, Console.WindowHeight);
            Console.CursorVisible = false;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;

            /* Width and height of the console buffer*/
            width = Console.BufferWidth;
            height = Console.BufferHeight;

            ImportFrames(path);
            RenderImages();
            return;
        }

        static string UnzipFrames()
        {
            string archive = $@"{Path.GetTempPath()}\Frames.zip";
            string folder = $@"{Path.GetTempPath()}\Frames";

            if (!File.Exists(archive) && !Directory.Exists(folder))  //If we have not got an archive and a folder
                File.WriteAllBytes(archive, Resources.Frames);
           
            if (!Directory.Exists(folder))  //If we have not got a folder
                ZipFile.ExtractToDirectory(archive, folder);
            
            /* Remove archive, if it exists */
            if (File.Exists(archive))
                File.Delete(archive);

            return folder;
        }

        async static void ImportFrames(string path)  //Method for importing and preparing frames
        {
            int count = Directory.GetFiles(path).Length;  //Count of files in directory
            images = new char[count][];

            PrepareImages(0, 100);  //Start prepare frames to render
            await Task.Run(() => PrepareImages(100, count));  //Async preparing frames during render in console

            void PrepareImages(int start, int end)
            {
                for (int i = start; i < end; i++)
                {
                    string file = Directory.GetFiles(path)[i];  //Get a file
                    Bitmap bitmap = new Bitmap(new Bitmap(file), new Size(width, height));  //Make bitmap of our file with size of our console window

                    /* We will pu chars in this array */
                    images[i] = new char[width * height];
                    int counter = 0;

                    for (int y = 0; y < height; y++)  //Get all pixels
                    {
                        for (int x = 0; x < width; x++)
                        {
                            /* Put brightness char in char array*/
                            byte index = Convert.ToByte(gradient_len * bitmap.GetPixel(x, y).GetBrightness());
                            images[i][counter] = gradient[index];
                            counter++;
                        }
                    }
                }
            }
        }

        static void RenderImages()
        {
            /* Play music */
            Assembly assembly = Assembly.GetExecutingAssembly();
            using (Stream resourceStream = assembly.GetManifestResourceStream(@"RickRoll.Media.audio.wav"))
            {
                SoundPlayer player = new SoundPlayer(resourceStream);
                player.Play();
            }

            double start = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            double duration = 211000.0d;

            ShowWindow(handle, 5); //Show Console
            float multiplier = 28; //Increase to speed up and decrease to slow down. Defaul must be 24 because original video contains 24 frames per second,…
                                   //…but with a value of 24 console video is slowly than original, so defaul value is 28

            for (int i = 0; i < images.Length; i++)
            {
                double time_ratio = duration / (DateTimeOffset.Now.ToUnixTimeMilliseconds() - start);
                double frames_ratio = Convert.ToDouble(images.Length) / Convert.ToDouble(i);

                /* We need it to sync audio and video data */
                if (time_ratio > frames_ratio)
                    multiplier -= 0.1f;
                else
                    multiplier += 0.1f;

                Console.SetCursorPosition(0, 0);
                Console.Write(images[i]);
                Thread.Sleep(Convert.ToInt32(1000.0f / multiplier));
            }
        }
    }
}
