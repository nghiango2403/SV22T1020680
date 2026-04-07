using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SV22T1020680.BusinessLayers
{
    /// <summary>
    /// Lớp lưu giữ các thông tin cấu hình chung cho Business Layer
    /// </summary>
    public static class Configuration
    {
        private static string _connectionString = "";

        /// <summary>
        /// Khởi tạo cấu hình cho Business Layer
        /// Hàm này phải gọi trước khi chạy ứng dụng
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối CSDL</param>
        public static void Initialize(string connectionString)
        {
            _connectionString = connectionString;
        }
        /// <summary>
        /// Lấy chuỗi tham số kết nối CSDL sử dụng trong hệ thống
        /// </summary>
        public static string ConnectionString => _connectionString;
    }
}
