using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockShare.BlockSharing.ConsolePrinting
{
    class ConsoleCanvas : ConsoleElement
    {
        private List<ConsoleElement> childElements = new List<ConsoleElement>();

        public ConsoleCanvas(Point location, int width, int height) : base(location, width, height)
        {

        }

        public void AddProgressBar(ConsoleProgressBar progressBar)
        {
            progressBar.Width = Width;
            childElements.Add(progressBar);
        }

        public void AddElement(ConsoleElement element)
        {
            childElements.Add(element);
        }

        public void RemoveElement(ConsoleElement element)
        {
            childElements.Remove(element);
        }

        public override void Update()
        {
            
        }
    }
}
