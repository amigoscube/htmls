using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Services;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class generator : System.Web.UI.Page
{

    public static String ConnectionString = ConfigurationManager.ConnectionStrings["testdbConnectionString"].ConnectionString;
    
    protected void Page_Load(object sender, EventArgs e)
    {

    }

    public static SqlConnection DbOpen()
    {
        SqlConnection con = new SqlConnection(ConnectionString);
        con.Open();

        return con;
    }
    public static void DbClose(SqlConnection con)
    {
        con.Close();
    }

    [WebMethod]
    public static Boolean HtmlFileSea(String htmlfile, String validfrom, String validto)
    {
        DateTime vf = DateTime.Parse(validfrom);
        DateTime vt = DateTime.Parse(validto);
        bool status = false;
        try
        {
            SqlConnection con = DbOpen();
            SqlCommand cmd = new SqlCommand("st_HTMLFileSea", con);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Parameters.Add("@HTMLFilename", System.Data.SqlDbType.VarChar).Value = htmlfile;
            cmd.Parameters.Add("@ValidFrom", System.Data.SqlDbType.DateTime).Value = vf;
            cmd.Parameters.Add("@ValidTo", System.Data.SqlDbType.DateTime).Value = vt;
//            cmd.Parameters.Add("@TimedOut", System.Data.SqlDbType.Bit).Value = timedout;
//            cmd.Parameters.Add("@NumberRequired", System.Data.SqlDbType.TinyInt).Value = number;


            SqlDataReader data = cmd.ExecuteReader();
            if (data.Read())
            {
                String htmlfilename = data.GetString(1);
                Decimal longitude = data.GetDecimal(2);
                Decimal latitude = data.GetDecimal(3);
                String ftphost = data.GetString(5);
                String ftpUsername = data.GetString(6);
                String ftpPassword = data.GetString(7);
                String Header = data.GetString(8);
                String Footer = data.GetString(9);

                //Call for HTML Content
                String content = HTMLFileDetailSea(longitude, latitude);

                //call for create html file
                String HtmlfilePath = CreateHtml(htmlfilename, Header, content, Footer);

                //upload file in ftp server
              status = FtpUpload(HtmlfilePath, ftpUsername, ftpPassword, ftphost);
            }

            DbClose(con);
        }
        catch (Exception e)
        {
            Debug.Print("Error: " + e.Message);
            HttpContext.Current.Response.Write("Error: " + e.Message);
        }

        return status;
    }

    /// <summary>
    /// 2nd Stored Procedure Call 
    /// </summary>
    /// <param name="longitude">Longitude</param>
    /// <param name="latitude">Latitude</param>
    /// <returns>HTML Body Content for html page</returns>
    public static String HTMLFileDetailSea(Decimal longitude, Decimal latitude)
    {
        var content = "";

        try
        {
            SqlConnection con = DbOpen();
            SqlCommand cmd = new SqlCommand("st_HTMLFileDetailSea", con);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Parameters.Add("@Longitude", System.Data.SqlDbType.Decimal).Value = longitude;
            cmd.Parameters.Add("@Latitude", System.Data.SqlDbType.Decimal).Value = latitude;

            SqlDataReader data = cmd.ExecuteReader();
            if (data.Read())
            {
                content = data.GetString(0);
            }

            DbClose(con);
        }
        catch (Exception e)
        {
            Debug.Print("Error: " + e.Message);
        }

        return content;
    }

    /// <summary>
    /// Creates a HTML Page in the Server
    /// </summary>
    /// <param name="filename">HTML File Name</param>
    /// <param name="header">Header for the HTML Page</param>
    /// <param name="content">Content for the HTML Page</param>
    /// <param name="footer">Footer for the HTML Page</param>
    /// <returns>Path for the html file created</returns>
    public static String CreateHtml(string filename, string header, string content, string footer)
    {
        var path = HttpContext.Current.Server.MapPath("html");
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        using (StreamWriter sw = new StreamWriter(HttpContext.Current.Server.MapPath("html/" + filename + ".html")))
        {
            using (HtmlTextWriter hw = new HtmlTextWriter(sw))
            {
                //html tag begins here
                hw.RenderBeginTag(HtmlTextWriterTag.Html);

                //head tag begins
                hw.RenderBeginTag(HtmlTextWriterTag.Head);
                hw.Write("<title>" + filename + "</title>");
                hw.RenderEndTag(); // head tag ends here

                //body tag begins
                hw.RenderBeginTag(HtmlTextWriterTag.Body);
                hw.Write(header + content + footer);
                hw.RenderEndTag(); //body tag ends here


                hw.RenderEndTag(); //html tag ends
            }
        }

        return path + Path.DirectorySeparatorChar + filename + ".html";
    }

    /// <summary>
    /// Uploads the created HTML file
    /// </summary>
    /// <param name="filepath">FilePath for the HTML File</param>
    /// <param name="Username">FTP Username</param>
    /// <param name="Password">FTP Password</param>
    /// <param name="url">Url for the FTP Server</param>
    private static Boolean FtpUpload(String filepath, String Username, String Password, String url)
    {
        String name = Path.GetFileName(filepath);
        Boolean TransferCode = false;
        string ftpPath = url + "/" + name;
        FtpWebRequest fw = (FtpWebRequest)FtpWebRequest.Create(ftpPath);
        fw.Credentials = new NetworkCredential(Username, Password);
        fw.KeepAlive = true;
        fw.UseBinary = true;
        fw.Method = WebRequestMethods.Ftp.UploadFile;
        FileStream fs = File.OpenRead(filepath);
        byte[] buffer = new byte[fs.Length];
        fs.Read(buffer, 0, buffer.Length);
        fs.Close();

        Stream ftpstream = fw.GetRequestStream();
        ftpstream.Write(buffer, 0, buffer.Length);
        ftpstream.Close();

        FtpWebResponse response = (FtpWebResponse)fw.GetResponse();
        String status = response.StatusDescription;

        //Status Message from FTP Server
        if(status.Contains("Transfer complete")){
            TransferCode = true;
        }

        return TransferCode;
    }

    [WebMethod]
    public static HtmlRenderData[] GetData() {
        List<HtmlRenderData> lists = new List<HtmlRenderData>();
        try
        {
            SqlConnection con = DbOpen();
            SqlCommand cmd = new SqlCommand("Select HTMLFilename, ValidFrom, ValidTo from HTMLFile", con);
            SqlDataReader data = cmd.ExecuteReader();
            while (data.Read()) {
                HtmlRenderData hrd = new HtmlRenderData();
                hrd.HtmlFilename = data.GetString(0);
                hrd.ValidFrom = data.GetDateTime(1);
                hrd.ValidTo = data.GetDateTime(2);

                lists.Add(hrd);
            }
        }
        catch(Exception e) {
            HttpContext.Current.Response.Write(e.Message);
        }

        return lists.ToArray();
    }

    public class HtmlRenderData{
        public String HtmlFilename { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
    }
}