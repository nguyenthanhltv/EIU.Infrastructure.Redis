namespace EIU.Infrastructure.Redis.Core
{
    /// <summary>
    /// Cấu hình cho Redis Cache (thiết lập thông tin chung cho toàn hệ thống)
    /// </summary>
    public class RedisCacheOptions
    {
        /// <summary>
        /// Chuỗi kết nối đến Redis Server (ví dụ: "localhost:6379" hoặc "10.0.0.5:6379,password=xxx")
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Alias (bí danh) cho dự án – dùng để phân biệt cache giữa các service khác nhau
        /// </summary>
        public string? ProjectAlias { get; set; }

        /// <summary>
        /// Tự động sinh khóa (key) dựa trên tham số đầu vào của Action hay Service
        /// </summary>
        public bool AutoKeyByParameters { get; set; } = true;

        /// <summary>
        /// Thời gian cache mặc định (giây)
        /// </summary>
        public int DefaultDurationSeconds { get; set; } = 60;

        /// <summary>
        /// Bật/tắt Redis cache toàn hệ thống.
        /// Nếu false thì hệ thống bỏ qua toàn bộ cache.
        /// </summary>
        public bool Enabled { get; set; } = true;
    }
}
