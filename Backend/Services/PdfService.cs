using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using HardwareStoreAPI.Models;

namespace HardwareStoreAPI.Services
{
    public interface IPdfService
    {
        byte[] GenerateBillPdf(BillPdfData billData);
        byte[] GenerateQuotationPdf(QuotationPdfData quotationData);
    }

    public class PdfService : IPdfService
    {
        public byte[] GenerateQuotationPdf(QuotationPdfData quotationData)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(11));

                    page.Content().Column(column =>
                    {
                        // Header
                        column.Item().AlignCenter().Text("Hardware Store").Bold().FontSize(24);
                        column.Item().AlignCenter().Text("Main Bazar Lahore").FontSize(12);
                        column.Item().AlignCenter().Text("Phone: 03021222005").FontSize(12);
                        column.Item().PaddingVertical(10).LineHorizontal(1);

                        // Quotation Info
                        column.Item().PaddingBottom(10).Row(row =>
                        {
                            row.RelativeItem().Column(infoCol =>
                            {
                                infoCol.Item().Text($"Customer: {quotationData.CustomerName}").Bold();
                                infoCol.Item().Text($"Quotation #: {quotationData.QuotationNumber}");  // ✅ Changed from Invoice
                            });
                            row.RelativeItem().AlignRight().Column(dateCol =>
                            {
                                dateCol.Item().Text($"Date: {quotationData.QuotationDate:dd-MMM-yyyy}");
                                dateCol.Item().Text($"Time: {quotationData.QuotationDate:hh:mm tt}");
                                if (quotationData.ValidUntil.HasValue)
                                {
                                    dateCol.Item().Text($"Valid Until: {quotationData.ValidUntil.Value:dd-MMM-yyyy}").FontColor("#dc3545");
                                }
                            });
                        });

                        column.Item().PaddingBottom(15).LineHorizontal(0.5f);

                        // Table Header
                        column.Item().PaddingBottom(5).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);    // Product
                                columns.ConstantColumn(60);   // Size
                                columns.ConstantColumn(70);   // Unit
                                columns.ConstantColumn(60);   // Qty
                                columns.ConstantColumn(70);   // Price
                                columns.ConstantColumn(60);   // Discount
                                columns.ConstantColumn(80);   // Total
                            });

                            table.Header(header =>
                            {
                                header.Cell().Padding(5).Background("#f0f0f0").Text("Product").Bold();
                                header.Cell().Padding(5).Background("#f0f0f0").AlignRight().Text("Size").Bold();
                                header.Cell().Padding(5).Background("#f0f0f0").AlignRight().Text("Unit").Bold();
                                header.Cell().Padding(5).Background("#f0f0f0").AlignRight().Text("Qty").Bold();
                                header.Cell().Padding(5).Background("#f0f0f0").AlignRight().Text("Price").Bold();
                                header.Cell().Padding(5).Background("#f0f0f0").AlignRight().Text("Discount").Bold();
                                header.Cell().Padding(5).Background("#f0f0f0").AlignRight().Text("Total").Bold();
                            });
                        });

                        // Items
                        int itemCount = 0;
                        foreach (var item in quotationData.Items)
                        {
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3);
                                    columns.ConstantColumn(60);
                                    columns.ConstantColumn(70);
                                    columns.ConstantColumn(60);
                                    columns.ConstantColumn(70);
                                    columns.ConstantColumn(60);
                                    columns.ConstantColumn(80);
                                });

                                table.Cell().Padding(5).Text(item.ProductName);
                                table.Cell().Padding(5).AlignRight().Text(item.Size ?? "-");
                                table.Cell().Padding(5).AlignRight().Text(item.UnitOfMeasure);
                                table.Cell().Padding(5).AlignRight().Text(item.Quantity.ToString("N2"));
                                table.Cell().Padding(5).AlignRight().Text($"Rs. {item.UnitPrice:N2}");
                                table.Cell().Padding(5).AlignRight().Text($"Rs. {(item.UnitPrice - item.LineTotal / item.Quantity):N2}");
                                table.Cell().Padding(5).AlignRight().Text($"Rs. {item.LineTotal:N2}").Bold();
                            });

                            if (itemCount < quotationData.Items.Count - 1)
                            {
                                column.Item().PaddingHorizontal(10).LineHorizontal(0.2f);
                            }
                            itemCount++;
                        }

                        // Summary
                        column.Item().PaddingTop(20).Table(summaryTable =>
                        {
                            summaryTable.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.ConstantColumn(150);
                            });

                            summaryTable.Cell().Padding(3).AlignRight().Text("Subtotal:");
                            summaryTable.Cell().Padding(3).AlignRight().Text($"Rs. {quotationData.Subtotal:N2}");

                            if (quotationData.DiscountAmount > 0)
                            {
                                summaryTable.Cell().Padding(3).AlignRight().Text("Total Discount:");
                                summaryTable.Cell().Padding(3).AlignRight().Text($"Rs. {quotationData.DiscountAmount:N2}");
                            }

                            summaryTable.Cell().Padding(5).Background("#e8f4fd").AlignRight().Text("TOTAL:").Bold();
                            summaryTable.Cell().Padding(5).Background("#e8f4fd").AlignRight().Text($"Rs. {quotationData.TotalAmount:N2}").Bold().FontSize(12);

                            // ✅ No Amount Paid or Balance for Quotations
                        });

                        column.Item().PaddingVertical(15).LineHorizontal(1);

                        // Footer
                        column.Item().AlignCenter().Text("Thank you for your interest!").Bold().FontSize(14);
                        column.Item().PaddingVertical(5).AlignCenter().Text("This is a quotation, not an invoice");
                        column.Item().AlignCenter().Text("آپ کی دلچسپی کا شکریہ");

                        column.Item().PaddingVertical(15).AlignCenter().Text("Terms & Conditions:").SemiBold();
                        column.Item().AlignCenter().Text("• This quotation is valid for the mentioned period");
                        column.Item().AlignCenter().Text("• Prices are subject to change without prior notice");
                        column.Item().AlignCenter().Text("• Please confirm your order to proceed");

                        column.Item().PaddingVertical(20).LineHorizontal(0.5f);

                        column.Item().AlignCenter().Text("Developed By: devinfantary.com | 03477048001").FontSize(9);
                        column.Item().AlignCenter().Text($"Printed on: {DateTime.Now:dd-MMM-yyyy hh:mm tt}").FontSize(9);
                    });
                });
            });

            return document.GeneratePdf();
        }

        public byte[] GenerateBillPdf(BillPdfData billData)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(11));

                    page.Content().Column(column =>
                    {
                        // Header
                        column.Item().AlignCenter().Text("Hardware Store").Bold().FontSize(24);
                        column.Item().AlignCenter().Text("Main Bazar Lahore").FontSize(12);
                        column.Item().AlignCenter().Text("Phone: 03021222005").FontSize(12);
                        column.Item().PaddingVertical(10).LineHorizontal(1);

                        // Bill Info
                        column.Item().PaddingBottom(10).Row(row =>
                        {
                            row.RelativeItem().Column(infoCol =>
                            {
                                infoCol.Item().Text($"Customer: {billData.CustomerName}").Bold();
                                infoCol.Item().Text($"Invoice #: {billData.BillNumber}");
                            });
                            row.RelativeItem().AlignRight().Column(dateCol =>
                            {
                                dateCol.Item().Text($"Date: {billData.BillDate:dd-MMM-yyyy}");
                                dateCol.Item().Text($"Time: {billData.BillDate:hh:mm tt}");
                            });
                        });

                        column.Item().PaddingBottom(15).LineHorizontal(0.5f);

                        // Table Header
                        column.Item().PaddingBottom(5).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);    // Product
                                columns.ConstantColumn(60);   // Size
                                columns.ConstantColumn(70);   // Unit
                                columns.ConstantColumn(60);   // Qty
                                columns.ConstantColumn(70);   // Price
                                columns.ConstantColumn(60);   // Discount
                                columns.ConstantColumn(80);   // Total
                            });

                            table.Header(header =>
                            {
                                header.Cell().Padding(5).Background("#f0f0f0").Text("Product").Bold();
                                header.Cell().Padding(5).Background("#f0f0f0").AlignRight().Text("Size").Bold();
                                header.Cell().Padding(5).Background("#f0f0f0").AlignRight().Text("Unit").Bold();
                                header.Cell().Padding(5).Background("#f0f0f0").AlignRight().Text("Qty").Bold();
                                header.Cell().Padding(5).Background("#f0f0f0").AlignRight().Text("Price").Bold();
                                header.Cell().Padding(5).Background("#f0f0f0").AlignRight().Text("Discount").Bold();
                                header.Cell().Padding(5).Background("#f0f0f0").AlignRight().Text("Total").Bold();
                            });
                        });

                        // Items
                        int itemCount = 0;
                        foreach (var item in billData.Items)
                        {
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3);
                                    columns.ConstantColumn(60);
                                    columns.ConstantColumn(70);
                                    columns.ConstantColumn(60);
                                    columns.ConstantColumn(70);
                                    columns.ConstantColumn(60);
                                    columns.ConstantColumn(80);
                                });

                                table.Cell().Padding(5).Text(item.ProductName);
                                table.Cell().Padding(5).AlignRight().Text(item.Size ?? "-");
                                table.Cell().Padding(5).AlignRight().Text(item.UnitOfMeasure);
                                table.Cell().Padding(5).AlignRight().Text(item.Quantity.ToString("N2"));
                                table.Cell().Padding(5).AlignRight().Text($"Rs. {item.UnitPrice:N2}");
                                table.Cell().Padding(5).AlignRight().Text($"Rs. {(item.UnitPrice - item.LineTotal / item.Quantity):N2}");
                                table.Cell().Padding(5).AlignRight().Text($"Rs. {item.LineTotal:N2}").Bold();
                            });

                            if (itemCount < billData.Items.Count - 1)
                            {
                                column.Item().PaddingHorizontal(10).LineHorizontal(0.2f);
                            }
                            itemCount++;
                        }

                        // Summary
                        column.Item().PaddingTop(20).Table(summaryTable =>
                        {
                            summaryTable.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.ConstantColumn(150);
                            });

                            summaryTable.Cell().Padding(3).AlignRight().Text("Subtotal:");
                            summaryTable.Cell().Padding(3).AlignRight().Text($"Rs. {billData.Subtotal:N2}");

                            if (billData.DiscountAmount > 0)
                            {
                                summaryTable.Cell().Padding(3).AlignRight().Text("Total Discount:");
                                summaryTable.Cell().Padding(3).AlignRight().Text($"Rs. {billData.DiscountAmount:N2}");
                            }

                            summaryTable.Cell().Padding(5).Background("#e8f4fd").AlignRight().Text("TOTAL:").Bold();
                            summaryTable.Cell().Padding(5).Background("#e8f4fd").AlignRight().Text($"Rs. {billData.TotalAmount:N2}").Bold().FontSize(12);

                            summaryTable.Cell().Padding(3).AlignRight().Text("Amount Paid:");
                            summaryTable.Cell().Padding(3).AlignRight().Text($"Rs. {billData.AmountPaid:N2}");

                            summaryTable.Cell().Padding(5).Background("#fff8dc").AlignRight().Text("BALANCE:").Bold();
                            summaryTable.Cell().Padding(5).Background("#fff8dc").AlignRight().Text($"Rs. {billData.AmountDue:N2}").Bold();
                        });

                        column.Item().PaddingVertical(15).LineHorizontal(1);

                        // Footer
                        column.Item().AlignCenter().Text("Thank you for your shopping here!").Bold().FontSize(14);
                        column.Item().PaddingVertical(5).AlignCenter().Text("بل کے بغیر واپسی نہیں ہوگی");
                        column.Item().AlignCenter().Text("آپ کے اعتماد کا شکریہ");

                        column.Item().PaddingVertical(15).AlignCenter().Text("Terms & Conditions:").SemiBold();
                        column.Item().AlignCenter().Text("• Goods once sold cannot be returned or exchanged");
                        column.Item().AlignCenter().Text("• Please check items at the time of purchase");

                        column.Item().PaddingVertical(20).LineHorizontal(0.5f);

                        column.Item().AlignCenter().Text("Developed By: devinfantary.com | 03477048001").FontSize(9);
                        column.Item().AlignCenter().Text($"Printed on: {DateTime.Now:dd-MMM-yyyy hh:mm tt}").FontSize(9);
                    });
                });
            });

            return document.GeneratePdf();
        }
    }



    // PDF Data Model
    public class BillPdfData
    {
        public string BillNumber { get; set; } = string.Empty;
        public DateTime BillDate { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public List<BillItemPdfData> Items { get; set; } = new();
        public decimal Subtotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal AmountDue { get; set; }
    }

    public class BillItemPdfData
    {
        public string ProductName { get; set; } = string.Empty;
        public string? Size { get; set; }
        public decimal Quantity { get; set; }
        public string UnitOfMeasure { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }

    // Quotation PDF Data Model
    public class QuotationPdfData
    {
        public string QuotationNumber { get; set; } = string.Empty;
        public DateTime QuotationDate { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public List<QuotationItemPdfData> Items { get; set; } = new();
        public decimal Subtotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime? ValidUntil { get; set; }
    }

    public class QuotationItemPdfData
    {
        public string ProductName { get; set; } = string.Empty;
        public string? Size { get; set; }
        public decimal Quantity { get; set; }
        public string UnitOfMeasure { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }
}