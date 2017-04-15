using ADTSReader;

namespace ADTSView
{
    /// <summary>
    /// DataGridにバインドするクラス
    /// </summary>
    public class ADTSViewEntry
    {
        public int No { get; private set; }
        public long Offset { get; private set; }
        public float Time { get; private set; }
        public float Duration { get; private set; }
        public int Size { get; private set; }
        public float Kbps { get; private set; }
        public int ID { get; private set; }
        public int ErrProtection { get; private set; }
        public string Profile { get; private set; }
        public int Fs { get; private set; }
        public int Private { get; private set; }
        public int Ch { get; private set; }
        public int Original { get; private set; }
        public int CopyRight { get; private set; }

        public ADTSViewEntry(int n, double time, AdtsFrameInfo inf)
        {
            No = n;
            Offset = inf.Offset;
            Time = (float)time;
            Duration = (float)inf.Duration;
            Size = inf.Header.FrameSize;
            Kbps = (float)inf.Bps / 1000;
            ID = inf.Header.ID ? 0 : 1;
            ErrProtection = inf.Header.ProtectionBit ? 0 : 1;
            Profile = inf.Header.ProfileName;
            Fs = inf.Header.SamplingRate;
            Private = inf.Header.PrivateBit ? 0 : 1;
            Ch = inf.Header.ChannelCount;
            Original = inf.Header.Original ? 0 : 1;
            CopyRight = inf.Header.Copyright ? 0 : 1;
        }
    }
}
