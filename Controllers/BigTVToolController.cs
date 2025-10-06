using PlusCP.Models;
using System;
using System.Linq;
using System.Web.Mvc;

namespace PlusCP.Controllers
{
    public class BigTVToolController : Controller
    {
        BigTVTool oBigTVTool;

        // GET: BigTVTool
        public ActionResult Index()
        {
            oBigTVTool = new BigTVTool();

            // Fetch colors
            var oColors = new BigTVTool();
            oColors.GetColor();

            ViewBag.ReportTitle = "Tool Availability Dashboard";
            ViewBag.DashboardTableBg = oColors.lstColors.FirstOrDefault(c => c["SysDesc"].ToString() == "DashboardTableBg")?["SysValue"].ToString() ?? "#ffffff";
            ViewBag.DashboardTableHeaderText = oColors.lstColors.FirstOrDefault(c => c["SysDesc"].ToString() == "DashboardTableHeaderText")?["SysValue"].ToString() ?? "#f1c40f";
            ViewBag.DashboardTableDataText = oColors.lstColors.FirstOrDefault(c => c["SysDesc"].ToString() == "DashboardTableDataText")?["SysValue"].ToString() ?? "#000000";
            ViewBag.DashboardHeaderText = oColors.lstColors.FirstOrDefault(c => c["SysDesc"].ToString() == "DashboardHeaderText")?["SysValue"].ToString() ?? "#ffffff";
         
            ViewBag.DashboardBodyBg = oColors.lstColors.FirstOrDefault(c => c["SysDesc"].ToString() == "DashboardBodyBg")?["SysValue"].ToString() ?? "#f4f6f9";
            ViewBag.DashboardHeaderText = oColors.lstColors.FirstOrDefault(c => c["SysDesc"].ToString() == "DashboardHeaderText")?["SysValue"].ToString() ?? "#ffffff";
            return View(oBigTVTool);
        }

        public JsonResult GetList()
        {
            oBigTVTool = new BigTVTool();
            oBigTVTool.GetData();

            var jsonResult = Json(oBigTVTool, JsonRequestBehavior.AllowGet);
            jsonResult.MaxJsonLength = int.MaxValue;
            oBigTVTool.serializer = new System.Web.Script.Serialization.JavaScriptSerializer { MaxJsonLength = Int32.MaxValue };
            return jsonResult;
        }
    }
}
