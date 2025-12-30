-- Update ALL WorkItem notification messages to show only TBKT_ID
UPDATE n
SET n.Message = ISNULL(ts.TBKT_ID, 'N/A')
FROM Notifications n
INNER JOIN WorkItem wi ON n.RelatedEntityId = wi.WorkItemID AND n.RelatedEntityType = 'WorkItem'
INNER JOIN MachineAssignment ma ON wi.AssignmentID = ma.AssignmentID
LEFT JOIN TechnicalSheet ts ON ma.TBKT_ID = ts.TBKT_ID
WHERE n.RelatedEntityType = 'WorkItem';

PRINT 'Updated all WorkItem notifications to show only TBKT_ID';

