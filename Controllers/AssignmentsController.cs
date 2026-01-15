using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using quanlyfilesBE.Data;
using quanlyfilesBE.Models;
using quanlyfilesBE.DTOs;
using quanlyfilesBE.Services;
using quanlyfilesBE.Hubs;

namespace quanlyfilesBE.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AssignmentsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IPowerAutomateService? _powerAutomateService;
    private readonly IHubContext<NotificationHub>? _hubContext;
    private readonly ILogger<AssignmentsController>? _logger;

    public AssignmentsController(
        ApplicationDbContext context, 
        IPowerAutomateService? powerAutomateService = null,
        IHubContext<NotificationHub>? hubContext = null,
        ILogger<AssignmentsController>? logger = null)
    {
        _context = context;
        _powerAutomateService = powerAutomateService;
        _hubContext = hubContext;
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
                RequestDocument = a.RequestDocument,
                StandardRequirement = a.StandardRequirement,
                AdditionalRequest = a.AdditionalRequest,
                DeliveryDate = a.DeliveryDate,
                Designer = a.Designer,
                TeamLeader = a.TeamLeader,
                FilePath = a.FilePath,
                Status = a.Status,
                IsLocked = a.IsLocked,
                TechnicalSheet = a.TechnicalSheet != null ? new TechnicalSheetDto
                {
                    TBKT_ID = a.TechnicalSheet.TBKT_ID,
                    Power_kVA = a.TechnicalSheet.Power_kVA,
                    VoltageSpec = a.TechnicalSheet.VoltageSpec,
                    Phase = a.TechnicalSheet.Phase,
                    StandardCode = a.TechnicalSheet.StandardCode,
                    Proposer = a.TechnicalSheet.Proposer,
                    DeliveryDate = a.TechnicalSheet.DeliveryDate,
                    DrawingDate = a.TechnicalSheet.DrawingDate,
                    Notes = a.TechnicalSheet.Notes,
                    SalesOrder = a.TechnicalSheet.SalesOrder,
                    HandOverDate = a.TechnicalSheet.HandOverDate,
                    ArchivedDate = a.TechnicalSheet.ArchivedDate,
                    RequesterElectrical = a.TechnicalSheet.RequesterElectrical,
                    RequesterMechanical = a.TechnicalSheet.RequesterMechanical
                } : null,
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
                    Notes = wi.Notes,
                    File_ID = wi.File_ID
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
                RequestDocument = assignment.RequestDocument,
                StandardRequirement = assignment.StandardRequirement,
                AdditionalRequest = assignment.AdditionalRequest,
                DeliveryDate = assignment.DeliveryDate,
                Designer = assignment.Designer,
                TeamLeader = assignment.TeamLeader,
                FilePath = assignment.FilePath,
                Status = assignment.Status,
                IsLocked = assignment.IsLocked,
                TechnicalSheet = assignment.TechnicalSheet != null ? new TechnicalSheetDto
                {
                    TBKT_ID = assignment.TechnicalSheet.TBKT_ID,
                    Power_kVA = assignment.TechnicalSheet.Power_kVA,
                    VoltageSpec = assignment.TechnicalSheet.VoltageSpec,
                    Phase = assignment.TechnicalSheet.Phase,
                    StandardCode = assignment.TechnicalSheet.StandardCode,
                    Proposer = assignment.TechnicalSheet.Proposer,
                    DeliveryDate = assignment.TechnicalSheet.DeliveryDate,
                    DrawingDate = assignment.TechnicalSheet.DrawingDate,
                    Notes = assignment.TechnicalSheet.Notes,
                    SalesOrder = assignment.TechnicalSheet.SalesOrder,
                    HandOverDate = assignment.TechnicalSheet.HandOverDate,
                    ArchivedDate = assignment.TechnicalSheet.ArchivedDate,
                    RequesterElectrical = assignment.TechnicalSheet.RequesterElectrical,
                    RequesterMechanical = assignment.TechnicalSheet.RequesterMechanical
                } : null,
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
                    Notes = wi.Notes,
                    File_ID = wi.File_ID
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

            // Use execution strategy to support retries with transaction
            MachineAssignment assignment = new();
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
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
                                TBKT_ID = dto.TBKT_ID,
                                Power_kVA = dto.TechnicalSheet?.Power_kVA,
                                VoltageSpec = dto.TechnicalSheet?.VoltageSpec,
                                Phase = dto.TechnicalSheet?.Phase,
                                StandardCode = dto.TechnicalSheet?.StandardCode,
                                Proposer = dto.TechnicalSheet?.Proposer,
                                DeliveryDate = dto.TechnicalSheet?.DeliveryDate ?? dto.DeliveryDate,
                                DrawingDate = dto.TechnicalSheet?.DrawingDate,
                                SalesOrder = dto.TechnicalSheet?.SalesOrder,
                                HandOverDate = dto.TechnicalSheet?.HandOverDate,
                                ArchivedDate = dto.TechnicalSheet?.ArchivedDate,
                                RequesterElectrical = dto.TechnicalSheet?.RequesterElectrical,
                                RequesterMechanical = dto.TechnicalSheet?.RequesterMechanical,
                                Notes = dto.TechnicalSheet?.Notes
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
                        _logger?.LogInformation("TechnicalSheet already exists with TBKT_ID: {TBKT_ID}, updating fields", dto.TBKT_ID);
                        // Update TechnicalSheet fields if provided
                        bool hasUpdates = false;
                        if (dto.TechnicalSheet != null)
                        {
                            if (dto.TechnicalSheet.Power_kVA.HasValue)
                            {
                                technicalSheet.Power_kVA = dto.TechnicalSheet.Power_kVA;
                                hasUpdates = true;
                            }
                            if (!string.IsNullOrEmpty(dto.TechnicalSheet.VoltageSpec))
                            {
                                technicalSheet.VoltageSpec = dto.TechnicalSheet.VoltageSpec;
                                hasUpdates = true;
                            }
                            if (dto.TechnicalSheet.Phase.HasValue)
                            {
                                technicalSheet.Phase = dto.TechnicalSheet.Phase;
                                hasUpdates = true;
                            }
                            if (!string.IsNullOrEmpty(dto.TechnicalSheet.StandardCode))
                            {
                                technicalSheet.StandardCode = dto.TechnicalSheet.StandardCode;
                                hasUpdates = true;
                            }
                            if (!string.IsNullOrEmpty(dto.TechnicalSheet.Proposer))
                            {
                                technicalSheet.Proposer = dto.TechnicalSheet.Proposer;
                                hasUpdates = true;
                            }
                            if (dto.TechnicalSheet.DeliveryDate.HasValue)
                            {
                                technicalSheet.DeliveryDate = dto.TechnicalSheet.DeliveryDate;
                                hasUpdates = true;
                            }
                            if (dto.TechnicalSheet.DrawingDate.HasValue)
                            {
                                technicalSheet.DrawingDate = dto.TechnicalSheet.DrawingDate;
                                hasUpdates = true;
                            }
                            if (!string.IsNullOrEmpty(dto.TechnicalSheet.SalesOrder))
                            {
                                technicalSheet.SalesOrder = dto.TechnicalSheet.SalesOrder;
                                hasUpdates = true;
                            }
                            if (dto.TechnicalSheet.HandOverDate.HasValue)
                            {
                                technicalSheet.HandOverDate = dto.TechnicalSheet.HandOverDate;
                                hasUpdates = true;
                            }
                            if (dto.TechnicalSheet.ArchivedDate.HasValue)
                            {
                                technicalSheet.ArchivedDate = dto.TechnicalSheet.ArchivedDate;
                                hasUpdates = true;
                            }
                            if (!string.IsNullOrEmpty(dto.TechnicalSheet.RequesterElectrical))
                            {
                                technicalSheet.RequesterElectrical = dto.TechnicalSheet.RequesterElectrical;
                                hasUpdates = true;
                            }
                            if (!string.IsNullOrEmpty(dto.TechnicalSheet.RequesterMechanical))
                            {
                                technicalSheet.RequesterMechanical = dto.TechnicalSheet.RequesterMechanical;
                                hasUpdates = true;
                            }
                            if (!string.IsNullOrEmpty(dto.TechnicalSheet.Notes))
                            {
                                technicalSheet.Notes = dto.TechnicalSheet.Notes;
                                hasUpdates = true;
                            }
                        }
                        
                        // Also update DeliveryDate from assignment if TechnicalSheet DeliveryDate is null and assignment has DeliveryDate
                        if (!technicalSheet.DeliveryDate.HasValue && dto.DeliveryDate.HasValue)
                        {
                            technicalSheet.DeliveryDate = dto.DeliveryDate;
                            hasUpdates = true;
                        }
                        
                        // Save TechnicalSheet updates if any
                        if (hasUpdates)
                        {
                            await _context.SaveChangesAsync();
                            _logger?.LogInformation("TechnicalSheet updated successfully with TBKT_ID: {TBKT_ID}", dto.TBKT_ID);
                        }
                    }

                    assignment = new MachineAssignment
                    {
                        TBKT_ID = dto.TBKT_ID,
                        MachineName = dto.MachineName,
                        RequestDocument = dto.RequestDocument,
                        StandardRequirement = dto.StandardRequirement,
                        AdditionalRequest = dto.AdditionalRequest,
                        DeliveryDate = dto.DeliveryDate,
                        Designer = dto.Designer,
                        TeamLeader = dto.TeamLeader,
                        FilePath = dto.FilePath,
                        // Đảm bảo status mặc định là 1 (new) nếu không được set hoặc là 0
                        Status = dto.Status > 0 ? dto.Status : 1
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
            });

            // Reload with related data
            await _context.Entry(assignment)
                .Reference(a => a.TechnicalSheet)
                .LoadAsync();
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
                RequestDocument = assignment.RequestDocument,
                StandardRequirement = assignment.StandardRequirement,
                AdditionalRequest = assignment.AdditionalRequest,
                DeliveryDate = assignment.DeliveryDate,
                Designer = assignment.Designer,
                TeamLeader = assignment.TeamLeader,
                FilePath = assignment.FilePath,
                Status = assignment.Status,
                IsLocked = assignment.IsLocked,
                TechnicalSheet = assignment.TechnicalSheet != null ? new TechnicalSheetDto
                {
                    TBKT_ID = assignment.TechnicalSheet.TBKT_ID,
                    Power_kVA = assignment.TechnicalSheet.Power_kVA,
                    VoltageSpec = assignment.TechnicalSheet.VoltageSpec,
                    Phase = assignment.TechnicalSheet.Phase,
                    StandardCode = assignment.TechnicalSheet.StandardCode,
                    Proposer = assignment.TechnicalSheet.Proposer,
                    DeliveryDate = assignment.TechnicalSheet.DeliveryDate,
                    DrawingDate = assignment.TechnicalSheet.DrawingDate,
                    Notes = assignment.TechnicalSheet.Notes,
                    SalesOrder = assignment.TechnicalSheet.SalesOrder,
                    HandOverDate = assignment.TechnicalSheet.HandOverDate,
                    ArchivedDate = assignment.TechnicalSheet.ArchivedDate,
                    RequesterElectrical = assignment.TechnicalSheet.RequesterElectrical,
                    RequesterMechanical = assignment.TechnicalSheet.RequesterMechanical
                } : null,
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
            if (dto.RequestDocument != null)
                assignment.RequestDocument = dto.RequestDocument;
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

            // Chỉ cho phép xóa khi status = 0 hoặc 1 (new/trạng thái mới)
            int currentStatus = assignment.Status;
            if (currentStatus != 0 && currentStatus != 1)
            {
                string statusText = currentStatus switch
                {
                    2 => "đang xử lý",
                    3 => "hoàn thành",
                    _ => $"không xác định ({currentStatus})"
                };
                return BadRequest(new { error = "Cannot delete assignment", message = $"Chỉ có thể xóa giao việc ở trạng thái 'new' (status = 0 hoặc 1). Giao việc đang ở trạng thái '{statusText}' (status = {currentStatus}) không thể xóa." });
            }

            // Kiểm tra xem có work item nào đã được cập nhật chưa
            // Work item được coi là đã cập nhật nếu có bất kỳ trường nào: StartDate, ExpectedFinish, ActualFinish, PersonConfirmation, Notes, File_ID
            bool hasUpdatedWorkItems = assignment.WorkItems.Any(wi => 
                wi.StartDate.HasValue || 
                wi.ExpectedFinish.HasValue || 
                wi.ActualFinish.HasValue || 
                wi.PersonConfirmation.HasValue || 
                !string.IsNullOrWhiteSpace(wi.Notes) || 
                !string.IsNullOrWhiteSpace(wi.File_ID)
            );

            if (hasUpdatedWorkItems)
            {
                return BadRequest(new { error = "Cannot delete assignment", message = "Không thể xóa giao việc này vì đã có công việc con được cập nhật (đã có ngày bắt đầu, ngày hoàn thành dự kiến, ngày hoàn thành thực tế, xác nhận, ghi chú hoặc file đính kèm). Chỉ có thể xóa giao việc mới chưa có công việc con nào được cập nhật." });
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
                // File_ID will be set when user uploads files for this work item
            };

            _context.WorkItems.Add(workItem);
            await _context.SaveChangesAsync();

            // Cập nhật trạng thái tổng của assignment dựa trên tất cả work items
            await AssignmentStatusHelper.UpdateAssignmentStatusAsync(
                _context, 
                id, 
                _logger);

            // Handle notification for assigned user
            try
            {
                await HandleWorkItemAssignmentNotificationAsync(workItem, assignment);
            }
            catch (Exception notifEx)
            {
                // Log but don't fail the work item creation if notification fails
                _logger?.LogWarning(notifEx, "Failed to send notification for work item assignment: {Message}", notifEx.Message);
            }

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
                Notes = workItem.Notes,
                File_ID = workItem.File_ID
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
            // Kiểm tra nếu user là admin hoặc manager - có quyền xem tất cả work items
            // Chỉ Manager và ManagerL1 mới thấy tất cả, ManagerL2 trở đi chỉ thấy workitems của họ
            var isAdmin = RoleHelper.IsAdministrator(User);
            var isManager = RoleHelper.IsManager(User);
            
            // Kiểm tra xem có phải Manager hoặc ManagerL1 không (không bao gồm ManagerL2, ManagerL3, etc.)
            bool isManagerOrManagerL1 = false;
            if (isManager)
            {
                var roleClaims = User.Claims
                    .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role || 
                               c.Type == "role" || 
                               c.Type == "roles" ||
                               c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" ||
                               c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role")
                    .Select(c => c.Value)
                    .ToList();
                
                // Chỉ Manager hoặc ManagerL1 mới có quyền xem tất cả
                isManagerOrManagerL1 = roleClaims.Any(role => 
                    role != null && 
                    (role.Equals("Manager", StringComparison.OrdinalIgnoreCase) || 
                     role.Equals("ManagerL1", StringComparison.OrdinalIgnoreCase)));
            }
            
            var canViewAll = isAdmin || isManagerOrManagerL1;
            
            // Lấy FirebaseUID từ JWT token
            var firebaseUID = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                ?? User.FindFirst("sub")?.Value;
            
            if (string.IsNullOrEmpty(firebaseUID))
            {
                return Unauthorized(new { error = "Invalid user token" });
            }

            // Lấy thông tin user từ database bằng FirebaseUID
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.FirebaseUID == firebaseUID);
            
            if (user == null)
            {
                // Nếu không tìm thấy theo FirebaseUID, thử tìm theo email
                var emailClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value 
                                ?? User.FindFirst("email")?.Value;
                
                if (!string.IsNullOrEmpty(emailClaim))
                {
                    user = await _context.Users
                        .FirstOrDefaultAsync(u => u.Email == emailClaim);
                }
                
                if (user == null)
                {
                    return NotFound(new { error = "User not found" });
                }
            }

            List<WorkItemRawData> workItemsData;
            
            if (canViewAll)
            {
                // Admin và Manager có quyền xem tất cả work items (để có thể unlock assignments)
                workItemsData = await _context.Database.SqlQueryRaw<WorkItemRawData>(
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
                      ORDER BY wi.StartDate DESC").ToListAsync();
            }
            else
            {
                // User thường (không phải Manager/Admin) chỉ thấy workitems của họ
                // Bao gồm tất cả workitem types: Review, Design, Material Leveling, và các loại khác
                var userIdString = user.UserId.ToString();
                
                // Query work items using raw SQL to handle PersonConfirmation type conversion
                // This avoids InvalidCastException when database has string instead of bit
                // PersonName có thể là số (UserId) hoặc string (UserName/FullName)
                // Lấy tất cả workitems có PersonName match (không filter theo WorkType)
                workItemsData = await _context.Database.SqlQueryRaw<WorkItemRawData>(
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
                      WHERE (
                        (ISNUMERIC(wi.PersonName) = 1 AND CAST(wi.PersonName AS INT) = {3})
                        OR CAST(wi.PersonName AS NVARCHAR(50)) = {0}
                        OR CAST(wi.PersonName AS NVARCHAR(50)) = {1}
                        OR CAST(wi.PersonName AS NVARCHAR(50)) = {2}
                      )
                      ORDER BY wi.StartDate DESC",
                    userIdString,
                    user.FullName ?? "",
                    user.UserName ?? "",
                    user.UserId).ToListAsync();
                
                _logger?.LogInformation("GetMyWorkItems: Found {Count} workitems for user {UserId}", workItemsData.Count, user.UserId);
            }

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
                        RequestDocument = assignment.RequestDocument,
                        StandardRequirement = assignment.StandardRequirement,
                        AdditionalRequest = assignment.AdditionalRequest,
                        DeliveryDate = assignment.DeliveryDate,
                        Designer = assignment.Designer,
                        TeamLeader = assignment.TeamLeader,
                        FilePath = assignment.FilePath,
                        Status = assignment.Status,
                        IsLocked = assignment.IsLocked
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

    private async Task HandleWorkItemAssignmentNotificationAsync(WorkItem workItem, MachineAssignment assignment)
    {
        if (string.IsNullOrWhiteSpace(workItem.PersonName))
        {
            _logger?.LogWarning("WorkItem {WorkItemID} has no PersonName, skipping notification", workItem.WorkItemID);
            return;
        }

        // Find user by PersonName (could be UserName, FullName, or UserId)
        var user = await _context.Users
            .FirstOrDefaultAsync(u => 
                u.UserName == workItem.PersonName || 
                u.FullName == workItem.PersonName ||
                u.UserId.ToString() == workItem.PersonName);

        if (user == null || string.IsNullOrEmpty(user.FirebaseUID))
        {
            _logger?.LogWarning("User not found for PersonName: {PersonName}, skipping notification", workItem.PersonName);
            return;
        }

        // Get notification preference from settings
        var notificationSetting = await _context.Settings
            .FirstOrDefaultAsync(s => s.Key == "send-email-notifications");
        
        var sendEmailNotifications = true; // Default to email
        if (notificationSetting != null)
        {
            bool.TryParse(notificationSetting.Value, out sendEmailNotifications);
        }

        if (sendEmailNotifications)
        {
            // Send email notification
            if (_powerAutomateService != null && !string.IsNullOrEmpty(user.Email))
            {
                var subject = $"Bạn đã được giao công việc mới: {assignment.MachineName}";
                var body = $@"
                    <h2>Bạn đã được giao công việc mới</h2>
                    <p><strong>Tên máy:</strong> {assignment.MachineName}</p>
                    <p><strong>Loại công việc:</strong> {workItem.WorkType}</p>
                    <p><strong>Ngày bắt đầu:</strong> {workItem.StartDate:dd/MM/yyyy}</p>
                    <p><strong>Dự kiến hoàn thành:</strong> {workItem.ExpectedFinish:dd/MM/yyyy}</p>
                    {(string.IsNullOrEmpty(workItem.Notes) ? "" : $"<p><strong>Ghi chú:</strong> {workItem.Notes}</p>")}
                ";

                // Create a minimal ApprovalWorkflowDto for email
                var workflowDto = new ApprovalWorkflowDto
                {
                    WorkflowID = 0,
                    RequestTitle = $"Công việc mới: {assignment.MachineName}",
                    RequesterName = workItem.PersonName,
                    RequesterEmail = user.Email
                };

                await _powerAutomateService.SendNotificationEmailAsync(
                    user.Email,
                    subject,
                    body,
                    workflowDto);
            }
        }
        else
        {
            // Get TBKT_ID from assignment
            var assignmentWithTBKT = await _context.MachineAssignments
                .Include(a => a.TechnicalSheet)
                .FirstOrDefaultAsync(a => a.AssignmentID == assignment.AssignmentID);
            
            var tbktId = assignmentWithTBKT?.TechnicalSheet?.TBKT_ID ?? "N/A";
            
            // Create notification badge - chỉ hiển thị TBKT_ID
            var notification = new Notification
            {
                UserId = user.FirebaseUID,
                Title = "Bạn đã được giao công việc mới",
                Message = tbktId,
                Type = "info",
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                RelatedEntityType = "WorkItem",
                RelatedEntityId = workItem.WorkItemID
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            _logger?.LogInformation("Created notification for user {FirebaseUID} about work item {WorkItemID}", user.FirebaseUID, workItem.WorkItemID);
            
            // Gửi SignalR notification
            if (_hubContext != null)
            {
                await _hubContext.Clients.Group($"user_{user.FirebaseUID}").SendAsync("NewNotification", new 
                { 
                    id = notification.Id,
                    title = notification.Title,
                    message = notification.Message,
                    type = notification.Type,
                    createdAt = notification.CreatedAt
                });
                await _hubContext.Clients.Group($"user_{user.FirebaseUID}").SendAsync("UnreadCountChanged");
            }
        }
    }

    // PUT: api/assignments/{id}/unlock
    // Mở khóa assignment để cho phép user thiết kế update workitem
    // Cho phép Manager/Admin hoặc user kiểm soát (người đã xác nhận review workitem)
    [HttpPut("{id}/unlock")]
    [Authorize]
    public async Task<IActionResult> UnlockAssignment(int id)
    {
        try
        {
            var assignment = await _context.MachineAssignments
                .Include(a => a.WorkItems)
                .FirstOrDefaultAsync(a => a.AssignmentID == id);

            if (assignment == null)
            {
                return NotFound(new { error = "Assignment not found", assignmentID = id });
            }

            if (!assignment.IsLocked)
            {
                return BadRequest(new { error = "Assignment is not locked", message = "Assignment này chưa bị khóa." });
            }

            // Kiểm tra quyền: Manager/Admin hoặc user kiểm soát đã xác nhận
            var isManagerOrAdmin = RoleHelper.IsAdministratorOrManager(User);
            var hasUnlockPermission = isManagerOrAdmin;

            // Nếu không phải Manager/Admin, kiểm tra xem có phải user kiểm soát đã xác nhận không
            if (!hasUnlockPermission)
            {
                // Lấy FirebaseUID từ JWT token
                var firebaseUID = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                    ?? User.FindFirst("sub")?.Value;
                
                if (!string.IsNullOrEmpty(firebaseUID))
                {
                    // Lấy thông tin user từ database bằng FirebaseUID
                    var user = await _context.Users
                        .FirstOrDefaultAsync(u => u.FirebaseUID == firebaseUID);
                    
                    if (user != null)
                    {
                        // PersonName có thể là: UserId (string), FullName, hoặc UserName
                        var userIdString = user.UserId.ToString();
                        var fullName = user.FullName ?? "";
                        var userName = user.UserName ?? "";
                        
                        // Kiểm tra xem user có phải là người được gán review workitem (Core Review hoặc Casing Review) không
                        // VÀ design workitem tương ứng đã được xác nhận
                        var userReviewWorkItems = assignment.WorkItems?.Where(wi => 
                            (wi.WorkType == "Core Review" || wi.WorkType == "Casing Review") &&
                            (wi.PersonName == userIdString || 
                             wi.PersonName == fullName ||
                             wi.PersonName == userName)
                        ).ToList();

                        if (userReviewWorkItems != null && userReviewWorkItems.Any())
                        {
                            // Kiểm tra xem design workitem tương ứng đã được xác nhận chưa
                            foreach (var reviewWorkItem in userReviewWorkItems)
                            {
                                string? designWorkType = null;
                                if (reviewWorkItem.WorkType == "Core Review")
                                {
                                    designWorkType = "Core Design";
                                }
                                else if (reviewWorkItem.WorkType == "Casing Review")
                                {
                                    designWorkType = "Casing Design";
                                }

                                if (!string.IsNullOrEmpty(designWorkType))
                                {
                                    // Tìm design workitem tương ứng
                                    var designWorkItem = assignment.WorkItems?.FirstOrDefault(wi =>
                                        wi.WorkType == designWorkType);

                                    if (designWorkItem != null)
                                    {
                                        // Kiểm tra design workitem đã được xác nhận chưa
                                        var isDesignConfirmed = designWorkItem.PersonConfirmation == true ||
                                                                 (designWorkItem.PersonConfirmation.HasValue && 
                                                                  designWorkItem.PersonConfirmation.Value);

                                        if (isDesignConfirmed)
                                        {
                                            hasUnlockPermission = true;
                                            _logger?.LogInformation("User {UserId} ({FullName}) has unlock permission as review workitem user for assignment {AssignmentID} (design workitem {DesignWorkItemID} is confirmed)", 
                                                user.UserId, user.FullName, id, designWorkItem.WorkItemID);
                                            break; // Đã tìm thấy permission, không cần kiểm tra tiếp
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (!hasUnlockPermission)
            {
                return StatusCode(403, new { error = "Forbidden", message = "Bạn không có quyền mở khóa assignment này. Chỉ Manager/Admin hoặc user kiểm soát đã xác nhận mới có quyền mở khóa." });
            }

            // Reset personConfirmation của workitem thiết kế (Core Design hoặc Casing Design) về false
            // Để user thiết kế có thể chỉnh sửa lại sau khi mở khóa
            // CHỈ reset workitems của user thiết kế tương ứng, không reset tất cả users
            // Đồng thời reset personConfirmation của workitem kiểm soát (Core Review hoặc Casing Review) về false
            // CHỈ reset review workitems tương ứng với design workitems đã được reset
            if (assignment.WorkItems != null && assignment.WorkItems.Any())
            {
                // Lấy danh sách PersonName của các design workitems (user thiết kế) đã được xác nhận
                // PersonName có thể là UserId (string), FullName, hoặc UserName
                var designWorkItemPersonNames = assignment.WorkItems
                    .Where(wi => wi != null 
                                 && (wi.WorkType == "Core Design" || wi.WorkType == "Casing Design")
                                 && wi.PersonConfirmation == true
                                 && !string.IsNullOrEmpty(wi.PersonName))
                    .Select(wi => wi.PersonName!)
                    .Distinct()
                    .ToList();
                
                if (designWorkItemPersonNames.Any())
                {
                    _logger?.LogInformation("Unlocking assignment {AssignmentID}: Found {Count} design user(s) to reset: {PersonNames}", 
                        id, designWorkItemPersonNames.Count, string.Join(", ", designWorkItemPersonNames));
                    
                    // Reset workitem thiết kế - CHỈ reset workitems của user thiết kế tương ứng
                    var designWorkItems = assignment.WorkItems
                        .Where(wi => wi != null
                                     && (wi.WorkType == "Core Design" || wi.WorkType == "Casing Design")
                                     && wi.PersonConfirmation == true
                                     && !string.IsNullOrEmpty(wi.PersonName)
                                     && designWorkItemPersonNames.Contains(wi.PersonName))
                        .ToList();
                    
                    foreach (var designWorkItem in designWorkItems)
                    {
                        if (designWorkItem != null)
                        {
                            designWorkItem.PersonConfirmation = false;
                            _logger?.LogInformation("Reset personConfirmation to false for design workitem {WorkItemID} (WorkType: {WorkType}, PersonName: {PersonName}) when unlocking assignment {AssignmentID}", 
                                designWorkItem.WorkItemID, designWorkItem.WorkType, designWorkItem.PersonName, id);
                        }
                    }
                    
                    // Reset workitem kiểm soát - CHỈ reset review workitems tương ứng với design workitems đã reset
                    // Tìm review workitems có cùng PersonName với design workitems đã được reset
                    var reviewWorkItems = assignment.WorkItems
                        .Where(wi => wi != null
                                     && (wi.WorkType == "Core Review" || wi.WorkType == "Casing Review")
                                     && wi.PersonConfirmation == true
                                     && !string.IsNullOrEmpty(wi.PersonName)
                                     && designWorkItemPersonNames.Contains(wi.PersonName))
                        .ToList();
                    
                    foreach (var reviewWorkItem in reviewWorkItems)
                    {
                        if (reviewWorkItem != null)
                        {
                            reviewWorkItem.PersonConfirmation = false;
                            _logger?.LogInformation("Reset personConfirmation to false for review workitem {WorkItemID} (WorkType: {WorkType}, PersonName: {PersonName}) when unlocking assignment {AssignmentID} to update status and show confirm button", 
                                reviewWorkItem.WorkItemID, reviewWorkItem.WorkType, reviewWorkItem.PersonName, id);
                        }
                    }
                }
                else
                {
                    _logger?.LogWarning("Unlocking assignment {AssignmentID}: No confirmed design workitems found with PersonName, skipping reset", id);
                }
            }
            else
            {
                _logger?.LogInformation("Unlocking assignment {AssignmentID}: No work items found, skipping reset", id);
            }

            assignment.IsLocked = false;
            
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException dbEx)
            {
                _logger?.LogError(dbEx, "Database error when unlocking assignment {AssignmentID}: {Message}. InnerException: {InnerException}", 
                    id, dbEx.Message, dbEx.InnerException?.Message);
                return StatusCode(500, new { 
                    error = "Database error when unlocking assignment", 
                    message = dbEx.Message,
                    innerException = dbEx.InnerException?.Message
                });
            }
            catch (Exception saveEx)
            {
                _logger?.LogError(saveEx, "Error saving changes when unlocking assignment {AssignmentID}: {Message}", id, saveEx.Message);
                return StatusCode(500, new { 
                    error = "Error saving changes when unlocking assignment", 
                    message = saveEx.Message
                });
            }

            _logger?.LogInformation("Unlocked assignment {AssignmentID} by user {Username}", id, User.Identity?.Name);

            return Ok(new { 
                message = "Assignment đã được mở khóa thành công", 
                assignmentID = id,
                isLocked = false
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error unlocking assignment {AssignmentID}: {Message}. StackTrace: {StackTrace}", 
                id, ex.Message, ex.StackTrace);
            return StatusCode(500, new { 
                error = "Error unlocking assignment", 
                message = ex.Message,
                details = ex.InnerException?.Message
            });
        }
    }

    // PUT: api/assignments/{id}/lock
    // Khóa assignment (tùy chọn - có thể dùng để khóa thủ công)
    // Chỉ user kiểm soát (Manager) mới có quyền lock
    [HttpPut("{id}/lock")]
    [Authorize(Roles = "Manager,Administrator,Admin")]
    public async Task<IActionResult> LockAssignment(int id)
    {
        try
        {
            var assignment = await _context.MachineAssignments
                .FirstOrDefaultAsync(a => a.AssignmentID == id);

            if (assignment == null)
            {
                return NotFound(new { error = "Assignment not found", assignmentID = id });
            }

            if (assignment.IsLocked)
            {
                return BadRequest(new { error = "Assignment is already locked", message = "Assignment này đã bị khóa." });
            }

            assignment.IsLocked = true;
            await _context.SaveChangesAsync();

            _logger?.LogInformation("Locked assignment {AssignmentID} by user", id);

            return Ok(new { 
                message = "Assignment đã được khóa thành công", 
                assignmentID = id,
                isLocked = true
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error locking assignment {AssignmentID}: {Message}", id, ex.Message);
            return StatusCode(500, new { error = "Error locking assignment", message = ex.Message });
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

