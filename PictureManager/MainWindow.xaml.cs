﻿using System;
using System.Collections.Generic;
using System.IO;
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
using System.Data;
using System.Data.SQLite;
using System.Threading;

namespace PictureManager
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
        FileInfo finishedPicture;
        FileInfo savingPicture;

        private void getProgramInfo()
        {
            FileInfo pictureConfig = new FileInfo("settings.config");
            StreamReader sr = new StreamReader("settings.config", Encoding.Unicode);
            while (!sr.EndOfStream)
            {
                string temps = sr.ReadLine();
                if (temps.Count() == 0) continue;
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

        public MainWindow()
        {
            InitializeComponent();

            getProgramInfo();

            finishedPicture = new FileInfo("finished.png");
            if (!finishedPicture.Exists)
                Properties.Resources.finished.Save("finished.png");

            //create a directory to save your pictures
            DirectoryInfo pathFileDirc = new DirectoryInfo(pictureSavingPath);
            if (!pathFileDirc.Exists)
                pathFileDirc.Create();

            //create a directory to 
            picFolder = new DirectoryInfo(pictureBufferPath);
            if (!picFolder.Exists)
                picFolder.Create();

            //select a new picture
            button_save.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, button_save));

            //create tables
            ExecuteQuery("CREATE TABLE IF NOT EXISTS tag(tagname varchar(20), pictureidlist varchar(4000));");
            ExecuteQuery("CREATE TABLE IF NOT EXISTS picture(picturehash varchar(100), pictureid integer primary key, owner varchar(20), picturetype varchar(20));");
        }

        private void ExecuteQuery(string txtQuery)
        {
            using (sql_con = new SQLiteConnection(ConnectionString))
            {
                sql_con.Open();
                sql_cmd = sql_con.CreateCommand();
                sql_cmd.CommandText = txtQuery;
                sql_cmd.ExecuteNonQuery();
            }
        }

        private int getMaxID()
        {
            using (sql_con = new SQLiteConnection(ConnectionString))
            {
                sql_con.Open();
                sql_cmd = sql_con.CreateCommand();
                sql_cmd.CommandText = "select * from picture";
                sql_cmd.ExecuteNonQuery();
                SQLiteDataReader sql_reader = sql_cmd.ExecuteReader();
                bool flag_isNull = true;
                while (sql_reader.Read())
                {
                    flag_isNull = false;
                }
                if (flag_isNull)
                    return 0;
            }
            int maxID = 0;
            using (sql_con = new SQLiteConnection(ConnectionString))
            {
                sql_con.Open();
                sql_cmd = sql_con.CreateCommand();
                sql_cmd.CommandText = "select *, max(pictureid) from picture";
                sql_cmd.ExecuteNonQuery();
                SQLiteDataReader sql_reader = sql_cmd.ExecuteReader();
                while (sql_reader.Read())
                {
                    maxID = Convert.ToInt32(sql_reader["pictureid"]);
                }
            }
            return maxID;
        }

        bool checkTag(string _tagName)
        {
            using (sql_con = new SQLiteConnection(ConnectionString))
            {
                sql_con.Open();
                sql_cmd = sql_con.CreateCommand();
                sql_cmd.CommandText = "select tagname from tag where tagname = @tagname;";
                sql_cmd.Parameters.AddRange(new[] {
                   new SQLiteParameter("@tagname", _tagName)
                });
                sql_cmd.ExecuteNonQuery();
                SQLiteDataReader sql_reader = sql_cmd.ExecuteReader();
                bool tagExist = false;
                while (sql_reader.Read())
                    tagExist = true;
                return tagExist;
            }
        }

        FileInfo nowShowingPicture;
        DirectoryInfo picFolder;
        FileInfo getNextPicture(FileInfo _nowShowingPicture)
        {
            FileInfo[] fileArray = picFolder.GetFiles();
            if (fileArray.Count() == 0)
                return null;
            if (_nowShowingPicture == null)
                return fileArray[0];

            int pos = 0;
            for (int i = 0; i < fileArray.Length; i++)
            {
                if (fileArray[i].Name == _nowShowingPicture.Name)
                {
                    pos = i;
                    break;
                }
            }
            if (fileArray.Length == 1 && pos == 0)
                return null;
            if (pos == fileArray.Length - 1)
                return fileArray[0];
            return fileArray[pos + 1];
        }

        void showNextPicture()
        {
            FileInfo nextPic = getNextPicture(nowShowingPicture);
            if (nextPic != null)
            {
                Media_picture.Source = new Uri(nextPic.FullName);
                nowShowingPicture = nextPic;
            }
            else
            {
                Media_picture.Source = new Uri(finishedPicture.FullName);
                nowShowingPicture = null;
            }
        }

        string bytesToString(byte[] _md5Bytes)
        {
            string opt = "";
            foreach (byte tempbyte in _md5Bytes)
            {
                string temps = Convert.ToString(tempbyte, 16).ToString();
                opt += temps;
            }
            return opt;
        }

        List<string> getPictureListByTag(string _tagName)
        {
            List<string> pictureIDList = new List<string>();
            using (sql_con = new SQLiteConnection(ConnectionString))
            {
                sql_con.Open();
                sql_cmd = sql_con.CreateCommand();
                sql_cmd.CommandText = "select pictureidlist from tag where tagname = @tagname;";
                sql_cmd.Parameters.AddWithValue("@tagname", _tagName);
                SQLiteDataReader sql_reader = sql_cmd.ExecuteReader();
                while (sql_reader.Read())
                {
                    pictureIDList.AddRange(Convert.ToString(sql_reader["pictureidlist"]).Split(' '));
                }
            }
            return pictureIDList;
        }

        void saveData(object _pictureInfo)
        {
            if (_pictureInfo == null) return;
            if (!(_pictureInfo is PictureInfo)) return;
            if (!(_pictureInfo as PictureInfo).Exists) return;
            FileInfo picture;
            List<string> taglist;
            picture = new FileInfo((_pictureInfo as PictureInfo).path);
            taglist = (_pictureInfo as PictureInfo).taglist;

            string Md5String = getMD5(picture);

            int pictureID = -1;
            using (sql_con = new SQLiteConnection(ConnectionString))
            {
                sql_con.Open();
                sql_cmd = sql_con.CreateCommand();
                sql_cmd.CommandText = "select pictureid from picture where picturehash = @picturehash;";
                sql_cmd.Parameters.AddWithValue("@picturehash", Md5String);
                SQLiteDataReader sql_reader = sql_cmd.ExecuteReader();
                while (sql_reader.Read())
                {
                    pictureID = Convert.ToInt32(sql_reader["pictureid"]);
                }
            }

            if (pictureID == -1)
            {
                //add picture's info to database
                using (sql_con = new SQLiteConnection(ConnectionString))
                {
                    sql_con.Open();
                    sql_cmd = new SQLiteCommand("insert into picture(picturehash, owner, picturetype) values(@picturehash,@owner,@picturetype)", sql_con);
                    sql_cmd.Parameters.AddWithValue("@picturehash", Md5String);
                    sql_cmd.Parameters.AddWithValue("@owner", "1");
                    sql_cmd.Parameters.AddWithValue("@picturetype", picture.Extension);
                    sql_cmd.ExecuteNonQuery();
                }
            }

            //save Tags
            int nextID = pictureID == -1 ? getMaxID() : pictureID;
            foreach (string temptag in taglist)
            {
                // illegal tag
                if (temptag == "")
                    continue;

                // this picture is already belong to this tag.
                if (getPictureListByTag(temptag).Contains(nextID.ToString()))
                    continue;

                if (checkTag(temptag))
                {
                    using (sql_con = new SQLiteConnection(ConnectionString))
                    {
                        sql_con.Open();
                        sql_cmd = sql_con.CreateCommand();
                        sql_cmd.CommandText = "update tag set pictureidlist = pictureidlist||@pictureid where tagname = @tagname;";
                        sql_cmd.Parameters.AddRange(new[] {
                            new SQLiteParameter("@pictureid", " "+nextID.ToString()),
                            new SQLiteParameter("@tagname", temptag)
                        });
                        sql_cmd.ExecuteNonQuery();
                    }
                }
                else
                {
                    using (sql_con = new SQLiteConnection(ConnectionString))
                    {
                        sql_con.Open();
                        sql_cmd = sql_con.CreateCommand();
                        sql_cmd.CommandText = "insert into tag(tagname, pictureidlist) values(@tagname, @pictureidlist);";
                        sql_cmd.Parameters.AddRange(new[] {
                            new SQLiteParameter("@tagname", temptag),
                            new SQLiteParameter("@pictureidlist", nextID)
                        });
                        sql_cmd.ExecuteNonQuery();
                    }
                }
            }
            picture.CopyTo(Environment.CurrentDirectory + "/" + pictureSavingPath + "/" + Md5String + picture.Extension, true);
            picture.Delete();
        }

        private string getMD5(FileInfo _picture)
        {
            //get picture's md5
            System.Security.Cryptography.MD5 fileMD5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            string Md5String;
            if (_picture == null)
                return null;
            using (FileStream picStream = new FileStream(_picture.FullName, FileMode.Open))
            {
                byte[] bytemd5 = fileMD5.ComputeHash(picStream);
                Md5String = bytesToString(bytemd5);
            }
            return Md5String;
        }

        private void textBox_tags_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                button_save.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, button_save));
            }
            if (e.Key == Key.Tab)
            {
                //TODO
                //it will help you to finish your Tag quickly
            }
        }

        // Repeat it plz my dear picture.
        private void Media_picture_MediaEnded(object sender, RoutedEventArgs e)
        {
            ((MediaElement)sender).Position = ((MediaElement)sender).Position.Add(TimeSpan.FromMilliseconds(1));
        }

        private void button_save_Click(object sender, RoutedEventArgs e)
        {
            if (nowShowingPicture == null)
            {
                showNextPicture();
                return;
            }
            string tagtext = textBox_tags.Text;
            string[] tags = tagtext.Split(new char[] { ',', '，', ' ', '`', '/', '\\', '、', ';', '；', '<', '>' });
            List<string> taglist = new List<string>();
            for (int i = 0; i < tags.Count(); i++)
            {
                if (tags[i] != "")
                    taglist.Add(tags[i]);
            }

            if (taglist.Count() == 0) return;

            PictureInfo tempinfo = new PictureInfo(taglist, nowShowingPicture.FullName);
            ThreadPool.QueueUserWorkItem(new WaitCallback(saveData), tempinfo);

            showNextPicture();
            textBox_tags.Focus();
            textBox_tags.Text = "";
        }
    }
    class PictureInfo
    {
        public List<string> taglist;
        public string path;
        public bool Exists;

        public PictureInfo(List<string> _taglist, string _path)
        {
            taglist = _taglist;
            path = _path;
            Exists = new FileInfo(path).Exists;
        }
    }
}
