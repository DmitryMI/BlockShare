using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockShare.BlockSharing.ConsolePrinting
{
    abstract class ConsoleElement
    {
        public Point Location { get; set; }
       
        public int Height { get; set; }
        public int Width { get; set; }

        protected ConsoleElement(Point location, int width, int height)
        {
            Location = location;
            Height = height;
            Width = width;
        }


        public static Point GetCursorLocation()
        {
            return new Point(Console.CursorLeft, Console.CursorTop);
        }

        public static void SetCursorLocation(Point point)
        {
            Console.CursorLeft = point.X;
            Console.CursorTop = point.Y;
        }

        public abstract void Update();
    }
}
