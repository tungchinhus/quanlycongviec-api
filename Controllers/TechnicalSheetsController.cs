using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using quanlyfilesBE.Models;
using quanlyfilesBE.DTOs;
using quanlyfilesBE.Data;
using quanlyfilesBE.Services;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace quanlyfilesBE.Controllers;

[ApiController]
[Route("api/technical-sheets")]
[Authorize]
public class TechnicalSheetsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TechnicalSheetsController>? _logger;
    private readonly IFileLoggerService _fileLogger;

    public TechnicalSheetsController(
        ApplicationDbContext context,
        ILogger<TechnicalSheetsController>? logger = null,
        IFileLoggerService? fileLogger = null)
    {
        _context = context;
        _logger = logger;
        _fileLogger = fileLogger ?? new FileLoggerService();
    }

    // Helper method để map TechnicalSheet sang DTO (bao gồm approvals từ bảng mới)
    private async Task<TechnicalSheetDto> MapToDtoAsync(TechnicalSheet sheet)
    {
        var approvals = await _context.TechnicalSheetApprovals
            .Where(a => a.TBKT_ID == sheet.TBKT_ID)
            .OrderByDescending(a => a.ApprovalDate)
            .Select(a => new TechnicalSheetApprovalDto
            {
                ApprovalID = a.ApprovalID,
                TBKT_ID = a.TBKT_ID,
                ApprovalLevel = a.ApprovalLevel,
                ApprovalStatus = a.ApprovalStatus,
                ApproverFirebaseUID = a.ApproverFirebaseUID,
                ApproverName = a.ApproverName,
                ApprovalDate = a.ApprovalDate,
                Notes = a.Notes,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

        return new TechnicalSheetDto
        {
            TBKT_ID = sheet.TBKT_ID,
            Power_kVA = sheet.Power_kVA,
            VoltageSpec = sheet.VoltageSpec,
            Phase = sheet.Phase,
            StandardCode = sheet.StandardCode,
            Proposer = sheet.Proposer,
            DeliveryDate = sheet.DeliveryDate,
            DrawingDate = sheet.DrawingDate,
            Notes = sheet.Notes,
            SalesOrder = sheet.SalesOrder,
            HandOverDate = sheet.HandOverDate,
            ArchivedDate = sheet.ArchivedDate,
            RequesterElectrical = sheet.RequesterElectrical,
            RequesterMechanical = sheet.RequesterMechanical,
            ManagerL1ApprovalStatus = sheet.ManagerL1ApprovalStatus,
            ManagerL1ApproverFirebaseUID = sheet.ManagerL1ApproverFirebaseUID,
            ManagerL1ApprovalDate = sheet.ManagerL1ApprovalDate,
            ManagerL1ApprovalNotes = sheet.ManagerL1ApprovalNotes,
            ManagerApprovalStatus = sheet.ManagerApprovalStatus,
            ManagerApproverFirebaseUID = sheet.ManagerApproverFirebaseUID,
            ManagerApprovalDate = sheet.ManagerApprovalDate,
            ManagerApprovalNotes = sheet.ManagerApprovalNotes,
            TechnicalSheetApprovals = approvals
        };
    }

    // GET: api/technical-sheets?firebaseUID={firebaseUID}&needsApproval={needsApproval}
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TechnicalSheetDto>>> GetAll([FromQuery] string? firebaseUID = null, [FromQuery] bool? needsApproval = null)
    {
        try
        {
            _logger?.LogInformation("GetAll: Starting to fetch TechnicalSheets. Requested firebaseUID: {FirebaseUID}", firebaseUID ?? "NULL");
            await _fileLogger.LogInfoAsync($"TechnicalSheetsController.GetAll: Starting to fetch TechnicalSheets. Requested firebaseUID: {firebaseUID ?? "NULL"}");
            
            // Log thông tin user hiện tại để debug
            var isAuthenticated = User?.Identity?.IsAuthenticated ?? false;
            var userName = User?.Identity?.Name ?? "Unknown";
            _logger?.LogInformation("GetAll: User authenticated: {IsAuthenticated}, UserName: {UserName}", 
                isAuthenticated, userName);
            
            // Kiểm tra xem user có phải admin hoặc manager không (bao gồm managerL, managerL1, managerL2)
            var isAdminOrManager = RoleHelper.IsAdministratorOrManager(User);
            var userRoles = User?.Claims?.Where(c => c.Type == ClaimTypes.Role || c.Type == "role" || c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                .Select(c => c.Value)
                .ToList() ?? new List<string>();
            _logger?.LogInformation("GetAll: IsAdminOrManager: {IsAdminOrManager}, UserRoles: {UserRoles}", 
                isAdminOrManager, string.Join(", ", userRoles));
            
            // #region agent log
            try {
                var logPath = @"c:\MyData\projects\quanlyfiles\quanlyfileFE\.cursor\debug.log";
                var logEntry = JsonSerializer.Serialize(new {
                    id = $"log_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_A",
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    location = "TechnicalSheetsController.cs:48",
                    message = "Role check result",
                    data = new { isAdminOrManager, userRoles = string.Join(", ", userRoles), firebaseUIDParam = firebaseUID ?? "NULL" },
                    sessionId = "debug-session",
                    runId = "run1",
                    hypothesisId = "A"
                });
                await System.IO.File.AppendAllTextAsync(logPath, logEntry + "\n");
            } catch {}
            // #endregion
            
            // Logic phân quyền:
            // - User thường: chỉ xem được danh sách đề nghị do chính họ tạo (Proposer = FirebaseUID của họ)
            // - Manager (managerL, managerL1, managerL2): xem được tất cả đề nghị, bỏ qua firebaseUID parameter
            
            // Lấy FirebaseUID từ token của user hiện tại
            var currentUserFirebaseUID = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? User?.FindFirst("sub")?.Value;
            
            // #region agent log
            try {
                var logPath = @"c:\MyData\projects\quanlyfiles\quanlyfileFE\.cursor\debug.log";
                var logEntry = JsonSerializer.Serialize(new {
                    id = $"log_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_D",
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    location = "TechnicalSheetsController.cs:60",
                    message = "Current user FirebaseUID extracted",
                    data = new { currentUserFirebaseUID = currentUserFirebaseUID ?? "NULL", firebaseUIDParam = firebaseUID ?? "NULL" },
                    sessionId = "debug-session",
                    runId = "run1",
                    hypothesisId = "D"
                });
                await System.IO.File.AppendAllTextAsync(logPath, logEntry + "\n");
            } catch {}
            // #endregion
            
            string? filterFirebaseUID = null;
            
            // Nếu là Manager: bỏ qua firebaseUID parameter, luôn trả về tất cả
            if (isAdminOrManager)
            {
                _logger?.LogInformation("GetAll: User is Admin/Manager, ignoring firebaseUID parameter, will return all TechnicalSheets");
                filterFirebaseUID = null; // Không filter, trả về tất cả
                
                // #region agent log
                try {
                    var logPath = @"c:\MyData\projects\quanlyfiles\quanlyfileFE\.cursor\debug.log";
                    var logEntry = JsonSerializer.Serialize(new {
                        id = $"log_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_B",
                        timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        location = "TechnicalSheetsController.cs:69",
                        message = "Manager branch: filterFirebaseUID set to null",
                        data = new { isAdminOrManager, filterFirebaseUID = filterFirebaseUID ?? "NULL", firebaseUIDParam = firebaseUID ?? "NULL" },
                        sessionId = "debug-session",
                        runId = "run1",
                        hypothesisId = "B"
                    });
                    await System.IO.File.AppendAllTextAsync(logPath, logEntry + "\n");
                } catch {}
                // #endregion
            }
            else
            {
                // User thường: chỉ xem được danh sách của chính họ
                if (!string.IsNullOrEmpty(firebaseUID))
                {
                    // User thường: chỉ cho phép filter theo firebaseUID của chính họ
                    if (string.IsNullOrEmpty(currentUserFirebaseUID) || currentUserFirebaseUID != firebaseUID)
                    {
                        _logger?.LogWarning("GetAll: User {CurrentFirebaseUID} tried to filter by firebaseUID {RequestedFirebaseUID} but is not admin/manager. Access denied.", 
                            currentUserFirebaseUID ?? "NULL", firebaseUID);
                        await _fileLogger.LogWarningAsync($"GetAll: Access denied - CurrentFirebaseUID: {currentUserFirebaseUID ?? "NULL"}, RequestedFirebaseUID: {firebaseUID}");
                        return BadRequest(new { 
                            error = "Access denied", 
                            message = "Bạn chỉ có thể xem đề nghị của chính mình. Chỉ Admin/Manager mới có thể filter theo user khác." 
                        });
                    }
                    
                    filterFirebaseUID = firebaseUID;
                    _logger?.LogInformation("GetAll: User thường - Filtering by firebaseUID parameter: {FirebaseUID}", filterFirebaseUID);
                }
                else
                {
                    // User thường: không có firebaseUID parameter, tự động filter theo FirebaseUID của chính họ
                    if (string.IsNullOrEmpty(currentUserFirebaseUID))
                    {
                        _logger?.LogWarning("GetAll: User not found in token. Returning empty list.");
                        return Ok(new List<TechnicalSheetDto>());
                    }
                    
                    filterFirebaseUID = currentUserFirebaseUID;
                    _logger?.LogInformation("GetAll: User thường - No firebaseUID parameter, using current user FirebaseUID: {FirebaseUID}", filterFirebaseUID);
                }
                
                // #region agent log
                try {
                    var logPath = @"c:\MyData\projects\quanlyfiles\quanlyfileFE\.cursor\debug.log";
                    var logEntry = JsonSerializer.Serialize(new {
                        id = $"log_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_E",
                        timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        location = "TechnicalSheetsController.cs:103",
                        message = "Regular user branch: filterFirebaseUID set",
                        data = new { isAdminOrManager, filterFirebaseUID = filterFirebaseUID ?? "NULL", currentUserFirebaseUID = currentUserFirebaseUID ?? "NULL", firebaseUIDParam = firebaseUID ?? "NULL" },
                        sessionId = "debug-session",
                        runId = "run1",
                        hypothesisId = "E"
                    });
                    await System.IO.File.AppendAllTextAsync(logPath, logEntry + "\n");
                } catch {}
                // #endregion
            }
            
            // Try to fetch data with explicit type handling
            List<TechnicalSheet> sheets;
            try
            {
                var query = _context.TechnicalSheets
                    .Where(s => s.TBKT_ID != null && !string.IsNullOrEmpty(s.TBKT_ID));
                
                // Filter theo firebaseUID nếu có (Proposer lưu trực tiếp FirebaseUID)
                if (!string.IsNullOrEmpty(filterFirebaseUID))
                {
                    // Trim để xử lý khoảng trắng, so sánh case-insensitive
                    var trimmedFilterUID = filterFirebaseUID.Trim();
                    query = query.Where(s => !string.IsNullOrEmpty(s.Proposer) && 
                                             s.Proposer.Trim() == trimmedFilterUID);
                    _logger?.LogInformation("GetAll: Filtering by Proposer (FirebaseUID) = {FirebaseUID} (trimmed)", trimmedFilterUID);
                }
                else if (!isAdminOrManager)
                {
                    // Nếu không phải admin/manager và không có filterFirebaseUID, trả về danh sách rỗng
                    _logger?.LogWarning("GetAll: User is not admin/manager and no firebaseUID provided. Returning empty list.");
                    return Ok(new List<TechnicalSheetDto>());
                }
                else
                {
                    // Manager (managerL, managerL1, managerL2): trả về tất cả TechnicalSheets (bao gồm cả Proposer = NULL)
                    _logger?.LogInformation("GetAll: User is Admin/Manager, returning all TechnicalSheets (including NULL Proposer)");
                }

                // Filter theo needsApproval nếu có
                // TechnicalSheet đã hoàn thành (có ArchivedDate) và cần approval
                if (needsApproval == true && isAdminOrManager)
                {
                    // Kiểm tra role của user
                    var isAdmin = RoleHelper.IsAdministrator(User);
                    var isManager = RoleHelper.IsManager(User);
                    
                    // Kiểm tra xem user có phải ManagerL1 không
                    var isManagerL1 = false;
                    var isManagerOnly = false;
                    var roleClaims = User?.Claims?.Where(c => c.Type == ClaimTypes.Role || 
                                                              c.Type == "role" || 
                                                              c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                        .Select(c => c.Value)
                        .ToList() ?? new List<string>();
                    
                    isManagerL1 = roleClaims.Any(role => 
                        role != null && 
                        role.Equals("ManagerL1", StringComparison.OrdinalIgnoreCase));
                    
                    isManagerOnly = isManager && !isManagerL1;

                    // TechnicalSheet đã hoàn thành (có ArchivedDate)
                    query = query.Where(s => s.ArchivedDate != null);

                    if (isManagerL1)
                    {
                        // ManagerL1: thấy TechnicalSheet chưa có ManagerL1 approval hoặc đã reject
                        query = query.Where(s => 
                            string.IsNullOrEmpty(s.ManagerL1ApprovalStatus) || 
                            s.ManagerL1ApprovalStatus == "Pending" ||
                            s.ManagerL1ApprovalStatus == "Rejected");
                        _logger?.LogInformation("GetAll: Filtering for ManagerL1 - sheets needing ManagerL1 approval");
                    }
                    else if (isManagerOnly)
                    {
                        // Manager: chỉ thấy TechnicalSheet đã được ManagerL1 approve nhưng chưa có Manager approval
                        query = query.Where(s => 
                            s.ManagerL1ApprovalStatus == "Approved" &&
                            (string.IsNullOrEmpty(s.ManagerApprovalStatus) || 
                             s.ManagerApprovalStatus == "Pending"));
                        _logger?.LogInformation("GetAll: Filtering for Manager - sheets needing Manager approval (after ManagerL1 approved)");
                    }
                    else if (isAdmin)
                    {
                        // Admin: thấy tất cả TechnicalSheet đã hoàn thành cần approval (ở bất kỳ cấp nào)
                        query = query.Where(s => 
                            (string.IsNullOrEmpty(s.ManagerL1ApprovalStatus) || s.ManagerL1ApprovalStatus == "Pending") ||
                            (s.ManagerL1ApprovalStatus == "Approved" && 
                             (string.IsNullOrEmpty(s.ManagerApprovalStatus) || s.ManagerApprovalStatus == "Pending")));
                        _logger?.LogInformation("GetAll: Filtering for Admin - sheets needing approval at any level");
                    }
                }
                
                // #region agent log
                try {
                    var logPath = @"c:\MyData\projects\quanlyfiles\quanlyfileFE\.cursor\debug.log";
                    var logEntry = JsonSerializer.Serialize(new {
                        id = $"log_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_C",
                        timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        location = "TechnicalSheetsController.cs:133",
                        message = "Query filter state before execution",
                        data = new { isAdminOrManager, filterFirebaseUID = filterFirebaseUID ?? "NULL", willFilter = !string.IsNullOrEmpty(filterFirebaseUID) },
                        sessionId = "debug-session",
                        runId = "run1",
                        hypothesisId = "C"
                    });
                    await System.IO.File.AppendAllTextAsync(logPath, logEntry + "\n");
                } catch {}
                // #endregion
                
                sheets = await query
                    .OrderBy(x => x.TBKT_ID)
                    .ToListAsync();
                
                _logger?.LogInformation("GetAll: Retrieved {Count} TechnicalSheets from database", sheets.Count);
                
                // #region agent log
                try {
                    var logPath = @"c:\MyData\projects\quanlyfiles\quanlyfileFE\.cursor\debug.log";
                    var logEntry = JsonSerializer.Serialize(new {
                        id = $"log_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_FINAL",
                        timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        location = "TechnicalSheetsController.cs:137",
                        message = "Final result count",
                        data = new { isAdminOrManager, filterFirebaseUID = filterFirebaseUID ?? "NULL", resultCount = sheets.Count },
                        sessionId = "debug-session",
                        runId = "run1",
                        hypothesisId = "C"
                    });
                    await System.IO.File.AppendAllTextAsync(logPath, logEntry + "\n");
                } catch {}
                // #endregion
            }
            catch (Exception dbEx)
            {
                var dbErrorMsg = $"Database query error: {dbEx.Message}";
                _logger?.LogError(dbEx, dbErrorMsg);
                await _fileLogger.LogErrorAsync(
                    "TechnicalSheetsController.GetAll: Database query failed, attempting fallback with raw SQL",
                    dbEx,
                    $"Attempted to query TechnicalSheets table. Connection string: {_context.Database.GetConnectionString()?.Substring(0, Math.Min(50, _context.Database.GetConnectionString()?.Length ?? 0))}...");
                
                // Fallback: Try raw SQL query with explicit CAST to handle type mismatches
                try
                {
                    var whereClause = "TBKT_ID IS NOT NULL AND TBKT_ID != ''";
                    
                    // Thêm filter theo FirebaseUID nếu có (Proposer lưu trực tiếp FirebaseUID)
                    // Manager: bỏ qua filterFirebaseUID, trả về tất cả
                    if (!string.IsNullOrEmpty(filterFirebaseUID))
                    {
                        var trimmedFilterUID = filterFirebaseUID.Trim().Replace("'", "''");
                        whereClause += $" AND LTRIM(RTRIM(Proposer)) = '{trimmedFilterUID}'";
                        _logger?.LogInformation("GetAll (SQL fallback): Filtering by Proposer (FirebaseUID) = {FirebaseUID} (trimmed)", trimmedFilterUID);
                    }
                    else if (!isAdminOrManager)
                    {
                        // User thường: nếu không có filterFirebaseUID, trả về danh sách rỗng
                        _logger?.LogWarning("GetAll (SQL fallback): User is not admin/manager and no firebaseUID provided. Returning empty list.");
                        return Ok(new List<TechnicalSheetDto>());
                    }
                    else
                    {
                        // Manager: không filter, trả về tất cả
                        _logger?.LogInformation("GetAll (SQL fallback): User is Admin/Manager, returning all TechnicalSheets");
                    }
                    
                    var rawSql = $@"
                        SELECT 
                            CAST(TBKT_ID AS VARCHAR(50)) AS TBKT_ID,
                            Power_kVA,
                            VoltageSpec,
                            Phase,
                            StandardCode,
                            Proposer,
                            DeliveryDate,
                            DrawingDate,
                            Notes,
                            SalesOrder,
                            HandOverDate,
                            ArchivedDate,
                            RequesterElectrical,
                            RequesterMechanical
                        FROM TechnicalSheet
                        WHERE {whereClause}
                        ORDER BY TBKT_ID";
                    
                    sheets = await _context.TechnicalSheets
                        .FromSqlRaw(rawSql)
                        .ToListAsync();
                    
                    _logger?.LogWarning("Successfully retrieved TechnicalSheets using raw SQL fallback");
                    await _fileLogger.LogWarningAsync("TechnicalSheetsController.GetAll: Successfully retrieved data using raw SQL fallback");
                }
                catch (Exception fallbackEx)
                {
                    await _fileLogger.LogErrorAsync(
                        "TechnicalSheetsController.GetAll: Raw SQL fallback also failed",
                        fallbackEx,
                        $"Original error: {dbEx.Message}");
                    throw dbEx; // Throw original exception
                }
            }
            
            _logger?.LogInformation("GetAll: Fetched {Count} sheets from database. FilterFirebaseUID: {FilterFirebaseUID}, IsAdminOrManager: {IsAdminOrManager}", 
                sheets.Count, filterFirebaseUID ?? "NULL", isAdminOrManager);
            await _fileLogger.LogInfoAsync($"TechnicalSheetsController.GetAll: Fetched {sheets.Count} sheets from database. FilterFirebaseUID: {filterFirebaseUID ?? "NULL"}, IsAdminOrManager: {isAdminOrManager}");
            
            // Log thông tin về Proposer của các sheets để debug
            if (sheets.Count > 0)
            {
                var proposerInfo = sheets.Select(s => new { 
                    TBKT_ID = s.TBKT_ID, 
                    Proposer = s.Proposer ?? "NULL",
                    ProposerLength = s.Proposer?.Length ?? 0
                }).Take(5).ToList();
                _logger?.LogInformation("GetAll: Sample Proposer info (first 5): {ProposerInfo}", 
                    System.Text.Json.JsonSerializer.Serialize(proposerInfo));
            }
            
            // Load tất cả approvals một lần (tối ưu hiệu suất)
            var tbktIds = sheets.Select(s => s.TBKT_ID).Where(id => !string.IsNullOrEmpty(id)).ToList();
            var allApprovals = await _context.TechnicalSheetApprovals
                .Where(a => tbktIds.Contains(a.TBKT_ID))
                .OrderByDescending(a => a.ApprovalDate)
                .Select(a => new TechnicalSheetApprovalDto
                {
                    ApprovalID = a.ApprovalID,
                    TBKT_ID = a.TBKT_ID,
                    ApprovalLevel = a.ApprovalLevel,
                    ApprovalStatus = a.ApprovalStatus,
                    ApproverFirebaseUID = a.ApproverFirebaseUID,
                    ApproverName = a.ApproverName,
                    ApprovalDate = a.ApprovalDate,
                    Notes = a.Notes,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();
            
            // Group approvals theo TBKT_ID
            var approvalsByTBKT = allApprovals
                .GroupBy(a => a.TBKT_ID)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Map to DTOs with safe type conversion
            var dtos = new List<TechnicalSheetDto>();
            foreach (var s in sheets)
            {
                try
                {
                    var dto = new TechnicalSheetDto
                    {
                        TBKT_ID = s.TBKT_ID ?? string.Empty,
                        Power_kVA = s.Power_kVA,
                        VoltageSpec = s.VoltageSpec,
                        Phase = s.Phase,
                        StandardCode = s.StandardCode,
                        Proposer = s.Proposer,
                        DeliveryDate = s.DeliveryDate,
                        DrawingDate = s.DrawingDate,
                        Notes = s.Notes,
                        SalesOrder = s.SalesOrder,
                        HandOverDate = s.HandOverDate,
                        ArchivedDate = s.ArchivedDate,
                        RequesterElectrical = s.RequesterElectrical,
                        RequesterMechanical = s.RequesterMechanical,
                        ManagerL1ApprovalStatus = s.ManagerL1ApprovalStatus,
                        ManagerL1ApproverFirebaseUID = s.ManagerL1ApproverFirebaseUID,
                        ManagerL1ApprovalDate = s.ManagerL1ApprovalDate,
                        ManagerL1ApprovalNotes = s.ManagerL1ApprovalNotes,
                        ManagerApprovalStatus = s.ManagerApprovalStatus,
                        ManagerApproverFirebaseUID = s.ManagerApproverFirebaseUID,
                        ManagerApprovalDate = s.ManagerApprovalDate,
                        ManagerApprovalNotes = s.ManagerApprovalNotes,
                        // Load lịch sử approvals từ bảng mới (đã được load trước đó)
                        TechnicalSheetApprovals = approvalsByTBKT.ContainsKey(s.TBKT_ID) 
                            ? approvalsByTBKT[s.TBKT_ID] 
                            : new List<TechnicalSheetApprovalDto>()
                    };
                    dtos.Add(dto);
                }
                catch (Exception mapEx)
                {
                    var mapErrorMsg = $"Error mapping TechnicalSheet with TBKT_ID: {s?.TBKT_ID ?? "NULL"}";
                    _logger?.LogError(mapEx, mapErrorMsg);
                    await _fileLogger.LogErrorAsync(
                        mapErrorMsg,
                        mapEx,
                        $"TBKT_ID value: {s?.TBKT_ID}, Type: {s?.TBKT_ID?.GetType()?.FullName}");
                    // Continue with other records instead of failing completely
                }
            }
            
            _logger?.LogInformation("GetAll: Successfully mapped {Count} DTOs", dtos.Count);
            await _fileLogger.LogInfoAsync($"TechnicalSheetsController.GetAll: Successfully mapped {dtos.Count} DTOs");
            return Ok(dtos);
        }
        catch (Exception ex)
        {
            var errorMsg = "Error getting all TechnicalSheets";
            _logger?.LogError(ex, errorMsg);
            _logger?.LogError("Exception Type: {ExceptionType}", ex.GetType().FullName);
            _logger?.LogError("Exception Message: {Message}", ex.Message);
            _logger?.LogError("Inner Exception: {InnerException}", ex.InnerException?.Message ?? "None");
            _logger?.LogError("Stack Trace: {StackTrace}", ex.StackTrace);
            
            // Log to file with full details
            var additionalInfo = $"Request Path: {Request.Path}, Method: {Request.Method}, " +
                                $"User: {User?.Identity?.Name ?? "Anonymous"}, " +
                                $"Exception Type: {ex.GetType().FullName}, " +
                                $"Inner Exception: {(ex.InnerException != null ? $"{ex.InnerException.GetType().FullName}: {ex.InnerException.Message}" : "None")}";
            
            await _fileLogger.LogErrorAsync(errorMsg, ex, additionalInfo);
            
            // Return detailed error information
            var errorDetails = new
            {
                error = "Error retrieving TechnicalSheets",
                message = ex.Message,
                exceptionType = ex.GetType().FullName,
                innerException = ex.InnerException != null ? new
                {
                    type = ex.InnerException.GetType().FullName,
                    message = ex.InnerException.Message
                } : null,
                stackTrace = ex.StackTrace
            };
            
            return StatusCode(500, errorDetails);
        }
    }

    // GET: api/technical-sheets/next-id
    [HttpGet("next-id")]
    public async Task<ActionResult<object>> GetNextTbktId()
    {
        try
        {
            var ids = await _context.TechnicalSheets
                .Where(ts => !string.IsNullOrEmpty(ts.TBKT_ID))
                .Select(ts => ts.TBKT_ID!)
                .ToListAsync();

            if (!ids.Any())
            {
                return Ok(new { nextTbktId = "1" });
            }

            var candidates = ids
                .Select(id => new
                {
                    Raw = id.Trim(),
                    Upper = id.Trim().ToUpperInvariant(),
                    Numeric = ExtractNumericPart(id)
                })
                .Where(x => x.Numeric.HasValue)
                .ToList();

            if (!candidates.Any())
            {
                return Ok(new { nextTbktId = "1" });
            }

            var maxCandidate = candidates
                .OrderByDescending(c => c.Numeric!.Value)
                .ThenByDescending(c => c.Upper)
                .First();

            var nextNumber = maxCandidate.Numeric!.Value + 1;
            var suffix = ExtractSuffix(maxCandidate.Upper);
            var nextId = $"{nextNumber}{suffix}".Trim();

            return Ok(new { nextTbktId = nextId });
        }
        catch (Exception ex)
        {
            var errorMsg = "Error generating next TBKT_ID";
            _logger?.LogError(ex, errorMsg);
            await _fileLogger.LogErrorAsync(errorMsg, ex);
            return StatusCode(500, new { error = errorMsg, message = ex.Message });
        }
    }

    private static int? ExtractNumericPart(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        var match = Regex.Match(id, @"\d+");
        if (match.Success && int.TryParse(match.Value, out var number))
        {
            return number;
        }
        return null;
    }

    private static string ExtractSuffix(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return string.Empty;
        var match = Regex.Match(id.ToUpperInvariant(), @"^\s*\d+\s*([A-Z]+)?\s*$");
        if (match.Success)
        {
            return match.Groups[1].Value ?? string.Empty;
        }

        var trailingLetters = new string(id.ToUpperInvariant().Reverse().TakeWhile(char.IsLetter).Reverse().ToArray());
        return trailingLetters;
    }

    // GET: api/technical-sheets/{tbktId}
    [HttpGet("{tbktId}")]
    public async Task<ActionResult<TechnicalSheetDto>> GetById(string tbktId)
    {
        try
        {
            var sheet = await _context.TechnicalSheets
                .FirstOrDefaultAsync(ts => ts.TBKT_ID == tbktId);
            
            if (sheet == null)
            {
                return NotFound(new { message = $"TechnicalSheet with TBKT_ID '{tbktId}' not found" });
            }
            
            // Map sang DTO với approvals từ bảng mới
            var dto = await MapToDtoAsync(sheet);
            
            return Ok(dto);
        }
        catch (Exception ex)
        {
            var errorMsg = $"Error getting TechnicalSheet by TBKT_ID: {tbktId}";
            _logger?.LogError(ex, errorMsg);
            var additionalInfo = $"TBKT_ID: {tbktId}, Request Path: {Request.Path}, Method: {Request.Method}";
            await _fileLogger.LogErrorAsync(errorMsg, ex, additionalInfo);
            return StatusCode(500, new { error = "Error retrieving TechnicalSheet", message = ex.Message });
        }
    }

    // POST: api/technical-sheets
    [HttpPost]
    public async Task<ActionResult<TechnicalSheetDto>> Create([FromBody] CreateTechnicalSheetDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Check if TBKT_ID already exists
            var existing = await _context.TechnicalSheets
                .FirstOrDefaultAsync(ts => ts.TBKT_ID == dto.TBKT_ID);
            
            if (existing != null)
            {
                return Conflict(new { message = $"TechnicalSheet with TBKT_ID '{dto.TBKT_ID}' already exists" });
            }

            // Lấy FirebaseUID của user hiện tại nếu Proposer chưa được set
            string? proposerFirebaseUID = dto.Proposer?.Trim();
            if (string.IsNullOrEmpty(proposerFirebaseUID))
            {
                proposerFirebaseUID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value?.Trim() 
                    ?? User.FindFirst("sub")?.Value?.Trim();
                
                if (!string.IsNullOrEmpty(proposerFirebaseUID))
                {
                    _logger?.LogInformation("Create: Auto-set Proposer to current user FirebaseUID: {FirebaseUID}", proposerFirebaseUID);
                }
                else
                {
                    _logger?.LogWarning("Create: Could not get FirebaseUID from token. Proposer will be NULL. Claims: {Claims}", 
                        string.Join(", ", User?.Claims?.Select(c => $"{c.Type}={c.Value}") ?? new List<string>()));
                }
            }
            
            // Trim và validate Proposer
            proposerFirebaseUID = string.IsNullOrWhiteSpace(proposerFirebaseUID) ? null : proposerFirebaseUID.Trim();

            var newSheet = new TechnicalSheet
            {
                TBKT_ID = dto.TBKT_ID,
                Power_kVA = dto.Power_kVA,
                VoltageSpec = dto.VoltageSpec,
                Phase = dto.Phase,
                StandardCode = dto.StandardCode,
                Proposer = proposerFirebaseUID,
                DeliveryDate = dto.DeliveryDate,
                DrawingDate = dto.DrawingDate,
                SalesOrder = dto.SalesOrder,
                HandOverDate = dto.HandOverDate,
                // Set archivedDate to current date if not provided
                ArchivedDate = dto.ArchivedDate ?? DateTime.Now,
                RequesterElectrical = dto.RequesterElectrical,
                RequesterMechanical = dto.RequesterMechanical,
                Notes = dto.Notes
            };

            _context.TechnicalSheets.Add(newSheet);
            await _context.SaveChangesAsync();

            // Map sang DTO với approvals từ bảng mới (mới tạo nên chưa có approvals)
            var resultDto = await MapToDtoAsync(newSheet);

            return CreatedAtAction(nameof(GetById), new { tbktId = newSheet.TBKT_ID }, resultDto);
        }
        catch (Exception ex)
        {
            var errorMsg = "Error creating TechnicalSheet";
            _logger?.LogError(ex, errorMsg);
            var additionalInfo = $"TBKT_ID: {dto?.TBKT_ID}, Request Path: {Request.Path}, Method: {Request.Method}";
            await _fileLogger.LogErrorAsync(errorMsg, ex, additionalInfo);
            return StatusCode(500, new { error = "Error creating TechnicalSheet", message = ex.Message });
        }
    }

    // PUT: api/technical-sheets/{tbktId}
    [HttpPut("{tbktId}")]
    public async Task<IActionResult> Update(string tbktId, [FromBody] CreateTechnicalSheetDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var sheet = await _context.TechnicalSheets
                .FirstOrDefaultAsync(ts => ts.TBKT_ID == tbktId);
            
            if (sheet == null)
            {
                return NotFound(new { message = $"TechnicalSheet with TBKT_ID '{tbktId}' not found" });
            }

            // Update properties (except TBKT_ID which is the primary key)
            if (dto.Power_kVA.HasValue)
                sheet.Power_kVA = dto.Power_kVA;
            if (!string.IsNullOrEmpty(dto.VoltageSpec))
                sheet.VoltageSpec = dto.VoltageSpec;
            if (dto.Phase.HasValue)
                sheet.Phase = dto.Phase;
            if (!string.IsNullOrEmpty(dto.StandardCode))
                sheet.StandardCode = dto.StandardCode;
            if (!string.IsNullOrEmpty(dto.Proposer))
                sheet.Proposer = dto.Proposer;
            if (dto.DeliveryDate.HasValue)
                sheet.DeliveryDate = dto.DeliveryDate;
            if (dto.DrawingDate.HasValue)
                sheet.DrawingDate = dto.DrawingDate;
            if (!string.IsNullOrEmpty(dto.SalesOrder))
                sheet.SalesOrder = dto.SalesOrder;
            if (dto.HandOverDate.HasValue)
                sheet.HandOverDate = dto.HandOverDate;
            if (dto.ArchivedDate.HasValue)
                sheet.ArchivedDate = dto.ArchivedDate;
            if (!string.IsNullOrEmpty(dto.RequesterElectrical))
                sheet.RequesterElectrical = dto.RequesterElectrical;
            if (!string.IsNullOrEmpty(dto.RequesterMechanical))
                sheet.RequesterMechanical = dto.RequesterMechanical;
            if (!string.IsNullOrEmpty(dto.Notes))
                sheet.Notes = dto.Notes;

            await _context.SaveChangesAsync();

            // Map sang DTO với approvals từ bảng mới
            var resultDto = await MapToDtoAsync(sheet);

            return Ok(resultDto);
        }
        catch (Exception ex)
        {
            var errorMsg = $"Error updating TechnicalSheet with TBKT_ID: {tbktId}";
            _logger?.LogError(ex, errorMsg);
            var additionalInfo = $"TBKT_ID: {tbktId}, Request Path: {Request.Path}, Method: {Request.Method}";
            await _fileLogger.LogErrorAsync(errorMsg, ex, additionalInfo);
            return StatusCode(500, new { error = "Error updating TechnicalSheet", message = ex.Message });
        }
    }

    // GET: api/technical-sheets/debug/proposer-check
    [HttpGet("debug/proposer-check")]
    [Authorize]
    public async Task<IActionResult> GetDebugProposerCheck()
    {
        try
        {
            var currentUserFirebaseUID = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value?.Trim() 
                ?? User?.FindFirst("sub")?.Value?.Trim();
            
            var isAdminOrManager = RoleHelper.IsAdministratorOrManager(User);
            
            // Lấy tất cả TechnicalSheets với Proposer info
            var allSheets = await _context.TechnicalSheets
                .Where(s => s.TBKT_ID != null && !string.IsNullOrEmpty(s.TBKT_ID))
                .Select(s => new { 
                    s.TBKT_ID, 
                    Proposer = s.Proposer ?? "NULL",
                    ProposerTrimmed = s.Proposer != null ? s.Proposer.Trim() : "NULL",
                    ProposerLength = s.Proposer != null ? s.Proposer.Length : 0,
                    MatchesCurrentUser = s.Proposer != null && s.Proposer.Trim() == currentUserFirebaseUID
                })
                .OrderBy(s => s.TBKT_ID)
                .Take(50)
                .ToListAsync();
            
            return Ok(new
            {
                currentUserFirebaseUID = currentUserFirebaseUID ?? "NULL",
                isAdminOrManager,
                totalSheets = await _context.TechnicalSheets
                    .Where(s => s.TBKT_ID != null && !string.IsNullOrEmpty(s.TBKT_ID))
                    .CountAsync(),
                sheetsWithProposer = await _context.TechnicalSheets
                    .Where(s => s.TBKT_ID != null && !string.IsNullOrEmpty(s.TBKT_ID) && !string.IsNullOrEmpty(s.Proposer))
                    .CountAsync(),
                sheetsWithoutProposer = await _context.TechnicalSheets
                    .Where(s => s.TBKT_ID != null && !string.IsNullOrEmpty(s.TBKT_ID) && (s.Proposer == null || string.IsNullOrEmpty(s.Proposer)))
                    .CountAsync(),
                matchingSheets = !string.IsNullOrEmpty(currentUserFirebaseUID) 
                    ? await _context.TechnicalSheets
                        .Where(s => s.TBKT_ID != null && !string.IsNullOrEmpty(s.TBKT_ID) && 
                                   s.Proposer != null && s.Proposer.Trim() == currentUserFirebaseUID)
                        .CountAsync()
                    : 0,
                sampleSheets = allSheets
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in GetDebugProposerCheck");
            return StatusCode(500, new { error = "Error getting debug info", message = ex.Message });
        }
    }

    // GET: api/technical-sheets/debug/user-info
    [HttpGet("debug/user-info")]
    public async Task<IActionResult> GetDebugUserInfo()
    {
        try
        {
            var isAuthenticated = User?.Identity?.IsAuthenticated ?? false;
            var userName = User?.Identity?.Name ?? "Unknown";
            var allClaims = (User?.Claims?.Select(c => new { Type = c.Type, Value = c.Value }).ToList()) 
                ?? new List<System.Security.Claims.Claim>().Select(c => new { Type = c.Type, Value = c.Value }).ToList();
            
            var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? User?.FindFirst("sub")?.Value;
            var emailClaim = User?.FindFirst(ClaimTypes.Email)?.Value 
                ?? User?.FindFirst("email")?.Value;
            
            var isAdminOrManager = RoleHelper.IsAdministratorOrManager(User);
            
            Models.User? user = null;
            
            // Tìm user theo FirebaseUID từ token
            if (!string.IsNullOrEmpty(userIdClaim))
            {
                user = await _context.Users
                    .FirstOrDefaultAsync(u => u.FirebaseUID == userIdClaim);
            }
            
            // Nếu không tìm thấy, thử tìm theo email
            if (user == null && !string.IsNullOrEmpty(emailClaim))
            {
                user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == emailClaim);
            }
            
            // Lấy FirebaseUID trực tiếp từ token hoặc từ user object
            string? currentUserFirebaseUID = userIdClaim ?? user?.FirebaseUID;
            
            var proposalCount = 0;
            var userProposals = new List<object>();
            
            // Sử dụng FirebaseUID trực tiếp để đếm proposals (dùng trim để so sánh)
            if (!string.IsNullOrEmpty(currentUserFirebaseUID))
            {
                var trimmedUID = currentUserFirebaseUID.Trim();
                proposalCount = await _context.TechnicalSheets
                    .Where(s => !string.IsNullOrEmpty(s.Proposer) && s.Proposer.Trim() == trimmedUID)
                    .CountAsync();
                
                userProposals = await _context.TechnicalSheets
                    .Where(s => !string.IsNullOrEmpty(s.Proposer) && s.Proposer.Trim() == trimmedUID)
                    .Select(s => new { s.TBKT_ID, s.Proposer, s.StandardCode })
                    .ToListAsync<object>();
            }
            
            var totalProposals = await _context.TechnicalSheets
                .Where(s => s.TBKT_ID != null && !string.IsNullOrEmpty(s.TBKT_ID))
                .CountAsync();
            
            // Lấy tất cả TechnicalSheets với Proposer để debug
            var allProposalsWithProposer = await _context.TechnicalSheets
                .Where(s => s.TBKT_ID != null && !string.IsNullOrEmpty(s.TBKT_ID) && !string.IsNullOrEmpty(s.Proposer))
                .Select(s => new { s.TBKT_ID, s.Proposer })
                .ToListAsync<object>();
            
            return Ok(new
            {
                isAuthenticated,
                userName,
                claims = allClaims,
                userIdClaim,
                emailClaim,
                isAdminOrManager,
                user = user != null ? new
                {
                    user.UserId,
                    user.UserName,
                    user.Email,
                    user.FirebaseUID,
                    user.IsActive
                } : null,
                currentUserFirebaseUID,
                proposalCount,
                userProposals,
                totalProposals,
                allProposalsWithProposer,
                message = "Debug information for current user"
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in GetDebugUserInfo");
            return StatusCode(500, new { error = "Error getting debug info", message = ex.Message });
        }
    }

    // DELETE: api/technical-sheets/{tbktId}
    [HttpDelete("{tbktId}")]
    public async Task<IActionResult> Delete(string tbktId)
    {
        try
        {
            var sheet = await _context.TechnicalSheets
                .Include(ts => ts.MachineAssignments)
                .FirstOrDefaultAsync(ts => ts.TBKT_ID == tbktId);
            
            if (sheet == null)
            {
                return NotFound(new { message = $"TechnicalSheet with TBKT_ID '{tbktId}' not found" });
            }

            // Check if there are any MachineAssignments using this TechnicalSheet
            if (sheet.MachineAssignments != null && sheet.MachineAssignments.Any())
            {
                return BadRequest(new { 
                    message = $"Cannot delete TechnicalSheet with TBKT_ID '{tbktId}' because it is being used by {sheet.MachineAssignments.Count} assignment(s). Please delete the assignments first." 
                });
            }

            _context.TechnicalSheets.Remove(sheet);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            var errorMsg = $"Error deleting TechnicalSheet with TBKT_ID: {tbktId}";
            _logger?.LogError(ex, errorMsg);
            var additionalInfo = $"TBKT_ID: {tbktId}, Request Path: {Request.Path}, Method: {Request.Method}";
            await _fileLogger.LogErrorAsync(errorMsg, ex, additionalInfo);
            return StatusCode(500, new { error = "Error deleting TechnicalSheet", message = ex.Message });
        }
    }

    // POST: api/technical-sheets/{tbktId}/approve
    [HttpPost("{tbktId}/approve")]
    public async Task<ActionResult<TechnicalSheetDto>> ApproveTechnicalSheet(string tbktId, [FromBody] ApproveTechnicalSheetDto dto)
    {
        try
        {
            var sheet = await _context.TechnicalSheets
                .FirstOrDefaultAsync(ts => ts.TBKT_ID == tbktId);
            
            if (sheet == null)
            {
                return NotFound(new { message = $"TechnicalSheet with TBKT_ID '{tbktId}' not found" });
            }

            var currentUserFirebaseUID = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? User?.FindFirst("sub")?.Value;
            var currentUserEmail = User?.FindFirst(ClaimTypes.Email)?.Value 
                ?? User?.FindFirst("email")?.Value;

            // Tìm user trong database để lấy thông tin đầy đủ
            Models.User? currentUser = null;
            if (!string.IsNullOrEmpty(currentUserFirebaseUID))
            {
                currentUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.FirebaseUID == currentUserFirebaseUID);
            }

            // Kiểm tra role của user
            var isAdmin = RoleHelper.IsAdministrator(User);
            var isManager = RoleHelper.IsManager(User);
            
            // Kiểm tra xem user có phải ManagerL1 không
            var isManagerL1 = false;
            var roleClaims = User?.Claims?.Where(c => c.Type == ClaimTypes.Role || 
                                                      c.Type == "role" || 
                                                      c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                .Select(c => c.Value)
                .ToList() ?? new List<string>();
            
            isManagerL1 = roleClaims.Any(role => 
                role != null && 
                role.Equals("ManagerL1", StringComparison.OrdinalIgnoreCase));

            // Kiểm tra xem user có phải Manager (không phải ManagerL1) không
            var isManagerOnly = isManager && !isManagerL1;

            // Kiểm tra quyền approve
            if (dto.ApprovalLevel == "ManagerL1")
            {
                if (!isAdmin && !isManagerL1)
                {
                    return StatusCode(403, new { message = "Chỉ ManagerL1 hoặc Admin mới có quyền ký xác nhận cấp 1" });
                }

                // ManagerL1 chỉ có thể approve khi chưa có approval nào
                if (!string.IsNullOrEmpty(sheet.ManagerL1ApprovalStatus) && sheet.ManagerL1ApprovalStatus != "Pending")
                {
                    return BadRequest(new { message = "TechnicalSheet này đã được xử lý ở cấp ManagerL1" });
                }

                // Set ManagerL1 approval (DEPRECATED: Giữ để tương thích ngược)
                sheet.ManagerL1ApprovalStatus = dto.Action == "approve" ? "Approved" : "Rejected";
                sheet.ManagerL1ApproverFirebaseUID = currentUserFirebaseUID;
                sheet.ManagerL1ApprovalDate = DateTime.UtcNow;
                sheet.ManagerL1ApprovalNotes = dto.Notes;

                // Ghi vào bảng TechnicalSheetApproval mới (lưu lịch sử)
                var approval = new Models.TechnicalSheetApproval
                {
                    TBKT_ID = tbktId,
                    ApprovalLevel = "ManagerL1",
                    ApprovalStatus = dto.Action == "approve" ? "Approved" : "Rejected",
                    ApproverFirebaseUID = currentUserFirebaseUID ?? string.Empty,
                    ApproverName = currentUser?.FullName ?? currentUserEmail ?? "Unknown",
                    ApprovalDate = DateTime.UtcNow,
                    Notes = dto.Notes,
                    CreatedAt = DateTime.UtcNow
                };
                _context.TechnicalSheetApprovals.Add(approval);
            }
            else if (dto.ApprovalLevel == "Manager")
            {
                if (!isAdmin && !isManagerOnly)
                {
                    return StatusCode(403, new { message = "Chỉ Manager hoặc Admin mới có quyền duyệt cuối cùng" });
                }

                // Manager chỉ có thể approve sau khi ManagerL1 đã approve
                if (string.IsNullOrEmpty(sheet.ManagerL1ApprovalStatus) || sheet.ManagerL1ApprovalStatus != "Approved")
                {
                    return BadRequest(new { message = "TechnicalSheet này cần được ManagerL1 ký xác nhận trước" });
                }

                // Manager chỉ có thể approve khi chưa có approval
                if (!string.IsNullOrEmpty(sheet.ManagerApprovalStatus) && sheet.ManagerApprovalStatus != "Pending")
                {
                    return BadRequest(new { message = "TechnicalSheet này đã được xử lý ở cấp Manager" });
                }

                // Set Manager approval (DEPRECATED: Giữ để tương thích ngược)
                sheet.ManagerApprovalStatus = dto.Action == "approve" ? "Approved" : "Rejected";
                sheet.ManagerApproverFirebaseUID = currentUserFirebaseUID;
                sheet.ManagerApprovalDate = DateTime.UtcNow;
                sheet.ManagerApprovalNotes = dto.Notes;

                // Ghi vào bảng TechnicalSheetApproval mới (lưu lịch sử)
                var approval = new Models.TechnicalSheetApproval
                {
                    TBKT_ID = tbktId,
                    ApprovalLevel = "Manager",
                    ApprovalStatus = dto.Action == "approve" ? "Approved" : "Rejected",
                    ApproverFirebaseUID = currentUserFirebaseUID ?? string.Empty,
                    ApproverName = currentUser?.FullName ?? currentUserEmail ?? "Unknown",
                    ApprovalDate = DateTime.UtcNow,
                    Notes = dto.Notes,
                    CreatedAt = DateTime.UtcNow
                };
                _context.TechnicalSheetApprovals.Add(approval);
            }
            else
            {
                return BadRequest(new { message = "ApprovalLevel phải là 'ManagerL1' hoặc 'Manager'" });
            }

            await _context.SaveChangesAsync();

            // Map sang DTO với approvals từ bảng mới
            var resultDto = await MapToDtoAsync(sheet);

            _logger?.LogInformation("TechnicalSheet approval submitted: {TBKT_ID}, Level: {Level}, Action: {Action}", 
                tbktId, dto.ApprovalLevel, dto.Action);
            await _fileLogger.LogInfoAsync($"TechnicalSheetsController.ApproveTechnicalSheet: {dto.Action} submitted for {tbktId} at level {dto.ApprovalLevel}");

            return Ok(resultDto);
        }
        catch (Exception ex)
        {
            var errorMsg = $"Error approving TechnicalSheet with TBKT_ID: {tbktId}";
            _logger?.LogError(ex, errorMsg);
            var additionalInfo = $"TBKT_ID: {tbktId}, ApprovalLevel: {dto?.ApprovalLevel}, Action: {dto?.Action}";
            await _fileLogger.LogErrorAsync(errorMsg, ex, additionalInfo);
            return StatusCode(500, new { error = "Error approving TechnicalSheet", message = ex.Message });
        }
    }

    // Helper method để lấy user hiện tại từ FirebaseUID hoặc Email
    private async Task<Models.User?> GetCurrentUserAsync(string? userIdClaim)
    {
        if (string.IsNullOrEmpty(userIdClaim))
        {
            return null;
        }

        // Tìm user theo FirebaseUID
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.FirebaseUID == userIdClaim);

        if (user != null)
        {
            return user;
        }

        // Nếu không tìm thấy, thử tìm theo email từ claims
        var emailClaim = User?.FindFirst(ClaimTypes.Email)?.Value 
            ?? User?.FindFirst("email")?.Value;

        if (!string.IsNullOrEmpty(emailClaim))
        {
            user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == emailClaim);
        }

        return user;
    }
}

