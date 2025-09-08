using PlusCP.Models;
using System;
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
            ViewBag.ReportTitle = "Tools";
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
        public JsonResult CheckOut(int toolId, int qty, DateTime? expectedReturn, string notes)
        {
            oDAL = new cDAL(cDAL.ConnectionType.INIT);
            oTool = new Tool();
            var userId = Convert.ToInt32(Session["SigninId"]);

            // Step 1: Validate available stock
            int available = oTool.GetAvailableQty(toolId);
            if (qty <= 0 || qty > available)
                return Json(new { success = false, message = "Invalid quantity or not enough stock." });

            // Escape notes to avoid SQL injection
            string safeNotes = notes?.Replace("'", "''") ?? "";

            // Step 2: Insert transaction record
            string sqlTran = $@"
        INSERT INTO ToolTran (ToolId, UserId, TranQty, TranType, TranDate, Notes)
        VALUES ({toolId}, {userId}, {qty}, 'CheckOut', GETDATE(), '{safeNotes}')";
            oDAL.Execute(sqlTran);

            // Step 3: Insert allocation
            string sqlAlloc = $@"
        INSERT INTO ToolAllocation (ToolId, UserId, AllocatedQty, CheckoutDate, ExpectedReturnDate, CheckOutConditionNotes, IsReturned)
        VALUES ({toolId}, {userId}, {qty}, GETDATE(), 
                {(expectedReturn.HasValue ? $"'{expectedReturn.Value:yyyy-MM-dd}'" : "NULL")},
                '{safeNotes}', 0)";
            oDAL.Execute(sqlAlloc);

            // Step 4: Update tool status based on remaining stock
            string sqlStatus = $@"
        UPDATE Tools
        SET CurrentStatus = CASE 
            WHEN Quantity <= (
                SELECT ISNULL(SUM(AllocatedQty),0) 
                FROM ToolAllocation 
                WHERE ToolId = {toolId} AND IsReturned = 0
            ) 
            THEN 'Issued' 
            ELSE 'Available' 
        END
        WHERE ToolId = {toolId}";
            oDAL.Execute(sqlStatus);

            return Json(new { success = true });
        }

        [HttpPost]
        public JsonResult CheckIn(int allocationId, int toolId, int qty, string notes)
        {
            oDAL = new cDAL(cDAL.ConnectionType.INIT);
            var userId = Convert.ToInt32(Session["SigninId"]);

            // Get allocation details
            string sqlAlloc = $@"
        SELECT AllocatedQty, ReturnedQty 
        FROM ToolAllocation 
        WHERE AllocationId = {allocationId} AND ToolId = {toolId} AND UserId = {userId}";
            var dtAlloc = oDAL.GetData(sqlAlloc);

            if (dtAlloc.Rows.Count == 0)
                return Json(new { success = false, message = "No active allocation found." });

            int allocatedQty = Convert.ToInt32(dtAlloc.Rows[0]["AllocatedQty"]);
            int returnedQty = Convert.ToInt32(dtAlloc.Rows[0]["ReturnedQty"]);
            int remainingQty = allocatedQty - returnedQty;

            if (qty <= 0 || qty > remainingQty)
                return Json(new { success = false, message = "Invalid quantity to return." });

            // Insert transaction log
            string sqlTran = $@"
        INSERT INTO ToolTran (ToolId, UserId, TranQty, ReturnDate, TranType, TranDate, Notes)
        VALUES ({toolId}, {userId}, {qty}, 'CheckIn', GETDATE(), GETDATE(), '{notes}')";
            oDAL.GetObject(sqlTran);

            // Update allocation with partial/complete return
            string sqlUpdate = $@"
        UPDATE ToolAllocation
        SET ReturnedQty = ReturnedQty + {qty},
            IsReturned = CASE WHEN ReturnedQty + {qty} >= AllocatedQty THEN 1 ELSE 0 END,
            ReturnDate = CASE WHEN ReturnedQty + {qty} >= AllocatedQty THEN GETDATE() ELSE ReturnDate END,
            CheckInConditionNotes = '{notes}'
        WHERE AllocationId = {allocationId}";
            oDAL.GetObject(sqlUpdate);

            // Update tool status (Available/Issued)
            string sqlStatus = $@"
        UPDATE Tools
        SET CurrentStatus = CASE 
            WHEN Quantity <= (
                SELECT ISNULL(SUM(AllocatedQty - ReturnedQty),0) 
                FROM ToolAllocation 
                WHERE ToolId = {toolId} AND IsReturned = 0
            ) 
            THEN 'Issued' 
            ELSE 'Available' 
        END
        WHERE ToolId = {toolId}";
            oDAL.GetObject(sqlStatus);

            return Json(new { success = true, message = "Tool checked in successfully." });
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
    }
}