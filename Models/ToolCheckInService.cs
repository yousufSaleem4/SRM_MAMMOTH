using IP.Classess;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace PlusCP.Models
{
    public class ToolCheckInService
    {
        private static cDAL oDAL;

        // 🔹 Entry point for Check-In
        public static object ProcessCheckIn(
            int toolId,
            string toolName,
            string email,
            string password,
            List<int> serialIds,
            List<string> serialNo,
            List<int> partIds,
            List<string> partNo,
            string notes)
        {
            oDAL = new cDAL(cDAL.ConnectionType.INIT);

            var user = AuthenticateUser(email, password);
            if (user == null)
                return new { success = false, message = "Invalid email or password." };

            List<int> checkedInSerials = new List<int>();
            List<int> checkedInParts = new List<int>();
            List<int> unauthorizedSerials = new List<int>();
            List<int> unauthorizedParts = new List<int>();

            // 🔸 Process Serials
            if (serialIds != null && serialIds.Any())
                ProcessSerialCheckIn(user, serialIds, notes, checkedInSerials, unauthorizedSerials);

            // 🔸 Process Parts
            if (partIds != null && partIds.Any())
                ProcessPartCheckIn(user, partIds, notes, checkedInParts, unauthorizedParts);

            // 🔸 Validation
            if (unauthorizedSerials.Any() || unauthorizedParts.Any())
            {
                return new
                {
                    success = false,
                    message = "Unauthorized check-in attempt detected. Some items belong to other users.",
                    unauthorizedSerials,
                    unauthorizedParts
                };
            }

            // 🔹 Log a single consolidated transaction (for all checked-in items)
            if (checkedInSerials.Any() || checkedInParts.Any())
            {
                LogSingleTransaction(
                    toolId,
                    toolName,
                    user.UserId,
                    user.UserName,
                    "IN", // TranType
                    notes,
                    serialIds,
                    serialNo,
                    partIds,
                    partNo
                );
            }

            // ✅ Success
            return new
            {
                success = true,
                message = $"Successfully checked in {checkedInSerials.Count} tool(s) and {checkedInParts.Count} part(s).",
                unauthorizedSerials,
                unauthorizedParts
            };
        }

        // 🔹 Method 1: Authenticate user
        private static UserModel AuthenticateUser(string email, string password)
        {
            string passEncrypt = BasicEncrypt.Instance.Encrypt(password.Trim());

            string sql = $@"
SELECT UserId, FirstName + ' ' + LastName AS UserName, Email
FROM SRM.UserInfo 
WHERE Email = '{email}'
  AND Password = '{passEncrypt}'
  AND IsActive = 1";

            var dt = oDAL.GetData(sql);
            if (dt.Rows.Count == 0) return null;

            return new UserModel
            {
                UserId = Convert.ToInt32(dt.Rows[0]["UserId"]),
                UserName = dt.Rows[0]["UserName"].ToString(),
                Email = dt.Rows[0]["Email"].ToString()
            };
        }

        // 🔹 Method 2: Process Tool Serials
        private static void ProcessSerialCheckIn(UserModel user, List<int> serialIds, string notes, List<int> checkedInSerials, List<int> unauthorizedSerials)
        {
            foreach (int serialId in serialIds)
            {
                string sqlAlloc = $@"
SELECT AllocationId, ToolId, UserId
FROM Tool.ToolAllocation
WHERE SerialId = {serialId} AND IsReturned = 0";

                var dtAlloc = oDAL.GetData(sqlAlloc);
                if (dtAlloc.Rows.Count == 0)
                    continue;

                int allocUserId = Convert.ToInt32(dtAlloc.Rows[0]["UserId"]);
                if (allocUserId != user.UserId)
                {
                    unauthorizedSerials.Add(serialId);
                    continue;
                }

                checkedInSerials.Add(serialId);

                Guid allocationId = Guid.Parse(dtAlloc.Rows[0]["AllocationId"].ToString());
                int toolId = Convert.ToInt32(dtAlloc.Rows[0]["ToolId"]);

                LogToolTransaction(toolId, serialId, user.UserId, notes);
                UpdateToolAllocation(allocationId, notes);
                FreeToolSerial(serialId);
            }
        }

        // 🔹 Method 3: Process Parts
        private static void ProcessPartCheckIn(UserModel user, List<int> partIds, string notes, List<int> checkedInParts, List<int> unauthorizedParts)
        {
            foreach (int partId in partIds)
            {
                string sqlPartAlloc = $@"
SELECT AllocationId, UserId
FROM Tool.PartAllocation
WHERE PartId = {partId} AND IsReturned = 0";

                var dtPartAlloc = oDAL.GetData(sqlPartAlloc);
                if (dtPartAlloc.Rows.Count == 0)
                    continue;

                int allocUserId = Convert.ToInt32(dtPartAlloc.Rows[0]["UserId"]);
                if (allocUserId != user.UserId)
                {
                    unauthorizedParts.Add(partId);
                    continue;
                }

                checkedInParts.Add(partId);
                Guid partAllocId = Guid.Parse(dtPartAlloc.Rows[0]["AllocationId"].ToString());

                UpdatePartAllocation(partAllocId);
                UpdatePartNo(partId, user.UserName);
            }
        }

        // 🔹 Method 4: Log transaction
        private static void LogToolTransaction(int toolId, int serialId, int userId, string notes)
        {
            string sql = $@"
INSERT INTO Tool.ToolTransactions (ToolId, SerialId, UserId, TranType, TranQty, Notes, TranDate)
VALUES ({toolId}, {serialId}, {userId}, 'IN', 1, '{notes?.Replace("'", "''")}', GETDATE())";
            oDAL.Execute(sql);
        }

        // 🔹 Method 5: Update Tool Allocation
        private static void UpdateToolAllocation(Guid allocationId, string notes)
        {
            string sql = $@"
UPDATE Tool.ToolAllocation
SET ReturnDate = GETDATE(),
    IsReturned = 1,
    ConditionOnReturn = '{notes?.Replace("'", "''")}'
WHERE AllocationId = '{allocationId}'";
            oDAL.Execute(sql);
        }

        // 🔹 Method 6: Free Tool Serial
        private static void FreeToolSerial(int serialId)
        {
            string sql = $"UPDATE Tool.ToolSerials SET Status = 'Available' WHERE SerialId = {serialId}";
            oDAL.Execute(sql);
        }

        // 🔹 Method 7: Update Part Allocation
        private static void UpdatePartAllocation(Guid allocationId)
        {
            string sql = $@"
UPDATE Tool.PartAllocation
SET ReturnDate = GETDATE(), IsReturned = 1
WHERE AllocationId = '{allocationId}'";
            oDAL.Execute(sql);
        }

        // 🔹 Method 8: Update PartNo table
        private static void UpdatePartNo(int partId, string userName)
        {
            string sql = $@"
UPDATE Tool.PartNo
SET Status = 'Completed',
    ModifiedBy = '{userName.Replace("'", "''")}',
    ModifiedOn = GETDATE()
WHERE PartId = {partId}";
            oDAL.Execute(sql);
        }
        private static void LogSingleTransaction(
    int toolId,
    string toolName,
    int userId,
    string userName,
    string tranType,
    string notes,
    List<int> serialIds,
    List<string> serialNo,
    List<int> partIds,
    List<string> partNo)
        {
            // handle potential nulls gracefully
            string serialIdList = (serialIds != null && serialIds.Any()) ? string.Join(",", serialIds) : "";
            string serialNoList = (serialNo != null && serialNo.Any()) ? string.Join(",", serialNo.Select(s => s.Replace("'", "''"))) : "";
            string partIdList = (partIds != null && partIds.Any()) ? string.Join(",", partIds) : "";
            string partNoList = (partNo != null && partNo.Any()) ? string.Join(",", partNo.Select(p => p.Replace("'", "''"))) : "";
            string safeToolName = toolName?.Replace("'", "''") ?? "";
            string safeUserName = userName?.Replace("'", "''") ?? "";
            string safeNotes = notes?.Replace("'", "''") ?? "";

            int tranQty = (serialIds?.Count ?? 0) + (partIds?.Count ?? 0);

            string sql = $@"
INSERT INTO Tool.ToolTransactions 
(ToolId, ToolName, ToolSerialId, ToolSerialNumber, PartId, PartNo, TranType, TranQty, ExpectedReturnDate, UserId, Username, TranDate, Notes)
VALUES (
    {toolId},
    '{safeToolName}',
    '{serialIdList}',
    '{serialNoList}',
    '{partIdList}',
    '{partNoList}',
    '{tranType}',
    {tranQty},
    NULL,  -- ExpectedReturnDate is NULL for Check-In
    {userId},
    '{safeUserName}',
    GETDATE(),
    '{safeNotes}'
)";
            oDAL.Execute(sql);
        }

    }

    // Helper class to hold user info
    public class UserModel
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
    }
}
