-- Update notification messages to only show TBKT_ID for WorkItem notifications
-- This script updates existing notifications to match the new format

UPDATE n
SET n.Message = ISNULL(ts.TBKT_ID, 'N/A')
FROM Notifications n
INNER JOIN WorkItem wi ON n.RelatedEntityId = wi.WorkItemID AND n.RelatedEntityType = 'WorkItem'
INNER JOIN MachineAssignment ma ON wi.AssignmentID = ma.AssignmentID
INNER JOIN TechnicalSheet ts ON ma.TBKT_ID = ts.TBKT_ID
WHERE n.RelatedEntityType = 'WorkItem'
  AND n.Message LIKE 'Bạn đã được giao công việc%';

PRINT 'Updated notification messages to show only TBKT_ID';

