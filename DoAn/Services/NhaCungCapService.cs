using Falcon.Web.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Falcon.Web.Services
{
    public class NhaCungCapService
    {
        private readonly string _connectionString;

        public NhaCungCapService(IConfiguration configuration)
        {
            _connectionString =
                configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Không tìm thấy chuỗi kết nối DefaultConnection.");
        }

        private SqlConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }

        // 1. Lấy danh sách và tìm kiếm theo tên
        public async Task<List<NhaCungCap>> GetAllAsync(string? keyword = null)
        {
            var danhSach = new List<NhaCungCap>();

            const string sql = @"
                SELECT MaNhaCungCap, TenNhaCungCap, SoDienThoai,
                       Email, DiaChi, LoaiNhaCungCap, DangHoatDong
                FROM NhaCungCap
                WHERE (@Keyword IS NULL OR TenNhaCungCap LIKE @Search)
                ORDER BY MaNhaCungCap DESC;";

            using var connection = CreateConnection();
            using var command = new SqlCommand(sql, connection);

            string? tuKhoa = string.IsNullOrWhiteSpace(keyword)
                ? null
                : keyword.Trim();

            command.Parameters.Add("@Keyword", SqlDbType.NVarChar, 150)
                .Value = (object?)tuKhoa ?? DBNull.Value;

            command.Parameters.Add("@Search", SqlDbType.NVarChar, 152)
                .Value = tuKhoa == null
                    ? DBNull.Value
                    : "%" + tuKhoa + "%";

            await connection.OpenAsync();

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                danhSach.Add(MapNhaCungCap(reader));
            }

            return danhSach;
        }

        // 2. Lấy thông tin một nhà cung cấp
        public async Task<NhaCungCap?> GetByIdAsync(int id)
        {
            const string sql = @"
                SELECT MaNhaCungCap, TenNhaCungCap, SoDienThoai,
                       Email, DiaChi, LoaiNhaCungCap, DangHoatDong
                FROM NhaCungCap
                WHERE MaNhaCungCap = @Id;";

            using var connection = CreateConnection();
            using var command = new SqlCommand(sql, connection);

            command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

            await connection.OpenAsync();

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return MapNhaCungCap(reader);
            }

            return null;
        }

        // 3. Thêm nhà cung cấp
        public async Task CreateAsync(NhaCungCap model)
        {
            const string sql = @"
                INSERT INTO NhaCungCap
                    (TenNhaCungCap, SoDienThoai, Email, DiaChi,
                     LoaiNhaCungCap, DangHoatDong)
                VALUES
                    (@Ten, @SDT, @Email, @DiaChi, @Loai, @TrangThai);";

            using var connection = CreateConnection();
            using var command = new SqlCommand(sql, connection);

            AddParameters(command, model);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        // 4. Cập nhật nhà cung cấp
        public async Task<bool> UpdateAsync(NhaCungCap model)
        {
            const string sql = @"
                UPDATE NhaCungCap
                SET TenNhaCungCap = @Ten,
                    SoDienThoai = @SDT,
                    Email = @Email,
                    DiaChi = @DiaChi,
                    LoaiNhaCungCap = @Loai,
                    DangHoatDong = @TrangThai
                WHERE MaNhaCungCap = @Id;";

            using var connection = CreateConnection();
            using var command = new SqlCommand(sql, connection);

            AddParameters(command, model);

            command.Parameters.Add("@Id", SqlDbType.Int)
                .Value = model.MaNhaCungCap;

            await connection.OpenAsync();

            return await command.ExecuteNonQueryAsync() > 0;
        }

        // 5. Xóa hoặc chuyển sang ngừng hoạt động
        public async Task<string> DeleteAsync(int id)
        {
            using var connection = CreateConnection();
            await connection.OpenAsync();

            using var transaction =
                (SqlTransaction)await connection.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                const string checkSql = @"
                    SELECT COUNT(*)
                    FROM DonNhapKho WITH (UPDLOCK, HOLDLOCK)
                    WHERE MaNhaCungCap = @Id;";

                using var checkCommand =
                    new SqlCommand(checkSql, connection, transaction);

                checkCommand.Parameters.Add("@Id", SqlDbType.Int).Value = id;

                int soDonNhap = Convert.ToInt32(
                    await checkCommand.ExecuteScalarAsync());

                string sql;
                string ketQua;

                if (soDonNhap > 0)
                {
                    sql = @"
                        UPDATE NhaCungCap
                        SET DangHoatDong = 0
                        WHERE MaNhaCungCap = @Id;";

                    ketQua = "Nhà cung cấp đã có đơn nhập kho nên được chuyển sang ngừng hoạt động.";
                }
                else
                {
                    sql = @"
                        DELETE FROM NhaCungCap
                        WHERE MaNhaCungCap = @Id;";

                    ketQua = "Đã xóa nhà cung cấp thành công.";
                }

                using var command =
                    new SqlCommand(sql, connection, transaction);

                command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

                int rows = await command.ExecuteNonQueryAsync();

                if (rows == 0)
                {
                    await transaction.RollbackAsync();
                    return "Không tìm thấy nhà cung cấp cần xử lý.";
                }

                await transaction.CommitAsync();
                return ketQua;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // Đọc dữ liệu từ SQL Server
        private static NhaCungCap MapNhaCungCap(SqlDataReader reader)
        {
            return new NhaCungCap
            {
                MaNhaCungCap = reader.GetInt32(
                    reader.GetOrdinal("MaNhaCungCap")),

                TenNhaCungCap = reader.GetString(
                    reader.GetOrdinal("TenNhaCungCap")),

                SoDienThoai = reader.IsDBNull(reader.GetOrdinal("SoDienThoai")) ? null : reader.GetString(
                    reader.GetOrdinal("SoDienThoai")),

                Email = reader.IsDBNull(reader.GetOrdinal("Email"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("Email")),

                DiaChi = reader.IsDBNull(reader.GetOrdinal("DiaChi"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("DiaChi")),

                LoaiNhaCungCap = reader.GetString(
                    reader.GetOrdinal("LoaiNhaCungCap")),

                DangHoatDong = reader.GetBoolean(
                    reader.GetOrdinal("DangHoatDong"))
            };
        }

        // Tham số dùng chung cho thêm và sửa
        private static void AddParameters(
            SqlCommand command, NhaCungCap model)
        {
            command.Parameters.Add("@Ten", SqlDbType.NVarChar, 100)
                .Value = model.TenNhaCungCap.Trim();

            command.Parameters.Add("@SDT", SqlDbType.VarChar, 15)
                .Value = (object?)model.SoDienThoai?.Trim() ?? DBNull.Value;

            command.Parameters.Add("@Email", SqlDbType.VarChar, 50)
                .Value = (object?)model.Email?.Trim() ?? DBNull.Value;

            command.Parameters.Add("@DiaChi", SqlDbType.NVarChar, 255)
                .Value = (object?)model.DiaChi?.Trim() ?? DBNull.Value;

            command.Parameters.Add("@Loai", SqlDbType.NVarChar, 30)
                .Value = model.LoaiNhaCungCap.Trim();

            command.Parameters.Add("@TrangThai", SqlDbType.Bit)
                .Value = model.DangHoatDong;
        }
    }
}