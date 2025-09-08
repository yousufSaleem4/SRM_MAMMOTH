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


    }
}