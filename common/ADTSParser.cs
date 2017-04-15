using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace ADTSReader
{
    /// <summary>
    /// ADTSヘッダー情報
    /// </summary>
    public class AacAdtsHeader
    {
        /// <summary>ID.MPEG4なら0</summary>
        public bool ID { get; set; }    // 0:MPEG4 / 1:MPEG2
        /// <summary>CRC保護</summary>
        public bool ProtectionBit { get; set; } // 0:CRC保護あり, 1:CRC保護なし
        /// <summary>プロファイル(2bit)</summary>
        public byte Profile { get; set; }　      // 00:MAIN, 01:LC, 10:SSR, 11:reserved(LTP), 2bit
        /// <summary>サンプリングレート(4bit)</summary>
        public byte Fs { get; set; }             // 4bit
        /// <summary>プライベートビット</summary>
        public bool PrivateBit { get; set; }     // 0:あり, 1:なし
        /// <summary>チャンネル構成(3bit)</summary>
        public byte Channel { get; set; }        // 3bit
        /// <summary>オリジナルビット</summary>
        public bool Original { get; set; }       // 0:Yes, 1:No
        public bool Home { get; set; }
        public bool Copyright { get; set; }
        public bool Icopyright { get; set; }
        public Int16 FrameSize { get; set; }      // フレームサイズ（13bit）
        public Int16 BufferSize { get; set; }     // バッファサイズ(VBR=0x7FF) 
        public byte DBOffset { get; set; }        // データブロックまでのオフセット
        public UInt16 CRC { get; set; }           // ADTS Error check(CRC 16bits) : T(プロテクションビット)が0のときのみ

        // 以下可読パラメータ

        /// <summary>プロファイル名</summary>
        public string ProfileName
        {
            get
            {
                string s = "";
                if (Profile < 4) {
                    s = ProfileNameTbl[Profile];
                }
                return s;
            }
        }
        /// <summary>サンプリングレート</summary>
        public int SamplingRate
        {
            get
            {
                int fs = -1;
                if (Fs < 16) {
                    fs = SamplingReteTbl[Fs];
                }
                return fs;
            }
        }
        /// <summary>チャンネル数</summary>
        public int ChannelCount
        {
            get
            {
                if (Channel <= 6) {
                    return Channel;
                } else if (Channel == 7) {
                    return 8;
                }
                return -1;
            }
        }

        private static string[] ProfileNameTbl =
        {
            "Main",
            "LC",
            "SSR",
            "LTP"
        };

        private static int[] SamplingReteTbl =
        {
            96000, 88200, 64000, 48000,
            44100, 32000, 24000, 22050,
            16000, 12000, 11025, 8000,
            7350, -1, -1, -1
        };
    }

    /// <summary>
    /// ADTSフレーム情報
    /// </summary>
    public class AdtsFrameInfo
    {
        /// <summary>ADTSヘッダー</summary>
        public readonly AacAdtsHeader Header;
        /// <summary>ファイル内のフレームバイトオフセット</summary>
        public readonly long Offset;
        /// <summary>フレームの時間</summary>
        public readonly double Duration;
        /// <summary>フレームのビットレート</summary>
        public readonly double Bps;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="offset">フレームのバイトオフセット</param>
        public AdtsFrameInfo(AacAdtsHeader hdr, long offset)
        {
            // 1 aac frame is 1024 samples
            // AAC files always have 1024 samples per frame.
            // In MP4 files it can be 960 too,
            // 1024 samples per frame, per channel
            Header = hdr;
            Offset = offset;
            Duration = 1024.0 / Header.SamplingRate;
            Bps = Header.FrameSize * 8 / Duration;
        }
    }

    class ADTSParser
    {
        /// <summary>解析対象のファイル</summary>
        public readonly string FilePath;
        /// <summary>解フレーム情報のバッキングストア</summary>
        private readonly List<AdtsFrameInfo> m_List = new List<AdtsFrameInfo>();
        /// <summary>フレーム情報のリスト</summary>
        public ReadOnlyCollection<AdtsFrameInfo> List
        {
            get
            {
                return new ReadOnlyCollection<AdtsFrameInfo>(m_List);
            }
        }
        /// <summary>未解析となったバイト数</summary>
        public long RemainByte { get; private set; }
        /// <summary>全フレーム同一プロファイル</summary>
        public bool IsSingleProfile { get; private set; }
        /// <summary>全フレーム同一FS</summary>
        public bool IsSingleFs { get; private set; }
        /// <summary>全フレーム同一チャンネル</summary>
        public bool IsSingleCh { get; private set; }
        /// <summary>プロファイル</summary>
        public string Profile { get; private set; }
        /// <summary>チャンネル数</summary>
        public int Channel { get; private set; }
        /// <summary>サンプリングレート</summary>
        public int Freq { get; private set; }
        /// <summary>総演奏時間</summary>
        public double Duration { get; private set; }
        /// <summary>平均ビットレート</summary>
        public double AvgBps { get; private set; }
        /// <summary>最大ビットレート</summary>
        public double MaxBps { get; private set; }
        /// <summary>ビットレートの標準偏差</summary>
        public double StDevBps { get; private set; }

        public ADTSParser(string path)
        {
            FilePath = path;
        }

        public int parse()
        {
            if (string.IsNullOrWhiteSpace(FilePath)) {
                return 0;
            }
            if (!File.Exists(FilePath)) {
                return -2;
            }

            int result = 0;
            try {
                using (FileStream fs = new FileStream(FilePath, FileMode.Open))
                using (BinaryReader br = new BinaryReader(fs)) {
                    var res = parse(br, br.BaseStream.Length);
                    result = res.Result;
                    RemainByte = res.Remain;
                }
            } catch (Exception e) {
                System.Diagnostics.Debug.WriteLine(e.Message);
                result = -1;
            }

            if (result != -1 && m_List.Count > 0) {
                IsSingleProfile = (m_List.Select(d => d.Header.Profile).Distinct().ToList().Count == 1);
                IsSingleFs = (m_List.Select(d => d.Header.Fs).Distinct().ToList().Count == 1);
                IsSingleCh = (m_List.Select(d => d.Header.Channel).Distinct().ToList().Count == 1);
                Profile = m_List[0].Header.ProfileName;
                Freq = m_List[0].Header.SamplingRate;
                Channel = m_List[0].Header.ChannelCount;
                Duration = m_List.Select(d => d.Duration).Sum();
                AvgBps = m_List.Select(d => d.Bps).Average();
                MaxBps = m_List.Select(d => d.Bps).Max();
                StDevBps = calcBpsStdDeviation(m_List);
            }

            return result;
        }

        /// <summary>
        /// ストリームパーシング処理本体
        /// </summary>
        /// <param name="br">バイナリ読み込みオブジェクト</param>
        /// <param name="remain">読み取り可能なバイトサイズ</param>
        /// <returns></returns>
        private dynamic parse(BinaryReader br, long remain)
        {
            while (remain > 7) {
                // ヘッダー分読み込む
                long offset = br.BaseStream.Position;
                byte[] b = br.ReadBytes(7);

                var hdr = new AacAdtsHeader();

                // Sync Header
                if (b[0] != 0xFF && (b[1] & 0xF0) != 0xF0) {
                    System.Diagnostics.Debug.WriteLine("error");
                    return new { Result = 1, Remain = remain };
                }
                // ID
                hdr.ID = ((b[1] & 0x08) == 0x08);
                // Layer, 常に0
                if ((b[1] & 0x06) != 0) {
                    System.Diagnostics.Debug.WriteLine("error");
                    return new { Result = 2, Remain = remain };
                }
                // Protection bit
                hdr.ProtectionBit = ((b[1] & 0x01) == 0x01);

                // Profile
                hdr.Profile = (byte)((b[2] >> 6) & 0x03);
                // Fs
                hdr.Fs = (byte)((b[2] >> 2) & 0x0F);
                // PrivateBit
                hdr.PrivateBit = ((b[2] & 0x02) == 0x02);

                byte tmp = (byte)(b[2] & 0x01);
                tmp <<= 2;
                tmp |= (byte)((b[3] >> 6) & 0x3);
                hdr.Channel = tmp;

                // オリジナル
                hdr.Original = ((b[3] & 0x20) == 0x20);
                hdr.Home = ((b[3] & 0x10) == 0x10);
                hdr.Copyright = ((b[3] & 0x08) == 0x08);
                hdr.Icopyright = ((b[3] & 0x04) == 0x04);

                // フレームサイズ
                UInt16 size = (byte)(b[3] & 0x03);
                size <<= 8;
                size |= b[4];
                size <<= 3;
                size |= (UInt16)(b[5] >> 5);
                hdr.FrameSize = (Int16)size;

                // バッファサイズ
                size = (byte)(b[5] & 0x1F);
                size <<= 6;
                size |= (UInt16)((b[6] >> 2) & 0x3F);
                hdr.BufferSize = (Int16)size;

                // オフセット
                hdr.DBOffset = (byte)(b[6] & 0x03);

                if (!hdr.ProtectionBit) {
                    hdr.CRC = br.ReadUInt16();
                }

                // 保存
                AdtsFrameInfo fi = new AdtsFrameInfo(hdr, offset);
                m_List.Add(fi);

                // 移動
                br.BaseStream.Seek(offset + hdr.FrameSize, SeekOrigin.Begin);
                remain -= hdr.FrameSize;
            }
            return new { Result = 0, Remain = remain };
        }

        /// <summary>
        /// 標準偏差の計算
        /// </summary>
        /// <param name="list">標準偏差を計算するリスト</param>
        /// <returns></returns>
        /// <remarks>Math.NETがないこともあるので、自前で計算</remarks>
        private double calcBpsStdDeviation(List<AdtsFrameInfo> list)
        {
            double sd = 0;
            double avg = list.Select(d => d.Bps).Average();
            foreach (var afi in list) {
                sd += (afi.Bps - avg) * (afi.Bps - avg);
            }
            return Math.Sqrt(sd / list.Count);
        }
    }
}
