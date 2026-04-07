using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace SV22T1020680.Shop
{
    /// <summary>
    /// Thông tin tài khoản khách hàng được lưu trong phiên đăng nhập (cookie)
    /// </summary>
    public class WebUserData
    {
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? DisplayName { get; set; }
        public string? ContactName { get; set; }
        public string? Email { get; set; }
        public string? Province { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }

        /// <summary>
        /// Ánh xạ thông tin vào danh sách Claim
        /// </summary>
        private List<Claim> Claims
        {
            get
            {
                List<Claim> claims = new List<Claim>()
                {
                    new Claim(nameof(UserId), UserId ?? ""),
                    new Claim(nameof(UserName), UserName ?? ""),
                    new Claim(nameof(DisplayName), DisplayName ?? ""),
                    new Claim(nameof(ContactName), ContactName ?? ""),
                    new Claim(nameof(Email), Email ?? ""),
                    new Claim(nameof(Province), Province ?? ""),
                    new Claim(nameof(Address), Address ?? ""),
                    new Claim(nameof(Phone), Phone ?? "")
                };
                return claims;
            }
        }

        public ClaimsPrincipal CreatePrincipal()
        {
            var claimIdentity = new ClaimsIdentity(Claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var claimPrincipal = new ClaimsPrincipal(claimIdentity);
            return claimPrincipal;
        }
    }

    public static class WebUserExtensions
    {
        /// <summary>
        /// Đọc thông tin khách hàng từ Principal (Cookie)
        /// </summary>
        public static WebUserData? GetUserData(this ClaimsPrincipal principal)
        {
            try
            {
                if (principal == null || principal.Identity == null || !principal.Identity.IsAuthenticated)
                    return null;

                var userData = new WebUserData();

                // Dùng FindFirstValue để lấy giá trị từ Claim dựa trên tên thuộc tính
                userData.UserId = principal.FindFirstValue(nameof(userData.UserId));
                userData.UserName = principal.FindFirstValue(nameof(userData.UserName));
                userData.DisplayName = principal.FindFirstValue(nameof(userData.DisplayName));
                userData.ContactName = principal.FindFirstValue(nameof(userData.ContactName));
                userData.Email = principal.FindFirstValue(nameof(userData.Email));
                userData.Province = principal.FindFirstValue(nameof(userData.Province));
                userData.Address = principal.FindFirstValue(nameof(userData.Address));
                userData.Phone = principal.FindFirstValue(nameof(userData.Phone));

                return userData;
            }
            catch
            {
                return null;
            }
        }
    }
}