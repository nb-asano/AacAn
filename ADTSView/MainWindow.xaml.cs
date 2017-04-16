using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using ADTSReader;

namespace ADTSView
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>ストリームパーサ</summary>
        private ADTSParser m_Parser;
        /// <summary>表示用タイトル文字列のローカルコピー</summary>
        private readonly string m_Title;

        public MainWindow()
        {
            InitializeComponent();
            m_Parser = new ADTSParser("");
            AssemblyTitleAttribute asmttl = (AssemblyTitleAttribute)Attribute.GetCustomAttribute(
                                                Assembly.GetExecutingAssembly(),
                                                typeof(AssemblyTitleAttribute));
            m_Title = asmttl.Title;
            setTitle(m_Parser.FilePath);
        }

        private void openFile(string filePath)
        {
            var res = openAdtsFile(filePath);
            m_Parser = res.Obj;
            if (res.Result == 0) {
                changeFileView(m_Parser);
            } else {
                MessageBox.Show("ADTS解析に失敗しました。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region 表示関連

        /// <summary>
        /// 描画対象ファイルの変更。ファイル変更時の描画ルート
        /// </summary>
        /// <param name="parser">分析済みファイル</param>
        private void changeFileView(ADTSParser parser)
        {
            this.dataGrid.ItemsSource = createViewerList(parser);
            textBoxTotalFrame.Text = parser.List.Count.ToString();
            textBoxDuration.Text = parser.Duration.ToString();
            textBoxBitRate.Text = parser.AvgBps.ToString("0.00");
            textBoxFs.Text = parser.Freq.ToString();
            textBoxCh.Text = parser.Channel.ToString();
            textBoxProfile.Text = parser.Profile;
            textBoxMaxBitRate.Text = parser.MaxBps.ToString();
            textBoxBitrateStDev.Text = parser.StDevBps.ToString("0.00");
            setUnityString(parser);
            setTitle(parser.FilePath);
        }

        private void setUnityString(ADTSParser parser)
        {
            StringBuilder sb = new StringBuilder();
            if (!parser.IsSingleCh) {
                sb.Append("チャンネル数");
            }
            if (!parser.IsSingleFs) {
                sb.Append((sb.Length > 0) ? "," : "");
                sb.Append("サンプリングレート");
            }
            if (!parser.IsSingleProfile) {
                sb.Append((sb.Length > 0) ? "," : "");
                sb.Append("プロファイル");
            }
            if (sb.Length > 0) {
                labelUni.Content = "非統一：" + sb.ToString();
            }
        }

        private void setTitle(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) {
                this.Title = m_Title;
            } else {
                this.Title = m_Title + " - " + filePath;
            }
        }

        #endregion

        #region メニュー用イベントハンドラ

        private void MenuItem_F_O_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.FileName = "";
            ofd.Filter = "AACファイル(*.aac)|*.aac|ADTSファイル(*.adts)|*.adts|全てのファイル(*.*)|*.*";
            if (ofd.ShowDialog() == true) {
                openFile(ofd.FileName);
            }
        }

        private void MenuItem_F_S_Click(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog();
            sfd.Filter = "CSVファイル(*.csv)|*.csv|全てのファイル(*.*)|*.*";
            if (sfd.ShowDialog() == true) {

            }
        }

        private void MenuItem_F_X_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void MenuItem_V_N_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MenuItem_H_A_Click(object sender, RoutedEventArgs e)
        {
            System.Reflection.Assembly asm = System.Reflection.Assembly.GetExecutingAssembly();
            MessageBox.Show("バージョン:"+ asm.GetName().Version.ToString(), "Version", MessageBoxButton.OK);
        }

        #endregion

        #region その他のイベントハンドラ

        private void Window_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, true)) {
                e.Effects = DragDropEffects.Copy;
            } else {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files != null && files.Length > 0) {
                openFile(files[0]);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string[] files = System.Environment.GetCommandLineArgs();
            if (files.Length > 1) {
                openFile(files[1]);
            }
        }

        #endregion
    }
}
