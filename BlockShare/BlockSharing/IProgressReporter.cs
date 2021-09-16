using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockShare.BlockSharing
{
    public interface IProgressReporter
    {
        void ReportProgress(object sender, double progress);
        void ReportFinishing(object sender, bool success);
    }
}
