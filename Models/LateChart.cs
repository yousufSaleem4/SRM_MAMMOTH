﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;

namespace PlusCP.Models
{
    public class LateChart
    {
        public List<Hashtable> lstPerformance { get; set; }
        public string ErrorMessage { get; set; }
        public bool SupplierAllPerformance()
        {
            cDAL oDAL = new cDAL(cDAL.ConnectionType.ACTIVE);
            string sql = @"WITH OnTimePerformance AS (
    SELECT 
        v.VendorName,
        COUNT(*) AS TotalPOs,
        SUM(CASE WHEN DATEDIFF(DAY, v.CreatedOn, v.DueDate) >= 0 THEN 1 ELSE 0 END) AS OnTimePOs
    FROM [SRM].[VendorCommunication] v
    WHERE v.DueDate IS NOT NULL
    GROUP BY v.VendorName
)
SELECT 
    VendorName,
    TotalPOs,
    OnTimePOs,
    (OnTimePOs * 100.0 / TotalPOs) AS OnTimePercentage
FROM OnTimePerformance
ORDER BY OnTimePercentage DESC; ";
            DataTable dt = new DataTable();
            dt = oDAL.GetData(sql);
            
            if (oDAL.HasErrors)
            {
                ErrorMessage = oDAL.ErrMessage;
                return false;
            }
            else
            {
                lstPerformance = cCommon.ConvertDtToHashTable(dt);
                return true;
            }
        }

        public DataTable GetOnTimeChart()
        {
            cDAL oDAL = new cDAL(cDAL.ConnectionType.ACTIVE);
            string sql = @"select top(7) CONCAT(POHeader_PONum,+'-'+PODetail_POLine+'-'+PORel_PORelNum) AS [KEY], 
DATEDIFF(Day,Calculated_DueDate, Calculated_ArrivedDate)
AS [VALUE]
from [dbo].[tblPurchaseOrder] 
where Calculated_DueDate <= Calculated_ArrivedDate ";
            DataTable dt = new DataTable();
            dt = oDAL.GetData(sql);
            return dt;
        }

        public DataTable GetOnTimeVendors()
        {
            cDAL oDAL = new cDAL(cDAL.ConnectionType.ACTIVE);
            string sql = @"
--1. Supplier Response Rate
WITH SupplierResponse AS (
    SELECT 
        SUBSTRING(v.VendorName, 1, 20) AS ShortVendorName,  -- Shorten vendor name
        COUNT(*) AS TotalPOs,
        SUM(CASE WHEN v.IsAnswered = 1 THEN 1 ELSE 0 END) AS AnsweredPOs
    FROM [SRM].[VendorCommunication] v
    GROUP BY v.VendorName
)
SELECT 
    ShortVendorName AS VendorName,  -- Display the shortened name
    TotalPOs,
    AnsweredPOs,
    (AnsweredPOs * 100.0 / TotalPOs) AS ResponseRate
FROM SupplierResponse
ORDER BY ResponseRate DESC;

 ";
            DataTable dt = new DataTable();
            dt = oDAL.GetData(sql);
            return dt;
        }


        public DataTable GetSupplierQty()
        {
            cDAL oDAL = new cDAL(cDAL.ConnectionType.ACTIVE);
            string sql = @"
SELECT 
    SUBSTRING(v.VendorName, 1, 10) AS [KEY],  -- Truncate the vendor name
    SUM(v.Qty) AS [VALUE]
FROM [SRM].[VendorCommunication] v
GROUP BY v.VendorName
ORDER BY [VALUE] DESC;


 ";
            DataTable dt = new DataTable();
            dt = oDAL.GetData(sql);
            return dt;
        }

        public DataTable GetDDLSupplier(DataTable dt)
        {
            cDAL oDAL = new cDAL(cDAL.ConnectionType.ACTIVE);
            DataTable distinctSuppliersTable = dt.Clone();
            distinctSuppliersTable.Clear();

            
            dt.AsEnumerable()
              .Select(row => row.Field<string>("Vendor_Name"))
              .Distinct()
              .ToList()
              .ForEach(supplier => distinctSuppliersTable.Rows.Add(supplier));

            return distinctSuppliersTable;
        }

    }
}