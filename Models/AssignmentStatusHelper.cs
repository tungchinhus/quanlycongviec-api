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
    /// Tính trạng thái tổng của Assignment dựa trên tất cả WorkItems
    /// </summary>
    /// <param name="workItems">Danh sách WorkItems của assignment</param>
    /// <returns>1: Mới, 2: Đang xử lý, 3: Hoàn thành</returns>
    public static int CalculateAssignmentStatus(IEnumerable<WorkItem>? workItems)
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

        // Nếu tất cả đều là Hoàn thành (3), trạng thái tổng là Hoàn thành
        if (workItemStatuses.All(s => s == 3))
        {
            return 3; // Hoàn thành
        }

        // Nếu có ít nhất một khâu không còn Mới và ít nhất một khâu chưa Hoàn thành
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
            // Load assignment với work items
            var assignment = await context.MachineAssignments
                .Include(a => a.WorkItems)
                .FirstOrDefaultAsync(a => a.AssignmentID == assignmentId);

            if (assignment == null)
            {
                logger?.LogWarning("Assignment {AssignmentID} not found when updating status", assignmentId);
                return;
            }

            // Tính trạng thái mới
            int newStatus = CalculateAssignmentStatus(assignment.WorkItems);

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

