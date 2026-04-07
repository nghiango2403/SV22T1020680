using Microsoft.AspNetCore.Mvc;
using SV22T1020680.BusinessLayers;
using SV22T1020680.DataLayers.SQLServer;
using SV22T1020680.Models.Common;

namespace SV22T1020680.Admin.Controllers
{
    public class test : Controller
    {
        public async Task<IActionResult> Index(int page = 1, int pagesize = 10, string searchValues = "")
        {
            var input = new PaginationSearchInput
            {
                Page = page,
                PageSize = pagesize,
                SearchValue = searchValues
            };
            string connectionString = "Server=.;Database=LiteCommerceDB;Trusted_Connection=True;TrustServerCertificate=True;";
            var repository = new EmployeeRepository(connectionString);
            var data = await HRDataService.ListEmployeesAsync(input);
            //var data = await repository.ListAsync(input);

            return Json(data);
        }
    }
}
