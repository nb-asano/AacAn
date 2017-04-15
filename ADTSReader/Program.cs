using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ADTSReader
{
    class Program
    {
        static void Main(string[] args)
        {
            var parser = new ADTSParser(@"C:\Users\nbasa\Documents\audiodata\sample.aac");
            if (parser.parse() == 0) {
                System.Console.WriteLine(parser.IsSingleFs.ToString());
                System.Console.WriteLine(parser.Freq);

                var list = parser.List;
                System.Console.WriteLine(list.Count);
#if false
                double time = 0;
                double tbps = 0;
                foreach (var val in list) {
                    int fs = AdtsFrameInfo.BitAsign2Fs(val.Header.Fs);
                    double t = 1024.0 / fs;
                    time += t;
                    double bps = val.Header.FrameSize * 8 / t;
                    tbps += bps;
                    System.Console.WriteLine(t.ToString() + "," + time + "," + bps);
                }
                // System.Console.WriteLine(tbps / list.Count);
#endif
                System.Console.WriteLine(parser.Duration);
                System.Console.WriteLine(parser.AvgBps);
            } else {
                System.Console.WriteLine("parsing error.");
            }
            return;
        }
    }
}
