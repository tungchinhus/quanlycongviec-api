namespace quanlyfilesBE.Helpers;

/// <summary>
/// Trả về giờ Việt Nam (UTC+7) để lưu vào DB cho đúng với "ngày giờ hệ thống" mà user thấy.
/// Backend trước đây dùng DateTime.UtcNow nên trong DB hiển thị giờ UTC (ví dụ 02:21) trong khi máy user là 09:21.
/// </summary>
public static class DateTimeHelper
{
    private static readonly TimeZoneInfo? VietnamTz;

    static DateTimeHelper()
    {
        try
        {
            // Windows: "SE Asia Standard Time" (UTC+7)
            VietnamTz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
        catch
        {
            try
            {
                // Linux/macOS
                VietnamTz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
            }
            catch
            {
                VietnamTz = null;
            }
        }
    }

    /// <summary>
    /// Thời điểm hiện tại theo giờ Việt Nam (UTC+7). Dùng khi lưu ApprovalDate, CreatedAt, v.v. để DB và UI khớp với giờ user.
    /// Nếu không lấy được timezone Vietnam thì fallback DateTime.Now (giờ máy chủ).
    /// </summary>
    public static DateTime NowVietnam()
    {
        if (VietnamTz != null)
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTz);
        return DateTime.Now;
    }
}
