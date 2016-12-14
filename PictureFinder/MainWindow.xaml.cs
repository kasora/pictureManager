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
using System.Windows.Threading;

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
        private List<FileInfo> showingPictures;
        DirectoryInfo pathFileDirc;
        DispatcherTimer Timer_showPicture;

        public MainWindow()
        {
            InitializeComponent();

            getProgramInfo();

            showingPictures = new List<FileInfo>();

            pathFileDirc = new DirectoryInfo(pictureSavingPath);

            Timer_showPicture = new DispatcherTimer();
            Timer_showPicture.Tick += Timer_showPicture_Tick;
            Timer_showPicture.Interval = new TimeSpan(0,0,1);
        }

        private void getProgramInfo()
        {
            FileInfo pictureConfig = new FileInfo("settings.config");
            StreamReader sr = new StreamReader("settings.config", Encoding.Unicode);
            while (!sr.EndOfStream)
            {
                string temps = sr.ReadLine();
                if (temps.Length == 0) continue;
                if (temps.First() == '#') continue;
                string[] paths = temps.Split(':');
                if (paths.Length == 1)
                {
                    MessageBox.Show("Config file is error! \nPlease check your config file.");
                    continue;
                }
                if (paths[0] == "PictureBufferPath") pictureBufferPath = paths[1];
                if (paths[0] == "PictureSavingPath") pictureSavingPath = paths[1];
            }
        }

        private void textBox_tags_TextChanged(object sender, TextChangedEventArgs e)
        {
            Timer_showPicture.Stop();
            Timer_showPicture.Start();
        }

        private void Timer_showPicture_Tick(object sender, EventArgs e)
        {
            findPicture(getTagsFromTextBox());
            Timer_showPicture.Stop();
        }

        private List<FileInfo> updataPictureList(string[] _taglist)
        {
            List<FileInfo> tempPictures = new List<FileInfo>();

            if (!pathFileDirc.Exists)
                return tempPictures;

            using (sql_con = new SQLiteConnection(ConnectionString))
            {
                sql_con.Open();
                sql_cmd = sql_con.CreateCommand();
            }


            return tempPictures;
        }

        private void textBox_tags_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Timer_showPicture.Stop();
                findPicture(getTagsFromTextBox());
            }
        }

        private string[] getTagsFromTextBox()
        {
            string tags = textBox_tags.Text;
            string[] taglist = tags.Split(new char[] { ',', '，', ' ', '`', '/', '\\', '、', ';', '；', '<', '>' });
            return taglist;
        }

        private void findPicture(string[] tags)
        {
            List<long> list_pictureid = new List<long>();

            // Find pictures' ID.
            using (sql_con = new SQLiteConnection(ConnectionString))
            {
                sql_con.Open();
                bool flag_finded = false;
                foreach (string temps in tags)
                {
                    List<long> templist = new List<long>();
                    sql_cmd = sql_con.CreateCommand();
                    sql_cmd.CommandText = "select pictureidlist from tag where tagname = @tagname;";
                    sql_cmd.Parameters.AddRange(new[] {
                        new SQLiteParameter("@tagname", temps)
                    });
                    sql_cmd.ExecuteNonQuery();
                    SQLiteDataReader sql_reader = sql_cmd.ExecuteReader();
                    while (sql_reader.Read())
                    {
                        string[] s_pictureidlist = Convert.ToString(sql_reader["pictureidlist"]).Split(' ');
                        foreach (string tempi in s_pictureidlist)
                        {
                            templist.Add(Convert.ToInt64(tempi));
                        }
                    }
                    if (!flag_finded)
                    {
                        list_pictureid = templist;
                        if (templist.Count > 0)
                            flag_finded = true;
                    }
                    else
                    {
                        List<long> list_remove = new List<long>();
                        foreach (long tempLong in list_pictureid)
                        {
                            if (!templist.Contains(tempLong))
                            {
                                list_remove.Add(tempLong);
                            }
                        }
                        foreach (long tempLong in list_remove)
                        {
                            list_pictureid.Remove(tempLong);
                        }
                    }
                }
            }

            List<string> list_showingPicture = new List<string>();

            //Find filename by picture ID.
            using (sql_con = new SQLiteConnection(ConnectionString))
            {
                sql_con.Open();
                foreach (long tempLong in list_pictureid)
                {
                    sql_cmd = sql_con.CreateCommand();
                    sql_cmd.CommandText = "select picturehash, picturetype from picture where pictureid = @pictureid;";
                    sql_cmd.Parameters.AddRange(new[] {
                        new SQLiteParameter("@pictureid", tempLong)
                    });
                    sql_cmd.ExecuteNonQuery();
                    SQLiteDataReader sql_reader = sql_cmd.ExecuteReader();
                    while (sql_reader.Read())
                    {
                        string filePath = Environment.CurrentDirectory + "/"
                            + pictureSavingPath + "/"
                            + Convert.ToString(sql_reader["picturehash"]
                            + Convert.ToString(sql_reader["picturetype"]));

                        list_showingPicture.Add(filePath);
                    }
                }
            }

            listBox_shower.Items.Clear();

            // Show pictures
            foreach (string tempFilePath in list_showingPicture)
            {
                ListBoxItem tempItem = new ListBoxItem();
                tempItem.Width = 110;
                tempItem.Height = 110;
                MediaElement tempMedia = new MediaElement();
                tempMedia.Width = 100;
                tempMedia.Height = 100;
                tempMedia.Source = new Uri(tempFilePath);
                tempMedia.MediaEnded += Media_picture_MediaEnded;
                tempMedia.HorizontalAlignment = HorizontalAlignment.Left;
                tempMedia.VerticalAlignment = VerticalAlignment.Top;
                tempItem.HorizontalAlignment = HorizontalAlignment.Left;
                tempItem.VerticalAlignment = VerticalAlignment.Top;
                tempItem.Content = tempMedia;
                tempItem.Selected += ListBoxItem_Selected;
                listBox_shower.Items.Add(tempItem);
            }
        }

        private void ListBoxItem_Selected(object sender, RoutedEventArgs e)
        {
            string path = ((sender as ListBoxItem).Content as MediaElement).Source.AbsolutePath;
            SetImageToClipbord(path);
        }

        //some action will stop gif, jump to next ms can fix it.
        private void Media_picture_MediaEnded(object sender, RoutedEventArgs e)
        {
            ((MediaElement)sender).Position = ((MediaElement)sender).Position.Add(TimeSpan.FromMilliseconds(1));
        }

        //It's magic 
        public void SetImageToClipbord(string p_File)
        {
            var GifFilePath = p_File;
            Clipboard.SetText(string.Format(
                @"Version:0.9
                StartHTML:00000176
                EndHTML:00000326
                StartFragment:00000210
                EndFragment:00000290
                SourceURL:file:///
                <html><body>
                <!--StartFragment-->
                <p><img src=""file:///{0}"" /></p>
                <!--EndFragment-->
                </body>
                </html>
                ",
                GifFilePath.Replace("\\", "/")), 
                TextDataFormat.Html);
        }
    }
}