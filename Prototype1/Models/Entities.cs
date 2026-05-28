using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Prototype1.Models
{
    [DataContract]
    public class User
    {
        [DataMember] public string UserId { get; set; }
        [DataMember] public string Username { get; set; }
        [DataMember] public string PasswordHash { get; set; }
        [DataMember] public string FullName { get; set; }
        [DataMember] public string Role { get; set; }
        [DataMember] public bool Active { get; set; }
    }

    [DataContract]
    public class Staff
    {
        [DataMember] public string StaffId { get; set; }
        [DataMember] public string FullName { get; set; }
        [DataMember] public string Position { get; set; }
        [DataMember] public string Department { get; set; }
        [DataMember] public string Phone { get; set; }
        [DataMember] public string Email { get; set; }
        [DataMember] public DateTime HireDate { get; set; }
    }

    [DataContract]
    public class Customer
    {
        [DataMember] public string CustomerId { get; set; }
        [DataMember] public string CompanyName { get; set; }
        [DataMember] public string ContactPerson { get; set; }
        [DataMember] public string Phone { get; set; }
        [DataMember] public string Email { get; set; }
        [DataMember] public string Address { get; set; }
        [DataMember] public string CustomerType { get; set; }
    }

    [DataContract]
    public class Supplier
    {
        [DataMember] public string SupplierId { get; set; }
        [DataMember] public string CompanyName { get; set; }
        [DataMember] public string ContactPerson { get; set; }
        [DataMember] public string Phone { get; set; }
        [DataMember] public string Email { get; set; }
        [DataMember] public string Address { get; set; }
        [DataMember] public string PaymentTerms { get; set; }
    }

    [DataContract]
    public class Item
    {
        [DataMember] public string ItemId { get; set; }
        [DataMember] public string ItemName { get; set; }
        [DataMember] public string Category { get; set; }
        [DataMember] public string Unit { get; set; }
        [DataMember] public decimal UnitPrice { get; set; }
        [DataMember] public int StockQty { get; set; }
        [DataMember] public int ReorderLevel { get; set; }
        [DataMember] public string SupplierId { get; set; }
    }

    [DataContract]
    public class SalesOrder
    {
        [DataMember] public string OrderId { get; set; }
        [DataMember] public DateTime OrderDate { get; set; }
        [DataMember] public string CustomerId { get; set; }
        [DataMember] public string Status { get; set; }
        [DataMember] public DateTime RequiredDate { get; set; }
        [DataMember] public string Remarks { get; set; }
        [DataMember] public string CreatedBy { get; set; }
        [DataMember] public List<SalesOrderLine> Lines { get; set; }

        public SalesOrder()
        {
            Lines = new List<SalesOrderLine>();
        }

        public decimal TotalAmount
        {
            get
            {
                decimal sum = 0m;
                if (Lines != null)
                {
                    foreach (var line in Lines)
                    {
                        sum += line.LineTotal;
                    }
                }
                return sum;
            }
        }
    }

    [DataContract]
    public class SalesOrderLine
    {
        [DataMember] public string ItemId { get; set; }
        [DataMember] public string ItemName { get; set; }
        [DataMember] public int Quantity { get; set; }
        [DataMember] public decimal UnitPrice { get; set; }

        public decimal LineTotal { get { return Quantity * UnitPrice; } }
    }

    [DataContract]
    public class DeliveryNote
    {
        [DataMember] public string DeliveryId { get; set; }
        [DataMember] public string OrderId { get; set; }
        [DataMember] public DateTime DeliveryDate { get; set; }
        [DataMember] public string DriverName { get; set; }
        [DataMember] public string VehicleNo { get; set; }
        [DataMember] public string Status { get; set; }
        [DataMember] public string ReplySlipStatus { get; set; }
        [DataMember] public string CustomerSignature { get; set; }
        [DataMember] public string Remarks { get; set; }
    }

    [DataContract]
    public class GoodsReceived
    {
        [DataMember] public string ReceiptId { get; set; }
        [DataMember] public DateTime ReceiveDate { get; set; }
        [DataMember] public string SupplierId { get; set; }
        [DataMember] public string ItemId { get; set; }
        [DataMember] public int Quantity { get; set; }
        [DataMember] public string PurchaseOrderNo { get; set; }
        [DataMember] public string ReceivedBy { get; set; }
        [DataMember] public string Condition { get; set; }
        [DataMember] public string Remarks { get; set; }
    }

    [DataContract]
    public class AfterServiceRequest
    {
        [DataMember] public string RequestId { get; set; }
        [DataMember] public DateTime RequestDate { get; set; }
        [DataMember] public string OrderId { get; set; }
        [DataMember] public string CustomerId { get; set; }
        [DataMember] public string RequestType { get; set; }
        [DataMember] public string ItemId { get; set; }
        [DataMember] public int Quantity { get; set; }
        [DataMember] public string Reason { get; set; }
        [DataMember] public string Status { get; set; }
        [DataMember] public string HandledBy { get; set; }
        [DataMember] public string Resolution { get; set; }
    }

    // ============================================================
    //  PRODUCTION  —  Raw Material Request (RMR)
    // ============================================================
    [DataContract]
    public class RawMaterialRequest
    {
        [DataMember] public string RmrId { get; set; }            // RMR00001
        [DataMember] public DateTime RequestDate { get; set; }
        [DataMember] public string RequestedBy { get; set; }      // StaffId
        [DataMember] public string Department { get; set; }       // Production / Design / ...
        [DataMember] public string Status { get; set; }           // Pending / Approved / Rejected / Procured
        [DataMember] public string Notes { get; set; }
        [DataMember] public List<RmrLine> Lines { get; set; }

        public RawMaterialRequest()
        {
            Lines = new List<RmrLine>();
        }

        public int TotalQty
        {
            get
            {
                int sum = 0;
                if (Lines != null) foreach (var l in Lines) sum += l.QtyNeeded;
                return sum;
            }
        }
    }

    [DataContract]
    public class RmrLine
    {
        [DataMember] public string ItemId { get; set; }
        [DataMember] public string ItemName { get; set; }
        [DataMember] public int QtyNeeded { get; set; }
        [DataMember] public string Notes { get; set; }
    }

    // ============================================================
    //  PROCUREMENT  —  Purchase Order (PO)
    // ============================================================
    [DataContract]
    public class Procurement
    {
        [DataMember] public string PoId { get; set; }             // PO00001
        [DataMember] public string SupplierId { get; set; }
        [DataMember] public DateTime OrderDate { get; set; }
        [DataMember] public DateTime ExpectedDelivery { get; set; }
        [DataMember] public string Status { get; set; }           // Draft / Sent / PartiallyReceived / Completed / Cancelled
        [DataMember] public string LinkedRmrId { get; set; }      // optional, nullable
        [DataMember] public string CreatedBy { get; set; }
        [DataMember] public string Remarks { get; set; }
        [DataMember] public List<ProcurementLine> Lines { get; set; }

        public Procurement()
        {
            Lines = new List<ProcurementLine>();
        }

        public decimal TotalAmount
        {
            get
            {
                decimal sum = 0m;
                if (Lines != null) foreach (var l in Lines) sum += l.Subtotal;
                return sum;
            }
        }
    }

    [DataContract]
    public class ProcurementLine
    {
        [DataMember] public string ItemId { get; set; }
        [DataMember] public string ItemName { get; set; }
        [DataMember] public int Quantity { get; set; }
        [DataMember] public decimal UnitPrice { get; set; }

        public decimal Subtotal { get { return Quantity * UnitPrice; } }
    }

    [DataContract]
    public class AuditLog
    {
        [DataMember] public string LogId { get; set; }
        [DataMember] public DateTime Timestamp { get; set; }
        [DataMember] public string Username { get; set; }
        [DataMember] public string Action { get; set; }
        [DataMember] public string Detail { get; set; }
    }
}
