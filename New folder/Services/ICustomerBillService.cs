using HardwareStoreAPI.Models;
using HardwareStoreAPI.Models.DTOs;

namespace HardwareStoreAPI.Services
{
    public interface ICustomerBillService
    {
        Task<List<CustomerBillSummary>> GetAllCustomerBillSummariesAsync(string? search = null);
        Task<CustomerBillSummary?> GetCustomerBillSummaryAsync(int customerId);
        Task<List<CustomerBillDetail>> GetCustomerBillsAsync(int customerId);
        Task<CustomerBillDetail?> GetBillDetailAsync(int billId);
        Task<List<CustomerPaymentRecord>> GetCustomerPaymentRecordsAsync(int customerId);
        Task<PaymentDistributionResult> AddCustomerPaymentAsync(AddCustomerPaymentDto paymentDto);
    }
}