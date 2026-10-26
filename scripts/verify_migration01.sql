-- Run in WarehouseERP_Dev after applying Migration01_OrganizationCatalog.
-- Expected: 13 current temporal master tables; no operational or inventory tables.
SELECT s.name AS SchemaName, t.name AS TableName,
       t.temporal_type_desc AS TemporalType,
       OBJECT_SCHEMA_NAME(t.history_table_id) AS HistorySchema,
       OBJECT_NAME(t.history_table_id) AS HistoryTable
FROM sys.tables AS t
JOIN sys.schemas AS s ON s.schema_id = t.schema_id
WHERE s.name IN ('organization', 'catalog')
ORDER BY s.name, t.name;

SELECT s.name AS SchemaName, t.name AS TableName,
       i.name AS IndexName, i.is_unique, i.filter_definition
FROM sys.indexes AS i
JOIN sys.tables AS t ON t.object_id = i.object_id
JOIN sys.schemas AS s ON s.schema_id = t.schema_id
WHERE (s.name = 'organization' AND t.name = 'WarehouseLocations')
   OR (s.name = 'catalog' AND t.name IN ('Products', 'ProductUnits', 'ProductBarcodes'))
ORDER BY s.name, t.name, i.name;

SELECT s.name AS SchemaName, t.name AS TableName,
       cc.name AS ConstraintName, cc.definition
FROM sys.check_constraints AS cc
JOIN sys.tables AS t ON t.object_id = cc.parent_object_id
JOIN sys.schemas AS s ON s.schema_id = t.schema_id
WHERE s.name IN ('organization', 'catalog')
ORDER BY s.name, t.name, cc.name;
