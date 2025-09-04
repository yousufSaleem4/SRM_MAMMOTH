using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;

namespace PlusCP.Models
{
    public class Tool
    {
        public System.Web.Script.Serialization.JavaScriptSerializer serializer { get; set; }
        public string filterString { get; set; }
        public string ErrorMessage { get; set; }
        public string ErrorMessageUpdate { get; set; }
        public List<Hashtable> lstTool { get; set; }
        public List<Hashtable> lstGetTool { get; set; }
        public string Email { get; set; }
        public string Message { get; set; }
        cDAL oDAL;
        public bool GetList()
        {
            oDAL = new cDAL(cDAL.ConnectionType.INIT);
            string query = string.Empty;
            query = @"SELECT 
    [Username]
      ,[UserId]
    ,ISNULL(NULLIF(FirstName, ''), '-') AS FirstName,
    ISNULL(NULLIF(LastName, ''), '-') AS LastName,
    Email, 
    [Status] 
FROM [SRM].[Invitation] I
--WHERE [Type] = 'Supplier'
ORDER BY Id DESC  ";

            DataTable dt = oDAL.GetData(query);

            if (oDAL.HasErrors)
            {
                ErrorMessage = oDAL.ErrMessage;
                return false;
            }
            else
            {
                if (dt.Rows.Count > 0)
                    lstTool = cCommon.ConvertDtToHashTable(dt);
                return true;
            }
        }

        public DataTable GetToolUsers()
        {
            DataTable dt = new DataTable();
            cDAL oDAL = new cDAL(cDAL.ConnectionType.INIT);
            string sql = "SELECT distinct UserID AS ID, Name AS [NAME] FROM [TOOL].[SysUserFile]";
            dt = oDAL.GetData(sql);

            // Add "Select from List" row at the top
            if (dt != null && dt.Rows.Count > 0)
            {
                DataRow newRow = dt.NewRow();
                newRow["ID"] = "";
                newRow["NAME"] = "Select from List";
                dt.Rows.InsertAt(newRow, 0);
            }

            return dt;
        }

        public string GetUserEmail(string userId)
        {
            string email = string.Empty;
            cDAL oDAL = new cDAL(cDAL.ConnectionType.INIT);
            string sql = "SELECT TOP 1 [EMailAddress] FROM [TOOL].[SysUserFile] WHERE [UserID] = '" + userId + "'";

            DataTable dt = oDAL.GetData(sql);

            if (dt != null && dt.Rows.Count > 0)
            {
                email = dt.Rows[0]["EMailAddress"].ToString();
            }

            return email;
        }
        public string SendInvite(string Type, string Email, string userId, string username)
        {
            try
            {
                oDAL = new cDAL(cDAL.ConnectionType.INIT);
                cAuth oAuth = new cAuth();
                string sql = "";
                sql = "Select * from SRM.UserInfo Where Email = '" + Email + "'";
                DataTable dtCheck = new DataTable();
                dtCheck = oDAL.GetData(sql);
                if (dtCheck.Rows.Count == 0)
                {
                    string createdBy = HttpContext.Current.Session["Email"].ToString();
                    string ConnectionType = HttpContext.Current.Session["DefaultDB"].ToString();
                    string AdminName = HttpContext.Current.Session["LastName"].ToString() + ' ' + HttpContext.Current.Session["FirstName"].ToString();
                    Guid newGuid = Guid.NewGuid();
                    sql = @"INSERT INTO [SRM].[Invitation]
           (
           [Email]
      ,[UserId]
           ,[Type]
           ,[GUID]
           ,[CreatedBy]
           ,[ConnectionType]
           ,[Username]
           )
           VALUES
           (
		   '<Email>'
           ,'<userId>'
           ,'<Type>'
           ,'<GUID>'
           ,'<CreatedBy>'
           ,'<ConnectionType>'
           ,'<username>'
		   )";

                    sql = sql.Replace("<Email>", Email);
                    sql = sql.Replace("<userId>", userId);
                    sql = sql.Replace("<Type>", Type);
                    sql = sql.Replace("<GUID>", newGuid.ToString());
                    sql = sql.Replace("<CreatedBy>", createdBy);
                    sql = sql.Replace("<ConnectionType>", ConnectionType);
                    sql = sql.Replace("<username>", username);
                    oDAL.Execute(sql);
                    if (!oDAL.HasErrors)
                    {
                        string DeployMode = HttpContext.Current.Session["DefaultDB"].ToString();

                        //URL
                        DataTable dtURL = new DataTable();
                        dtURL = cCommon.GetEmailURL(DeployMode, "Registration");
                        string Url = dtURL.Rows[0]["URL"].ToString();
                        string Baseurl = Url + dtURL.Rows[0]["PageURL"].ToString() + newGuid;


                        var subject = "Collablly Account Invitation";
                        // Recipient's email address
                        string recipientEmail = Email;

                        string htmlBody = "";

                        // HTML body containing the form
                        DataTable dt = new DataTable();
                        string query = "";
                        cDAL oDAL = new cDAL(cDAL.ConnectionType.INIT);
                        query = "Select SYSValue from dbo.zSysIni WHERE SysDesc = 'JOINSRM' ";
                        dt = oDAL.GetData(query);
                        htmlBody = dt.Rows[0]["SYSValue"].ToString();


                        htmlBody = htmlBody.Replace("{AdminName}", AdminName);
                        htmlBody = htmlBody.Replace("{Baseurl}", Baseurl);
                        try
                        {
                            cCommon.SendEmail(recipientEmail, subject, htmlBody, "", null);
                            Message = "OK";
                            return Message;
                        }
                        catch (Exception)
                        {
                            Message = "Wrong";
                            return Message;
                        }
                    }
                    else
                    {
                        Message = "Wrong";
                        return Message;
                    }
                }
                else
                {
                    Message = "Already";
                    return Message;
                }
            }
            catch (Exception ex)
            {

                cLog oLog = new cLog();
                oLog.RecordError(ex.Message, ex.StackTrace, "Invitation Send Button");
                Message = "Wrong";
                return Message;
            }

        }



        public DataTable GetToolDropdown()
        {
            DataTable dt = new DataTable();
            cDAL oDAL = new cDAL(cDAL.ConnectionType.INIT);
            string sql = "SELECT distinct ToolId AS ID, ToolName AS [NAME] FROM dbo.Tools";
            dt = oDAL.GetData(sql);

            // Add "Select from List" row at the top
            if (dt != null && dt.Rows.Count > 0)
            {
                DataRow newRow = dt.NewRow();
                newRow["ID"] = 0;   // 0 = Dummy "Select" value
                newRow["NAME"] = "Select from List";
                dt.Rows.InsertAt(newRow, 0);

            }

            return dt;
        }
        public bool GetToolList()
        {
            oDAL = new cDAL(cDAL.ConnectionType.INIT);
            string query = string.Empty;
            query = @"SELECT [ToolId]
      ,[ToolName]
      ,[PartNum]
      ,[Quantity]
      ,[PurchaseDate]
      ,[PurchaseCost]
      ,[CurrentStatus]
      ,[CalibrationDueDate]
      ,[LastMaintenanceDate]
      ,[IsConsumable]
  FROM [dbo].[Tools]
  ";

            DataTable dt = oDAL.GetData(query);

            if (oDAL.HasErrors)
            {
                ErrorMessage = oDAL.ErrMessage;
                return false;
            }
            else
            {
                if (dt.Rows.Count > 0)
                    lstGetTool = cCommon.ConvertDtToHashTable(dt);
                return true;
            }
        }
    }
}