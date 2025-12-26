using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using quanlyfilesBE.Models;
using quanlyfilesBE.DTOs;
using quanlyfilesBE.Data;
using quanlyfilesBE.Services;
using System.Linq;
using System.Security.Claims;

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

    // GET: api/technical-sheets?firebaseUID={firebaseUID}
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TechnicalSheetDto>>> GetAll([FromQuery] string? firebaseUID = null)
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
            
            // Kiểm tra xem user có phải admin hoặc manager không
            var isAdminOrManager = RoleHelper.IsAdministratorOrManager(User);
            var userRoles = User?.Claims?.Where(c => c.Type == ClaimTypes.Role || c.Type == "role" || c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                .Select(c => c.Value)
                .ToList() ?? new List<string>();
            _logger?.LogInformation("GetAll: IsAdminOrManager: {IsAdminOrManager}, UserRoles: {UserRoles}", 
                isAdminOrManager, string.Join(", ", userRoles));
            
            // Lấy FirebaseUID từ token của user hiện tại
            var currentUserFirebaseUID = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? User?.FindFirst("sub")?.Value;
            
            string? filterFirebaseUID = null;
            
            // Nếu có firebaseUID parameter
            if (!string.IsNullOrEmpty(firebaseUID))
            {
                // Nếu không phải admin/manager, chỉ cho phép filter theo firebaseUID của chính họ
                if (!isAdminOrManager)
                {
                    // So sánh firebaseUID từ parameter với firebaseUID từ token
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
                }
                
                filterFirebaseUID = firebaseUID;
                _logger?.LogInformation("GetAll: Filtering by firebaseUID parameter: {FirebaseUID}", filterFirebaseUID);
            }
            else
            {
                // Nếu không có firebaseUID parameter, lấy từ user hiện tại
                if (!isAdminOrManager)
                {
                    if (string.IsNullOrEmpty(currentUserFirebaseUID))
                    {
                        _logger?.LogWarning("GetAll: User not found in token. Returning empty list.");
                        return Ok(new List<TechnicalSheetDto>());
                    }
                    
                    filterFirebaseUID = currentUserFirebaseUID;
                    _logger?.LogInformation("GetAll: No firebaseUID parameter, using current user FirebaseUID: {FirebaseUID}", filterFirebaseUID);
                }
                else
                {
                    _logger?.LogInformation("GetAll: User is Admin/Manager, no firebaseUID parameter, will return all TechnicalSheets");
                }
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
                    // Admin/Manager: trả về tất cả TechnicalSheets (bao gồm cả Proposer = NULL)
                    _logger?.LogInformation("GetAll: User is Admin/Manager, returning all TechnicalSheets (including NULL Proposer)");
                }
                
                sheets = await query
                    .OrderBy(x => x.TBKT_ID)
                    .ToListAsync();
                
                _logger?.LogInformation("GetAll: Retrieved {Count} TechnicalSheets from database", sheets.Count);
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
                    if (!string.IsNullOrEmpty(filterFirebaseUID))
                    {
                        var trimmedFilterUID = filterFirebaseUID.Trim().Replace("'", "''");
                        whereClause += $" AND LTRIM(RTRIM(Proposer)) = '{trimmedFilterUID}'";
                        _logger?.LogInformation("GetAll (SQL fallback): Filtering by Proposer (FirebaseUID) = {FirebaseUID} (trimmed)", trimmedFilterUID);
                    }
                    else if (!isAdminOrManager)
                    {
                        // Nếu không phải admin/manager và không có filterFirebaseUID, trả về danh sách rỗng
                        _logger?.LogWarning("GetAll (SQL fallback): User is not admin/manager and no firebaseUID provided. Returning empty list.");
                        return Ok(new List<TechnicalSheetDto>());
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
                        RequesterMechanical = s.RequesterMechanical
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
            
            var dto = new TechnicalSheetDto
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
                RequesterMechanical = sheet.RequesterMechanical
            };
            
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

            var resultDto = new TechnicalSheetDto
            {
                TBKT_ID = newSheet.TBKT_ID,
                Power_kVA = newSheet.Power_kVA,
                VoltageSpec = newSheet.VoltageSpec,
                Phase = newSheet.Phase,
                StandardCode = newSheet.StandardCode,
                Proposer = newSheet.Proposer,
                DeliveryDate = newSheet.DeliveryDate,
                DrawingDate = newSheet.DrawingDate,
                Notes = newSheet.Notes,
                SalesOrder = newSheet.SalesOrder,
                HandOverDate = newSheet.HandOverDate,
                ArchivedDate = newSheet.ArchivedDate,
                RequesterElectrical = newSheet.RequesterElectrical,
                RequesterMechanical = newSheet.RequesterMechanical
            };

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

            var resultDto = new TechnicalSheetDto
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
                RequesterMechanical = sheet.RequesterMechanical
            };

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
            
            User? user = null;
            
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

    // Helper method để lấy user hiện tại từ FirebaseUID hoặc Email
    private async Task<User?> GetCurrentUserAsync(string? userIdClaim)
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

