using AuthSystemApi.Data;
using AuthSystemApi.DTOs;
using AuthSystemApi.Services.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AuthSystemApi.Services
{
    public class CompanyService : ICompanyService
    {
        private readonly DbHelper _db;

        public CompanyService(DbHelper db)
        {
            _db = db;
        }

        // PROFILE
        public CompanyProfileDto GetProfile(int userId)
        {
            using var con = _db.GetConnection();
            using var cmd = new SqlCommand("sp_GetCompanyProfile", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@UserId", userId);

            con.Open();
            using var reader = cmd.ExecuteReader();

            if (!reader.Read())
            {
                reader.Close();

                using var createCmd = new SqlCommand("sp_CreateCompanyProfile", con);
                createCmd.CommandType = CommandType.StoredProcedure;
                createCmd.Parameters.AddWithValue("@UserId", userId);
                createCmd.Parameters.AddWithValue("@CompanyName", "New Company");
                createCmd.ExecuteNonQuery();

                using var fetchCmd = new SqlCommand("sp_GetCompanyProfile", con);
                fetchCmd.CommandType = CommandType.StoredProcedure;
                fetchCmd.Parameters.AddWithValue("@UserId", userId);

                using var newReader = fetchCmd.ExecuteReader();
                newReader.Read();
                return MapCompany(newReader);
            }

            return MapCompany(reader);
        }

        private CompanyProfileDto MapCompany(SqlDataReader reader)
        {
            return new CompanyProfileDto
            {
                EmployerName = reader["FirstName"] + " " + reader["LastName"],
                CompanyId = (int)reader["CompanyId"],
                CompanyName = reader["CompanyName"].ToString(),
                Industry = reader["Industry"]?.ToString(),
                Description = reader["Description"]?.ToString(),
                Address = reader["Address"]?.ToString(),
                Locations = reader["Locations"]?.ToString(),
                CompanyType = reader["CompanyType"]?.ToString()
            };
        }

        // EMPLOYER → REQUEST CHANGE
        public async Task RequestProfileChange(int companyId, string fieldName, string newValue, int userId)
        {
            using var con = _db.GetConnection();
            using var cmd = new SqlCommand("sp_CreateCompanyChangeRequest", con);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@CompanyId", companyId);
            cmd.Parameters.AddWithValue("@FieldName", fieldName);
            cmd.Parameters.AddWithValue("@NewValue", newValue);
            cmd.Parameters.AddWithValue("@UserId", userId);

            await con.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        // ADMIN → PENDING REQUESTS

        public async Task<List<PendingChangeRequestDto>> GetPendingRequests()
        {
            var list = new List<PendingChangeRequestDto>();

            using var con = _db.GetConnection();
            using var cmd = new SqlCommand(@"
                SELECT 
                    r.RequestId,
                    r.CompanyId,
                    c.CompanyName,
                    r.FieldName,
                    r.OldValue,
                    r.NewValue,
                    u.FirstName + ' ' + u.LastName AS RequestedBy,
                    r.RequestedAt
                FROM CompanyProfileChangeRequests r
                JOIN CompanyProfiles c ON r.CompanyId = c.CompanyId
                JOIN Users u ON r.RequestedBy = u.UserId
                WHERE r.Status='Pending'
                ORDER BY r.RequestedAt DESC", con);

            await con.OpenAsync();
            using var rd = await cmd.ExecuteReaderAsync();

            while (await rd.ReadAsync())
            {
                list.Add(new PendingChangeRequestDto
                {
                    RequestId = (int)rd["RequestId"],
                    CompanyId = (int)rd["CompanyId"],
                    CompanyName = rd["CompanyName"].ToString(),
                    FieldName = rd["FieldName"].ToString(),
                    OldValue = rd["OldValue"]?.ToString(),
                    NewValue = rd["NewValue"]?.ToString(),
                    RequestedBy = rd["RequestedBy"].ToString(),
                    RequestedAt = (DateTime)rd["RequestedAt"]
                });
            }

            return list;
        }

        // ADMIN → APPROVE

        public async Task ApproveChange(int requestId, int adminId)
        {
            using var con = _db.GetConnection();
            using var cmd = new SqlCommand("sp_ApproveCompanyChange", con);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@RequestId", requestId);
            cmd.Parameters.AddWithValue("@AdminId", adminId);

            await con.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        // ADMIN → REJECT
        public async Task RejectChange(int requestId, int adminId)
        {
            using var con = _db.GetConnection();
            using var cmd = new SqlCommand("sp_RejectCompanyChange", con);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@RequestId", requestId);
            cmd.Parameters.AddWithValue("@AdminId", adminId);

            await con.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        // HISTORY (PER COMPANY)


        public async Task<List<CompanyChangeHistoryDto>> GetCompanyHistory(int companyId)
        {
            var list = new List<CompanyChangeHistoryDto>();

            using var con = _db.GetConnection();
            using var cmd = new SqlCommand(@"
                SELECT 
                    c.CompanyName,
                    h.FieldName,
                    h.OldValue,
                    h.NewValue,
                    admin.FirstName + ' ' + admin.LastName AS ApprovedBy,
                    emp.FirstName + ' ' + emp.LastName AS RequestedBy,
                    h.ChangedAt
                FROM CompanyProfileChangeHistory h
                JOIN CompanyProfiles c ON h.CompanyId = c.CompanyId
                JOIN Users admin ON h.ChangedBy = admin.UserId
                JOIN Users emp ON h.RequestedBy = emp.UserId
                WHERE h.CompanyId = @CompanyId
                ORDER BY h.ChangedAt DESC", con);

            cmd.Parameters.AddWithValue("@CompanyId", companyId);

            await con.OpenAsync();
            using var rd = await cmd.ExecuteReaderAsync();

            while (await rd.ReadAsync())
            {
                list.Add(new CompanyChangeHistoryDto
                {
                    CompanyName = rd["CompanyName"].ToString(),
                    FieldName = rd["FieldName"].ToString(),
                    OldValue = rd["OldValue"]?.ToString(),
                    NewValue = rd["NewValue"]?.ToString(),
                    ApprovedBy = rd["ApprovedBy"].ToString(),
                    RequestedBy = rd["RequestedBy"].ToString(),
                    ChangedAt = (DateTime)rd["ChangedAt"]
                });
            }

            return list;
        }

        // GLOBAL HISTORY (ALL COMPANIES)


        public async Task<List<CompanyChangeHistoryDto>> GetAllHistory()
        {
            var list = new List<CompanyChangeHistoryDto>();

            using var con = _db.GetConnection();
            using var cmd = new SqlCommand(@"
                SELECT 
                    c.CompanyName,
                    h.FieldName,
                    h.OldValue,
                    h.NewValue,
                    admin.FirstName + ' ' + admin.LastName AS ApprovedBy,
                    emp.FirstName + ' ' + emp.LastName AS RequestedBy,
                    h.ChangedAt
                FROM CompanyProfileChangeHistory h
                JOIN CompanyProfiles c ON h.CompanyId = c.CompanyId
                JOIN Users admin ON h.ChangedBy = admin.UserId
                JOIN Users emp ON h.RequestedBy = emp.UserId
                ORDER BY h.ChangedAt DESC", con);

            await con.OpenAsync();
            using var rd = await cmd.ExecuteReaderAsync();

            while (await rd.ReadAsync())
            {
                list.Add(new CompanyChangeHistoryDto
                {
                    CompanyName = rd["CompanyName"].ToString(),
                    FieldName = rd["FieldName"].ToString(),
                    OldValue = rd["OldValue"]?.ToString(),
                    NewValue = rd["NewValue"]?.ToString(),
                    ApprovedBy = rd["ApprovedBy"].ToString(),
                    RequestedBy = rd["RequestedBy"].ToString(),
                    ChangedAt = (DateTime)rd["ChangedAt"]
                });
            }

            return list;
        }
    }
}
