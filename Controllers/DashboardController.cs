using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using quanlyfilesBE.Data;
using quanlyfilesBE.Models;

namespace quanlyfilesBE.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DashboardController>? _logger;

    public DashboardController(ApplicationDbContext context, ILogger<DashboardController>? logger = null)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("stats")]
    public async Task<ActionResult<object>> GetUserStats()
    {
        try
        {
            // Lấy FirebaseUID từ JWT token
            var firebaseUID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                             ?? User.FindFirst("sub")?.Value;
            
            _logger?.LogInformation("Dashboard GetUserStats - FirebaseUID from token: {FirebaseUID}", firebaseUID ?? "NULL");
            
            if (string.IsNullOrEmpty(firebaseUID))
            {
                _logger?.LogWarning("Dashboard GetUserStats - No FirebaseUID found in token");
                return Unauthorized(new { error = "Invalid user token" });
            }

            // Lấy thông tin user từ FirebaseUID
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.FirebaseUID == firebaseUID);
            
            _logger?.LogInformation("Dashboard GetUserStats - User lookup by FirebaseUID: {Found}, UserId: {UserId}", 
                user != null, user?.UserId);
            
            if (user == null)
            {
                // Nếu không tìm thấy theo FirebaseUID, thử tìm theo email
                var emailClaim = User.FindFirst(ClaimTypes.Email)?.Value 
                                ?? User.FindFirst("email")?.Value;
                
                _logger?.LogInformation("Dashboard GetUserStats - Trying email lookup: {Email}", emailClaim ?? "NULL");
                
                if (!string.IsNullOrEmpty(emailClaim))
                {
                    user = await _context.Users
                        .FirstOrDefaultAsync(u => u.Email == emailClaim);
                    
                    _logger?.LogInformation("Dashboard GetUserStats - User lookup by Email: {Found}, UserId: {UserId}", 
                        user != null, user?.UserId);
                }
                
                if (user == null)
                {
                    _logger?.LogWarning("Dashboard GetUserStats - User not found. FirebaseUID: {FirebaseUID}, Email: {Email}", 
                        firebaseUID, emailClaim ?? "NULL");
                    return NotFound(new { error = "User not found", firebaseUID, email = emailClaim });
                }
            }

            // Lấy username để filter theo PersonName trong WorkItems
            // PersonName có thể là: UserId (string), FullName, hoặc UserName
            var userName = user.UserName;
            var fullName = user.FullName;
            var userIdString = user.UserId.ToString();

            _logger?.LogInformation("Dashboard GetUserStats - UserId: {UserId}, UserName: {UserName}, FullName: {FullName}, UserIdString: {UserIdString}", 
                user.UserId, userName, fullName, userIdString);

            // Thống kê WorkItems theo user (PersonName)
            // PersonName có thể lưu UserId (số dạng string), UserName, hoặc FullName
            var myWorkItems = await _context.WorkItems
                .Where(wi => wi.PersonName == userName || 
                           wi.PersonName == fullName || 
                           wi.PersonName == userIdString)
                .ToListAsync();

            _logger?.LogInformation("Dashboard GetUserStats - Found {Count} work items for user {UserId}", myWorkItems.Count, user.UserId);

            var totalWorkItems = myWorkItems.Count;
            var completedWorkItems = myWorkItems.Count(wi => wi.ActualFinish.HasValue);
            var confirmedWorkItems = myWorkItems.Count(wi => wi.PersonConfirmation == true);
            // Đang xử lý = chưa hoàn thành VÀ chưa xác nhận
            var pendingWorkItemsCount = myWorkItems.Count(wi => 
                !wi.ActualFinish.HasValue && 
                (wi.PersonConfirmation != true));
            var overdueWorkItems = myWorkItems.Count(wi => 
                !wi.ActualFinish.HasValue && 
                (wi.PersonConfirmation != true) &&
                wi.ExpectedFinish.HasValue && 
                wi.ExpectedFinish.Value < DateTime.UtcNow);

            // Lấy danh sách công việc đang xử lý để hiển thị trong tooltip
            // Chỉ lấy những công việc chưa hoàn thành VÀ chưa xác nhận
            var pendingWorkItemsList = myWorkItems
                .Where(wi => !wi.ActualFinish.HasValue && (wi.PersonConfirmation != true))
                .ToList();

            // Lấy AssignmentIDs để query MachineAssignments
            var assignmentIds = pendingWorkItemsList
                .Select(wi => wi.AssignmentID)
                .Distinct()
                .ToList();

            // Lấy thông tin MachineAssignments
            var assignments = await _context.MachineAssignments
                .Where(a => assignmentIds.Contains(a.AssignmentID))
                .ToDictionaryAsync(a => a.AssignmentID, a => a.MachineName);

            // Tạo danh sách công việc đang xử lý với MachineName
            var pendingWorkItemsWithMachine = pendingWorkItemsList
                .Select(wi => new
                {
                    WorkItemID = wi.WorkItemID,
                    WorkType = wi.WorkType ?? "Không xác định",
                    AssignmentID = wi.AssignmentID,
                    MachineName = assignments.GetValueOrDefault(wi.AssignmentID, "Không xác định")
                })
                .Take(10) // Lấy tối đa 10 công việc
                .ToList();

            // Lấy danh sách máy đã kiểm soát (đã xác nhận)
            var confirmedWorkItemsList = myWorkItems
                .Where(wi => wi.PersonConfirmation == true)
                .ToList();

            var confirmedAssignmentIds = confirmedWorkItemsList
                .Select(wi => wi.AssignmentID)
                .Distinct()
                .ToList();

            var confirmedAssignments = await _context.MachineAssignments
                .Where(a => confirmedAssignmentIds.Contains(a.AssignmentID))
                .ToDictionaryAsync(a => a.AssignmentID, a => a.MachineName);

            var confirmedMachines = confirmedWorkItemsList
                .GroupBy(wi => wi.AssignmentID)
                .Select(g => new
                {
                    AssignmentID = g.Key,
                    MachineName = confirmedAssignments.GetValueOrDefault(g.Key, "Không xác định")
                })
                .Distinct()
                .Take(10) // Lấy tối đa 10 máy
                .Select(x => x.MachineName)
                .ToList();

            // Thống kê Assignments (nếu user là Designer hoặc TeamLeader)
            // Designer và TeamLeader có thể lưu UserId (số dạng string), UserName, hoặc FullName
            var myAssignments = await _context.MachineAssignments
                .Where(a => a.Designer == userName || a.Designer == fullName || a.Designer == userIdString ||
                           a.TeamLeader == userName || a.TeamLeader == fullName || a.TeamLeader == userIdString)
                .ToListAsync();

            _logger?.LogInformation("Dashboard GetUserStats - Found {Count} assignments for user {UserId}", myAssignments.Count, user.UserId);

            var totalAssignments = myAssignments.Count;
            var newAssignments = myAssignments.Count(a => a.Status == 1);
            var inProgressAssignments = myAssignments.Count(a => a.Status == 2);
            var completedAssignments = myAssignments.Count(a => a.Status == 3);

            // Thống kê theo thời gian (7 ngày gần nhất)
            // Hiển thị cho TẤT CẢ user có WorkType thiết kế (Core Design, Casing Design) và kiểm soát (Core Review, Casing Review)
            var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
            var designAndControlWorkTypes = new[] { "Core Design", "Casing Design", "Core Review", "Casing Review" };
            
            // Lấy tất cả work items của tất cả user có WorkType thiết kế hoặc kiểm soát trong 7 ngày qua
            var allRecentWorkItems = await _context.WorkItems
                .Where(wi => designAndControlWorkTypes.Contains(wi.WorkType ?? "") &&
                           wi.StartDate.HasValue && 
                           wi.StartDate.Value >= sevenDaysAgo)
                .OrderByDescending(wi => wi.StartDate)
                .ToListAsync();

            // Lấy danh sách tên máy từ các assignments gần đây
            var recentAssignmentIds = allRecentWorkItems
                .Select(wi => wi.AssignmentID)
                .Distinct()
                .ToList();

            // Lấy thông tin assignments
            var recentAssignments = await _context.MachineAssignments
                .Where(a => recentAssignmentIds.Contains(a.AssignmentID))
                .ToListAsync();

            // Tạo dictionary để map AssignmentID -> Latest StartDate
            var assignmentStartDates = allRecentWorkItems
                .GroupBy(wi => wi.AssignmentID)
                .ToDictionary(
                    g => g.Key,
                    g => g.Where(wi => wi.StartDate.HasValue)
                          .Select(wi => wi.StartDate!.Value)
                          .DefaultIfEmpty(DateTime.MinValue)
                          .Max()
                );

            // Sắp xếp assignments theo StartDate mới nhất và lấy tên máy
            var recentMachines = recentAssignments
                .OrderByDescending(a => assignmentStartDates.GetValueOrDefault(a.AssignmentID, DateTime.MinValue))
                .Take(10) // Lấy tối đa 10 máy gần đây nhất
                .Select(a => a.MachineName)
                .ToList();

            // Thống kê theo WorkType
            var workItemsByType = myWorkItems
                .Where(wi => !string.IsNullOrEmpty(wi.WorkType))
                .GroupBy(wi => wi.WorkType)
                .Select(g => new { WorkType = g.Key, Count = g.Count() })
                .ToList();

            // Thống kê theo tháng (6 tháng gần nhất)
            var monthlyStats = new List<object>();
            for (int i = 5; i >= 0; i--)
            {
                var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(-i);
                var monthEnd = monthStart.AddMonths(1);
                
                var monthWorkItems = myWorkItems
                    .Where(wi => wi.StartDate.HasValue && 
                               wi.StartDate.Value >= monthStart && 
                               wi.StartDate.Value < monthEnd)
                    .ToList();

                monthlyStats.Add(new
                {
                    Month = monthStart.ToString("yyyy-MM"),
                    MonthName = monthStart.ToString("MM/yyyy"),
                    Total = monthWorkItems.Count,
                    Completed = monthWorkItems.Count(wi => wi.ActualFinish.HasValue),
                    Pending = monthWorkItems.Count(wi => !wi.ActualFinish.HasValue)
                });
            }

            // Thống kê completion rate
            var completionRate = totalWorkItems > 0 
                ? Math.Round((double)completedWorkItems / totalWorkItems * 100, 2) 
                : 0;

            return Ok(new
            {
                user = new
                {
                    userId = user.UserId,
                    userName = user.UserName,
                    fullName = user.FullName,
                    email = user.Email
                },
                workItems = new
                {
                    total = totalWorkItems,
                    completed = completedWorkItems,
                    pending = pendingWorkItemsCount,
                    confirmed = confirmedWorkItems,
                    overdue = overdueWorkItems,
                    completionRate = completionRate,
                    pendingList = pendingWorkItemsWithMachine,
                    confirmedMachines = confirmedMachines
                },
                assignments = new
                {
                    total = totalAssignments,
                    @new = newAssignments,
                    inProgress = inProgressAssignments,
                    completed = completedAssignments
                },
                recent = new
                {
                    last7Days = allRecentWorkItems.Count,
                    machines = recentMachines
                },
                byType = workItemsByType,
                monthly = monthlyStats
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in GetUserStats: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error retrieving dashboard stats", message = ex.Message });
        }
    }
}

