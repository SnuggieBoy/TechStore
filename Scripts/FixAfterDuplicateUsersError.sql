-- Chạy script này TRÊN CÙNG DATABASE TechStoreDB sau khi chạy TechstoreDB.sql bị lỗi duplicate key ở dòng 240-241.
-- Script bổ sung index, default, FK còn thiếu (phần sau chỗ lỗi). Chạy 1 lần, an toàn chạy lại.
USE [TechStoreDB]
GO
SET ANSI_PADDING ON
GO

-- Indexes (bỏ qua nếu đã có)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ__Categori__737584F63FDDC228' AND object_id = OBJECT_ID('dbo.Categories'))
  ALTER TABLE [dbo].[Categories] ADD UNIQUE NONCLUSTERED ([Name] ASC) ON [PRIMARY]
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Categories_PublicId' AND object_id = OBJECT_ID('dbo.Categories'))
  CREATE UNIQUE NONCLUSTERED INDEX [IX_Categories_PublicId] ON [dbo].[Categories]([PublicId] ASC) ON [PRIMARY]
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Orders_PublicId' AND object_id = OBJECT_ID('dbo.Orders'))
  CREATE UNIQUE NONCLUSTERED INDEX [IX_Orders_PublicId] ON [dbo].[Orders]([PublicId] ASC) ON [PRIMARY]
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Products_PublicId' AND object_id = OBJECT_ID('dbo.Products'))
  CREATE UNIQUE NONCLUSTERED INDEX [IX_Products_PublicId] ON [dbo].[Products]([PublicId] ASC) ON [PRIMARY]
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ__Users__536C85E4168524B7' AND object_id = OBJECT_ID('dbo.Users'))
  ALTER TABLE [dbo].[Users] ADD UNIQUE NONCLUSTERED ([Username] ASC) ON [PRIMARY]
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ__Users__A9D10534F9C13A88' AND object_id = OBJECT_ID('dbo.Users'))
  ALTER TABLE [dbo].[Users] ADD UNIQUE NONCLUSTERED ([Email] ASC) ON [PRIMARY]
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_PublicId' AND object_id = OBJECT_ID('dbo.Users'))
  CREATE UNIQUE NONCLUSTERED INDEX [IX_Users_PublicId] ON [dbo].[Users]([PublicId] ASC) ON [PRIMARY]
GO

-- Defaults (bỏ qua nếu đã có)
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE c.object_id = OBJECT_ID('dbo.Orders') AND c.name = 'OrderDate')
  ALTER TABLE [dbo].[Orders] ADD DEFAULT (getutcdate()) FOR [OrderDate]
GO
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE c.object_id = OBJECT_ID('dbo.Orders') AND c.name = 'Status')
  ALTER TABLE [dbo].[Orders] ADD DEFAULT ('Pending') FOR [Status]
GO
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE c.object_id = OBJECT_ID('dbo.Products') AND c.name = 'StockQuantity')
  ALTER TABLE [dbo].[Products] ADD DEFAULT ((0)) FOR [StockQuantity]
GO
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE c.object_id = OBJECT_ID('dbo.Products') AND c.name = 'CreatedAt')
  ALTER TABLE [dbo].[Products] ADD DEFAULT (getutcdate()) FOR [CreatedAt]
GO
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE c.object_id = OBJECT_ID('dbo.Users') AND c.name = 'Role')
  ALTER TABLE [dbo].[Users] ADD DEFAULT ('Customer') FOR [Role]
GO
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE c.object_id = OBJECT_ID('dbo.Users') AND c.name = 'CreatedAt')
  ALTER TABLE [dbo].[Users] ADD DEFAULT (getutcdate()) FOR [CreatedAt]
GO

-- Foreign keys (bỏ qua nếu cột đã có FK)
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys fk JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id WHERE fk.parent_object_id = OBJECT_ID('dbo.OrderItems') AND COL_NAME(fkc.parent_object_id, fkc.parent_column_id) = 'OrderId')
  ALTER TABLE [dbo].[OrderItems] ADD CONSTRAINT [FK_OrderItems_Orders] FOREIGN KEY([OrderId]) REFERENCES [dbo].[Orders]([Id]) ON DELETE CASCADE
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys fk JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id WHERE fk.parent_object_id = OBJECT_ID('dbo.OrderItems') AND COL_NAME(fkc.parent_object_id, fkc.parent_column_id) = 'ProductId')
  ALTER TABLE [dbo].[OrderItems] ADD CONSTRAINT [FK_OrderItems_Products] FOREIGN KEY([ProductId]) REFERENCES [dbo].[Products]([Id])
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys fk JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id WHERE fk.parent_object_id = OBJECT_ID('dbo.Orders') AND COL_NAME(fkc.parent_object_id, fkc.parent_column_id) = 'UserId')
  ALTER TABLE [dbo].[Orders] ADD CONSTRAINT [FK_Orders_Users] FOREIGN KEY([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys fk JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id WHERE fk.parent_object_id = OBJECT_ID('dbo.Products') AND COL_NAME(fkc.parent_object_id, fkc.parent_column_id) = 'CategoryId')
  ALTER TABLE [dbo].[Products] ADD CONSTRAINT [FK_Products_Categories] FOREIGN KEY([CategoryId]) REFERENCES [dbo].[Categories]([Id]) ON DELETE CASCADE
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys fk JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id WHERE fk.parent_object_id = OBJECT_ID('dbo.ProductSpecs') AND COL_NAME(fkc.parent_object_id, fkc.parent_column_id) = 'ProductId')
  ALTER TABLE [dbo].[ProductSpecs] ADD CONSTRAINT [FK_ProductSpecs_Products] FOREIGN KEY([ProductId]) REFERENCES [dbo].[Products]([Id]) ON DELETE CASCADE
GO

PRINT 'Fix hoan tat. Database TechStoreDB da day du index, default, FK.'
GO
