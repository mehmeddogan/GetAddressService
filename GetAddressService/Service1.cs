using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Threading;
using System.Data.SqlClient;
using System.Runtime.Serialization;
using System.Net.Http;
using System.IO;

//asdasdasd
namespace GetAddressService
{
    [RunInstaller(true)]
    public partial class Service1 : ServiceBase
    {

        int ScheduleTime = Convert.ToInt32(ConfigurationSettings.AppSettings["ThreadTime"]);
        public Thread Worker = null;
        SqlConnection con;
        SqlCommand query = new SqlCommand();
        public Service1()
        {
            InitializeComponent();
            con = new SqlConnection(@"Data Source=MEMMED\SQLEXPRESS;Initial Catalog=Deneme;Integrated Security=True");
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                ThreadStart start = new ThreadStart(Working);
                Worker = new Thread(start);
                Worker.Start();
            }
            catch (Exception)
            {

                throw;
            }


        }
        public void Working()
        {
            SqlCommand cmd1 = new SqlCommand("sp_LatLong1", con);
            SqlDataReader dr1 = cmd1.ExecuteReader();
            SqlCommand cmd2 = new SqlCommand("sp_LatLong2", con);
            SqlDataReader dr2 = cmd2.ExecuteReader();
            while (true)
            {
                if (dr1[0].ToString()!=null || dr1[1].ToString()!=null)
                {
                    Thread th1 = new Thread(InsertAddress1);
                    th1.Start();
                }
                else if (dr2[0].ToString()!=null || dr2[1].ToString()!=null)
                {
                    Thread th2 = new Thread(InsertAddress2);
                    th2.Start();
                }
            }
            Thread.Sleep(ScheduleTime * 60 * 1000);
        }
        private void InsertAddress(string Latitude,string Longitude,string Address)
        {
            query.Connection = con;
            query.CommandType = CommandType.StoredProcedure;
            query.CommandText = "sp_InsertAddress";
            query.Parameters.AddWithValue("@Latitude", Latitude);
            query.Parameters.AddWithValue("@Longitude", Longitude);
            query.Parameters.AddWithValue("@Address", Address);
            query.ExecuteNonQuery();
        }
        private void InsertAddress2()
        {
            SqlCommand cmd2 = new SqlCommand("sp_LatLong2", con);
            SqlDataReader dr2 = cmd2.ExecuteReader();
            string REQUEST_GEO = "2";
            if (con != null && con.State == ConnectionState.Closed)
            {
                con.Open();
                SentCoordinate(dr2[0].ToString(), dr2[1].ToString(), REQUEST_GEO);
                con.Close();
            }
            else
            {
                SentCoordinate(dr2[0].ToString(), dr2[1].ToString(), REQUEST_GEO);
                con.Close();
            }
        }

        private async void InsertAddress1()
        {
            SqlCommand cmd1 = new SqlCommand("sp_LatLong1", con);
            SqlDataReader dr1 = cmd1.ExecuteReader();
            string REQUEST_GEO = "1";
            if (con != null && con.State == ConnectionState.Closed)
            {
                con.Open();
                SentCoordinate(dr1[0].ToString(), dr1[1].ToString(), REQUEST_GEO);
                con.Close();
            }
            else
            {
                SentCoordinate(dr1[0].ToString(), dr1[1].ToString(), REQUEST_GEO);
                con.Close();
            }
            

        }
        private async void SentCoordinate(string Lat,string Long, string REQUEST_GEO)
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri("https://maps.googleapis.com/");
            HttpResponseMessage response = await client.GetAsync("maps/api/geocode/xml?latlng=" + Lat + "," + Long + "&key=AIzaSyADd2ntBd2tlefA5W9BYb4ymnZWSuG2iWU");
            String result = await response.Content.ReadAsStringAsync();
            List<String> addresses = new List<String>();

            using (StreamReader reader = new StreamReader(await response.Content.ReadAsStreamAsync(), Encoding.UTF8))
            {
                DataSet dsResult = new DataSet();
                dsResult.ReadXml(reader);
                try
                {
                    foreach (DataRow row in dsResult.Tables["result"].Rows)
                    {
                        string fullAddress = row["formatted_address"].ToString();
                        //Console.WriteLine(fullAddress);
                        addresses.Add(fullAddress);
                    }
                }
                catch (Exception)
                {

                }
                if (addresses.Count!=0)
                {
                    foreach (String address in addresses)
                    {
                        InsertAddress(Lat, Long, address);
                    }
                    Delete(REQUEST_GEO);
                }
                else
                {

                }
                
            }
        }
        private void Delete(string delete)
        {
            string deleted = "sp_Delete" + delete;
            query.Connection = con;
            query.CommandType = CommandType.StoredProcedure;
            query.CommandText = deleted;
            query.ExecuteNonQuery();
        }
        protected override void OnStop()
        {
            try
            {
                if ((Worker != null) & Worker.IsAlive)
                {
                    Worker.Abort();
                }

            }
            catch (Exception)
            {

                throw;
            }
        }

    }
}
