using APIService.ManagePOService;
using APIService.Models;
using APIService.QueryPurchaseOrder;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web.Http;

namespace APIService.Controllers
{
    [AuthApi]
    public class SupplierInvoicingController : ApiController
    {
        List<string> logDetais = new List<string>();
 

        [Route("api/SupplierInvoicing/Create")]
        [HttpPost]
        public IHttpActionResult Create([FromBody]SupplierInvoiceModel value)
        {
            var client = new ManageSupInvService.ManageSupplierInvoiceInClient("binding_ManageSupInv");
            client.ClientCredentials.UserName.UserName = ConfigurationManager.AppSettings["UserName_BU"];
            client.ClientCredentials.UserName.Password = ConfigurationManager.AppSettings["Password_BU"];

            var countryMapClient = new QueryCountryUnitMap.Y2E47QWTY_CountryUnitMapWSClient("binding_QryCountryMap");
            countryMapClient.ClientCredentials.UserName.UserName = ConfigurationManager.AppSettings["UserName_BU"];
            countryMapClient.ClientCredentials.UserName.Password = ConfigurationManager.AppSettings["Password_BU"];

            string invoiceId = "";

            try
            {
                var param = new ManageSupInvService.SupplierInvoiceBundleMaintainRequestMessage_sync();
                var bodyParam = new List<ManageSupInvService.SupplierInvoiceMaintainRequestBundle>();
                var body = new ManageSupInvService.SupplierInvoiceMaintainRequestBundle();

                body.BusinessTransactionDocumentTypeCode = new ManageSupInvService.BusinessTransactionDocumentTypeCode() { Value = "004" };
                body.Date = value.invoice_date == null ? "" : value.invoice_date.ToString("yyyy-MM-dd");
                body.ReceiptDate = value.receipt_date == null ? "" : value.receipt_date.ToString("yyyy-MM-dd");
                body.TransactionDate = value.posting_date == null ? "" : value.posting_date.ToString("yyyy-MM-dd");
                body.CashDiscountTerms = new ManageSupInvService.MaintenanceCashDiscountTerms()
                {
                    PaymentBaselineDate = value.due_date == null ? "" : value.due_date.ToString("yyyy-MM-dd")
                };

                #region invoice number
                body.CustomerInvoiceReference = new ManageSupInvService.SupplierInvoiceMaintainRequestBundleBusinessTransactionDocumentReference()
                {
                    BusinessTransactionDocumentReference = new ManageSupInvService.BusinessTransactionDocumentReference()
                    {
                        ID = new ManageSupInvService.BusinessTransactionDocumentID() { Value = value.invoice_id },
                        ItemTypeCode = "28"
                    }
                };

                body.MEDIUM_Name = new ManageSupInvService.MEDIUM_Name() { Value = value.invoice_description };

                #endregion

                #region seller
                body.SellerParty = new ManageSupInvService.SupplierInvoiceMaintainRequestBundleParty()
                {
                    PartyKey = new ManageSupInvService.PartyKey()
                    {
                        PartyTypeCode = new ManageSupInvService.BusinessObjectTypeCode() { Value = "147" },
                        PartyID = new ManageSupInvService.PartyID() { Value = value.vendor_id }
                    }
                };
                #endregion

                #region get company from buyer
                var companyId = "";

                // EmployeeResponsibleParty - Company Id
                var countryMapHeader = new QueryCountryUnitMap.CountryUnitMappingCounstryUnitMapViewCountryUnitMapQrySimpleByRequest();
                var selectionBycountryList = new QueryCountryUnitMap.CountryUnitMappingCounstryUnitMapViewCountryUnitMapQrySimpleByRequestSelectionBycountry[1];
                var selectionBycountry = new QueryCountryUnitMap.CountryUnitMappingCounstryUnitMapViewCountryUnitMapQrySimpleByRequestSelectionBycountry();
                selectionBycountry.InclusionExclusionCode = "I";
                selectionBycountry.IntervalBoundaryTypeCode = "1";
                var LowerBoundaryInternalID = new QueryCountryUnitMap.BusinessTransactionDocumentID();
                LowerBoundaryInternalID.Value = value.country.ToUpper();
                selectionBycountry.LowerBoundarycountry = LowerBoundaryInternalID;
                selectionBycountryList[0] = selectionBycountry;
                countryMapHeader.SelectionBycountry = selectionBycountryList;
                var qryCountryUnitMapReq = new QueryCountryUnitMap.CountryUnitMappingCounstryUnitMapViewCountryUnitMapQrySimpleByRequestMessage_sync();
                qryCountryUnitMapReq.CountryUnitMappingSimpleSelectionBy = countryMapHeader;
                var countryConfirm = countryMapClient.CountryUnitMapQry(qryCountryUnitMapReq);
                if (countryConfirm.Log.MaximumLogItemSeverityCode == "1")
                {
                    foreach (var countryConfirmGet in countryConfirm.CountryUnitMapping)
                    {
                        companyId = countryConfirmGet.company_id;
                        break;
                    }
                }
                else
                {
                    logDetais.Add("Mapping country " + value.country.ToUpper() + " does not exist");
                    return Ok(MessageModel.LogError("Validation Error", logDetais));
                }
                #endregion

                #region buyer
                body.BuyerParty = new ManageSupInvService.SupplierInvoiceMaintainRequestBundleParty()
                {
                    PartyKey = new ManageSupInvService.PartyKey()
                    {
                        PartyID = new ManageSupInvService.PartyID() { Value = companyId },
                        PartyTypeCode = new ManageSupInvService.BusinessObjectTypeCode() { Value = "200" }
                    }
                };
                #endregion

                body.BillToParty = new ManageSupInvService.SupplierInvoiceMaintainRequestBundleParty()
                {
                    PartyKey = new ManageSupInvService.PartyKey()
                    {
                        PartyID = new ManageSupInvService.PartyID() { Value = companyId }
                    }
                };


                var POList = value.lines.Select(x => x.purchaseorder_id).Distinct().ToList();
                #region get po item id
                var clientPO = new ManagePOService.ManagePurchaseOrderInClient("binding_ManagePurchaseOrderIn");
                clientPO.ClientCredentials.UserName.UserName = ConfigurationManager.AppSettings.Get("Username_CU");
                clientPO.ClientCredentials.UserName.Password = ConfigurationManager.AppSettings.Get("Password_CU");
                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls11;

                var paramPO = new PurchaseOrderByIDQueryMessage_sync();
                paramPO.PurchaseOrder = new PurchaseOrderByIDQuery() { ID = ServicePOIds(POList).ToArray() };
                var GetBindingPO = clientPO.ManagePurchaseOrderInRead(paramPO);
                var DataBindingPO = GetBindingPO.PurchaseOrder.ToList();
                #endregion

                var items = new List<ManageSupInvService.SupplierInvoiceMaintainRequestBundleItem>();

                double taxAmount = 0;
                double totalAmount = 0;

                if (value.lines.Count > 0)
                {
                    foreach (var d in value.lines)
                    {
                        var i = new ManageSupInvService.SupplierInvoiceMaintainRequestBundleItem();
                        var GetPO = DataBindingPO.FirstOrDefault(x => x.ID.Value == d.purchaseorder_id);
                        if (GetPO != null)
                        {
                            var GetItem = GetPO.Item.FirstOrDefault(x => x.ItemProduct.ProductKey.ProductID.Value == d.item_id);
                            if (GetItem == null)
                            {
                                throw new Exception(d.item_id + " not found in purchase orde " + GetPO.ID.Value);
                            }

                            if (d.net_amount > GetItem.NetAmount.Value)
                            {
                                logDetais.Add("Invice amount " + string.Format("{0:n}", d.net_amount) + " greater than po amount " + string.Format("{0:n}", GetItem.NetAmount.Value));
                            }

                            i.ItemID = GetItem.ItemID;
                            i.Product = new ManageSupInvService.SupplierInvoiceMaintainRequestBundleItemProduct()
                            {
                                ProductKey = new ManageSupInvService.ProductKey()
                                {
                                    ProductID = new ManageSupInvService.ProductID() { Value = GetItem.ItemProduct.ProductKey.ProductID.Value },
                                    ProductTypeCode = "1",
                                    ProductIdentifierTypeCode = "3"
                                }
                            };
                            i.BusinessTransactionDocumentItemTypeCode = "002";
                            i.SHORT_Description = new ManageSupInvService.SHORT_Description() { Value = GetItem.Description.Value };
                            i.Quantity = new ManageSupInvService.Quantity() { Value = Convert.ToDecimal(d.quantity), unitCode = GetItem.Quantity.unitCode };
                            i.NetUnitPrice = new ManageSupInvService.Price() { Amount = new ManageSupInvService.Amount() { Value = d.net_price } };
                            i.NetAmount = new ManageSupInvService.Amount() { Value = d.net_amount };
                            i.PurchaseOrderReference = new ManageSupInvService.SupplierInvoiceMaintainRequestBundleItemBusinessTransactionDocumentReference()
                            {
                                BusinessTransactionDocumentReference = new ManageSupInvService.BusinessTransactionDocumentReference()
                                {
                                    ID = new ManageSupInvService.BusinessTransactionDocumentID() { Value = GetPO.ID.Value },
                                    ItemID = GetItem.ItemID
                                }
                            };

                            //i.ProductTax = new ManageSupInvService.SupplierInvoiceMaintainRequestBundleItemProductTax()
                            //{
                            //    actionCode = ManageSupInvService.ActionCode.Item01,
                            //    ProductTaxationCharacteristicsCode = new ManageSupInvService.ProductTaxationCharacteristicsCode() { Value = "010", listID = "" },
                            //    CountryCode = "ID",
                            //};

                            double Vat = Convert.ToDouble(GetItem.ItemTaxCalculation?.ItemProductTaxDetails?.FirstOrDefault()?.TransactionCurrencyProductTax.Percent);
                            double VataMT = Convert.ToDouble(d.net_amount) * (Vat / 100);

                            taxAmount += VataMT;
                            totalAmount += Convert.ToDouble(d.net_amount);
                            items.Add(i);
                        }
                    }

                    body.Item = items.ToArray();
                    body.itemListCompleteTransmissionIndicator = true;
                }


                bodyParam.Add(body);
                body.TaxAmount = new ManageSupInvService.Amount()
                {
                    Value = Convert.ToDecimal(taxAmount),
                    currencyCode = value.currency
                };

                body.GrossAmount = new ManageSupInvService.Amount()
                {
                    currencyCode = value.currency,
                    Value = Convert.ToDecimal(totalAmount + taxAmount)
                };

                var BinaryFiles = new List<BinaryObject>();
                if (value.Attachments != null && value.Attachments.Count > 0)
                {
                    foreach (var file in value.Attachments)
                    {
                        BinaryFiles.Add(new BinaryObject()
                        {
                            format = file.MediaType,
                            fileName = file.FileName,
                            Value = file.Buffer
                        });
                    }

                    var attchFolder = new ManageSupInvService.MaintenanceAttachmentFolder();
                    attchFolder.DocumentListCompleteTransmissionIndicator = true;
                    attchFolder.DocumentListCompleteTransmissionIndicatorSpecified = true;
                    attchFolder.ActionCodeSpecified = true;
                    attchFolder.Document = attachDocument(BinaryFiles).ToArray();
                    body.AttachmentFolder = attchFolder;
                }

                param.SupplierInvoice = bodyParam.ToArray();

                if (logDetais.Count() > 0)
                {
                    return Ok(MessageModel.LogError("Exception Log", logDetais));
                }

                var SendData = client.MaintainBundle(param);

                if (SendData.Log != null && SendData.Log.MaximumLogItemSeverityCode == "3")
                {
                    foreach (var x in SendData.Log.Item)
                    {
                        logDetais.Add(x.Note);
                    }
                    return Ok(MessageModel.LogError("Exception Log", logDetais));
                }

                invoiceId = SendData.SupplierInvoice.FirstOrDefault()?.BusinessTransactionDocumentID?.Value;
            }
            catch (Exception e)
            {
                logDetais.Add("> " + e.Message);
                return Ok(MessageModel.LogError("Exception Log", logDetais));
            }
            return Ok(MessageModel.LogSuccess("Supplier invoice " + invoiceId + " has been successfully inserted to SAP"));
        }

        private List<ManageSupInvService.MaintenanceAttachmentFolderDocument> attachDocument(List<BinaryObject> fileBinary)
        {
            var data = new List<ManageSupInvService.MaintenanceAttachmentFolderDocument>();
            foreach (var f in fileBinary)
            {
                var a = new ManageSupInvService.MaintenanceAttachmentFolderDocument();
                a.VisibleIndicator = true;
                a.VisibleIndicatorSpecified = true;
                a.TypeCode = new ManageSupInvService.DocumentTypeCode() { Value = "10001" };
                a.ActionCode = ManageSupInvService.ActionCode.Item04;
                a.Description = new ManageSupInvService.Description() { Value = f.fileName };
                a.CategoryCode = "2";
                a.FileContent = new ManageSupInvService.MaintenanceAttachmentFolderDocumentFileContent()
                {
                    BinaryObject = new ManageSupInvService.BinaryObject() { fileName = f.fileName, format = f.format, uri = f.uri, Value = f.Value, mimeCode = f.mimeCode },
                    ActionCode = ManageSupInvService.ActionCode.Item04,
                    ActionCodeSpecified = true
                };
                a.Name = f.fileName;
                a.PropertyListCompleteTransmissionIndicator = true;
                a.PropertyListCompleteTransmissionIndicatorSpecified = true;
                data.Add(a);
            }
            return data;
        }

        private List<QueryProjectService.ProjectByQueryResponse> getPOProject(string projId)
        {
            var client = new QueryProjectService.QueryProjectInClient("binding_QryProject");
            client.ClientCredentials.UserName.UserName = ConfigurationManager.AppSettings["UserName_BU"];
            client.ClientCredentials.UserName.Password = ConfigurationManager.AppSettings["Password_BU"];

            var param = new QueryProjectService.ProjectByElementsQueryMessage_sync();

            var selectionName = new List<QueryProjectService.ProjectSelectionByProjectId>();
            selectionName.Add(new QueryProjectService.ProjectSelectionByProjectId()
            {
                IntervalBoundaryTypeCode = "1",
                InclusionExclusionCode = "I",
                LowerBoundaryProjectID = new QueryProjectService.ProjectID() { Value = projId }
            });
            param.ProjectSelectionByElements = new QueryProjectService.ProjectByElementsQuerySelectionByElements
            {
                SelectionByProjectID = selectionName.ToArray()
            };
            var get = client.FindProjectByElements(param);

            var result = new List<QueryProjectService.ProjectByQueryResponse>();
            result.AddRange(get.ProjectQueryResponse);
            return result;
        }

        private string GetStatus(string param)
        {
            switch (param)
            {
                case "Item1":
                    return "In Process";
                case "Item2":
                    return "Exception";
                case "Item3":
                    return "Ready for Posting";
                case "Item4":
                    return "In Approval";
                case "Item5":
                    return "In Revision";
                case "Item6":
                    return "Approved";
                case "Item7":
                    return "Voided";
                case "Item8":
                    return "Posted";
                case "Item9":
                    return "Canceled";
                case "Item10":
                    return "	In Cancellation";
                case "Item11":
                    return "Partially Paid";
                case "Item12":
                    return "Paid";
                case "Item13":
                    return "Delayed Verification in Process";
                default:
                    return "";
            }
        }

        private List<ManagePOService.BusinessTransactionDocumentID> ServicePOIds(List<string> POIds)
        {
            var data = new List<ManagePOService.BusinessTransactionDocumentID>();
            if (POIds.Count > 0)
            {
                foreach (var Id in POIds)
                {
                    var x = new ManagePOService.BusinessTransactionDocumentID();
                    x.Value = Id;
                    data.Add(x);
                }
            }

            return data;
        }
        private List<PurchaseOrderSimpleByElementsResponse> GetPONumber(List<string> SupplierId)
        {
            var data = new List<PurchaseOrderSimpleByElementsResponse>();
            var client = new QueryPurchaseOrder.QueryPurchaseOrderQueryInClient("binding_QueryPurchaseOrder");
            client.ClientCredentials.UserName.UserName = ConfigurationManager.AppSettings.Get("Username_CU");
            client.ClientCredentials.UserName.Password = ConfigurationManager.AppSettings.Get("Password_CU");
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls11;

            var Request = new PurchaseOrderSimpleByElementsQueryMessage_sync();
            var PurchaseOrderSimpleSelectionByElements = new PurchaseOrderSimpleByElementsQuery();
            //PurchaseOrderSimpleSelectionByElements.SelectionByPurchaseOrderLifeCycleStatusCode = POQuery().ToArray();
            PurchaseOrderSimpleSelectionByElements.SelectionByPartySellerPartyKeyPartyID = SupplierFilter(SupplierId).ToArray();
            Request.PurchaseOrderSimpleSelectionByElements = PurchaseOrderSimpleSelectionByElements;

            var limit = new QueryPurchaseOrder.QueryProcessingConditions();
            limit.QueryHitsMaximumNumberValue = 100000;
            limit.QueryHitsUnlimitedIndicator = true;

            Request.ProcessingConditions = limit;
            var fetchData = client.FindSimpleByElements(Request);
            if (fetchData.PurchaseOrder != null)
            {
                data = fetchData.PurchaseOrder.ToList();
            }
            return data;
        }

        private List<PurchaseOrderSimpleByElementsQuerySelectionByPartySellerPartyKeyPartyID> SupplierFilter(List<string> Ids)
        {
            var data = new List<PurchaseOrderSimpleByElementsQuerySelectionByPartySellerPartyKeyPartyID>();
            if (Ids.Count > 0)
            {
                foreach (var d in Ids)
                {
                    var x = new PurchaseOrderSimpleByElementsQuerySelectionByPartySellerPartyKeyPartyID();
                    x.InclusionExclusionCode = "I";
                    x.IntervalBoundaryTypeCode = "1";
                    x.LowerBoundarySellerPartyID = new QueryPurchaseOrder.PartyID() { Value = d };
                    data.Add(x);
                }
            }

            return data;
        }
    }
}
 
