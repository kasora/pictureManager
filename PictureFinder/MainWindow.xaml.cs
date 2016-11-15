using System;
using System.Collections.Generic;
using System.Linq;
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
using System.Data.SQLite;
using System.Data;
using System.IO;

namespace PictureFinder
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private string ConnectionString = "Data Source=" + Environment.CurrentDirectory + "/pictureFinder.db;Version=3;New=False;Compress=True;";
        private string pictureBufferPath;
        private string pictureSavingPath;
        private SQLiteConnection sql_con;
        private SQLiteCommand sql_cmd;

        public MainWindow()
        {
            InitializeComponent();

            getPictureDir();
        }

        private void getPictureDir()
        {
            FileInfo pictureConfig = new FileInfo("settings.config");
            StreamReader sr = new StreamReader("settings.config", Encoding.Unicode);
            while (!sr.EndOfStream)
            {
                string temps = sr.ReadLine();
                if (temps.First() == '#') continue;
                string[] paths = temps.Split(':');
                if (paths[0] == "PictureBufferPath") pictureBufferPath = paths[1];
                if (paths[0] == "PictureSavingPath") pictureSavingPath = paths[1];
            }
        }

        private void textBox_tags_TextChanged(object sender, TextChangedEventArgs e)
        {
            string tags = textBox_tags.Text;
            string[] taglist = tags.Split(new char[] { ',', '，', ' ', '`', '/', '\\', '、', ';', '；', '<', '>' });

        }

        private void updataPictureList(string[] _taglist)
        {
            string sqlstr = "";
        }
    }
}