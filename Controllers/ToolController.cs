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

        public ActionResult GetToolRepair(string RptCode, string menuTitle)
        {
            oTool = new Tool();
            TempData["ReportTitle"] = menuTitle;
            TempData["RptCode"] = RptCode;
            ViewBag.ReportTitle = "Repair  ";
            ViewBag.ddlTool = cCommon.ToDropDownList(oTool.GetToolDropdown(), "ID", "NAME", Session["ProgramId"].ToString(), "ID");
            
            return View(oTool);
        }

        public JsonResult GetToolRepairList(string status)
        {
            string menuTitle = string.Empty;
            string RptCode;
            DataTable dt = new DataTable();

            oTool.GetToolRepair(status);

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



        public ActionResult GetToolCreation(string RptCode, string menuTitle)
        {
            oTool = new Tool();
            TempData["ReportTitle"] = menuTitle;
            TempData["RptCode"] = RptCode;
            ViewBag.ReportTitle = "Tools";
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

        public JsonResult GetToolCreationList()
        {
            string menuTitle = string.Empty;
            string RptCode;
            DataTable dt = new DataTable();

            oTool.GetToolCreationList();

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
        public JsonResult AddTool(AddToolModel model)
        {
            try
            {
                string createdBy = Session["Firstname"] != null
                    ? Session["Firstname"].ToString()
                    : "System";

                oDAL = new cDAL(cDAL.ConnectionType.INIT);

                if (string.IsNullOrEmpty(model.ToolName))
                    return Json(new { success = false, message = "Tool name required." });

                if (model.SerialNumbers == null || model.SerialNumbers.Count == 0)
                    return Json(new { success = false, message = "At least one serial number required." });

                // 🔹 Insert Tool
                string sqlTool = $@"
INSERT INTO Tool.Tools (ToolName,  TotalQty, CreatedBy, CreatedOn)
VALUES (
    '{model.ToolName.Replace("'", "''")}', 
    {model.SerialNumbers.Count}, 
    '{createdBy}', 
    GETDATE()
);
SELECT SCOPE_IDENTITY();";

                int toolId = Convert.ToInt32(oDAL.GetObject(sqlTool));

                // 🔹 Insert Serial Numbers
                foreach (var serial in model.SerialNumbers)
                {
                    string sqlSerial = $@"
INSERT INTO Tool.ToolSerials (ToolId, SerialNumber, Status, TotalHours, CreatedBy, CreatedOn)
VALUES (
    {toolId}, 
    '{serial.Replace("'", "''")}', 
    'Available', 
{model.Hours},
    '{createdBy}', 
    GETDATE()
);";

                    oDAL.Execute(sqlSerial);
                }

                return Json(new { success = true, message = "Tool added successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        [HttpPost]
        public JsonResult UpdateTool(UpdateToolModel model)
        {
            string userName = Session["Firstname"] != null ? Session["Firstname"].ToString() : "System";

            try
            {
                oDAL = new cDAL(cDAL.ConnectionType.INIT);

                if (string.IsNullOrEmpty(model.ToolName))
                    return Json(new { success = false, message = "Tool name required." });

                // 🔹 Step 1: Update only ToolName + Modified Info
                string sqlToolUpdate = $@"
UPDATE Tool.Tools 
SET 
    ToolName = '{model.ToolName.Replace("'", "''")}',
    ModifiedBy = '{userName}',
    ModifiedOn = GETDATE()
WHERE ToolId = {model.ToolId};";

                oDAL.Execute(sqlToolUpdate);

                // 🔹 Step 2: Delete old serials
                string sqlDeleteSerials = $"DELETE FROM Tool.ToolSerials WHERE ToolId = {model.ToolId};";
                oDAL.Execute(sqlDeleteSerials);

                // 🔹 Step 3: Insert updated serials WITH HOURS
                foreach (var serial in model.SerialNumbers)
                {
                    string sqlSerialInsert = $@"
INSERT INTO Tool.ToolSerials 
(ToolId, SerialNumber, Status, TotalHours, ConsumedHours, CreatedBy, CreatedOn)
VALUES (
    {model.ToolId}, 
    '{serial.Replace("'", "''")}', 
    'Available',
    {model.Hours},        -- ✅ TotalHours updated here
    0,                    -- ConsumedHours reset to zero on update
    '{userName}',
    GETDATE()
);";

                    oDAL.Execute(sqlSerialInsert);
                }

                // 🔹 Step 4: Update Total Quantity
                string sqlUpdateQty = $@"
UPDATE Tool.Tools 
SET TotalQty = (SELECT COUNT(*) FROM Tool.ToolSerials WHERE ToolId = {model.ToolId})
WHERE ToolId = {model.ToolId};";

                oDAL.Execute(sqlUpdateQty);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }



        public class UpdateToolModel
        {
            public int ToolId { get; set; }
            public string ToolName { get; set; }
            public List<string> SerialNumbers { get; set; }
            public int Hours { get; set; }
        }


        [HttpGet]
        public JsonResult GetToolById(int id)
        {
            try
            {
                oDAL = new cDAL(cDAL.ConnectionType.INIT);

                // Tool basic info
                string sqlTool = $@"
            SELECT ToolId, ToolName 
            FROM Tool.Tools 
            WHERE ToolId = {id}";

                var dtTool = oDAL.GetData(sqlTool);
                if (dtTool.Rows.Count == 0)
                    return Json(new { success = false, message = "Tool not found." }, JsonRequestBehavior.AllowGet);

                // GET HOURS FROM ToolSerials
                string sqlHours = $@"
            SELECT TOP 1 TotalHours 
            FROM Tool.ToolSerials 
            WHERE ToolId = {id}
            ORDER BY SerialId ASC";

                int totalHours = Convert.ToInt32(oDAL.GetObject(sqlHours));

                // Serial numbers
                string sqlSerials = $"SELECT SerialNumber FROM Tool.ToolSerials WHERE ToolId = {id}";
                var dtSerials = oDAL.GetData(sqlSerials);
                var serials = dtSerials.AsEnumerable().Select(x => x["SerialNumber"].ToString()).ToList();

                var data = new
                {
                    ToolId = Convert.ToInt32(dtTool.Rows[0]["ToolId"]),
                    ToolName = dtTool.Rows[0]["ToolName"].ToString(),
                    TotalHours = totalHours,   // 🔥 Correct (coming from ToolSerials)
                    SerialNumbers = serials
                };

                return Json(new { success = true, data = data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }


        public class EditToolModel
        {
            public int ToolId { get; set; }
            public string ToolName { get; set; }
            public List<string> SerialNumbers { get; set; }
            public string ModifiedBy { get; set; }
        }



        public class AddToolModel
        {
            public string ToolName { get; set; }
            public List<string> SerialNumbers { get; set; }
            public int Hours { get; set; }
        }


        public class CheckOutRequest
        {
            public List<CheckoutItem> CheckoutItems { get; set; }
        }

        public class CheckoutItem
        {
            public int ToolId { get; set; }
            public string ToolName { get; set; }
            public string UserId { get; set; }              // 👈 individual user for this serial
            public string UserName { get; set; }         // 👈 display name
            public List<int> SerialIds { get; set; }     // serials assigned to this user
            public List<string> SerialNumbers { get; set; }
            public List<int> PartIds { get; set; }
            public List<string> PartNumbers { get; set; }
            public string ExpectedReturn { get; set; }
            public string Notes { get; set; }
        }





        [HttpPost]
        public JsonResult CheckOut(CheckOutRequest req)
        {
            try
            {
                oDAL = new cDAL(cDAL.ConnectionType.INIT);

                if (req == null || req.CheckoutItems == null || !req.CheckoutItems.Any())
                    return Json(new { success = false, message = "Invalid checkout request." });

                foreach (var item in req.CheckoutItems)
                {
                    // ❗ UserId string hai → yeh correct condition hai
                    int userId;
                    if (!int.TryParse(item.UserId, out userId))
                        continue;

                    // ★ Username must never be empty — always take frontend dropdown text
                    string userName = string.IsNullOrWhiteSpace(item.UserName)
                        ? "Unknown User"
                        : item.UserName.Trim();


                    int toolId = item.ToolId;
                    string toolName = item.ToolName ?? "";

                    var serialIds = item.SerialIds ?? new List<int>();
                    var partIds = item.PartIds ?? new List<int>();

                    string expectedReturn = item.ExpectedReturn;
                    string notes = item.Notes ?? "";

                    // ========================================================
                    // 🔹 SERIAL CHECKOUT
                    // ========================================================
                    foreach (var serialId in serialIds)
                    {
                        string sqlStatus = $"SELECT Status FROM Tool.ToolSerials WHERE SerialId = {serialId}";
                        var status = oDAL.GetObject(sqlStatus)?.ToString();

                        if (status != "Available")
                            continue;

                        // Get SerialNumber
                        string sqlGetSerial = $"SELECT SerialNumber FROM Tool.ToolSerials WHERE SerialId = {serialId}";
                        string serialNumber = oDAL.GetObject(sqlGetSerial)?.ToString() ?? "";

                        // Allocation
                        string sqlAlloc = $@"
INSERT INTO Tool.ToolAllocation 
(AllocationId, SerialId, ToolId, UserId, CheckoutDate, ExpectedReturnDate, IsReturned, ConditionOnReturn)
VALUES (NEWID(), {serialId}, {toolId}, {userId}, GETDATE(),
{(string.IsNullOrEmpty(expectedReturn) ? "NULL" : $"'{expectedReturn}'")}, 0, '{notes.Replace("'", "''")}')";

                        oDAL.Execute(sqlAlloc);

                        // Update status
                        oDAL.Execute($"UPDATE Tool.ToolSerials SET Status = 'CheckedOut' WHERE SerialId = {serialId}");

                        // SERIAL TRANSACTION LOG
                        string sqlSerTrans = $@"
INSERT INTO Tool.ToolTransactions
(ToolId, ToolName, ToolSerialId, ToolSerialNumber, TranType, TranQty, ExpectedReturnDate, UserId, Username, TranDate, Notes)
VALUES
({toolId}, '{toolName.Replace("'", "''")}', 
 '{serialId}',
 '{serialNumber.Replace("'", "''")}',
 'OUT', 1,
 {(string.IsNullOrEmpty(expectedReturn) ? "NULL" : $"'{expectedReturn}'")},
 '{userId}', '{userName.Replace("'", "''")}', GETDATE(), '{notes.Replace("'", "''")}'
)";
                        oDAL.Execute(sqlSerTrans);
                    }


                    // ========================================================
                    // 🔹 PART CHECKOUT
                    // ========================================================
                    foreach (var partId in partIds)
                    {
                        string sqlPartStatus = $"SELECT Status FROM Tool.PartNo WHERE PartId = {partId}";
                        var status = oDAL.GetObject(sqlPartStatus)?.ToString();

                        if (status != "Pending")
                            continue;

                        // Insert into PartAllocation
                        string sqlPartAlloc = $@"
INSERT INTO Tool.PartAllocation
(AllocationId, PartId, UserId, CheckoutDate, ExpectedReturnDate, IsReturned)
VALUES (NEWID(), {partId}, {userId}, GETDATE(),
{(string.IsNullOrEmpty(expectedReturn) ? "NULL" : $"'{expectedReturn}'")}, 0)";
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

                        // 🟩 PART TRANSACTION LOG - 1 row per part
                        string sqlPartTrans = $@"
INSERT INTO Tool.ToolTransactions
(ToolId, ToolName, PartId, TranType, TranQty, ExpectedReturnDate, UserId, Username, TranDate, Notes)
VALUES
({toolId}, '{toolName.Replace("'", "''")}',
 '{partId}', 
 'OUT', 1,
 {(string.IsNullOrEmpty(expectedReturn) ? "NULL" : $"'{expectedReturn}'")},
 '{userId}', '{userName.Replace("'", "''")}', GETDATE(), '{notes.Replace("'", "''")}')";
                        oDAL.Execute(sqlPartTrans);
                    }

                    // ❌ NO COMBINED TRANSACTION ENTRY (Removed)
                }

                return Json(new { success = true, message = "Checkout completed successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }






        public JsonResult GetUsersForCheckout()
        {
            oDAL = new cDAL(cDAL.ConnectionType.INIT);

            string sql = @"SELECT distinct ID, Name AS [NAME] FROM [TOOL].[SysUserFile] ";

            var dt = oDAL.GetData(sql);

            var list = dt.AsEnumerable().Select(r => new
            {
                ID = r["ID"].ToString(),
                NAME = r["NAME"].ToString()
            }).ToList();

            return Json(list, JsonRequestBehavior.AllowGet);
        }




        [HttpPost]
        public JsonResult CheckIn(List<PlusCP.Models.ToolCheckInService.CheckInModel> checkins)
        {
            if (checkins == null || !checkins.Any())
                return Json(new { success = false, message = "No check-in data provided." });

            try
            {
                var result = ToolCheckInService.ProcessBatchCheckIn(checkins);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }




        [HttpGet]
        public JsonResult GetAllocatedToolSerials(string toolId)
        {
            oDAL = new cDAL(cDAL.ConnectionType.INIT);

            string sql = $@"
        SELECT 
            TS.SerialId, 
            TS.SerialNumber, 
            TA.AllocationId,
             TA.UserId,
             u.Name AS AllocatedUser
        FROM Tool.ToolAllocation TA
        INNER JOIN Tool.ToolSerials TS ON TS.SerialId = TA.SerialId
LEFT JOIN TOOL.SysUserFile u ON TA.UserId = u.ID
        WHERE TA.ToolId = '{toolId}' AND TA.IsReturned = 0";

            var dt = oDAL.GetData(sql);

            var result = dt.AsEnumerable().Select(r => new
            {
                AllocationId = r["AllocationId"].ToString(),
                SerialId = Convert.ToInt32(r["SerialId"]),
                SerialNumber = r["SerialNumber"].ToString(),
                AllocatedUser = r["AllocatedUser"]?.ToString() ?? ""

            }).ToList();

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetAllocatedPartNos(string toolId)
        {
            oDAL = new cDAL(cDAL.ConnectionType.INIT);

            string sql = $@"
        SELECT DISTINCT
            PN.PartId,
            PN.PartNo
        FROM TOOL.PartAllocation PA
        INNER JOIN TOOL.PartNo PN ON PN.PartId = PA.PartId
        WHERE PA.IsReturned = 0
          AND PN.Status = 'InProgress'
          AND PN.ToolUsedId = '{toolId}'";  // 🔹 Tool-wise filter

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
        [HttpGet]
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

            string sql = @"
        SELECT SerialId,
               SerialNumber
        FROM Tool.ToolSerials ts
        INNER JOIN TOOL.Tools t ON t.ToolId = ts.ToolId
        WHERE ts.ToolId = " + toolId + @" AND ts.Status = 'Available'
        ORDER BY SerialNumber";

            DataTable dt = oDAL.GetData(sql);

            List<object> list = new List<object>();

            foreach (DataRow dr in dt.Rows)
            {
                list.Add(new
                {
                    SerialId = Convert.ToInt32(dr["SerialId"]),
                    SerialNumber = dr["SerialNumber"].ToString()   // ✔ FORCE STRING (IMPORTANT)
                });
            }

            return Json(list, JsonRequestBehavior.AllowGet);
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
        [HttpPost]
        public JsonResult MoveToolToInventory(ToolMoveWrapper req)
        {
            try
            {
                oDAL = new cDAL(cDAL.ConnectionType.INIT);

                foreach (var item in req.list)
                {
                    int toolId = item.ToolId;
                    string serialNumber = item.SerialNumber.Replace("'", "''");

                    // ==============================
                    // 1) GET TOOL + SERIAL INFO
                    // ==============================
                    string sqlFetch = $@"
                SELECT 
                    ts.SerialId,
                    t.ToolName,
                    a.UserId,
                    sf.Name AS Username
                FROM Tool.ToolSerials ts
                INNER JOIN Tool.Tools t ON t.ToolId = ts.ToolId
                LEFT JOIN Tool.ToolAllocation a ON a.SerialId = ts.SerialId AND a.IsReturned = 0
                LEFT JOIN Tool.SysUserFile sf ON sf.ID = a.UserId
                WHERE ts.SerialNumber = '{serialNumber}' AND ts.ToolId = {toolId}";

                    DataTable dt = oDAL.GetData(sqlFetch);

                    if (dt.Rows.Count == 0)
                        continue;

                    int serialId = Convert.ToInt32(dt.Rows[0]["SerialId"]);
                    string toolName = dt.Rows[0]["ToolName"].ToString();
                    int userId = dt.Rows[0]["UserId"] == DBNull.Value ? 0 : Convert.ToInt32(dt.Rows[0]["UserId"]);
                    string username = dt.Rows[0]["Username"]?.ToString() ?? "System";

                    // ==============================
                    // 2) SERIAL Update
                    // ==============================
                    string sqlSerial = $@"
                UPDATE TOOL.ToolSerials
                SET Status = 'Available', ConsumedHours = 0
                WHERE SerialNumber = '{serialNumber}' AND ToolId = {toolId}";
                    oDAL.Execute(sqlSerial);

                    // ==============================
                    // 3) REPAIR Update
                    // ==============================
                    string sqlRepair = $@"
                UPDATE TOOL.Repair
                SET Status = 'Inventory'
                WHERE SerialNumber = '{serialNumber}' AND ToolId = {toolId}";
                    oDAL.Execute(sqlRepair);

                    // ==============================
                    // 4) INSERT Transaction
                    // ==============================
                    string sqlTrans = $@"
                INSERT INTO Tool.ToolTransactions
                (ToolId, ToolName, ToolSerialId, ToolSerialNumber, TranType, TranQty,
                 UserId, Username, TranDate, Notes)
                VALUES
                ({toolId},
                '{toolName.Replace("'", "''")}',
                '{serialId}',
                '{serialNumber}',
                'Inventory',
                1,
                {userId},
                '{username.Replace("'", "''")}',
                GETDATE(),
                'Moved to Inventory')";
                    oDAL.Execute(sqlTrans);
                }

                return Json(new { success = true, message = "Tool(s) moved to Inventory." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }



        public class ToolMoveWrapper
        {
            public List<ToolMoveRequest> list { get; set; }
        }


        public class ToolMoveRequest
        {
            public int ToolId { get; set; }
            public string SerialNumber { get; set; }
        }

    }
}