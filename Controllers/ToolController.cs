using IP.Classess;
using PlusCP.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PlusCP.Controllers
{
    public class ToolController : Controller
    {
        // GET: Tool
        Tool oTool = new Tool();
        cDAL oDAL;
        public ActionResult Index(string RptCode, string menuTitle)
        {
            oTool = new Tool();
            TempData["ReportTitle"] = menuTitle;
            TempData["RptCode"] = RptCode;
            ViewBag.ReportTitle = "Tools Management";
            if (Session["isAdmin"].ToString() == "True")
            {

                ViewBag.ddlUsers = cCommon.ToDropDownList(oTool.GetToolUsers(), "ID", "NAME", Session["ProgramId"].ToString(), "ID");
            }
            else
            {
                ViewBag.ddlUsers = cCommon.ToDropDownList(oTool.GetToolUsers(), "ID", "NAME", Session["ProgramId"].ToString(), "ID");
            }
            return View(oTool);
        }

        public JsonResult GetToolUser()
        {
            string menuTitle = string.Empty;
            string RptCode;
            DataTable dt = new DataTable();

            oTool.GetList();

            var jsonResult = Json(oTool, JsonRequestBehavior.AllowGet);
            jsonResult.MaxJsonLength = int.MaxValue;
            //LOAD MRU & LOG QUERY
            if (TempData["ReportTitle"] != null && TempData["RptCode"] != null)
            {
                menuTitle = TempData["ReportTitle"] as string;
                RptCode = TempData["RptCode"].ToString();
                TempData.Keep();
                cLog oLog = new cLog();
                oLog.SaveLog(menuTitle, Request.Url.PathAndQuery, RptCode);
            }
            return jsonResult;
        }
        public ActionResult GetUserEmail(string vendorId)
        {
            Tool oTool = new Tool();
            string email = oTool.GetUserEmail(vendorId);

            return Content(email);
        }

        public JsonResult SendInvite(string Type, string Email, string userId, string username)
        {
            oTool = new Tool();
            //oUserRole.GetMnu();
            oTool.SendInvite(Type, Email, userId, username);
            var jsonResult = Json(oTool, JsonRequestBehavior.AllowGet);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }


        public ActionResult GetTool(string RptCode, string menuTitle)
        {
            oTool = new Tool();
            TempData["ReportTitle"] = menuTitle;
            TempData["RptCode"] = RptCode;
            ViewBag.ReportTitle = "Tools Management";
            ViewBag.ddlTool = cCommon.ToDropDownList(oTool.GetToolDropdown(), "ID", "NAME", Session["ProgramId"].ToString(), "ID");
            //if (Session["isAdmin"].ToString() == "True")
            //{

            //    ViewBag.ddlUsers = cCommon.ToDropDownList(oTool.GetToolUsers(), "ID", "NAME", Session["ProgramId"].ToString(), "ID");
            //}
            //else
            //{
            //    ViewBag.ddlUsers = cCommon.ToDropDownList(oTool.GetToolUsers(), "ID", "NAME", Session["ProgramId"].ToString(), "ID");
            //}
            return View(oTool);
        }

        public JsonResult GetToolList()
        {
            string menuTitle = string.Empty;
            string RptCode;
            DataTable dt = new DataTable();

            oTool.GetToolList();

            var jsonResult = Json(oTool, JsonRequestBehavior.AllowGet);
            jsonResult.MaxJsonLength = int.MaxValue;
            //LOAD MRU & LOG QUERY
            if (TempData["ReportTitle"] != null && TempData["RptCode"] != null)
            {
                menuTitle = TempData["ReportTitle"] as string;
                RptCode = TempData["RptCode"].ToString();
                TempData.Keep();
                cLog oLog = new cLog();
                oLog.SaveLog(menuTitle, Request.Url.PathAndQuery, RptCode);
            }
            return jsonResult;
        }
        [HttpPost]
        public JsonResult CheckOut(int toolId, string toolName, List<int> serialIds, List<string> serialNumbers, List<int> partIds, List<string> partNumbers, string expectedReturn, string notes, string email, string password)
        {
            oDAL = new cDAL(cDAL.ConnectionType.INIT);
            var passEncrypt = BasicEncrypt.Instance.Encrypt(password.Trim());

            // 🔹 Step 1: Authenticate user
            string sqlAuth = $@"
            SELECT UserId, FirstName + ' ' + LastName AS UserName, Email
            FROM SRM.UserInfo 
            WHERE Email = '{email}'
              AND Password = '{passEncrypt}'
              AND Type IN ('Admin', 'Tool Crib')
              AND IsActive = 1";

            var dtUser = oDAL.GetData(sqlAuth);

            if (dtUser.Rows.Count == 0)
                return Json(new { success = false, message = "Invalid email or password." });

            int userId = Convert.ToInt32(dtUser.Rows[0]["UserId"]);
            string userName = dtUser.Rows[0]["UserName"].ToString();

            // 🔹 Step 2: Basic validation
            if ((serialIds == null || !serialIds.Any()) && (partIds == null || !partIds.Any()))
                return Json(new { success = false, message = "Please select at least one tool serial or part number to check out." });

            // 🔹 Step 3: Handle serial allocations
            if (serialIds != null && serialIds.Any())
            {
                foreach (int serialId in serialIds)
                {
                    // Verify serial availability
                    string sqlStatus = $"SELECT Status FROM Tool.ToolSerials WHERE SerialId = {serialId}";
                    var status = oDAL.GetObject(sqlStatus)?.ToString();
                    if (status == null || status != "Available")
                        continue;

                    // Insert into ToolAllocation
                    string sqlAlloc = $@"
INSERT INTO Tool.ToolAllocation (AllocationId, SerialId, ToolId, UserId, CheckoutDate, ExpectedReturnDate, IsReturned, ConditionOnReturn)
VALUES (NEWID(), {serialId}, {toolId}, {userId}, GETDATE(), 
{(string.IsNullOrEmpty(expectedReturn) ? "NULL" : $"'{expectedReturn}'")},0, '{notes?.Replace("'", "''")}')";
                    oDAL.Execute(sqlAlloc);

                    // Update serial status
                    string sqlUpdateSerial = $"UPDATE Tool.ToolSerials SET Status = 'CheckedOut' WHERE SerialId = {serialId}";
                    oDAL.Execute(sqlUpdateSerial);
                }
            }

            // 🔹 Step 4: Handle part allocations
            if (partIds != null && partIds.Any())
            {
                foreach (int partId in partIds)
                {
                    // Only allocate parts that are still pending
                    string sqlPartStatus = $"SELECT Status FROM Tool.PartNo WHERE PartId = {partId}";
                    var partStatus = oDAL.GetObject(sqlPartStatus)?.ToString();
                    if (partStatus != "Pending")
                        continue;

                    // Insert into PartAllocation
                    string sqlPartAlloc = $@"
INSERT INTO Tool.PartAllocation (AllocationId, PartId, UserId, CheckoutDate, ExpectedReturnDate, IsReturned)
VALUES (NEWID(), {partId}, {userId}, GETDATE(),
        {(string.IsNullOrEmpty(expectedReturn) ? "NULL" : $"'{expectedReturn}'")},0)";
                    oDAL.Execute(sqlPartAlloc);

                    // Update PartNo
                    string sqlUpdatePart = $@"
UPDATE Tool.PartNo
SET Status = 'InProgress',
    ToolUsedId = {toolId},
    ModifiedBy = '{userName.Replace("'", "''")}',
    ModifiedOn = GETDATE()
WHERE PartId = {partId}";
                    oDAL.Execute(sqlUpdatePart);
                }
            }

            // 🔹 Step X: Log consolidated transaction
            string sqlTran = $@"
INSERT INTO Tool.ToolTransactions 
(ToolId, ToolName, ToolSerialId, ToolSerialNumber, PartId, PartNo, TranType, TranQty, ExpectedReturnDate, UserId, Username, TranDate, Notes)
VALUES (
    {toolId},
    '{toolName?.Replace("'", "''") ?? ""}',
    '{string.Join(",", serialIds ?? new List<int>())}',
    '{string.Join(",", (serialNumbers ?? new List<string>()).Select(s => s.Replace("'", "''")))}',
    '{string.Join(",", partIds ?? new List<int>())}',
    '{string.Join(",", (partNumbers ?? new List<string>()).Select(p => p.Replace("'", "''")))}',
    'OUT',
    {((serialIds?.Count ?? 0) + (partIds?.Count ?? 0))},
    {(string.IsNullOrEmpty(expectedReturn) ? "NULL" : $"'{expectedReturn}'")},
    {userId},
    '{userName?.Replace("'", "''") ?? ""}',
    GETDATE(),
    '{notes?.Replace("'", "''") ?? ""}'
)";

            oDAL.Execute(sqlTran);

            return Json(new { success = true, message = "Tool(s) and Part(s) checked out successfully." });
        }


        [HttpPost]
        public JsonResult CheckIn(int toolId, string toolName, string email, string password, List<int> serialIds, List<string> serialNo, List<int> partIds, List<string> partNo, string notes)
        {
            var result = ToolCheckInService.ProcessCheckIn(toolId, toolName, email, password, serialIds, serialNo, partIds, partNo, notes);
            return Json(result);
        }

        [HttpGet]
        public JsonResult GetAllocatedToolSerials(string toolId)
        {
            oDAL = new cDAL(cDAL.ConnectionType.INIT);

            string sql = $@"
        SELECT 
            TS.SerialId, 
            TS.SerialNumber, 
            TA.AllocationId
        FROM Tool.ToolAllocation TA
        INNER JOIN Tool.ToolSerials TS ON TS.SerialId = TA.SerialId
        WHERE TA.ToolId = '{toolId}' AND TA.IsReturned = 0";

            var dt = oDAL.GetData(sql);

            var result = dt.AsEnumerable().Select(r => new
            {
                AllocationId = r["AllocationId"].ToString(),
                SerialId = Convert.ToInt32(r["SerialId"]),
                SerialNumber = r["SerialNumber"].ToString()
            }).ToList();

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetAllocatedPartNos()
        {
            oDAL = new cDAL(cDAL.ConnectionType.INIT);

            string sql = @"
        SELECT DISTINCT
    PN.PartId,
    PN.PartNo
FROM TOOL.PartAllocation PA
INNER JOIN TOOL.PartNo PN ON PN.PartId = PA.PartId
WHERE PA.IsReturned = 0
  AND PN.Status = 'InProgress'
";

            var dt = oDAL.GetData(sql);

            var result = dt.AsEnumerable().Select(r => new
            {
                PartId = Convert.ToInt32(r["PartId"]),
                PartNo = r["PartNo"].ToString()
            }).ToList();

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetAllocationId()
        {
            var userId = Convert.ToInt32(Session["SigninId"]);

            string sql = $@"
        SELECT AllocationId, ToolId, AllocatedQty
        FROM ToolAllocation
        WHERE UserId = {userId}
          AND IsReturned = 0";

            oDAL = new cDAL(cDAL.ConnectionType.INIT);
            var dt = oDAL.GetData(sql);

            var list = new List<object>();
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new
                {
                    AllocationId = Convert.ToInt32(row["AllocationId"]),
                    ToolId = Convert.ToInt32(row["ToolId"]),
                    AllocatedQty = Convert.ToInt32(row["AllocatedQty"])
                });
            }

            return Json(list, JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetToolTransaction(string toolId)
        {
            string menuTitle = string.Empty;
            string RptCode;

            oTool = new Tool();
            oTool.GetToolTransaction(toolId);

            var jsonResult = Json(oTool, JsonRequestBehavior.AllowGet);
            jsonResult.MaxJsonLength = int.MaxValue;

            //LOAD MRU & LOG QUERY
            if (TempData["ReportTitle"] != null && TempData["RptCode"] != null)
            {
                menuTitle = TempData["ReportTitle"] as string;
                RptCode = TempData["RptCode"].ToString();
                TempData.Keep();
                cLog oLog = new cLog();
                oLog.SaveLog(menuTitle, Request.Url.PathAndQuery, RptCode);
            }

            return jsonResult;
        }

        [HttpGet]
        public JsonResult GetWidgetCounts()
        {
            DataTable dt = oTool.GetToolStats();

            if (dt.Rows.Count > 0)
            {
                var result = new
                {
                    TotalTools = Convert.ToInt32(dt.Rows[0]["TotalTools"]),
                    Available = Convert.ToInt32(dt.Rows[0]["Available"]),
                    CheckedOut = Convert.ToInt32(dt.Rows[0]["CheckedOut"]),
                };

                return Json(result, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { TotalTools = 0, Available = 0, CheckedOut = 0, CheckedIn = 0 }, JsonRequestBehavior.AllowGet);
            }
        }//Created by Mohsin
        public JsonResult GetCheckedOutWeekly()
        {
            DataTable dt = oTool.GetCheckedOutWeeklyStats(); // 👈 aapko new method banani hogi SQL ke liye

            var result = dt.AsEnumerable().Select(r => new
            {
                YearNumber = Convert.ToInt32(r["YearNumber"]),
                WeekNumber = Convert.ToInt32(r["WeekNumber"]),
                CheckedOutCount = Convert.ToInt32(r["CheckedOutCount"])
            }).ToList();

            return Json(result, JsonRequestBehavior.AllowGet);
        }//Created by Mohsin
        public JsonResult GetTopUsedTool()
        {
            DataTable dt = oTool.GetTopUsedToolStats();

            var result = dt.AsEnumerable().Select(r => new
            {
                ToolName = r["ToolName"].ToString(),
                UsageCount = Convert.ToInt32(r["UsageCount"])
            }).ToList();

            return Json(result, JsonRequestBehavior.AllowGet);
        }//Created by Mohsin

        [HttpGet]
        public JsonResult GetAvailableSerials(int toolId)
        {
            oDAL = new cDAL(cDAL.ConnectionType.INIT);

            string sql = $@"
        SELECT SerialId,
        SerialNumber AS ToolName
        FROM Tool.ToolSerials ts
INNER JOIN TOOL.Tools t ON t.ToolId = ts.ToolId
        WHERE ts.ToolId = {toolId} AND ts.Status = 'Available'
        ORDER BY SerialNumber";

            var dt = oDAL.GetData(sql);
            var serials = cCommon.ConvertDtToHashTable(dt);
            return Json(serials, JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetPartNo(int toolId)
        {
            oDAL = new cDAL(cDAL.ConnectionType.INIT);

            string sql = $@"
         SELECT PN.PartId, PN.PartNo
FROM TOOL.PartNo PN
WHERE PN.Status = 'Pending' AND PN.PartId NOT IN (
    SELECT PartId
    FROM Tool.PartAllocation
    WHERE IsReturned = 0
);";

            var dt = oDAL.GetData(sql);

            var lstPartNo = dt.AsEnumerable().Select(row => new
            {
                PartId = row["PartId"].ToString(),
                PartNo = row["PartNo"].ToString(),
            }).ToList();

            return Json(lstPartNo, JsonRequestBehavior.AllowGet);
        }
    }
}