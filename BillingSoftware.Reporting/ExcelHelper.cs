using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OfficeOpenXml;
using BillingSoftware.Core.Entities;

namespace BillingSoftware.Reporting;

public static class ExcelHelper
{
    static ExcelHelper()
    {
        // Set EPPlus license context for non-commercial/community use
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    public static void ExportToExcel<T>(IEnumerable<T> data, string filePath, string sheetName = "Sheet1")
    {
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add(sheetName);

        // Get properties
        var properties = typeof(T).GetProperties()
            .Where(p => p.PropertyType.IsPrimitive || 
                        p.PropertyType == typeof(string) || 
                        p.PropertyType == typeof(decimal) || 
                        p.PropertyType == typeof(double) || 
                        p.PropertyType == typeof(int) || 
                        p.PropertyType == typeof(DateTime) ||
                        p.PropertyType == typeof(Guid) ||
                        p.PropertyType == typeof(bool))
            .ToList();

        // Write Headers
        for (int i = 0; i < properties.Count; i++)
        {
            worksheet.Cells[1, i + 1].Value = properties[i].Name;
            worksheet.Cells[1, i + 1].Style.Font.Bold = true;
        }

        // Write Data
        var list = data.ToList();
        for (int row = 0; row < list.Count; row++)
        {
            for (int col = 0; col < properties.Count; col++)
            {
                var val = properties[col].GetValue(list[row]);
                worksheet.Cells[row + 2, col + 1].Value = val?.ToString();
            }
        }

        worksheet.Cells.AutoFitColumns();
        
        var fileInfo = new FileInfo(filePath);
        package.SaveAs(fileInfo);
    }

    public static List<Customer> ImportCustomers(string filePath)
    {
        var list = new List<Customer>();
        using var package = new ExcelPackage(new FileInfo(filePath));
        var worksheet = package.Workbook.Worksheets[0];
        
        int rowCount = worksheet.Dimension.Rows;
        
        // Assume row 1 is headers. Headers could be: Name, GSTIN, PAN, Mobile, Email, Address, City, State, Pincode, OpeningBalance, CreditDays, CustomerType
        for (int row = 2; row <= rowCount; row++)
        {
            string name = worksheet.Cells[row, 1].Text;
            if (string.IsNullOrWhiteSpace(name)) continue;

            string gstin = worksheet.Cells[row, 2].Text;
            string pan = worksheet.Cells[row, 3].Text;
            string mobile = worksheet.Cells[row, 4].Text;
            string email = worksheet.Cells[row, 5].Text;
            string address = worksheet.Cells[row, 6].Text;
            string city = worksheet.Cells[row, 7].Text;
            string state = worksheet.Cells[row, 8].Text;
            string pincode = worksheet.Cells[row, 9].Text;
            
            decimal.TryParse(worksheet.Cells[row, 10].Text, out decimal openingBal);
            int.TryParse(worksheet.Cells[row, 11].Text, out int creditDays);
            string custType = worksheet.Cells[row, 12].Text;

            list.Add(new Customer
            {
                Id = Guid.NewGuid(),
                Name = name,
                GSTIN = gstin,
                PAN = pan,
                Mobile = mobile,
                Email = email,
                Address = address,
                City = city,
                State = string.IsNullOrWhiteSpace(state) ? "Maharashtra" : state,
                Pincode = pincode,
                OpeningBalance = openingBal,
                OutstandingBalance = openingBal,
                CreditDays = creditDays == 0 ? 30 : creditDays,
                CustomerType = string.IsNullOrWhiteSpace(custType) ? "Unregistered" : custType,
                IsActive = true
            });
        }
        
        return list;
    }

    public static List<Product> ImportProducts(string filePath, Guid defaultCategoryId, Guid defaultUnitId)
    {
        var list = new List<Product>();
        using var package = new ExcelPackage(new FileInfo(filePath));
        var worksheet = package.Workbook.Worksheets[0];
        
        int rowCount = worksheet.Dimension.Rows;
        
        // Assume row 1 is headers. Headers: Name, Description, HSNCode, GSTPercentage, PurchasePrice, SalesPrice, OpeningStock, MinimumStock
        for (int row = 2; row <= rowCount; row++)
        {
            string name = worksheet.Cells[row, 1].Text;
            if (string.IsNullOrWhiteSpace(name)) continue;

            string desc = worksheet.Cells[row, 2].Text;
            string hsn = worksheet.Cells[row, 3].Text;
            
            decimal.TryParse(worksheet.Cells[row, 4].Text, out decimal gst);
            decimal.TryParse(worksheet.Cells[row, 5].Text, out decimal purchasePrice);
            decimal.TryParse(worksheet.Cells[row, 6].Text, out decimal salesPrice);
            decimal.TryParse(worksheet.Cells[row, 7].Text, out decimal openingStock);
            decimal.TryParse(worksheet.Cells[row, 8].Text, out decimal minStock);

            list.Add(new Product
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = desc,
                CategoryId = defaultCategoryId,
                UnitId = defaultUnitId,
                HSNCode = hsn,
                GSTPercentage = gst,
                PurchasePrice = purchasePrice,
                SalesPrice = salesPrice,
                OpeningStock = openingStock,
                CurrentStock = openingStock,
                MinimumStockLevel = minStock,
                IsActive = true
            });
        }
        
        return list;
    }
}
