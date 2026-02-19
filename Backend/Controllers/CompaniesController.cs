using HardwareStoreAPI.Models;
using HardwareStoreAPI.Models.DTOs;
using HardwareStoreAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace HardwareStoreAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CompaniesController : ControllerBase
    {
        private readonly ICompanyService _service;

        public CompaniesController(ICompanyService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<Company>>>> GetAll()
        {
            var companies = await _service.GetAllAsync();
            return Ok(ApiResponse<List<Company>>.SuccessResponse(companies));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<Company>>> GetById(int id)
        {
            var company = await _service.GetByIdAsync(id);
            if (company == null)
                return NotFound(ApiResponse<Company>.ErrorResponse("Company not found"));

            return Ok(ApiResponse<Company>.SuccessResponse(company));
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<Company>>> Create(CompanyDto dto)
        {
            var company = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById),
                new { id = company.CompanyId },
                ApiResponse<Company>.SuccessResponse(company, "Company created"));
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> Update(int id, CompanyUpdateDto dto)
        {
            var success = await _service.UpdateAsync(id, dto);
            if (!success)
                return NotFound(ApiResponse<bool>.ErrorResponse("Company not found"));

            return Ok(ApiResponse<bool>.SuccessResponse(true, "Updated successfully"));
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success)
                return NotFound(ApiResponse<bool>.ErrorResponse("Company not found"));

            return Ok(ApiResponse<bool>.SuccessResponse(true, "Deleted successfully"));
        }

        [HttpPost("{id}/restore")]
        public async Task<ActionResult<ApiResponse<bool>>> Restore(int id)
        {
            var success = await _service.RestoreAsync(id);
            if (!success)
                return NotFound(ApiResponse<bool>.ErrorResponse("Company not found"));

            return Ok(ApiResponse<bool>.SuccessResponse(true, "Restored successfully"));
        }
    }
}
