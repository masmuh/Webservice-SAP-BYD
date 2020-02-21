using MultipartDataMediaFormatter.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace APIService.Models
{
    public class SupplierInvoiceModel
    {
        public string vendor_id { get; set; }
        public string country { get; set; }
        public string invoice_id { get; set; }
        public string invoice_description { get; set; }
        public DateTime due_date { get; set; }
        public DateTime receipt_date { get; set; }
        public DateTime invoice_date { get; set; }
        public DateTime posting_date { get; set; }
        public string currency { get; set; }
        public string project_id { get; set; }
        public List<SupplierInvoiceLineModel> lines { get; set; }
        public List<HttpFile> Attachments { get; set; }

        public SupplierInvoiceModel()
        {
            project_id = "";
            Attachments = new List<HttpFile>();
        }
    }

    public class SupplierInvoiceLineModel
    {
        public string item_id { get; set; }
        public string line_description { get; set; }
        public int quantity { get; set; }
        public decimal net_price { get; set; }
        public decimal net_amount { get; set; }
        public string tax_code { get; set; }
        public decimal tax_rate { get; set; }

        public string purchaseorder_id { get; set; }

        public SupplierInvoiceLineModel()
        {
            purchaseorder_id = "";
        }
    }
}
