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
        public List<Hashtable> lstToolTransaction { get; set; }

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
            var userId = HttpContext.Current.Session["SigninId"].ToString();
            string query = string.Empty;
            query = @"SELECT 
    t.ToolId,
    t.ToolName,
    t.PartNum,
    t.Quantity AS TotalQty,
    ISNULL(t.Quantity - SUM(a.AllocatedQty - a.ReturnedQty), t.Quantity) AS AvailableQty,
    CASE 
        WHEN SUM(a.AllocatedQty - a.ReturnedQty) >= t.Quantity THEN 'Issued'
        ELSE 'Available'
    END AS CurrentStatus,
    t.IsConsumable,
    CASE 
        WHEN ISNULL(t.Quantity - SUM(a.AllocatedQty - a.ReturnedQty), t.Quantity) > 0 THEN CAST(1 AS BIT)
        ELSE CAST(0 AS BIT)
    END AS CanCheckOut,
    CASE 
        WHEN EXISTS (
            SELECT 1 
            FROM ToolAllocation 
            WHERE ToolId = t.ToolId AND UserId = @UserId AND IsReturned = 0
        ) THEN CAST(1 AS BIT)
        ELSE CAST(0 AS BIT)
    END AS CanCheckIn,
Allocations = STUFF((
        SELECT ',' + CAST(a.AllocationId AS VARCHAR)
        FROM ToolAllocation a
        WHERE a.ToolId = t.ToolId 
          AND a.UserId = @UserId
          AND a.IsReturned = 0
        FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 1, '')
FROM Tools t
LEFT JOIN ToolAllocation a 
    ON t.ToolId = a.ToolId AND a.IsReturned = 0
GROUP BY t.ToolId, t.ToolName, t.PartNum, t.Quantity, t.IsConsumable;

  ";
            query = query.Replace("@UserId", userId);
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

        public int GetAvailableQty(int toolId)
        {
            oDAL = new cDAL(cDAL.ConnectionType.INIT);

            string sql = $@"
        SELECT t.Quantity - ISNULL(SUM(a.AllocatedQty), 0) AS AvailableStock
        FROM Tools t
        LEFT JOIN ToolAllocation a 
            ON t.ToolId = a.ToolId AND a.IsReturned = 0
        WHERE t.ToolId = {toolId}
        GROUP BY t.Quantity";

            object result = oDAL.GetObject(sql);
            return result == null ? 0 : Convert.ToInt32(result);
        }

        public bool UserHasTool(int toolId, int userId)
        {
            oDAL = new cDAL(cDAL.ConnectionType.INIT);

            string sql = $@"
        SELECT COUNT(*) 
        FROM ToolAllocation 
        WHERE ToolId = {toolId} AND UserId = {userId} AND IsReturned = 0";

            int count = Convert.ToInt32(oDAL.GetObject(sql));
            return count > 0;
        }
        public bool GetToolTransaction(string toolId)
        {
            cDAL oDAL = new cDAL(cDAL.ConnectionType.ACTIVE);
            string query = @"SELECT 
    tt.TranId,
    t.ToolName,
    tt.ToolId,
    tt.UserId,
    ta.AllocationId,
    ta.CheckoutDate,
    ta.ReturnDate,
CAST(DATEDIFF(MINUTE, ta.CheckoutDate, ISNULL(ta.ReturnDate, GETDATE())) / 1440 AS VARCHAR(10)) + 'd ' +
CAST((DATEDIFF(MINUTE, ta.CheckoutDate, ISNULL(ta.ReturnDate, GETDATE())) % 1440) / 60 AS VARCHAR(10)) + 'h ' +
CAST(DATEDIFF(MINUTE, ta.CheckoutDate, ISNULL(ta.ReturnDate, GETDATE())) % 60 AS VARCHAR(10)) + 'm'
AS Duration,
tt.TranType,
    tt.TranDate,
    tt.TranQty,
    --tt.Notes AS TransactionNotes,
    ta.CheckOutConditionNotes,
    ta.CheckInConditionNotes

FROM dbo.ToolTran tt
INNER JOIN dbo.Tools t 
    ON tt.ToolId = t.ToolId
LEFT JOIN dbo.ToolAllocation ta 
    ON tt.ToolId = ta.ToolId 
   AND tt.UserId = ta.UserId

where t.ToolId = <toolId>
--ORDER BY tt.TranDate DESC;";

            query = query.Replace("<toolId>", toolId);
            DataTable dt = oDAL.GetData(query);

            if (!oDAL.HasErrors)
            {
                lstToolTransaction = new List<Hashtable>();
                lstToolTransaction = cCommon.ConvertDtToHashTable(dt);
                return true;
            }
            else
            {
                return false;
            }
        }

    }
    public class ToolTracking
    {
        public int ToolId { get; set; }
        public string ToolName { get; set; }
        public string PartNum { get; set; }
        public int TotalQty { get; set; }
        public int AvailableQty { get; set; }
        public bool CanCheckOut { get; set; }
        public bool CanCheckIn { get; set; }
    }

    public class ToolTransaction
    {
        public int TranId { get; set; }
        public int ToolId { get; set; }
        public int UserId { get; set; }
        public int TranQty { get; set; }
        public string TranType { get; set; }  // "OUT" or "IN"
        public DateTime TranDate { get; set; }
        public string Notes { get; set; }
    }

    public class ToolAllocation
    {
        public int AllocationId { get; set; }
        public int ToolId { get; set; }
        public int UserId { get; set; }
        public int AllocatedQty { get; set; }
        public DateTime CheckoutDate { get; set; }
        public DateTime? ExpectedReturnDate { get; set; }
        public bool IsReturned { get; set; }
        public DateTime? ReturnDate { get; set; }
        public string ConditionNotes { get; set; }
    }

}