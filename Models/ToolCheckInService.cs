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

            // ===========================
            // 1 — GET TOOL TOTAL HOURS
            // ===========================
            string sqlTool = $@"SELECT ISNULL(TotalHours,0) FROM Tool.Tools WHERE ToolId = {toolId}";
            int totalHours = Convert.ToInt32(oDAL.GetObject(sqlTool));

            var serials = serialItems ?? new List<SerialCheckinItem>();

            // ===========================
            // 2 — SERIAL CHECK-IN
            // ===========================
            foreach (var s in serials)
            {
                string sqlAlloc = $@"
SELECT TOP 1 
    a.AllocationId,
    a.UserId,
    sf.Name AS UserName
FROM Tool.ToolAllocation a
LEFT JOIN TOOL.SysUserFile sf ON sf.ID = a.UserId
WHERE a.SerialId = {s.SerialId} AND a.IsReturned = 0";

                var dtAlloc = oDAL.GetData(sqlAlloc);
                if (dtAlloc.Rows.Count == 0) continue;

                Guid allocationId = Guid.Parse(dtAlloc.Rows[0]["AllocationId"].ToString());
                int allocUserId = Convert.ToInt32(dtAlloc.Rows[0]["UserId"]);
                string allocUserName = dtAlloc.Rows[0]["UserName"]?.ToString() ?? "Unknown";

                lastUserId = allocUserId;
                lastUserName = allocUserName;

                checkedInSerials.Add(s.SerialId);
                checkedInSerialNos.Add(s.SerialNo);

                // close allocation
                oDAL.Execute($@"
UPDATE Tool.ToolAllocation
SET ReturnDate = GETDATE(),
    IsReturned = 1,
    ConditionOnReturn = '{(notes ?? "").Replace("'", "''")}'
WHERE AllocationId = '{allocationId}'");

                // ===========================
                // 3 — GET EXISTING CONSUMED HOURS
                // ===========================
                string sqlConsumed = $@"SELECT ISNULL(ConsumedHours,0) FROM Tool.ToolSerials WHERE SerialId = {s.SerialId}";
                int consumedSoFar = Convert.ToInt32(oDAL.GetObject(sqlConsumed));

                int currentHours = Convert.ToInt32(s.Hours ?? 0);
                int newConsumed = consumedSoFar + currentHours;

                // update consumed hours
                oDAL.Execute($@"
UPDATE Tool.ToolSerials
SET ConsumedHours = {newConsumed}
WHERE SerialId = {s.SerialId}");

                // ===========================
                // 4 — AUTO / MANUAL REPAIR CHECK
                // ===========================
                bool autoRepair = newConsumed >= totalHours;     // hours exceed?
                bool manualRepair = !string.IsNullOrEmpty(s.RepairAction); // user selected something

                // If hours exceed → ALWAYS repair entry create hogi
                // If manual action selected → ALSO repair entry create hogi
                bool finalRepair = autoRepair || manualRepair;

                // default
                string repairStatus = "Repair";


                // ------ MANUAL REPAIR ACTION ------
                if (manualRepair)
                {
                    string rs = s.RepairAction.ToLower();

                    if (rs == "repair")
                        repairStatus = "Repair";

                    else if (rs == "broken")
                        repairStatus = "Broken";

                    else if (rs == "calibration" || rs == "Calibration")
                        repairStatus = "Calibration";
                }


                // ------ AUTO CALIBRATION (Hours exceed) ------
                else if (autoRepair)
                {
                    repairStatus = "Calibration";  // auto calibration
                }


                // ------ NOTHING SELECTED + HOURS NOT EXCEED ------
                else
                {
                    finalRepair = false;   // no repair entry
                }



                // ===========================
                // 5 — INSERT REPAIR RECORD  (NOW ALWAYS FOR AUTO CALIBRATION)
                // ===========================
                if (finalRepair)
                {
                    // REPAIR TABLE INSERT
                    string sqlRepair = $@"
INSERT INTO Tool.Repair
(RepairId, ToolId, SerialId, SerialNumber, ReportedByUserId, ReportedByName, ReportedDate, Hours, Rating, Status)
VALUES (
    NEWID(),
    {toolId},
    {s.SerialId},
    '{s.SerialNo.Replace("'", "''")}',
    {allocUserId},
    '{allocUserName.Replace("'", "''")}',
    GETDATE(),
    {(s.Hours ?? 0)},
    {(s.Rating ?? 0)},
    '{repairStatus}'
)";
                    oDAL.Execute(sqlRepair);

                    // Update Serial Status
                    oDAL.Execute($@"UPDATE Tool.ToolSerials 
                    SET Status = '{repairStatus}' 
                    WHERE SerialId = {s.SerialId}");

                    // ⭐ Only 1 transaction for Repair/Broken/Calibration
                    string sqlRepairTrans =
  "INSERT INTO Tool.ToolTransactions " +
  "(ToolId, ToolName, ToolSerialId, ToolSerialNumber, TranType, TranQty, UserId, Username, TranDate, Notes, Hours, Rating) " +
  "VALUES (" +
  toolId + "," +
  "'" + toolName.Replace("'", "''") + "'," +
  "'" + s.SerialId + "'," +
  "'" + s.SerialNo.Replace("'", "''") + "'," +
  "'" + repairStatus + "'," +
  "1," +
  allocUserId + "," +
  "'" + allocUserName.Replace("'", "''") + "'," +
  "GETDATE()," +
  "'" + (notes ?? "").Replace("'", "''") + "'," +
  (s.Hours ?? 0) + "," +
  (s.Rating ?? 0) +
  ")";

                    oDAL.Execute(sqlRepairTrans);
                }





                else
                {
                    // Normal available
                    oDAL.Execute($"UPDATE Tool.ToolSerials SET Status = 'Available' WHERE SerialId = {s.SerialId}");
                }
            }
        
    

                // ===========================
                // 6 — PART CHECK-IN
                // ===========================
                if (partIds != null && partIds.Any())
            {
                foreach (var partId in partIds)
                {
                    string sqlPart = $@"
SELECT TOP 1 AllocationId
FROM Tool.PartAllocation
WHERE PartId = {partId} AND IsReturned = 0";

                    var dt = oDAL.GetData(sqlPart);
                    if (dt.Rows.Count == 0) continue;

                    Guid allocationId = Guid.Parse(dt.Rows[0]["AllocationId"].ToString());
                    checkedInParts.Add(partId);

                    // close allocation
                    oDAL.Execute($@"
UPDATE Tool.PartAllocation
SET ReturnDate = GETDATE(), IsReturned = 1
WHERE AllocationId = '{allocationId}'");

                    // complete part
                    oDAL.Execute($@"
UPDATE Tool.PartNo
SET Status = 'Completed',
    ModifiedBy = '{lastUserName.Replace("'", "''")}',
    ModifiedOn = GETDATE()
WHERE PartId = {partId}");
                }
            }

            // ===========================
            // 7 — TRANSACTION LOG
            // ===========================
            if (checkedInSerials.Any() || checkedInParts.Any())
            {
                string sqlTran = $@"
INSERT INTO Tool.ToolTransactions 
(ToolId, ToolName, ToolSerialId, ToolSerialNumber, PartId, PartNo, TranType, TranQty,
 UserId, Username, TranDate, Notes, Hours, Rating)
VALUES (
    {toolId},
    '{toolName.Replace("'", "''")}',
    '{string.Join(",", checkedInSerials)}',
    '{string.Join(",", checkedInSerialNos)}',
    '{string.Join(",", checkedInParts)}',
    '{string.Join(",", (partNo ?? new List<string>()).Select(x => x.Replace("'", "''")))}',
    'IN',
    {(checkedInSerials.Count + checkedInParts.Count)},
    {lastUserId},
    '{lastUserName.Replace("'", "''")}',
    GETDATE(),
    '{(notes ?? "").Replace("'", "''")}',
    {(hours ?? 0)},
    {(rating ?? 0)}
)";
                oDAL.Execute(sqlTran);
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
