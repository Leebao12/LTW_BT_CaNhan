
using Microsoft.Data.SqlClient;

namespace Falcon.Web.Services;

public class DashboardData
{
    public int HetHang {get;set;}
    public int NoiBat {get;set;}
    public int TaiKhoan {get;set;}
    public int NhaCungCapHoatDong {get;set;}
    public int TongSoMauXe { get; set; }
    public int TongSoLuongXe { get; set; }
    public int SoXeConTrongKho { get; set; }
    public int SoXeDaBan { get; set; }

    public List<MonthlyRevenue> DoanhThuTheoThang
    { get; set; } = new();
}

public class MonthlyRevenue
{
    public int Nam { get; set; }
    public int Thang { get; set; }
    public decimal DoanhThu { get; set; }
}

public class DashboardService
{
    private readonly string _connectionString;

    public DashboardService(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Chưa cấu hình DefaultConnection.");
    }

    public async Task<DashboardData> GetDashboardDataAsync()
    {
        var result = new DashboardData();
        int currentYear = DateTime.Today.Year;


        const string sql = @"
    SELECT
        COUNT(*) AS TongSoMauXe,
        COALESCE(SUM(CASE WHEN stock_quantity=0 THEN 1 ELSE 0 END),0) AS HetHang,
        COALESCE(SUM(CASE WHEN is_featured=1 THEN 1 ELSE 0 END),0) AS NoiBat,
        (SELECT COUNT(*) FROM dbo.Users) AS TaiKhoan,
        (SELECT COUNT(*) FROM dbo.NhaCungCap WHERE DangHoatDong=1) AS NhaCungCapHoatDong,
        COALESCE(SUM(stock_quantity), 0) AS TongSoLuongXe,
        COALESCE(SUM(
            CASE
                WHEN stock_quantity > 0
                THEN stock_quantity
                ELSE 0
            END
        ), 0) AS SoXeConTrongKho
    FROM dbo.Products;

    SELECT
        COALESCE(SUM(d.Quantity), 0) AS SoXeDaBan
    FROM dbo.OrderDetails d
    INNER JOIN dbo.Orders o
        ON d.OrderId = o.OrderId
    WHERE o.OrderStatus = 'Completed';

    SELECT
        MONTH(UpdatedAt) AS Thang,
        COALESCE(SUM(TotalAmount), 0) AS DoanhThu
    FROM dbo.Orders
    WHERE OrderStatus = 'Completed'
        AND UpdatedAt IS NOT NULL
        AND YEAR(UpdatedAt) = @Nam
    GROUP BY MONTH(UpdatedAt)
    ORDER BY Thang;
";


        await using var connection =
            new SqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command =
            new SqlCommand(sql, connection);

        command.Parameters.AddWithValue("@Nam", currentYear);

        await using var reader =
            await command.ExecuteReaderAsync();

        // Thống kê sản phẩm
        if (await reader.ReadAsync())
        {
            result.HetHang=Convert.ToInt32(reader["HetHang"]);
            result.NoiBat=Convert.ToInt32(reader["NoiBat"]);
            result.TaiKhoan=Convert.ToInt32(reader["TaiKhoan"]);
            result.NhaCungCapHoatDong=Convert.ToInt32(reader["NhaCungCapHoatDong"]);
            result.TongSoMauXe =
                Convert.ToInt32(reader["TongSoMauXe"]);

            result.TongSoLuongXe =
                Convert.ToInt32(reader["TongSoLuongXe"]);

            result.SoXeConTrongKho =
                Convert.ToInt32(reader["SoXeConTrongKho"]);
        }

        // Số xe trong các đơn đã hoàn tất
        if (await reader.NextResultAsync() &&
            await reader.ReadAsync())
        {
            result.SoXeDaBan =
                Convert.ToInt32(reader["SoXeDaBan"]);
        }

        var revenue = new decimal[12];

        // Doanh thu theo tháng
        if (await reader.NextResultAsync())
        {
            while (await reader.ReadAsync())
            {
                int month =
                    Convert.ToInt32(reader["Thang"]);

                if (month >= 1 && month <= 12)
                {
                    revenue[month - 1] =
                        Convert.ToDecimal(reader["DoanhThu"]);
                }
            }
        }

        for (int month = 1; month <= 12; month++)
        {
            result.DoanhThuTheoThang.Add(new MonthlyRevenue
            {
                Nam = currentYear,
                Thang = month,
                DoanhThu = revenue[month - 1]
            });
        }

        return result;
    }
}
