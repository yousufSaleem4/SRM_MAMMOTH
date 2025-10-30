using IP.Classess;
using PlusCP.Classess;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data.Sql;
using System.Data.SqlClient;
namespace PlusCP.Externals
{
    public partial class POHeadPDF : System.Web.UI.Page
    {
        public string DBConnectionString { get; set; }
        

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                string PONo = "";
                string ConnectionType = "";
                string GUID = "";

                // Get Query String Values
                PONo = Request.QueryString["PONo"]?.ToString();
                ConnectionType = Request.QueryString["Connection"]?.ToString();
                GUID = Request.QueryString["GUID"]?.ToString();

                

                // Build SQL
                string sql = @"SELECT [Id]
                             ,[PONumber]
                             ,[Vendor]
                             ,[VendorEmail]
                             ,[Buyer]
                        FROM [dbo].[BuyerPOHeader]
                        WHERE PONumber = @PONo";

                // Read Connection String
                string fileName = ConfigurationManager.AppSettings["Key"];
                string DBConnectionString = BasicEncrypt.Instance.Decrypt(System.IO.File.ReadAllLines(fileName)[0].ToString());

                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(DBConnectionString))
                {
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@PONo", PONo);
                        SqlDataAdapter da = new SqlDataAdapter(cmd);
                        da.Fill(dt);
                    }
                }

                if (dt.Rows.Count > 0)
                {
                    gvPOData.DataSource = dt;
                    gvPOData.DataBind();
                }
               
            }
        }


        protected void btnAccept_Click(object sender, EventArgs e)
        {
            string PONo = Request.QueryString["PONo"];
            string GUID = Request.QueryString["GUID"];

            ExternalDAL oDAL = new ExternalDAL();
            string fileName = ConfigurationManager.AppSettings["Key"];
            DBConnectionString = BasicEncrypt.Instance.Decrypt(System.IO.File.ReadAllLines(fileName)[0]);

            string sql = @"UPDATE [dbo].[BuyerPOHeader] 
                           SET STATUS='Received',
                               UpdatedOn=GETDATE(),
                               UpdatedBy='Vendor',
                               IsAnswerd=1
                           WHERE PONumber='" + PONo + "' AND GUID='" + GUID + "'";

            oDAL.Execute(sql, DBConnectionString);

            // Success message
            ClientScript.RegisterStartupScript(this.GetType(), "alert", "alert('PO Accepted Successfully!'); closeModal();", true);
        }

        protected void btnReject_Click(object sender, EventArgs e)
        {
            string PONo = Request.QueryString["PONo"];
            string GUID = Request.QueryString["GUID"];

            ExternalDAL oDAL = new ExternalDAL();
            string fileName = ConfigurationManager.AppSettings["Key"];
            DBConnectionString = BasicEncrypt.Instance.Decrypt(System.IO.File.ReadAllLines(fileName)[0]);

            string sql = @"UPDATE [dbo].[BuyerPOHeader] 
                           SET STATUS='Rejected',
                               UpdatedOn=GETDATE(),
                               UpdatedBy='Vendor',
                               IsAnswerd=1
                           WHERE PONumber='" + PONo + "' AND GUID='" + GUID + "'";

            oDAL.Execute(sql, DBConnectionString);

            // Success message
            ClientScript.RegisterStartupScript(this.GetType(), "alert", "alert('PO Rejected Successfully!'); closeModal();", true);
        }

        //protected void Page_Load(object sender, EventArgs e)
        //{

        //    string PONo = "";
        //    string ConnectionType = "";
        //    string GUID = "";
        //    PONo = Request.QueryString["PONo"].ToString();
        //    ConnectionType = Request.QueryString["Connection"].ToString();
        //    GUID = Request.QueryString["GUID"].ToString();
        //    string sql = "";
        //    DataTable dt = new DataTable();
        //    ExternalDAL oDAL = new ExternalDAL();
        //    string fileName = ConfigurationManager.AppSettings["Key"];
        //    DBConnectionString = BasicEncrypt.Instance.Decrypt(System.IO.File.ReadAllLines(fileName)[0].ToString());
        //    sql = "SELECT [Id]\r\n      ,[PONumber]\r\n      ,[Vendor]\r\n      ,[VendorEmail]\r\n  ,[Buyer]\r\n  FROM [SRMDBPILOT].[dbo].[BuyerPOHeader] WHERE PONumber = '<PO>' ";

        //    sql = sql.Replace("<PO>", PONo);


        //    //sql = "Select IsAnswerd from [dbo].[BuyerPOHeader] WHERE PONumber = '<PO>' AND GUID = '<GUID>' AND IsAnswerd = 0";
        //    //sql = sql.Replace("<PO>", PONo);
        //    //sql = sql.Replace("<GUID>", GUID);
        //    dt = oDAL.GetData(sql, DBConnectionString);

        //    //if (dt.Rows.Count > 0)
        //    //{
        //    //    DateTime LocalDateTime = ConvertGenericDateTime();

        //    //    sql = "UPDATE [dbo].[BuyerPOHeader] SET STATUS = 'Received', UpdatedOn = '<UpdateOn>', UpdatedBy = 'Vendor', IsAnswerd = 1 WHERE PONumber = '<PO>' AND GUID = '<GUID>' ";
        //    //    sql = sql.Replace("<PO>", PONo);
        //    //    sql = sql.Replace("<GUID>", GUID);
        //    //    sql = sql.Replace("<UpdateOn>", LocalDateTime.ToString());

        //    //    oDAL.Execute(sql, DBConnectionString);
        //    //}
        //    //else
        //    //{
        //    //    // If already answered, change the message
        //    //    lblMessage.Text = @"<h2>⚠️ Already Acknowledged</h2>
        //    //               <p>You have already acknowledged this PO.</p>
        //    //               <p>No further action is required.</p>";
        //    //}
        //}
        public DateTime ConvertGenericDateTime()
        {
            string sql = "";
            DataTable dt = new DataTable();
            ExternalDAL oDAL = new ExternalDAL();
            sql = "Select top 1 TimeZone from SRM.UserInfo ";
            string fileName = ConfigurationManager.AppSettings["Key"];
            DBConnectionString = BasicEncrypt.Instance.Decrypt(System.IO.File.ReadAllLines(fileName)[0].ToString());

            dt = oDAL.GetData(sql, DBConnectionString);
            DateTime localTime = new DateTime();
            if (dt.Rows.Count > 0)
            {
                string timezone = dt.Rows[0]["TimeZone"].ToString(); //HttpContext.Current.Session["TimeZone"].ToString();

                DateTime utcTime = DateTime.UtcNow;

                // Get the TimeZoneInfo object for the selected timezone
                TimeZoneInfo selectedTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timezone);

                // Convert UTC time to the selected time zone, DST-aware
                 localTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, selectedTimeZone);
            }
           

            return localTime;
        }

    }
}