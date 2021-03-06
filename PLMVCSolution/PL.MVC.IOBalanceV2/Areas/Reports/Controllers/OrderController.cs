﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

//Business
using PL.Business.Common;
using PL.Business.Dto.IOBalanceV2;
using PL.Business.Interface.IOBalanceV2;

//MVC
using PL.MVC.IOBalanceV2.Models;

using PL.MVC.IOBalanceV2.Infrastructure;
using Infrastructure.Utilities.Extensions;
using Infrastructure.Utilities;
using System.Web.Http;

//Kendo
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;
using PL.MVC.IOBalanceV2.Controllers;
using PL.MVC.IOBalanceV2.Areas.OrderManagement.Models;
using LinqKit;
using PL.MVC.IOBalanceV2.Areas.Reports.Models;
using System.Data;
using System.IO;
using OfficeOpenXml;
using System.Data.Entity;

namespace PL.MVC.IOBalanceV2.Areas.Reports.Controllers
{
    public partial class OrderController : BaseController
    {
        #region Declarations and constructors
        private readonly IInventoryService _inventoryService;
        private readonly IOrderService _orderService;

        public OrderController(
            IInventoryService inventoryService,
            IOrderService orderService
            )
        {
            this._inventoryService = inventoryService;
            this._orderService = orderService;

        }
        #endregion Declarations and constructors

        #region Action methods
        
        #region PO
        public virtual ActionResult PurchaseOrder()
        {
            
            return View();
        }

        public virtual ActionResult GetPOReport([DataSourceRequest] DataSourceRequest request, PurchaseOrderSearchModel searchModel)
        {
            var list = GetPurchaseOrderReport(searchModel);
            return Json(list.ToDataSourceResult(request), JsonRequestBehavior.AllowGet);
        }

        public virtual ActionResult ExportPOExcel(PurchaseOrderSearchModel searchModel)
        {
            return ReportPurchaseOrderExtract(searchModel);
        }
        #endregion PO

        #region SO
        public virtual ActionResult SalesOrder()
        {
            return View();
        }

        public virtual ActionResult GetSOReport([DataSourceRequest] DataSourceRequest request, SalesOrderSearchModel searchModel)
        {
            var list = GetSalesOrderReport(searchModel);
            return Json(list.ToDataSourceResult(request), JsonRequestBehavior.AllowGet);
        }

        public virtual ActionResult ExportSOExcel(SalesOrderSearchModel searchModel)
        {
            return ReportSalesOrderExtract(searchModel);
        }
        #endregion SO
        
        #endregion Action methods

        #region Private methods
        //var list = _orderService.GetAllSalesOrderReport();
        //    foreach (var item in list)
        //    {
        //        var salesOrderlist = _orderService.GetAllSalesOrderDetail(item.SalesOrderId).ToList();
        //        item.salesOderDetails = salesOrderlist;
        //    }
        private IQueryable<ReportPurchaseOrderDto> GetPurchaseOrderReport(PurchaseOrderSearchModel searchModel)
        {
            IQueryable<ReportPurchaseOrderDto> result = null;


            if ((searchModel.DateFrom.IsNull() && searchModel.DateTo.IsNull() && searchModel.ProductId == 0 && searchModel.SupplierId == 0))
            {
                result = _orderService.GetAllPurchaseOrderReport();
            }
            else
            {
                var predicate = PredicateBuilder.True<ReportPurchaseOrderDto>();


                if (searchModel.ProductId != 0)
                {
                    predicate = predicate.And(p => p.ProductId == searchModel.ProductId);
                }

                if (searchModel.SupplierId != 0)
                {
                    predicate = predicate.And(p => p.SupplierId == searchModel.SupplierId);
                }

                if (!searchModel.DateFrom.IsNull() && searchModel.DateTo.IsNull())
                {
                    predicate = predicate.And(a => DbFunctions.TruncateTime(a.DateCreated) >= DbFunctions.TruncateTime(searchModel.DateFrom));
                }
                else if (searchModel.DateFrom.IsNull() && !searchModel.DateTo.IsNull())
                {
                    predicate = predicate.And(a => DbFunctions.TruncateTime(a.DateCreated) <= DbFunctions.TruncateTime(searchModel.DateTo));
                }
                else if (!searchModel.DateFrom.IsNull() && !searchModel.DateTo.IsNull())
                {
                    predicate = predicate.And(a => DbFunctions.TruncateTime(a.DateCreated) >= DbFunctions.TruncateTime(searchModel.DateFrom) && DbFunctions.TruncateTime(a.DateCreated) <= DbFunctions.TruncateTime(searchModel.DateTo));
                }

                result = _orderService.GetAllPurchaseOrderReport().AsExpandable().Where(predicate);
            }

            return result;

        }

        private IQueryable<ReportSalesOrderDto> GetSalesOrderReport(SalesOrderSearchModel searchModel)
        {
            IQueryable<ReportSalesOrderDto> result = null;


            if ((searchModel.DateFrom.IsNull() && searchModel.DateTo.IsNull() && searchModel.CustomerId == 0 && searchModel.SalesNo.IsNull()))
            {
                result = _orderService.GetAllSalesOrderReport();
            }
            else
            {
                var predicate = PredicateBuilder.True<ReportSalesOrderDto>();


                if (searchModel.CustomerId != 0)
                {
                    predicate = predicate.And(p => p.CustomerId == searchModel.CustomerId);
                }

                if (!searchModel.SalesNo.IsNull())
                {
                    predicate = predicate.And(p => p.SalesNo.Contains(searchModel.SalesNo));
                }

                if (!searchModel.DateFrom.IsNull() && searchModel.DateTo.IsNull())
                {
                    predicate = predicate.And(a => DbFunctions.TruncateTime(a.DateCreated) >= DbFunctions.TruncateTime(searchModel.DateFrom));
                }
                else if (searchModel.DateFrom.IsNull() && !searchModel.DateTo.IsNull())
                {
                    predicate = predicate.And(a => DbFunctions.TruncateTime(a.DateCreated) <= DbFunctions.TruncateTime(searchModel.DateTo));
                }
                else if (!searchModel.DateFrom.IsNull() && !searchModel.DateTo.IsNull())
                {
                    predicate = predicate.And(a => DbFunctions.TruncateTime(a.DateCreated) >= DbFunctions.TruncateTime(searchModel.DateFrom) && DbFunctions.TruncateTime(a.DateCreated) <= DbFunctions.TruncateTime(searchModel.DateTo));
                }

                result = _orderService.GetAllSalesOrderReport().AsExpandable().Where(predicate);
            }

            return result;

        }

        private System.Web.Mvc.FileResult ReportPurchaseOrderExtract(PurchaseOrderSearchModel searchModel)
        {
            
            int rowId = 0;
            int colId = 0;

            var list = GetPurchaseOrderReport(searchModel).ToList();

            var dir = Server.MapPath(string.Format("~/{0}", Constants.ProductExcelTemplateDir));
            var fileNameTemplate = string.Format("{0}{1}", Constants.ReportPOTemplate, ".xlsx");
            var path = System.IO.Path.Combine(dir, fileNameTemplate);
            var fileNameGenerated = string.Format("{0}{1}", Constants.ReportPerPurchaseOrder, ".xlsx");

            var contentType = "application/vnd.ms-excel";

            var templateFile = new FileInfo(path);
            //var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
            ;

            var package = new ExcelPackage(templateFile);
            var workSheet = package.Workbook.Worksheets[1];

            rowId = 2;
            foreach (var detail in list)
            {
                workSheet.Cells["A" + rowId.ToString()].Value = detail.product.ProductInfoDisplay;
                workSheet.Cells["B" + rowId.ToString()].Value = detail.supplier.SupplierInfoDisplay;
                workSheet.Cells["C" + rowId.ToString()].Value = detail.Quantity;
                workSheet.Cells["D" + rowId.ToString()].Value = detail.DateCreatedWithFormat;
                rowId++;
            }

            


            var memoryStream = new MemoryStream();
            //package.Save();
            package.SaveAs(memoryStream);
            memoryStream.Position = 0;

            return File(memoryStream, contentType, fileNameGenerated);
        }

        private System.Web.Mvc.FileResult ReportSalesOrderExtract(SalesOrderSearchModel searchModel)
        {

            int rowId = 0;
            int colId = 0;

            var list = GetSalesOrderReport(searchModel).ToList();

            var dir = Server.MapPath(string.Format("~/{0}", Constants.ProductExcelTemplateDir));
            var fileNameTemplate = string.Format("{0}{1}", Constants.ReportSOTemplate, ".xlsx");
            var path = System.IO.Path.Combine(dir, fileNameTemplate);
            var fileNameGenerated = string.Format("{0}{1}", Constants.ReportPerSalesOrder, ".xlsx");

            var contentType = "application/vnd.ms-excel";

            var templateFile = new FileInfo(path);
            //var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
            

            var package = new ExcelPackage(templateFile);
            var workSheet = package.Workbook.Worksheets[1];

            rowId = 2;
            foreach (var detail in list)
            {


                var salesOrderList = _orderService.GetAllSalesOrderDetail(detail.SalesOrderId).ToList();

                foreach (var salesDetail in salesOrderList)
                {
                    workSheet.Cells["A" + rowId.ToString()].Value = detail.SalesNo;
                    workSheet.Cells["B" + rowId.ToString()].Value = detail.customer.CustomerDropDownDisplay;
                    workSheet.Cells["C" + rowId.ToString()].Value = salesDetail.product.ProductInfoDisplay;
                    workSheet.Cells["D" + rowId.ToString()].Value = salesDetail.UnitPrice;
                    workSheet.Cells["E" + rowId.ToString()].Value = salesDetail.SalesPrice;
                    workSheet.Cells["F" + rowId.ToString()].Value = salesDetail.Quantity;
                    workSheet.Cells["G" + rowId.ToString()].Value = detail.DateCreatedWithFormat;
                    rowId++;
                }



                
            }




            var memoryStream = new MemoryStream();
            //package.Save();
            package.SaveAs(memoryStream);
            memoryStream.Position = 0;

            return File(memoryStream, contentType, fileNameGenerated);
        }
        #endregion Private methods

        
    }
}