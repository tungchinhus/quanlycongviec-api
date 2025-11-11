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

            // Check if TechnicalSheet exists, if not create a basic one
            var technicalSheet = await _context.TechnicalSheets.FindAsync(dto.TBKT_ID);
            if (technicalSheet == null)
            {
                technicalSheet = new TechnicalSheet
                {
                    TBKT_ID = dto.TBKT_ID
                };
                _context.TechnicalSheets.Add(technicalSheet);
            }

            var assignment = new MachineAssignment
            {
                TBKT_ID = dto.TBKT_ID,
                MachineName = dto.MachineName,
                StandardRequirement = dto.StandardRequirement,
                AdditionalRequest = dto.AdditionalRequest,
                DeliveryDate = dto.DeliveryDate,
                Designer = dto.Designer,
                TeamLeader = dto.TeamLeader
            };

            _context.MachineAssignments.Add(assignment);
            await _context.SaveChangesAsync();

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
                AssignmentApprovals = new List<AssignmentApprovalDto>(),
                WorkChanges = new List<WorkChangeDto>(),
                WorkItems = new List<WorkItemDto>()
            };

            return CreatedAtAction(nameof(GetAssignment), new { id = assignment.AssignmentID }, assignmentDto);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in CreateAssignment: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error creating assignment", message = ex.Message });
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
            var assignment = await _context.MachineAssignments.FindAsync(id);
            if (assignment == null)
            {
                return NotFound(new { error = "Assignment not found" });
            }

            _context.MachineAssignments.Remove(assignment);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in DeleteAssignment: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error deleting assignment", message = ex.Message });
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
}

