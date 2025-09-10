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
        public JsonResult CheckOut(int toolId, string[] serialIds, DateTime? expectedReturn, string notes)
        {
            oDAL = new cDAL(cDAL.ConnectionType.INIT);
            var userId = Convert.ToInt32(Session["SigninId"]);

            if (serialIds == null || serialIds.Length == 0)
                return Json(new { success = false, message = "No tool units selected for checkout." });

            // Create a comma-separated list of serial IDs
            string serialList = string.Join(",", serialIds);

            // Insert ONE transaction log (with combined serials)
            string sqlTran = $@"
INSERT INTO Tool.ToolTransactions (ToolId, SerialId, UserId, TranType, TranQty, Notes)
VALUES ({toolId}, '{serialList}', {userId}, 'OUT', {serialIds.Length}, '{notes?.Replace("'", "''")}')";
            oDAL.Execute(sqlTran);

            foreach (var serialId in serialIds)
            {
                // Insert into ToolAllocation (still per serial for tracking)
                string sqlAlloc = $@"
INSERT INTO Tool.ToolAllocation (AllocationId, SerialId, ToolId, UserId, CheckoutDate, ExpectedReturnDate, IsReturned)
VALUES (NEWID(), {serialId}, {toolId}, {userId}, GETDATE(),
        {(expectedReturn.HasValue ? $"'{expectedReturn.Value:yyyy-MM-dd}'" : "NULL")},
        0)";
                oDAL.Execute(sqlAlloc);

                // Update ToolSerials status
                string sqlStatus = $@"
UPDATE Tool.ToolSerials
SET Status = 'CheckedOut'
WHERE SerialId = {serialId}";
                oDAL.Execute(sqlStatus);
            }

            return Json(new { success = true });
        }


        [HttpPost]
        public JsonResult CheckIn(List<Guid> allocationIds, string notes)
        {
            oDAL = new cDAL(cDAL.ConnectionType.INIT);
            var userId = Convert.ToInt32(Session["SigninId"]);

            List<int> serialsToLog = new List<int>();
            int toolId = 0; // we’ll grab this from allocation rows

            foreach (var allocationId in allocationIds)
            {
                string sqlAlloc = $@"
SELECT SerialId, ToolId
FROM Tool.ToolAllocation
WHERE AllocationId = '{allocationId}' AND UserId = {userId} AND IsReturned = 0";
                var dt = oDAL.GetData(sqlAlloc);

                if (dt.Rows.Count == 0)
                    continue;

                int serialId = Convert.ToInt32(dt.Rows[0]["SerialId"]);
                toolId = Convert.ToInt32(dt.Rows[0]["ToolId"]);
                serialsToLog.Add(serialId);

                // Update allocation
                string sqlUpdateAlloc = $@"
UPDATE Tool.ToolAllocation
SET ReturnDate = GETDATE(),
    IsReturned = 1,
    ConditionOnReturn = '{notes.Replace("'", "''")}'
WHERE AllocationId = '{allocationId}'";
                oDAL.Execute(sqlUpdateAlloc);

                // Update serial
                string sqlStatus = $@"
UPDATE Tool.ToolSerials
SET Status = 'Available'
WHERE SerialId = {serialId}";
                oDAL.Execute(sqlStatus);
            }

            if (serialsToLog.Count > 0)
            {
                // Log ONE transaction with combined serials
                string serialList = string.Join(",", serialsToLog);
                string sqlTran = $@"
INSERT INTO Tool.ToolTransactions (ToolId, SerialId, UserId, TranType, TranQty, Notes)
VALUES ({toolId}, '{serialList}', {userId}, 'IN', {serialsToLog.Count}, '{notes.Replace("'", "''")}')";
                oDAL.Execute(sqlTran);
            }

            return Json(new { success = true });
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
        }
        public JsonResult GetCheckedOutWeekly()
        {
            DataTable dt = oTool.GetCheckedOutWeeklyStats(); // 👈 aapko new method banani hogi SQL ke liye

            var result = dt.AsEnumerable().Select(r => new {
                YearNumber = Convert.ToInt32(r["YearNumber"]),
                WeekNumber = Convert.ToInt32(r["WeekNumber"]),
                CheckedOutCount = Convert.ToInt32(r["CheckedOutCount"])
            }).ToList();

            return Json(result, JsonRequestBehavior.AllowGet);
        }



        public JsonResult GetTopUsedTool()
        {
            DataTable dt = oTool.GetTopUsedToolStats();

            var result = dt.AsEnumerable().Select(r => new {
                ToolName = r["ToolName"].ToString(),
                UsageCount = Convert.ToInt32(r["UsageCount"])
            }).ToList();

            return Json(result, JsonRequestBehavior.AllowGet);
        }


        [HttpGet]
        public JsonResult GetAvailableSerials(int toolId)
        {
            oDAL = new cDAL(cDAL.ConnectionType.INIT);

            string sql = $@"
        SELECT SerialId,
        t.ToolName + '-' + SerialNumber AS ToolName
        FROM Tool.ToolSerials ts
INNER JOIN TOOL.Tools t ON t.ToolId = ts.ToolId
        WHERE ts.ToolId = {toolId} AND ts.Status = 'Available'
        ORDER BY SerialNumber";

            var dt = oDAL.GetData(sql);

            //var serials = dt.AsEnumerable().Select(row => new
            //{
            //    SerialId = row["SerialId"],
            //    SerialNumber = row["SerialNumber"]
            //}).ToList();
            var serials = cCommon.ConvertDtToHashTable(dt);
            return Json(serials, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetUserAllocations(int toolId)
        {
            oDAL = new cDAL(cDAL.ConnectionType.INIT);
            var userId = Convert.ToInt32(Session["SigninId"]);

            string sql = $@"
        SELECT 
            A.AllocationId, 
            A.SerialId,
            A.ToolId, 
            S.SerialNumber, 
            A.CheckoutDate, 
            A.ExpectedReturnDate
        FROM Tool.ToolAllocation A
        INNER JOIN Tool.ToolSerials S ON A.SerialId = S.SerialId
        WHERE A.ToolId = {toolId} AND A.UserId = {userId} AND A.IsReturned = 0
        ORDER BY A.CheckoutDate";

            var dt = oDAL.GetData(sql);

            var allocations = dt.AsEnumerable().Select(row => new
            {
                AllocationId = row["AllocationId"].ToString(),
                //SerialId = row["SerialId"],
                //ToolId = row["ToolId"],
                SerialNumber = row["SerialNumber"].ToString()
            }).ToList();
            var lstAllocation = cCommon.ConvertDtToHashTable(dt);
            return Json(lstAllocation, JsonRequestBehavior.AllowGet);
        }

    }
}