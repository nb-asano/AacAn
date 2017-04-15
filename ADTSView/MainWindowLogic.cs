using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using ADTSReader;

namespace ADTSView
{
    public partial class MainWindow : Window
    {
        /// <summary>
        /// 表示用のリスト生成
        /// </summary>
        /// <param name="parser">分析済みのパーサー</param>
        /// <returns>生成したリスト</returns>
        private List<ADTSViewEntry> createViewerList(ADTSParser parser)
        {
            var list = new List<ADTSViewEntry>();
            double time = 0;

            foreach (var obj in m_Parser.List.Select((value, index) => new { value, index })) {
                var e = new ADTSViewEntry(obj.index, time, obj.value);
                list.Add(e);
                time += obj.value.Duration;
            }
            return list;
        }

        /// <summary>
        /// ファイルのオープン、解析処理
        /// </summary>
        /// <param name="filePath">ファイルのパス</param>
        /// <returns>オブジェクトおよび解析結果</returns>
        private dynamic openAdtsFile(string filePath)
        {
            // とりあえずここで同期的に読み、読み込み結果もオブジェクトとセットで返しておく
            ADTSParser parser = new ADTSParser(filePath);
            int x = parser.parse();
            return new
            {
                Obj = parser,
                Result = x
            };
        }
    }
}
