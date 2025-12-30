-- Xóa notifications của work items đã hoàn thành (ActualFinish != null)
DELETE n
FROM Notifications n
INNER JOIN WorkItem wi ON n.RelatedEntityId = wi.WorkItemID AND n.RelatedEntityType = 'WorkItem'
WHERE wi.ActualFinish IS NOT NULL;

PRINT 'Deleted notifications for completed work items';

