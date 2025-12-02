using IP.Classess;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace PlusCP.Models
{
    public static class ToolCheckInService
    {

        public class SerialCheckinItem
        {
            public int SerialId { get; set; }
            public string SerialNo { get; set; }
            public bool IsRepair { get; set; }
            public string RepairAction { get; set; }
            public decimal? Hours { get; set; }
            public int? Rating { get; set; }
        }

        public class CheckInModel
        {
            public int ToolId { get; set; }
            public string ToolName { get; set; }
            public List<SerialCheckinItem> SerialItems { get; set; }
            public List<int> PartIds { get; set; }
            public List<string> PartNo { get; set; }
            public string Notes { get; set; }
            public decimal? Hours { get; set; }
            public int? Rating { get; set; }
        }

        private static cDAL oDAL;

        public static object ProcessBatchCheckIn(List<CheckInModel> checkins)

        {
            oDAL = new cDAL(cDAL.ConnectionType.INIT);

            var results = new List<object>();

            foreach (var item in checkins)
            {
                results.Add(ProcessSingleToolCheckInWithRepair(
                    item.ToolId,
                    item.ToolName,
                    item.SerialItems,
                    item.PartIds,
                    item.PartNo,
                    item.Notes,
                    item.Hours,
                    item.Rating
                ));
            }

            return new
            {
                success = true,
                message = "Check-in processed successfully.",
                details = results
            };
        }

        private static object ProcessSingleToolCheckInWithRepair(
        int toolId,
        string toolName,
        List<SerialCheckinItem> serialItems,
        List<int> partIds,
        List<string> partNo,
        string notes,
        decimal? hours,
        int? rating
    )
        {
            oDAL = new cDAL(cDAL.ConnectionType.INIT);

            var checkedInSerials = new List<int>();
            var checkedInSerialNos = new List<string>();
            var checkedInParts = new List<int>();

            int lastUserId = 0;
            string lastUserName = "Unknown";

            // 1 — GET TOOL TOTAL HOURS
            string sqlTool = @"SELECT ISNULL(TotalHours,0) FROM Tool.Tools WHERE ToolId = " + toolId;
            int totalHours = Convert.ToInt32(oDAL.GetObject(sqlTool));

            var serials = serialItems ?? new List<SerialCheckinItem>();

            // ========== SERIAL CHECK-IN LOOP ==========
            foreach (var s in serials)
            {
                // 1 — Get allocation info
                string sqlAlloc = @"
SELECT TOP 1 
    a.AllocationId,
    a.UserId,
    sf.Name AS UserName
FROM Tool.ToolAllocation a
LEFT JOIN TOOL.SysUserFile sf ON sf.ID = a.UserId
WHERE a.SerialId = " + s.SerialId + " AND a.IsReturned = 0";

                var dtAlloc = oDAL.GetData(sqlAlloc);
                if (dtAlloc.Rows.Count == 0) continue;

                Guid allocationId = Guid.Parse(dtAlloc.Rows[0]["AllocationId"].ToString());
                int allocUserId = Convert.ToInt32(dtAlloc.Rows[0]["UserId"]);
                string allocUserName = dtAlloc.Rows[0]["UserName"]?.ToString() ?? "Unknown";

                lastUserId = allocUserId;
                lastUserName = allocUserName;

                checkedInSerials.Add(s.SerialId);
                checkedInSerialNos.Add(s.SerialNo);

                // 2 — CLOSE allocation
                oDAL.Execute(@"
UPDATE Tool.ToolAllocation
SET ReturnDate = GETDATE(), IsReturned = 1,
    ConditionOnReturn = '" + (notes ?? "").Replace("'", "''") + @"'
WHERE AllocationId = '" + allocationId + "'");

                // 3 — Update consumed hours
                string sqlConsumed = @"SELECT ISNULL(ConsumedHours,0) FROM Tool.ToolSerials WHERE SerialId = " + s.SerialId;
                int consumedSoFar = Convert.ToInt32(oDAL.GetObject(sqlConsumed));

                int currentHours = Convert.ToInt32(s.Hours ?? 0);
                int newConsumed = consumedSoFar + currentHours;

                oDAL.Execute("UPDATE Tool.ToolSerials SET ConsumedHours = " + newConsumed + " WHERE SerialId = " + s.SerialId);

                // 4 — DECIDE FINAL TRANSACTION TYPE
                bool manualRepair = !string.IsNullOrEmpty(s.RepairAction);
                bool hoursExceeded = newConsumed >= totalHours;

                string finalType = "IN"; // default

                if (manualRepair)
                {
                    string r = s.RepairAction.ToLower();
                    if (r == "repair") finalType = "Repair";
                    else if (r == "broken") finalType = "Broken";
                    else if (r == "calibration") finalType = "Calibration";
                }
                else if (hoursExceeded)
                {
                    finalType = "Calibration";
                }

                // 🔥 IMPORTANT: ONLY ONE TRANSACTION WILL BE INSERTED (IN / Repair / Broken / Calibration)
                string sqlTrans =
                    "INSERT INTO Tool.ToolTransactions " +
                    "(ToolId, ToolName, ToolSerialId, ToolSerialNumber, TranType, TranQty, UserId, Username, TranDate, Notes, Hours, Rating) " +
                    "VALUES (" +
                    toolId + "," +
                    "'" + toolName.Replace("'", "''") + "'," +
                    s.SerialId + "," +
                    "'" + s.SerialNo.Replace("'", "''") + "'," +
                    "'" + finalType + "'," +
                    "1," +
                    allocUserId + "," +
                    "'" + allocUserName.Replace("'", "''") + "'," +
                    "GETDATE()," +
                    "'" + (notes ?? "").Replace("'", "''") + "'," +
                    (s.Hours ?? 0) + "," +
                    (s.Rating ?? 0) +
                    ")";

                oDAL.Execute(sqlTrans);

                // 5 — UPDATE STATUS
                oDAL.Execute("UPDATE Tool.ToolSerials SET Status = '" + (finalType == "IN" ? "Available" : finalType) + "' WHERE SerialId = " + s.SerialId);

                // 6 — INSERT IN REPAIR TABLE ONLY IF repair/broken/calibration
                if (finalType != "IN")
                {
                    string sqlRepair =
                        "INSERT INTO Tool.Repair " +
                        "(RepairId, ToolId, SerialId, SerialNumber, ReportedByUserId, ReportedByName, ReportedDate, Hours, Rating, Status) " +
                        "VALUES (" +
                        "NEWID()," +
                        toolId + "," +
                        s.SerialId + "," +
                        "'" + s.SerialNo.Replace("'", "''") + "'," +
                        allocUserId + "," +
                        "'" + allocUserName.Replace("'", "''") + "'," +
                        "GETDATE()," +
                        (s.Hours ?? 0) + "," +
                        (s.Rating ?? 0) + "," +
                        "'" + finalType + "'" +
                        ")";
                    oDAL.Execute(sqlRepair);
                }
            }

            // ========== PART CHECK-IN ==========
            if (partIds != null && partIds.Any())
            {
                foreach (var partId in partIds)
                {
                    string sqlPart = @"
SELECT TOP 1 AllocationId
FROM Tool.PartAllocation
WHERE PartId = " + partId + " AND IsReturned = 0";

                    var dt = oDAL.GetData(sqlPart);
                    if (dt.Rows.Count == 0) continue;

                    Guid allocationId = Guid.Parse(dt.Rows[0]["AllocationId"].ToString());
                    checkedInParts.Add(partId);

                    oDAL.Execute("UPDATE Tool.PartAllocation SET ReturnDate = GETDATE(), IsReturned = 1 WHERE AllocationId = '" + allocationId + "'");

                    oDAL.Execute(
                        "UPDATE Tool.PartNo SET Status = 'Completed', ModifiedBy = '" +
                        lastUserName.Replace("'", "''") + "', ModifiedOn = GETDATE() WHERE PartId = " + partId);
                }
            }

            return new
            {
                toolId,
                toolName,
                serialsChecked = checkedInSerials.Count,
                partsChecked = checkedInParts.Count
            };
        }


    }
}
