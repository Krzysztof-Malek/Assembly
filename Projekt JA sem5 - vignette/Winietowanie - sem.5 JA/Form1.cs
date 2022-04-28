using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace Winietowanie___sem._5_JA
{
    public partial class Form1 : Form
    {
        [DllImport(@"C:\Users\krzys\source\repos\C#\Projekt JA sem5 - vignette\Winietowanie - sem.5 JA\x64\Release\MyDLL.dll")]
        private static extern unsafe int Vignette(byte* pImg, float* imgWidthHeightInnerCirclePtr, float* vigColorPtr, int* imageWidthHeight);

        private Bitmap bitmapImage;
        private Stopwatch mywatch = new Stopwatch();

        public Form1()
        {
            InitializeComponent();
            int logicalProcessors = Environment.ProcessorCount;
            trackBar2.Value = logicalProcessors;
            label3.Text = Convert.ToString(logicalProcessors);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            /*////Temp for time write////
            var fileName = @"C:\Users\krzys\Desktop\Polibuda\sem. 5\JA\Raport + prezentacja\times.txt";
            using FileStream fs = File.OpenWrite(fileName);

            for (int i = 1; i < 65; i++)
            {
            trackBar2.Value = i;*/
            if (bitmapImage == null)
            {
                MessageBox.Show("There is no choosen file", "File error");
            }
            else
            {
                pictureBoxRight.Image = null;
                button1.Enabled = false;

                //Setting amounts of Threads
                //ThreadPool.SetMinThreads(trackBar2.Value, trackBar2.Value);
                ThreadPool.SetMaxThreads(trackBar2.Value, trackBar2.Value);

                ///////////////////////TEMPORARY Bitmap
                Bitmap bmp = new Bitmap(100, 100);
                using (Graphics graph = Graphics.FromImage(bmp))
                {
                    Rectangle ImageSize = new Rectangle(0, 0, 100, 100);
                    graph.FillRectangle(Brushes.White, ImageSize);
                }
                //////////////////////

                if (radioButton1.Checked)
                {
                    mywatch.Start();
                    pictureBoxRight.Image = FilterImg(bitmapImage, false, vignetteColor.BackColor, trackBar1.Value);
                    mywatch.Stop();
                    TimeSpan ts = mywatch.Elapsed;

                    // Format and display the TimeSpan value.
                    string elapsedTime = String.Format("{0:00}:{1:00}.{2:000}",
                        ts.Minutes, ts.Seconds,
                        ts.Milliseconds / 10);

                    label5.Text = elapsedTime + " sec";
                    mywatch.Reset();

                    /*////Temp for time write////
                    byte[] bytes = Encoding.UTF8.GetBytes(elapsedTime + '\n');
                    fs.Write(bytes, 0, bytes.Length);*/
                }
                else
                {
                    mywatch.Start();
                    pictureBoxRight.Image = FilterImg(bitmapImage, true, vignetteColor.BackColor, trackBar1.Value);
                    mywatch.Stop();
                    TimeSpan ts = mywatch.Elapsed;

                    // Format and display the TimeSpan value.
                    string elapsedTime = String.Format("{0:00}:{1:00}.{2:000}",
                        ts.Minutes, ts.Seconds,
                        ts.Milliseconds / 10);

                    label5.Text = elapsedTime + " sec";
                    mywatch.Reset();

                    /*////Temp for time write////
                    byte[] bytes = Encoding.UTF8.GetBytes(elapsedTime + '\n');
                    fs.Write(bytes, 0, bytes.Length);*/
                }
                // }
                button1.Enabled = true;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "jpgs|*.jpg|png|*.png|Bitmaps|*.bmp";

            if (dialog.ShowDialog() == DialogResult.OK && dialog.CheckFileExists)
            {
                string pattern = @"\b.(jpg)?(png)?(gif)?\b";
                string extn = dialog.SafeFileName;
                Match m = Regex.Match(extn, pattern, RegexOptions.IgnoreCase);
                if (!m.Success)
                {
                    MessageBox.Show("Wrong file extension", "File error");
                }
                bitmapImage = new Bitmap(Image.FromFile(dialog.FileName));
                pictureBoxLeft.Image = bitmapImage;
            }
            else
            {
                MessageBox.Show("Bad or corupted file", "File error");
            }
        }

        /// <summary>
        /// Checks first radioButton (C#) for clicks
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked)
            {
                radioButton2.Checked = false;
            }
        }

        /// <summary>
        /// Checks second radioButton (ASM) for clicks
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton2.Checked)
            {
                radioButton1.Checked = false;
            }
        }

        /// <summary>
        /// Change the number you see (amount of threads)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            label3.Text = Convert.ToString(trackBar2.Value);
        }

        /// <summary>
        /// Change of the color of the vignette
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void vignetteColor_Click(object sender, EventArgs e)
        {
            ColorDialog colorPicker = new ColorDialog();

            if (colorPicker.ShowDialog() == DialogResult.OK)
            {
                vignetteColor.BackColor = colorPicker.Color;
            }
        }

        /// <summary>
        /// Makes vignette on bitmap by C# or ASM function depending on which button is checked
        /// </summary>
        /// <param name="kernelFilterSize"></param>
        /// <param name="image"></param>
        /// <param name="isAsm"></param>
        /// <param name="vigColor"></param>
        /// <param name="strength"></param>
        /// <returns>Bitmap</returns>
        public unsafe Bitmap FilterImg(Bitmap image, bool isAsm, Color vigColor, float strength)
        {
            //RGB values from vignette color
            byte vigColorR = vigColor.R;
            byte vigColorG = vigColor.G;
            byte vigColorB = vigColor.B;

            //Static values for calculations
            float halfWidth = image.Width / 2;
            float halfHeight = image.Height / 2;
            float vignetteStrength = strength / 10;

            //Checking which size is smaller so the circle from vignette isn't too big
            float innerCircle = halfWidth;
            if (image.Width > image.Height)
            {
                innerCircle = halfHeight;
            }
            //Multiply innerCircle half width/height with vignetteStrength to get innerCircle
            innerCircle *= vignetteStrength;

            //Calculation of the distance from the innerCircle so we can get smoother vignette, needed for getting the percentage of vignette
            double greatestDistanceFromCircle = (halfWidth * halfWidth + halfHeight * halfHeight) - (innerCircle * innerCircle);

            //New Bitmap and matrix for getting pixels from oryginal image
            Bitmap filteredImg = new Bitmap(image);

            /////////////////////////////////////////////////////
            //Threads managing
            var doneEventsss = new ManualResetEvent[1];
            doneEventsss[0] = new ManualResetEvent(false);

            //Creating new bitmap in PixelFormat.Format24bppRgb where there is no alpha byte
            //And locking it in memmory of program
            BitmapData data = filteredImg.LockBits(new Rectangle(0, 0, filteredImg.Width, filteredImg.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            //Getting pointer on first pixel of bitmap
            byte* imgPtr = (byte*)data.Scan0;

            //Calculating remainder so we will be able to move pointer at the end of image row (bits in a row is always divisible by 4 -> exmp. width: 407*3 = 1221 -> 1221 % 4 = 1 soo we need to move pointer by 3 to get to the next row)
            int rowRemainder = 4 - ((filteredImg.Width * 3) % 4);
            if (rowRemainder == 4)
            {
                rowRemainder = 0;
            }
            //Getting size of whole 1 row with adjustment to 4 bits
            int rowBits = rowRemainder + filteredImg.Width * 3;

            //Setting for ManualResetEvent for the amount of threads choosen
            int amountOfThreads = trackBar2.Value;

            //Setting how many rows of image will be in one thread and calculating remainder that will be added in last thread
            int rowsForOneThread = image.Height / amountOfThreads;
            int heightRemainder = image.Height % amountOfThreads;

            var done = new CountdownEvent(1);

            //Counting height for each thread
            int j = 0;

            if (isAsm)
            {
                fixed (float* vigColorPtr = new float[] { vigColor.R, vigColor.G, vigColor.B })
                fixed (float* imgWidthHeightInnerCirclePtr = new float[] { halfWidth, halfWidth, halfWidth, halfWidth, halfHeight, halfHeight, halfHeight, halfHeight, innerCircle, innerCircle, innerCircle, innerCircle })

                    for (int k = 0; k < amountOfThreads; k++)
                    {
                        done.AddCount();

                        byte* lastBit = imgPtr + rowBits * rowsForOneThread;
                        if (k == amountOfThreads - 1)
                        {
                            lastBit += rowBits * heightRemainder;
                        }
                        lastBit -= rowRemainder;

                        fixed (int* imgWidthHeight = new int[] { filteredImg.Width, j })
                        {
                            vignetteAsmArgs vigArgs = new vignetteAsmArgs(imgPtr, lastBit, vigColorPtr, imgWidthHeightInnerCirclePtr, imgWidthHeight);

                            ThreadPool.QueueUserWorkItem(
                             (state) =>
                             {
                                 try
                                 {
                                     vigArgs.ThreadPoolCallback(state);
                                 }
                                 finally
                                 {
                                     done.Signal();
                                 }
                             }, k);
                        }
                        //Changing height by rowsForOneThread for propper calculations in threads
                        j += rowsForOneThread;

                        //Getting pointer for pixel for next thread from lastBit
                        imgPtr = lastBit + rowRemainder;
                    }
            }
            else
            {
                for (int k = 0; k < amountOfThreads; k++)
                {
                    done.AddCount();

                    //Getting last bit in the last row to know when thread should stop
                    byte* lastBit = imgPtr + rowBits * rowsForOneThread;
                    if (k == amountOfThreads - 1)
                    {
                        lastBit += rowBits * heightRemainder;
                    }
                    lastBit--;

                    vignetteCCArgs CCArgs = new vignetteCCArgs(imgPtr, lastBit, rowRemainder, k, 0, j, halfWidth, halfHeight, innerCircle, vigColorR, vigColorG, vigColorB, greatestDistanceFromCircle);

                    ThreadPool.QueueUserWorkItem(
                     (state) =>
                     {
                         try
                         {
                             CCArgs.ThreadPoolCallback(state);
                         }
                         finally
                         {
                             done.Signal();
                         }
                     }, k);

                    //Changing height by rowsForOneThread for propper calculations in threads
                    j += rowsForOneThread;

                    //Getting pointer for pixel for next thread from lastBit
                    imgPtr = lastBit + 1;
                }
            }
            done.Signal();
            done.Wait();

            //Unlocks bitmap from memmory
            filteredImg.UnlockBits(data);

            return filteredImg;
        }
    }

    //Class for referencing asm args to threads (you can pass only one object to class threads)
    public unsafe class vignetteAsmArgs
    {
        [DllImport(@"C:\Users\krzys\source\repos\C#\Projekt JA sem5 - vignette\Winietowanie - sem.5 JA\x64\Release\MyDLL.dll")]
        private static extern unsafe int Vignette(byte* imgPtr, byte* lastBit, float* vigColorPtr, float* imgWidthHeightInnerCirclePtr, int* imageWidth);

        public vignetteAsmArgs(byte* imgPtr, byte* lastBit, float* vigColorPtr, float* imgWidthHeightInnerCirclePtr, int* imgWidthHeight)
        {
            this.imgPtr = imgPtr;
            this.lastBit = lastBit;
            this.vigColorPtr = vigColorPtr;
            this.imgWidthHeightInnerCirclePtr = imgWidthHeightInnerCirclePtr;
            this.imgWidthHeight = imgWidthHeight;
        }

        public byte* imgPtr
        {
            get; set;
        }

        public byte* lastBit
        {
            get; set;
        }

        public float* vigColorPtr
        {
            get; set;
        }

        public float* imgWidthHeightInnerCirclePtr
        {
            get; set;
        }

        public int* imgWidthHeight
        {
            get; set;
        }

        public int imgHeight
        {
            get; set;
        }

        public unsafe void ThreadPoolCallback(Object threadContext)
        {
            Vignette(this.imgPtr, this.lastBit, this.vigColorPtr, this.imgWidthHeightInnerCirclePtr, this.imgWidthHeight);
            //Vignette(this.imgPtr, this.lastBit, this.vigColorPtr, this.imgWidthHeightInnerCirclePtr, this.imgHeight, this.imgWidth);
        }
    }

    //Class for referencing C# args to threads (you can pass only one object to class threads)
    public unsafe class vignetteCCArgs
    {
        public vignetteCCArgs(byte* imgPtr, byte* lastBit, int rowRemainder, int k, int i, int j, float halfWidth, float halfHeight, float innerCircle, byte vigColorR, byte vigColorG, byte vigColorB, double greatestDistanceFromCircle)
        {
            this.bit = imgPtr;
            this.lastBit = lastBit;
            this.remainder = rowRemainder;
            this.k = k;
            this.i = 0;
            this.j = j;
            this.halfWidth = halfWidth;
            this.halfHeight = halfHeight;
            this.innerCircle = innerCircle;
            this.vigColorR = vigColorR;
            this.vigColorG = vigColorG;
            this.vigColorB = vigColorB;
            this.greatestDistanceFromCircle = greatestDistanceFromCircle;
        }

        public byte* bit
        {
            get; set;
        }

        public byte* lastBit
        {
            get; set;
        }

        public int remainder
        {
            get; set;
        }

        public int k
        {
            get; set;
        }

        public int i
        {
            get; set;
        }

        public int j
        {
            get; set;
        }

        public float halfWidth
        {
            get; set;
        }

        public float halfHeight
        {
            get; set;
        }

        public float innerCircle
        {
            get; set;
        }

        public byte vigColorR
        {
            get; set;
        }

        public byte vigColorG
        {
            get; set;
        }

        public byte vigColorB
        {
            get; set;
        }

        public double greatestDistanceFromCircle
        {
            get; set;
        }

        public unsafe void ThreadPoolCallback(Object threadContext)
        {
            //System.Diagnostics.Debug.WriteLine($"Thread {this.k} started...");

            float innerCircle = this.innerCircle * this.innerCircle;

            while (this.bit + 2 <= this.lastBit)
            {
                //Calculating distance of the current pixel from the CENTER to check if the vignette is affecting it
                double distanceFromTheCenter = ((this.i - this.halfWidth) * (this.i - this.halfWidth)) + ((this.j - this.halfHeight) * (this.j - this.halfHeight));
                //Calculating distance of the current pixel from the CIRCLE to calculate the strength of the vignette
                double distanceFromCircle = ((this.i - this.halfWidth) * (this.i - this.halfWidth)) + ((this.j - this.halfHeight) * (this.j - this.halfHeight)) - innerCircle;

                if (innerCircle > distanceFromTheCenter)
                {
                }
                else
                {
                    //Percenage of the effect from Vignette on pixel - farther from circle = greater color change
                    double percentageOfPixelChange = distanceFromCircle / this.greatestDistanceFromCircle;
                    //For being sure that the percentage will not be more than 1 => 100%, it would cross RGB value, which is [0,255]
                    if (percentageOfPixelChange > 1)
                    {
                        percentageOfPixelChange = 1;
                    }
                    //We want every pixel to be closer to the RGB values of the color we chose so we are reducing the distance from those two values
                    *(this.bit + 0) -= (byte)((*(this.bit + 0) - this.vigColorB) * percentageOfPixelChange);
                    *(this.bit + 1) -= (byte)((*(this.bit + 1) - this.vigColorG) * percentageOfPixelChange);
                    *(this.bit + 2) -= (byte)((*(this.bit + 2) - this.vigColorR) * percentageOfPixelChange);
                }

                this.bit += 3;
                this.i++;
                //If i will be more than image.width we reset it soo it's correct with the position of next pixel
                if (this.i >= (int)(this.halfWidth * 2))
                {
                    this.i = 0;
                    this.j++;
                    this.bit += this.remainder;
                }
            }

            //System.Diagnostics.Debug.WriteLine($"Thread {this.k} ended");
        }
    }
}