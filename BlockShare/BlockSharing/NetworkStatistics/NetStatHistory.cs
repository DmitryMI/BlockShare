using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockShare.BlockSharing.NetworkStatistics
{
    class NetStatHistory
    {
        private NetStat[] netstatHistory;
        private int netstatHistoryIndex = 0;
        private int count = 0;

        public NetStatHistory(int historyLength)
        {
            netstatHistory = new NetStat[historyLength];
        }

        public void Clear()
        {
            netstatHistoryIndex = 0;
            count = 0;

            for(int i = 0; i < netstatHistory.Length; i++)
            {
                netstatHistory[0]?.Clear();
            }
        }

        public NetStat GetHistoricalRecord(int relativeIndex)
        {
            int index = (netstatHistory.Length + (netstatHistoryIndex - relativeIndex)) % netstatHistory.Length;
            if (index < 0)
            {
                index = -index;
            }
            //Debug.WriteLine("GetHistoricalRecord: " + index);
            return netstatHistory[index];
        }

        public void AppendHistoricalRecord(NetStat netStat)
        {
            int nextIndex = (netstatHistoryIndex + 1) % netstatHistory.Length;
            netstatHistoryIndex = nextIndex;
            netstatHistory[nextIndex] = netStat;

            if(count < netstatHistory.Length)
            {
                count++;
            }
        }

        
        public NetStatSpeed GetAverageSpeed(double timeSpan)
        {
            NetStat firstRecord = GetHistoricalRecord(count - 1);
            NetStat lastRecord = GetHistoricalRecord(0);
            NetStat diff = lastRecord - firstRecord;
            double sentAverage = (double)diff.TotalSent / netstatHistory.Length;
            double receivedAverage = (double)diff.TotalReceived / netstatHistory.Length;
            //double payloadAverage = (double)diff.Payload / netstatHistory.Length;

            double upSpeed = sentAverage / timeSpan;
            double downSpeed = receivedAverage / timeSpan;
            return new NetStatSpeed(downSpeed, upSpeed);
        }
    }
}
