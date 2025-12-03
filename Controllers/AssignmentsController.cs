using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using quanlyfilesBE.Data;
using quanlyfilesBE.Models;
using quanlyfilesBE.DTOs;

namespace quanlyfilesBE.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AssignmentsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AssignmentsController>? _logger;

    public AssignmentsController(ApplicationDbContext context, ILogger<AssignmentsController>? logger = null)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/assignments
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MachineAssignmentDto>>> GetAssignments()
    {
        try
        {
            var assignments = await _context.MachineAssignments
                .Include(a => a.AssignmentApprovals)
                .Include(a => a.WorkChanges)
                .Include(a => a.WorkItems)
                .Include(a => a.TechnicalSheet)
                .OrderByDescending(a => a.AssignmentID)
                .ToListAsync();

            var assignmentDtos = assignments.Select(a => new MachineAssignmentDto
            {
                AssignmentID = a.AssignmentID,
                TBKT_ID = a.TBKT_ID,
                MachineName = a.MachineName,
                StandardRequirement = a.StandardRequirement,
                AdditionalRequest = a.AdditionalRequest,
                DeliveryDate = a.DeliveryDate,
                Designer = a.Designer,
                TeamLeader = a.TeamLeader,
                FilePath = a.FilePath,
                Status = a.Status,
                AssignmentApprovals = a.AssignmentApprovals.Select(aa => new AssignmentApprovalDto
                {
                    ApprovalID = aa.ApprovalID,
                    AssignmentID = aa.AssignmentID,
                    ApproverRole = aa.ApproverRole,
                    ApproverName = aa.ApproverName,
                    ApprovalDate = aa.ApprovalDate,
                    Notes = aa.Notes
                }).ToList(),
                WorkChanges = a.WorkChanges.Select(wc => new WorkChangeDto
                {
                    ChangeID = wc.ChangeID,
                    AssignmentID = wc.AssignmentID,
                    ChangeType = wc.ChangeType,
                    Description = wc.Description
                }).ToList(),
                WorkItems = a.WorkItems.Select(wi => new WorkItemDto
                {
                    WorkItemID = wi.WorkItemID,
                    AssignmentID = wi.AssignmentID,
                    WorkType = wi.WorkType,
                    PersonName = wi.PersonName,
                    StartDate = wi.StartDate,
                    ExpectedFinish = wi.ExpectedFinish,
                    ActualFinish = wi.ActualFinish,
                    PersonConfirmation = wi.PersonConfirmation,
                    Notes = wi.Notes
                }).ToList()
            }).ToList();

            return Ok(assignmentDtos);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in GetAssignments: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error retrieving assignments", message = ex.Message });
        }
    }

    // GET: api/assignments/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<MachineAssignmentDto>> GetAssignment(int id)
    {
        try
        {
            var assignment = await _context.MachineAssignments
                .Include(a => a.AssignmentApprovals)
                .Include(a => a.WorkChanges)
                .Include(a => a.WorkItems)
                .Include(a => a.TechnicalSheet)
                .FirstOrDefaultAsync(a => a.AssignmentID == id);

            if (assignment == null)
            {
                return NotFound(new { error = "Assignment not found" });
            }

            var assignmentDto = new MachineAssignmentDto
            {
                AssignmentID = assignment.AssignmentID,
                TBKT_ID = assignment.TBKT_ID,
                MachineName = assignment.MachineName,
                StandardRequirement = assignment.StandardRequirement,
                AdditionalRequest = assignment.AdditionalRequest,
                DeliveryDate = assignment.DeliveryDate,
                Designer = assignment.Designer,
                TeamLeader = assignment.TeamLeader,
                FilePath = assignment.FilePath,
                Status = assignment.Status,
                AssignmentApprovals = assignment.AssignmentApprovals.Select(aa => new AssignmentApprovalDto
                {
                    ApprovalID = aa.ApprovalID,
                    AssignmentID = aa.AssignmentID,
                    ApproverRole = aa.ApproverRole,
                    ApproverName = aa.ApproverName,
                    ApprovalDate = aa.ApprovalDate,
                    Notes = aa.Notes
                }).ToList(),
                WorkChanges = assignment.WorkChanges.Select(wc => new WorkChangeDto
                {
                    ChangeID = wc.ChangeID,
                    AssignmentID = wc.AssignmentID,
                    ChangeType = wc.ChangeType,
                    Description = wc.Description
                }).ToList(),
                WorkItems = assignment.WorkItems.Select(wi => new WorkItemDto
                {
                    WorkItemID = wi.WorkItemID,
                    AssignmentID = wi.AssignmentID,
                    WorkType = wi.WorkType,
                    PersonName = wi.PersonName,
                    StartDate = wi.StartDate,
                    ExpectedFinish = wi.ExpectedFinish,
                    ActualFinish = wi.ActualFinish,
                    PersonConfirmation = wi.PersonConfirmation,
                    Notes = wi.Notes
                }).ToList()
            };

            return Ok(assignmentDto);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in GetAssignment: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error retrieving assignment", message = ex.Message });
        }
    }

    // POST: api/assignments
    [HttpPost]
    public async Task<ActionResult<MachineAssignmentDto>> CreateAssignment([FromBody] CreateMachineAssignmentDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(dto.TBKT_ID))
            {
                return BadRequest(new { error = "TBKT_ID is required" });
            }
            if (string.IsNullOrWhiteSpace(dto.MachineName))
            {
                return BadRequest(new { error = "MachineName is required" });
            }

            // Use transaction to ensure both TechnicalSheet and MachineAssignment are created atomically
            MachineAssignment assignment;
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Check if TechnicalSheet exists, if not create a basic one
                // Use FirstOrDefaultAsync instead of FindAsync for string keys
                var technicalSheet = await _context.TechnicalSheets
                    .FirstOrDefaultAsync(ts => ts.TBKT_ID == dto.TBKT_ID);
                
                if (technicalSheet == null)
                {
                    _logger?.LogInformation("Creating new TechnicalSheet with TBKT_ID: {TBKT_ID}", dto.TBKT_ID);
                    try
                    {
                        technicalSheet = new TechnicalSheet
                        {
                            TBKT_ID = dto.TBKT_ID
                        };
                        _context.TechnicalSheets.Add(technicalSheet);
                        // Save TechnicalSheet first to ensure it exists before creating MachineAssignment
                        await _context.SaveChangesAsync();
                        _logger?.LogInformation("TechnicalSheet created successfully with TBKT_ID: {TBKT_ID}", dto.TBKT_ID);
                    }
                    catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx) when (
                        dbEx.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx && 
                        (sqlEx.Number == 2627 || sqlEx.Number == 2601)) // Primary key or unique constraint violation
                    {
                        // TechnicalSheet was created by another request concurrently, query it again
                        _logger?.LogWarning("TechnicalSheet with TBKT_ID {TBKT_ID} was created concurrently, querying again...", dto.TBKT_ID);
                        // Remove the entity from context to avoid tracking conflicts
                        if (technicalSheet != null)
                        {
                            _context.Entry(technicalSheet).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                        }
                        // Query again
                        technicalSheet = await _context.TechnicalSheets
                            .FirstOrDefaultAsync(ts => ts.TBKT_ID == dto.TBKT_ID);
                        
                        if (technicalSheet == null)
                        {
                            // Still null after retry, this is unexpected
                            _logger?.LogError("TechnicalSheet with TBKT_ID {TBKT_ID} still not found after duplicate key error", dto.TBKT_ID);
                            throw new Exception($"Failed to create or retrieve TechnicalSheet with TBKT_ID '{dto.TBKT_ID}'. Duplicate key error occurred but record not found on retry.", dbEx);
                        }
                        _logger?.LogInformation("Successfully retrieved TechnicalSheet with TBKT_ID: {TBKT_ID} after concurrent creation", dto.TBKT_ID);
                    }
                }
                else
                {
                    _logger?.LogInformation("TechnicalSheet already exists with TBKT_ID: {TBKT_ID}", dto.TBKT_ID);
                }

                assignment = new MachineAssignment
                {
                    TBKT_ID = dto.TBKT_ID,
                    MachineName = dto.MachineName,
                    StandardRequirement = dto.StandardRequirement,
                    AdditionalRequest = dto.AdditionalRequest,
                    DeliveryDate = dto.DeliveryDate,
                    Designer = dto.Designer,
                    TeamLeader = dto.TeamLeader,
                    FilePath = dto.FilePath,
                    Status = dto.Status
                };

                _context.MachineAssignments.Add(assignment);
                _logger?.LogInformation("Adding MachineAssignment with TBKT_ID: {TBKT_ID}, MachineName: {MachineName}", dto.TBKT_ID, dto.MachineName);
                await _context.SaveChangesAsync();
                _logger?.LogInformation("MachineAssignment saved successfully with AssignmentID: {AssignmentID}", assignment.AssignmentID);
                
                // Commit transaction
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger?.LogError(ex, "Error in transaction, rolling back. Error: {Message}", ex.Message);
                throw;
            }

            // Reload with related data
            await _context.Entry(assignment)
                .Collection(a => a.AssignmentApprovals)
                .LoadAsync();
            await _context.Entry(assignment)
                .Collection(a => a.WorkChanges)
                .LoadAsync();
            await _context.Entry(assignment)
                .Collection(a => a.WorkItems)
                .LoadAsync();

            var assignmentDto = new MachineAssignmentDto
            {
                AssignmentID = assignment.AssignmentID,
                TBKT_ID = assignment.TBKT_ID,
                MachineName = assignment.MachineName,
                StandardRequirement = assignment.StandardRequirement,
                AdditionalRequest = assignment.AdditionalRequest,
                DeliveryDate = assignment.DeliveryDate,
                Designer = assignment.Designer,
                TeamLeader = assignment.TeamLeader,
                FilePath = assignment.FilePath,
                Status = assignment.Status,
                AssignmentApprovals = new List<AssignmentApprovalDto>(),
                WorkChanges = new List<WorkChangeDto>(),
                WorkItems = new List<WorkItemDto>()
            };

            return CreatedAtAction(nameof(GetAssignment), new { id = assignment.AssignmentID }, assignmentDto);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
        {
            // Extract inner exception message - this usually contains the real database error
            string innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;
            
            // Try to get more details from inner exception
            string fullDetails = innerMessage;
            if (dbEx.InnerException != null)
            {
                fullDetails = $"{innerMessage}\n\nInner Exception: {dbEx.InnerException.GetType().Name}\n{dbEx.InnerException.Message}";
                if (dbEx.InnerException.InnerException != null)
                {
                    fullDetails += $"\n\nNested Inner: {dbEx.InnerException.InnerException.Message}";
                }
            }
            
            _logger?.LogError(dbEx, "Database error in CreateAssignment: {Message}\nInner: {InnerMessage}\nFull: {FullDetails}", 
                dbEx.Message, innerMessage, fullDetails);
            
            return StatusCode(500, new { 
                error = "Database error creating assignment", 
                message = innerMessage,
                details = fullDetails,
                stackTrace = dbEx.StackTrace
            });
        }
        catch (Exception ex)
        {
            string innerMsg = ex.InnerException?.Message ?? string.Empty;
            _logger?.LogError(ex, "Error in CreateAssignment: {Message}\nInner: {InnerMessage}", ex.Message, innerMsg);
            return StatusCode(500, new { 
                error = "Error creating assignment", 
                message = ex.Message,
                innerException = innerMsg,
                details = ex.ToString()
            });
        }
    }

    // PUT: api/assignments/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAssignment(int id, [FromBody] UpdateMachineAssignmentDto dto)
    {
        try
        {
            var assignment = await _context.MachineAssignments.FindAsync(id);
            if (assignment == null)
            {
                return NotFound(new { error = "Assignment not found" });
            }

            if (!string.IsNullOrEmpty(dto.TBKT_ID))
                assignment.TBKT_ID = dto.TBKT_ID;
            if (!string.IsNullOrEmpty(dto.MachineName))
                assignment.MachineName = dto.MachineName;
            if (dto.StandardRequirement != null)
                assignment.StandardRequirement = dto.StandardRequirement;
            if (dto.AdditionalRequest != null)
                assignment.AdditionalRequest = dto.AdditionalRequest;
            if (dto.DeliveryDate.HasValue)
                assignment.DeliveryDate = dto.DeliveryDate;
            if (!string.IsNullOrEmpty(dto.Designer))
                assignment.Designer = dto.Designer;
            if (!string.IsNullOrEmpty(dto.TeamLeader))
                assignment.TeamLeader = dto.TeamLeader;
            if (dto.FilePath != null)
                assignment.FilePath = dto.FilePath;
            if (dto.Status.HasValue)
                assignment.Status = dto.Status.Value;

            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in UpdateAssignment: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error updating assignment", message = ex.Message });
        }
    }

    // DELETE: api/assignments/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAssignment(int id)
    {
        try
        {
            // Load assignment với các navigation properties
            var assignment = await _context.MachineAssignments
                .Include(a => a.AssignmentApprovals)
                .Include(a => a.WorkChanges)
                .Include(a => a.WorkItems)
                .FirstOrDefaultAsync(a => a.AssignmentID == id);

            if (assignment == null)
            {
                return NotFound(new { error = "Assignment not found" });
            }

            // Chỉ cho phép xóa khi status = 1 (new)
            int currentStatus = assignment.Status;
            if (currentStatus != 1)
            {
                string statusText = currentStatus switch
                {
                    2 => "đang xử lý",
                    3 => "hoàn thành",
                    _ => $"không xác định ({currentStatus})"
                };
                return BadRequest(new { error = "Cannot delete assignment", message = $"Chỉ có thể xóa giao việc ở trạng thái 'new' (status = 1). Giao việc đang ở trạng thái '{statusText}' (status = {currentStatus}) không thể xóa." });
            }

            // Xóa các Files liên quan trước (cần xóa file vật lý trên disk)
            var relatedFiles = await _context.Files
                .Where(f => f.AssignmentID == id)
                .ToListAsync();

            foreach (var file in relatedFiles)
            {
                // Xóa file vật lý trên disk
                if (System.IO.File.Exists(file.FilePath))
                {
                    try
                    {
                        System.IO.File.Delete(file.FilePath);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Could not delete physical file: {FilePath}", file.FilePath);
                        // Tiếp tục xóa record trong database dù không xóa được file vật lý
                    }
                }
            }

            // Xóa các bản ghi Files trong database và save changes ngay
            if (relatedFiles.Any())
            {
                _context.Files.RemoveRange(relatedFiles);
                await _context.SaveChangesAsync(); // Save changes để xóa files trước
            }

            // Xóa các bản ghi liên quan khác
            if (assignment.AssignmentApprovals.Any())
            {
                _context.AssignmentApprovals.RemoveRange(assignment.AssignmentApprovals);
            }
            if (assignment.WorkChanges.Any())
            {
                _context.WorkChanges.RemoveRange(assignment.WorkChanges);
            }
            if (assignment.WorkItems.Any())
            {
                _context.WorkItems.RemoveRange(assignment.WorkItems);
            }

            // Xóa assignment và save changes lần cuối
            _context.MachineAssignments.Remove(assignment);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
        {
            string innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;
            _logger?.LogError(dbEx, "Database error in DeleteAssignment for ID {Id}: {Message}\nInner: {InnerMessage}", id, dbEx.Message, innerMessage);
            
            // Trả về inner exception message để dễ debug
            return StatusCode(500, new { 
                error = "Database error deleting assignment", 
                message = innerMessage,
                fullException = dbEx.ToString()
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in DeleteAssignment for ID {Id}: {Message}", id, ex.Message);
            return StatusCode(500, new { 
                error = "Error deleting assignment", 
                message = ex.Message, 
                details = ex.ToString() 
            });
        }
    }

    // POST: api/assignments/{id}/work-changes
    [HttpPost("{id}/work-changes")]
    public async Task<ActionResult<WorkChangeDto>> AddWorkChange(int id, [FromBody] CreateWorkChangeDto dto)
    {
        try
        {
            var assignment = await _context.MachineAssignments.FindAsync(id);
            if (assignment == null)
            {
                return NotFound(new { error = "Assignment not found" });
            }

            var workChange = new WorkChange
            {
                AssignmentID = id,
                ChangeType = dto.ChangeType,
                Description = dto.Description
            };

            _context.WorkChanges.Add(workChange);
            await _context.SaveChangesAsync();

            var workChangeDto = new WorkChangeDto
            {
                ChangeID = workChange.ChangeID,
                AssignmentID = workChange.AssignmentID,
                ChangeType = workChange.ChangeType,
                Description = workChange.Description
            };

            return CreatedAtAction(nameof(GetAssignment), new { id = id }, workChangeDto);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in AddWorkChange: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error adding work change", message = ex.Message });
        }
    }

    // POST: api/assignments/{id}/work-items
    [HttpPost("{id}/work-items")]
    public async Task<ActionResult<WorkItemDto>> AddWorkItem(int id, [FromBody] CreateWorkItemDto dto)
    {
        try
        {
            var assignment = await _context.MachineAssignments.FindAsync(id);
            if (assignment == null)
            {
                return NotFound(new { error = "Assignment not found" });
            }

            var workItem = new WorkItem
            {
                AssignmentID = id,
                WorkType = dto.WorkType,
                PersonName = dto.PersonName,
                StartDate = dto.StartDate,
                ExpectedFinish = dto.ExpectedFinish,
                ActualFinish = dto.ActualFinish,
                PersonConfirmation = dto.PersonConfirmation,
                Notes = dto.Notes
            };

            _context.WorkItems.Add(workItem);
            await _context.SaveChangesAsync();

            var workItemDto = new WorkItemDto
            {
                WorkItemID = workItem.WorkItemID,
                AssignmentID = workItem.AssignmentID,
                WorkType = workItem.WorkType,
                PersonName = workItem.PersonName,
                StartDate = workItem.StartDate,
                ExpectedFinish = workItem.ExpectedFinish,
                ActualFinish = workItem.ActualFinish,
                PersonConfirmation = workItem.PersonConfirmation,
                Notes = workItem.Notes
            };

            return CreatedAtAction(nameof(GetAssignment), new { id = id }, workItemDto);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in AddWorkItem: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error adding work item", message = ex.Message });
        }
    }

    // POST: api/assignments/{id}/approvals
    [HttpPost("{id}/approvals")]
    public async Task<ActionResult<AssignmentApprovalDto>> AddApproval(int id, [FromBody] CreateAssignmentApprovalDto dto)
    {
        try
        {
            var assignment = await _context.MachineAssignments.FindAsync(id);
            if (assignment == null)
            {
                return NotFound(new { error = "Assignment not found" });
            }

            var approval = new AssignmentApproval
            {
                AssignmentID = id,
                ApproverRole = dto.ApproverRole,
                ApproverName = dto.ApproverName,
                ApprovalDate = dto.ApprovalDate ?? DateTime.Now,
                Notes = dto.Notes
            };

            _context.AssignmentApprovals.Add(approval);
            await _context.SaveChangesAsync();

            var approvalDto = new AssignmentApprovalDto
            {
                ApprovalID = approval.ApprovalID,
                AssignmentID = approval.AssignmentID,
                ApproverRole = approval.ApproverRole,
                ApproverName = approval.ApproverName,
                ApprovalDate = approval.ApprovalDate,
                Notes = approval.Notes
            };

            return CreatedAtAction(nameof(GetAssignment), new { id = id }, approvalDto);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in AddApproval: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error adding approval", message = ex.Message });
        }
    }

    // GET: api/assignments/my-work-items
    [HttpGet("my-work-items")]
    public async Task<ActionResult<IEnumerable<WorkItemWithAssignmentDto>>> GetMyWorkItems()
    {
        try
        {
            // Lấy UserId từ JWT token
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                ?? User.FindFirst("sub")?.Value;
            
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { error = "Invalid user token" });
            }

            // Lấy thông tin user từ database
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound(new { error = "User not found" });
            }

            // Tìm work items theo PersonName
            // PersonName có thể là: UserId (string), FullName, hoặc UserName
            var userIdString = userId.ToString();
            
            // Query work items using raw SQL to handle PersonConfirmation type conversion
            // This avoids InvalidCastException when database has string instead of bit
            var workItemsData = await _context.Database.SqlQueryRaw<WorkItemRawData>(
                @"SELECT 
                    wi.WorkItemID,
                    wi.AssignmentID,
                    wi.WorkType,
                    wi.PersonName,
                    wi.StartDate,
                    wi.ExpectedFinish,
                    wi.ActualFinish,
                    CAST(wi.PersonConfirmation AS NVARCHAR(10)) AS PersonConfirmationRaw,
                    wi.Notes
                  FROM WorkItem wi
                  WHERE wi.PersonName = {0} OR wi.PersonName = {1} OR wi.PersonName = {2}
                  ORDER BY wi.StartDate DESC",
                userIdString,
                user.FullName ?? "",
                user.UserName ?? "").ToListAsync();

            // Get assignment IDs to load MachineAssignments
            var assignmentIds = workItemsData.Select(w => w.AssignmentID).Distinct().ToList();
            var assignments = await _context.MachineAssignments
                .Where(ma => assignmentIds.Contains(ma.AssignmentID))
                .AsNoTracking()
                .ToListAsync();

            // Convert PersonConfirmation from string to bool
            bool? ConvertPersonConfirmation(string? rawValue)
            {
                if (string.IsNullOrWhiteSpace(rawValue))
                    return null;
                
                var trimmed = rawValue.Trim();
                if (bool.TryParse(trimmed, out bool boolValue))
                    return boolValue;
                
                if (trimmed.Equals("1", StringComparison.OrdinalIgnoreCase) || 
                    trimmed.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.Equals("yes", StringComparison.OrdinalIgnoreCase))
                    return true;
                
                if (trimmed.Equals("0", StringComparison.OrdinalIgnoreCase) || 
                    trimmed.Equals("false", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.Equals("no", StringComparison.OrdinalIgnoreCase))
                    return false;
                
                return null;
            }

            var workItemDtos = workItemsData.Select(wi => 
            {
                var assignment = assignments.FirstOrDefault(a => a.AssignmentID == wi.AssignmentID);
                
                return new WorkItemWithAssignmentDto
                {
                    WorkItemID = wi.WorkItemID,
                    AssignmentID = wi.AssignmentID,
                    WorkType = wi.WorkType,
                    PersonName = wi.PersonName,
                    StartDate = wi.StartDate,
                    ExpectedFinish = wi.ExpectedFinish,
                    ActualFinish = wi.ActualFinish,
                    PersonConfirmation = ConvertPersonConfirmation(wi.PersonConfirmationRaw),
                    Notes = wi.Notes,
                    Assignment = assignment != null ? new MachineAssignmentDto
                    {
                        AssignmentID = assignment.AssignmentID,
                        TBKT_ID = assignment.TBKT_ID,
                        MachineName = assignment.MachineName,
                        StandardRequirement = assignment.StandardRequirement,
                        AdditionalRequest = assignment.AdditionalRequest,
                        DeliveryDate = assignment.DeliveryDate,
                        Designer = assignment.Designer,
                        TeamLeader = assignment.TeamLeader,
                        FilePath = assignment.FilePath,
                        Status = assignment.Status
                    } : null
                };
            }).ToList();

            return Ok(workItemDtos);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in GetMyWorkItems: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error retrieving work items", message = ex.Message });
        }
    }
}

// Helper class for raw SQL query result to handle PersonConfirmation type conversion
internal class WorkItemRawData
{
    public int WorkItemID { get; set; }
    public int AssignmentID { get; set; }
    public string? WorkType { get; set; }
    public string? PersonName { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? ExpectedFinish { get; set; }
    public DateTime? ActualFinish { get; set; }
    public string? PersonConfirmationRaw { get; set; }
    public string? Notes { get; set; }
}

