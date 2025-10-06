using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Web;


namespace PlusCP.Models
{
    public class BigTVTool
    {
        cDAL oDAL;

        public System.Web.Script.Serialization.JavaScriptSerializer serializer { get; set; }
        string query = string.Empty;
        public string filterString { get; set; }
        public string ErrorMessage { get; set; }
        public List<Hashtable> lstBigTVTool { get; set; }
        public List<Hashtable> lstColors { get; set; }
        public bool GetData()
        {

            oDAL = new cDAL(cDAL.ConnectionType.INIT);
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




            DataTable dt = oDAL.GetData(query);



            if (!oDAL.HasErrors)
            {
                lstBigTVTool = cCommon.ConvertDtToHashTable(dt);
                return true;
            }
            else
            {
                ErrorMessage = oDAL.ErrMessage;
                return false;
            }
        }

        public bool GetColor()
        {
            oDAL = new cDAL(cDAL.ConnectionType.INIT);
            string query = @"
                SELECT SysDesc, SysValue 
                FROM zSysIni 
                WHERE SysCode BETWEEN 20001 AND 20100
            ";

            DataTable dt = oDAL.GetData(query);

            if (!oDAL.HasErrors)
            {
                lstColors = cCommon.ConvertDtToHashTable(dt);
                return true;
            }
            else
            {
                ErrorMessage = oDAL.ErrMessage;
                return false;
            }
        }
    }
}