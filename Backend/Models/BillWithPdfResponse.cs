using HardwareStoreAPI.Models;

public class BillWithPdfResponse
{
    public Bill Bill { get; set; } = null!;
    public string PdfFileName { get; set; } = string.Empty;
    public string PdfUrl { get; set; } = string.Empty;
    public byte[] PdfBytes { get; set; } = Array.Empty<byte>();
}