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


        public int ToolId { get; set; }
        public string ToolName { get; set; }
        public string PartNum { get; set; }
        public int Quantity { get; set; }   // total qty
        public DateTime PurchaseDate { get; set; }
        public decimal PurchaseCost { get; set; }
        public string CurrentStatus { get; set; }   // Available / Issued
        public DateTime? CalibrationDueDate { get; set; }
        public DateTime? LastMaintenanceDate { get; set; }
        public bool IsConsumable { get; set; }


        public List<Hashtable> lstTool { get; set; }
        public List<Hashtable> lstGetTool { get; set; }
        public string Email { get; set; }
        public string Message { get; set; }
        public List<Hashtable> lstToolTransaction { get; private set; }

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
                        query = "Select SYSValue from TOOL.zSysIni WHERE SysDesc = 'JOINSRM' ";
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
            string sql = "SELECT distinct ToolId AS ID, ToolName AS [NAME] FROM TOOL.Tools";
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
            query = $@"SELECT 
    t.ToolId,
    t.ToolName,
    t.PartNum,
    t.TotalQty AS TotalQty,

    -- Available = count of ToolSerials that are Available
    ISNULL(SUM(CASE WHEN ts.Status = 'Available' THEN 1 ELSE 0 END), 0) AS AvailableQty,

    -- Status at tool-level (if all units are issued, mark as Issued)
    CASE 
        WHEN SUM(CASE WHEN ts.Status = 'Available' THEN 1 ELSE 0 END) = 0 THEN 'Issued'
        ELSE 'Available'
    END AS CurrentStatus,

    t.IsConsumable,

    -- Can check out if at least one serial is Available
    CASE 
        WHEN SUM(CASE WHEN ts.Status = 'Available' THEN 1 ELSE 0 END) > 0 THEN CAST(1 AS BIT)
        ELSE CAST(0 AS BIT)
    END AS CanCheckOut,

    -- Can check in if current user has any unreturned allocations
    CASE 
        WHEN EXISTS (
            SELECT 1 
            FROM Tool.ToolAllocation a
            INNER JOIN Tool.ToolSerials s ON a.SerialId = s.SerialId
            WHERE a.ToolId = t.ToolId 
              AND a.UserId = ${userId} -- replace with logged-in userId
              AND a.IsReturned = 0
        ) THEN CAST(1 AS BIT)
        ELSE CAST(0 AS BIT)
    END AS CanCheckIn,

    -- All allocations (for check-in buttons)
    Allocations = STUFF((
        SELECT ',' + CAST(a.AllocationId AS VARCHAR(50))
        FROM Tool.ToolAllocation a
        WHERE a.ToolId = t.ToolId
          AND a.UserId = 25
          AND a.IsReturned = 0
        FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 1, '')

FROM Tool.Tools t
LEFT JOIN Tool.ToolSerials ts ON t.ToolId = ts.ToolId
GROUP BY t.ToolId, t.ToolName, t.PartNum, t.TotalQty, t.IsConsumable;


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

        public DataTable GetToolStats()
        {
            cDAL oDAL = new cDAL(cDAL.ConnectionType.INIT);

            string sql = @"
SELECT 
    -- Total tools count
    (SELECT COUNT(*) FROM [TOOL].[Tools]) AS TotalTools,

    -- Available tools (TotalQty - User ke active checkouts)
    (SELECT 
         SUM(t.TotalQty) - ISNULL(SUM(chk.CheckedOutQty), 0)
     FROM [TOOL].[Tools] t
     OUTER APPLY (
         SELECT COUNT(*) AS CheckedOutQty
         FROM [TOOL].[ToolAllocation] a
         WHERE a.ToolId = t.ToolId
           AND a.UserId = " + HttpContext.Current.Session["SigninId"].ToString() + @"
           AND a.IsReturned = 0
     ) chk
    ) AS Available,

    -- Checked out count (transactions OUT by user)
    (SELECT COUNT(*) 
     FROM [TOOL].[ToolAllocation]
     WHERE isreturned = 0
         AND UserId = " + HttpContext.Current.Session["SigninId"].ToString() + @"
    ) AS CheckedOut;

";

            return oDAL.GetData(sql);
        }

        public DataTable GetCheckedOutWeeklyStats()
        {
            cDAL oDAL = new cDAL(cDAL.ConnectionType.INIT);

            string sql = @"
      SELECT 
    DATEPART(YEAR, TranDate) AS YearNumber,
    DATEPART(WEEK, TranDate) AS WeekNumber,
    COUNT(*) AS CheckedOutCount
FROM [TOOL].[ToolTransactions]
WHERE TranType = 'OUT'
 AND UserId = " + HttpContext.Current.Session["SigninId"].ToString() + @"
        GROUP BY DATEPART(YEAR, TranDate), DATEPART(WEEK, TranDate)
ORDER BY YearNumber, WeekNumber; ";

            return oDAL.GetData(sql);
        }

        public DataTable GetTopUsedToolStats()
        {
            cDAL oDAL = new cDAL(cDAL.ConnectionType.INIT);

            string sql = @"
        SELECT TOP 3
            t.ToolId,
            tm.ToolName,
            COUNT(*) AS UsageCount
        FROM TOOL.[ToolTransactions] t
        INNER JOIN TOOL.[Tools] tm ON t.ToolId = tm.ToolId
        WHERE t.TranType = 'OUT'
          AND t.UserId = " + HttpContext.Current.Session["SigninId"].ToString() + @"
          GROUP BY t.ToolId, tm.ToolName
        ORDER BY UsageCount DESC;
    ";

            return oDAL.GetData(sql);
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
            var userId = HttpContext.Current.Session["SigninId"];

            string query = $@"

SELECT 
    t.[TranId],
    t.[SerialId] AS ToolSerialIds,      
    ts.[ToolName],
    CONCAT(u.FirstName, ' ', u.LastName) AS UserName,
    t.[TranType],
    t.[TranQty],
    FORMAT(t.[TranDate], 'yyyy-MM-dd HH:mm') AS TranDate,
    MAX(ta.ExpectedReturnDate) AS ExpectedReturnDate,
    t.[Notes],
    -- Possession duration computed from allocations that belong to this transaction's serials
    CASE 
      WHEN MIN(ta.CheckoutDate) IS NULL THEN '-' 
      ELSE
        CASE 
          WHEN DATEDIFF(MINUTE, MIN(ta.CheckoutDate), ISNULL(MAX(ta.ReturnDate), GETDATE())) < 60
            THEN CAST(DATEDIFF(MINUTE, MIN(ta.CheckoutDate), ISNULL(MAX(ta.ReturnDate), GETDATE())) AS VARCHAR(10)) + ' min'
          WHEN DATEDIFF(HOUR, MIN(ta.CheckoutDate), ISNULL(MAX(ta.ReturnDate), GETDATE())) < 24
            THEN CAST(DATEDIFF(HOUR, MIN(ta.CheckoutDate), ISNULL(MAX(ta.ReturnDate), GETDATE())) AS VARCHAR(10)) + ' hrs'
          WHEN DATEDIFF(DAY, MIN(ta.CheckoutDate), ISNULL(MAX(ta.ReturnDate), GETDATE())) < 30
            THEN CAST(DATEDIFF(DAY, MIN(ta.CheckoutDate), ISNULL(MAX(ta.ReturnDate), GETDATE())) AS VARCHAR(10)) + ' days'
          WHEN DATEDIFF(MONTH, MIN(ta.CheckoutDate), ISNULL(MAX(ta.ReturnDate), GETDATE())) < 12
            THEN CAST(DATEDIFF(MONTH, MIN(ta.CheckoutDate), ISNULL(MAX(ta.ReturnDate), GETDATE())) AS VARCHAR(10)) + ' months'
          ELSE CAST(DATEDIFF(YEAR, MIN(ta.CheckoutDate), ISNULL(MAX(ta.ReturnDate), GETDATE())) AS VARCHAR(10)) + ' yrs'
        END
    END AS PossessionDuration
FROM [TOOL].[ToolTransactions] t
INNER JOIN [TOOL].[Tools] ts
    ON t.ToolId = ts.ToolId
INNER JOIN [SRM].[UserInfo] u
    ON t.UserId = u.UserId

-- split the SerialId CSV into rows (trim spaces)
OUTER APPLY (
    SELECT LTRIM(RTRIM(value)) AS Serial
    FROM STRING_SPLIT(t.SerialId, ',')
) s

-- join allocations only for those serials
LEFT JOIN [TOOL].[ToolAllocation] ta
    ON ta.SerialId = s.Serial
   AND ta.ToolId = t.ToolId
   AND ta.UserId = t.UserId

WHERE t.UserId = {userId}
  AND t.ToolId = {toolId}

GROUP BY
    t.[TranId],
    t.[SerialId],
    ts.[ToolName],
    u.FirstName,
    u.LastName,
    t.[TranType],
    t.[TranQty],
    t.[TranDate],
    t.[Notes]

ORDER BY MAX(t.TranDate) DESC;
";

            DataTable dt = oDAL.GetData(query);

            if (!oDAL.HasErrors)
            {
                lstToolTransaction = new List<Hashtable>();
                lstToolTransaction = cCommon.ConvertDtToHashTableWithZero(dt);
                return true;
            }
            else
            {
                return false;
            }
        }

    }
    public class ToolSerial
    {
        public int SerialId { get; set; }
        public int ToolId { get; set; }
        public string SerialNumber { get; set; }
        public string Status { get; set; }   // Available / CheckedOut / Maintenance / Damaged / Lost
        public DateTime? PurchaseDate { get; set; }
        public DateTime? WarrantyExpiry { get; set; }
        public string ConditionNotes { get; set; }
        public string Location { get; set; }
        public DateTime CreatedOn { get; set; }

        // Navigation
        public Tool Tool { get; set; }
    }
    public class ToolAllocation
    {
        public Guid AllocationId { get; set; }
        public int SerialId { get; set; }
        public int ToolId { get; set; }
        public int UserId { get; set; }
        public DateTime CheckoutDate { get; set; }
        public DateTime? ExpectedReturnDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public bool IsReturned { get; set; }
        public string ConditionOnReturn { get; set; }

        // Navigation
        public Tool Tool { get; set; }
        public ToolSerial ToolSerial { get; set; }
    }

    public class ToolTransaction
    {
        public int TranId { get; set; }
        public int SerialId { get; set; }
        public int ToolId { get; set; }
        public int? UserId { get; set; }
        public string TranType { get; set; }   // OUT / IN / MAINTENANCE / DAMAGE / LOST
        public int TranQty { get; set; }
        public DateTime TranDate { get; set; }
        public string Notes { get; set; }

        // Navigation
        public Tool Tool { get; set; }
        public ToolSerial ToolSerial { get; set; }
    }

}
