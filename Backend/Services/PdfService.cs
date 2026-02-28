//using System.ComponentModel;
//using System.Reflection.Metadata;
//using QuestPDF.Fluent;
//using QuestPDF.Helpers;
//using QuestPDF.Infrastructure;

//namespace HardwareStoreAPI.Services
//{
//    public interface IPdfService
//    {
//        byte[] GenerateCustomerBill(CustomerBillPdfData billData);
//    }

//    public class PdfService : IPdfService
//    {
//        public byte[] GenerateCustomerBill(CustomerBillPdfData billData)
//        {
//            QuestPDF.Settings.License = LicenseType.Community;

//            var document = Document.Create(container =>
//            {
//                container.Page(page =>
//                {
//                    page.Size(PageSizes.A4);
//                    page.Margin(2, Unit.Centimetre);
//                    page.PageColor(Colors.White);
//                    page.DefaultTextStyle(x => x.FontSize(10));

//                    page.Header()
//                        .Text("HARDWARE STORE - SALES INVOICE")
//                        .FontSize(20)
//                        .Bold()
//                        .FontColor(Colors.Blue.Medium);

//                    page.Content()
//                        .PaddingVertical(1, Unit.Centimetre)
//                        .Column(column =>
//                        {
//                            column.Spacing(5);

//                            // Bill Header Info
//                            column.Item().Row(row =>
//                            {
//                                row.RelativeItem().Column(col =>
//                                {
//                                    col.Item().Text($"Bill No: {billData.BillNumber}").Bold();
//                                    col.Item().Text($"Date: {billData.BillDate:dd/MM/yyyy}");
//                                    col.Item().Text($"Time: {billData.BillDate:hh:mm tt}");
//                                });

//                                row.RelativeItem().Column(col =>
//                                {
//                                    col.Item().AlignRight().Text($"Customer: {billData.CustomerName}").Bold();
//                                    col.Item().AlignRight().Text($"Contact: {billData.CustomerContact}");
//                                    col.Item().AlignRight().Text($"Served By: {billData.StaffName}");
//                                });
//                            });

//                            column.Item().LineHorizontal(1);

//                            // Items Table
//                            column.Item().Table(table =>
//                            {
//                                table.ColumnsDefinition(columns =>
//                                {
//                                    columns.ConstantColumn(30);   // Sr#
//                                    columns.RelativeColumn(3);    // Product Name
//                                    columns.RelativeColumn(2);    // Size/Type
//                                    columns.RelativeColumn(1);    // Qty
//                                    columns.RelativeColumn(1);    // Unit
//                                    columns.RelativeColumn(1.5f); // Price
//                                    columns.RelativeColumn(1);    // Disc%
//                                    columns.RelativeColumn(1.5f); // Total
//                                });

//                                // Header
//                                table.Header(header =>
//                                {
//                                    header.Cell().Element(CellStyle).Text("Sr#").Bold();
//                                    header.Cell().Element(CellStyle).Text("Product Name").Bold();
//                                    header.Cell().Element(CellStyle).Text("Size/Type").Bold();
//                                    header.Cell().Element(CellStyle).Text("Qty").Bold();
//                                    header.Cell().Element(CellStyle).Text("Unit").Bold();
//                                    header.Cell().Element(CellStyle).Text("Price").Bold();
//                                    header.Cell().Element(CellStyle).Text("Disc%").Bold();
//                                    header.Cell().Element(CellStyle).Text("Total").Bold();
//                                });

//                                // Items
//                                int srNo = 1;
//                                foreach (var item in billData.Items)
//                                {
//                                    table.Cell().Element(CellStyle).Text(srNo++.ToString());
//                                    table.Cell().Element(CellStyle).Text(item.ProductName);
//                                    table.Cell().Element(CellStyle).Text(item.SizeType ?? "-");
//                                    table.Cell().Element(CellStyle).AlignRight().Text(item.Quantity.ToString("N2"));
//                                    table.Cell().Element(CellStyle).Text(item.UnitOfMeasure);
//                                    table.Cell().Element(CellStyle).AlignRight().Text(item.UnitPrice.ToString("N2"));
//                                    table.Cell().Element(CellStyle).AlignRight().Text(item.Discount.ToString("N0") + "%");
//                                    table.Cell().Element(CellStyle).AlignRight().Text(item.TotalPrice.ToString("N2"));
//                                }
//                            });

//                            column.Item().LineHorizontal(1);

//                            // Totals
//                            column.Item().AlignRight().Row(row =>
//                            {
//                                row.ConstantItem(150).Text("Subtotal:").Bold();
//                                row.ConstantItem(100).AlignRight().Text($"Rs. {billData.SubTotal:N2}");
//                            });

//                            if (billData.DiscountAmount > 0)
//                            {
//                                column.Item().AlignRight().Row(row =>
//                                {
//                                    row.ConstantItem(150).Text("Discount:").Bold();
//                                    row.ConstantItem(100).AlignRight().Text($"Rs. {billData.DiscountAmount:N2}");
//                                });
//                            }

//                            column.Item().AlignRight().Row(row =>
//                            {
//                                row.ConstantItem(150).Text("Grand Total:").FontSize(14).Bold();
//                                row.ConstantItem(100).AlignRight().Text($"Rs. {billData.NetAmount:N2}").FontSize(14).Bold();
//                            });

//                            column.Item().PaddingTop(10).LineHorizontal(1);

//                            // Footer
//                            if (!string.IsNullOrEmpty(billData.Notes))
//                            {
//                                column.Item().Text($"Notes: {billData.Notes}").FontSize(9);
//                            }

//                            column.Item().PaddingTop(20).Text("Thank you for your business!")
//                                .FontSize(12).Bold().FontColor(Colors.Blue.Medium);
//                        });

//                    page.Footer()
//                        .AlignCenter()
//                        .Text(x =>
//                        {
//                            x.Span("Generated on: ");
//                            x.Span(DateTime.Now.ToString("dd/MM/yyyy hh:mm tt"));
//                        });
//                });
//            });

//            return document.GeneratePdf();
//        }

//        private static IContainer CellStyle(IContainer container)
//        {
//            return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
//        }
//    }

//    // PDF Data Model
//    public class CustomerBillPdfData
//    {
//        public string BillNumber { get; set; } = string.Empty;
//        public DateTime BillDate { get; set; }
//        public string CustomerName { get; set; } = string.Empty;
//        public string CustomerContact { get; set; } = string.Empty;
//        public string StaffName { get; set; } = string.Empty;
//        public List<BillItemPdfData> Items { get; set; } = new();
//        public decimal SubTotal { get; set; }
//        public decimal DiscountAmount { get; set; }
//        public decimal NetAmount { get; set; }
//        public string? Notes { get; set; }
//    }

//    public class BillItemPdfData
//    {
//        public string ProductName { get; set; } = string.Empty;
//        public string? SizeType { get; set; }
//        public decimal Quantity { get; set; }
//        public string UnitOfMeasure { get; set; } = string.Empty;
//        public decimal UnitPrice { get; set; }
//        public decimal Discount { get; set; }
//        public decimal TotalPrice { get; set; }
//    }
//}