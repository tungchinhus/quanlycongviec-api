using Microsoft.EntityFrameworkCore;
using quanlyfilesBE.Data;

namespace quanlyfilesBE.Models;

/// <summary>
/// Helper class để tính và cập nhật trạng thái của Assignment dựa trên WorkItems
/// </summary>
public static class AssignmentStatusHelper
{
    /// <summary>
    /// Tính trạng thái của một WorkItem (khâu) dựa trên dữ liệu
    /// </summary>
    /// <param name="workItem">WorkItem cần tính trạng thái</param>
    /// <returns>1: Mới, 2: Đang xử lý, 3: Hoàn thành</returns>
    public static int CalculateWorkItemStatus(WorkItem? workItem)
    {
        if (workItem == null)
        {
            return 1; // Mới - chưa có work item
        }

        // Kiểm tra xem có dữ liệu nào được cập nhật không
        bool hasAnyUpdate = workItem.StartDate.HasValue ||
                           workItem.ExpectedFinish.HasValue ||
                           workItem.ActualFinish.HasValue ||
                           workItem.PersonConfirmation.HasValue ||
                           !string.IsNullOrWhiteSpace(workItem.Notes) ||
                           !string.IsNullOrWhiteSpace(workItem.File_ID);

        // Nếu không có cập nhật nào, trạng thái là Mới
        if (!hasAnyUpdate)
        {
            return 1; // Mới
        }

        // Kiểm tra xem đã hoàn thành chưa
        // Theo yêu cầu business: chỉ cần PersonConfirmation = true là coi như HOÀN THÀNH,
        // không bắt buộc phải có ActualFinish
        bool isCompleted = workItem.PersonConfirmation == true;

        if (isCompleted)
        {
            return 3; // Hoàn thành
        }

        // Nếu có cập nhật nhưng chưa hoàn thành, trạng thái là Đang xử lý
        return 2; // Đang xử lý
    }

    /// <summary>
    /// Tính trạng thái tổng của Assignment dựa trên tất cả WorkItems và Approval Status
    /// </summary>
    /// <param name="workItems">Danh sách WorkItems của assignment</param>
    /// <param name="technicalSheet">TechnicalSheet để kiểm tra approval status (optional)</param>
    /// <returns>1: Mới, 2: Đang xử lý, 3: Hoàn thành</returns>
    public static int CalculateAssignmentStatus(IEnumerable<WorkItem>? workItems, TechnicalSheet? technicalSheet = null)
    {
        if (workItems == null || !workItems.Any())
        {
            return 1; // Mới - chưa có work items
        }

        // Tính trạng thái cho từng work item
        var workItemStatuses = workItems.Select(wi => CalculateWorkItemStatus(wi)).ToList();

        // Nếu tất cả đều là Mới (1), trạng thái tổng là Mới
        if (workItemStatuses.All(s => s == 1))
        {
            return 1; // Mới
        }

        // Kiểm tra xem tất cả work items đã hoàn thành chưa
        bool allWorkItemsCompleted = workItemStatuses.All(s => s == 3);
        
        // Kiểm tra TechnicalSheet approval status
        bool isFullyApproved = technicalSheet != null &&
            !string.IsNullOrEmpty(technicalSheet.ManagerL1ApprovalStatus) &&
            technicalSheet.ManagerL1ApprovalStatus == "Approved" &&
            !string.IsNullOrEmpty(technicalSheet.ManagerApprovalStatus) &&
            technicalSheet.ManagerApprovalStatus == "Approved";

        // Chỉ đánh dấu "Hoàn thành" khi:
        // 1. Tất cả work items đã hoàn thành VÀ
        // 2. TechnicalSheet đã được ký duyệt hoàn thành (ManagerL1 và Manager đã approve)
        // Nếu chưa được approve, không thể coi là "Hoàn thành" dù work items đã hoàn thành
        if (allWorkItemsCompleted && isFullyApproved)
        {
            return 3; // Hoàn thành
        }
        
        // Nếu TechnicalSheet đã được approve nhưng work items chưa hoàn thành, vẫn là "Đang xử lý"
        // (Trường hợp này có thể xảy ra nếu approve trước khi work items hoàn thành)

        // Nếu có ít nhất một khâu không còn Mới và ít nhất một khâu chưa Hoàn thành
        // Hoặc chưa được approve hoàn toàn
        // Trạng thái tổng là Đang xử lý
        return 2; // Đang xử lý
    }

    /// <summary>
    /// Cập nhật trạng thái của Assignment dựa trên WorkItems
    /// </summary>
    /// <param name="context">Database context</param>
    /// <param name="assignmentId">ID của assignment cần cập nhật</param>
    /// <param name="logger">Optional logger</param>
    /// <returns>Task</returns>
    public static async Task UpdateAssignmentStatusAsync(
        ApplicationDbContext context,
        int assignmentId,
        ILogger? logger = null)
    {
        try
        {
            // Load assignment với work items và TechnicalSheet để kiểm tra approval status
            var assignment = await context.MachineAssignments
                .Include(a => a.WorkItems)
                .Include(a => a.TechnicalSheet)
                .FirstOrDefaultAsync(a => a.AssignmentID == assignmentId);

            if (assignment == null)
            {
                logger?.LogWarning("Assignment {AssignmentID} not found when updating status", assignmentId);
                return;
            }

            // Tính trạng thái mới - kiểm tra cả work items và approval status
            int newStatus = CalculateAssignmentStatus(assignment.WorkItems, assignment.TechnicalSheet);

            // Chỉ cập nhật nếu trạng thái thay đổi
            if (assignment.Status != newStatus)
            {
                int oldStatus = assignment.Status;
                assignment.Status = newStatus;
                
                logger?.LogInformation(
                    "Updated Assignment {AssignmentID} status from {OldStatus} to {NewStatus}",
                    assignmentId, oldStatus, newStatus);

                // Lưu thay đổi trạng thái
                await context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error updating assignment status for AssignmentID {AssignmentID}: {Message}",
                assignmentId, ex.Message);
            // Không throw exception để không làm gián đoạn flow chính
        }
    }
}

