using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020680.DataLayers.Interfaces;
using SV22T1020680.Models.DataDictionary;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace SV22T1020680.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho tỉnh/thành phố (Provinces)
    /// </summary>
    public class ProvinceRepository : IDataDictionaryRepository<Province>
    {
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo Repository với chuỗi kết nối
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối CSDL</param>
        public ProvinceRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Lấy danh sách tất cả các tỉnh thành
        /// </summary>
        /// <returns>Danh sách đối tượng Province</returns>
        public async Task<List<Province>> ListAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = "SELECT ProvinceName FROM Provinces ORDER BY ProvinceName";

                // Sử dụng Dapper để map kết quả vào List<Province>
                var result = await connection.QueryAsync<Province>(sql);
                return result.ToList();
            }
        }
    }
}