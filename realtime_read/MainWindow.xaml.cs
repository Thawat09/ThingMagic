using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using MySql.Data.MySqlClient;
using ThingMagic;

namespace realtime_read
{
    public partial class MainWindow : Window
    {
        private static readonly HttpClient client = new HttpClient();
        private MySqlConnection conn;

        public MainWindow()
        {
            InitializeComponent();
            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0);
            dispatcherTimer.Start();

            string connStr = "server=localhost;user=root;database=project;port=3306;password=sudb6224769";
            conn = new MySqlConnection(connStr);
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        public async void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            Reader reader = Reader.Create("eapi:///com6");
            try
            {
                reader.Connect();
            }
            catch (IOException a)
            {
                Console.WriteLine(a.Message);
                return;
            }
            if (Reader.Region.UNSPEC.Equals(reader.ParamGet("/reader/region/id")))
            {
                reader.ParamSet("/reader/region/id", Reader.Region.NA);
            }
            if (reader is SerialReader)
            {
                SerialReader.TagMetadataFlag flagSet = SerialReader.TagMetadataFlag.ALL;
                reader.ParamSet("/reader/metadata", flagSet);
            }
            int[] antennaList = new int[] { 1, 2 };
            SimpleReadPlan plan = new SimpleReadPlan(antennaList, TagProtocol.GEN2, null, null, 1000);
            reader.ParamSet("/reader/read/plan", plan);
            TagReadData[] tagReads = reader.Read(5000);
            reader.Destroy();

            try
            {
                conn.Open();
                string sql = "";
                MySqlCommand cmd;

                for (int i = 0; i < tagReads.Length; i++)
                {
                    if (tagReads[i].EpcString != "E28011700000020FEBE848AD")
                    {
                        sql = $"SELECT COUNT(*) FROM test WHERE tag_number = '{tagReads[i].EpcString}'";
                        cmd = new MySqlCommand(sql, conn);
                        int count = Convert.ToInt32(cmd.ExecuteScalar());

                        DateTime currentDate = DateTime.Now;
                        string timestamp = currentDate.ToString("yyyy-MM-dd HH:mm:ss");

                        if (count == 0)
                        {
                            sql = $"INSERT INTO test (tag_number, date) VALUES ('{tagReads[i].EpcString}', '{timestamp}')";
                        }
                        else
                        {
                            sql = $"UPDATE test SET date = '{timestamp}' WHERE tag_number = '{tagReads[i].EpcString}'";
                        }

                        cmd = new MySqlCommand(sql, conn);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                conn.Close();
            }

            bool isConnectedToInternet = CheckForInternet();
            if (isConnectedToInternet)
            {
                textbox1.Text = "Data Saved Successfully!";
                try
                {
                    var value = new Dictionary<string, string>
                    {
                        { "Building", "F11" },
                        { "Tagid", tagReads[0].EpcString }  // Assuming you're sending the first read. Modify as per your requirements.
                    };
                    var content = new FormUrlEncodedContent(value);
                    await client.PostAsync("http://www.thzsoo.com/electronic-lab/api.php/tagid", content);
                }
                catch (Exception)
                {

                }
            }
            else
            {
                textbox1.Text = "Network Error!!!";
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
        }

        public static bool CheckForInternet()
        {
            try
            {
                using (var client = new WebClient())
                using (client.OpenRead("https://script.google.com/macros/s/AKfycbwZfJvc3fn1wCzNiSKVydIMkYbKosfVd3zWI4iNxW-bsuBi5PUHgz0hXWo9zpOz_oV_/exec"))
                    return true;
            }
            catch
            {
                return false;
            }
        }
    }
}