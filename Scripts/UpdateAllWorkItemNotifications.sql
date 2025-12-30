-- Update ALL notification messages for WorkItem to show only TBKT_ID
UPDATE n
SET n.Message = ISNULL(ts.TBKT_ID, 'N/A')
FROM Notifications n
INNER JOIN WorkItem wi ON n.RelatedEntityId = wi.WorkItemID AND n.RelatedEntityType = 'WorkItem'
INNER JOIN MachineAssignment ma ON wi.AssignmentID = ma.AssignmentID
LEFT JOIN TechnicalSheet ts ON ma.TBKT_ID = ts.TBKT_ID
WHERE n.RelatedEntityType = 'WorkItem';

SELECT 
    n.Id,
    n.Message AS OldMessage,
    ISNULL(ts.TBKT_ID, 'N/A') AS NewMessage
FROM Notifications n
INNER JOIN WorkItem wi ON n.RelatedEntityId = wi.WorkItemID AND n.RelatedEntityType = 'WorkItem'
INNER JOIN MachineAssignment ma ON wi.AssignmentID = ma.AssignmentID
LEFT JOIN TechnicalSheet ts ON ma.TBKT_ID = ts.TBKT_ID
WHERE n.RelatedEntityType = 'WorkItem';

PRINT 'Preview of notifications to be updated (run UPDATE separately if preview looks correct)';

