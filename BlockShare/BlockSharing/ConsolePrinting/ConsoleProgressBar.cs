using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockShare.BlockSharing.ConsolePrinting
{
    class ConsoleProgressBar : ConsoleElement
    {       

        public string Label { get; set; }       

        public double Progress { get; private set; }

        protected double PreviousProgress { get; set; }

        public ConsoleProgressBar(Point location, int width, int height, string label, double progress, double previousProgress) : base(location, width, height)
        {
            Label = label;
            Progress = progress;
            PreviousProgress = previousProgress;
        }


        public void ReportProgress(double progress)
        {
            PreviousProgress = Progress;
            Progress = PreviousProgress;
        }

        public override void Update()
        {
            Point backupLocation = GetCursorLocation();

            int barLength = Width - Label.Length - 3;
            int filledBars = (int)(barLength * Progress);
            if(Math.Abs(Progress - 1) < 0.01f)
            {
                filledBars = barLength;
            }
            SetCursorLocation(Location);
            Console.Write(Label);
            Console.Write(": ");
            Console.Write("[");

            for(int i = 0; i < barLength; i++)
            {
                if(i <= filledBars)
                {
                    Console.Write('#');
                }
                else
                {
                    Console.Write('_');
                }
            }

            SetCursorLocation(backupLocation);
        }
    }
}
