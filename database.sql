-- ====================================
-- TechStore Database Schema
-- SQL Server
-- ====================================

CREATE DATABASE TechStore;
GO

USE TechStore;
GO

-- Users Table
CREATE TABLE Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL UNIQUE,
    Email NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    FullName NVARCHAR(100) NULL,
    Role NVARCHAR(20) NOT NULL DEFAULT 'Customer',
    Phone NVARCHAR(15) NULL,
    Address NVARCHAR(255) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- Categories Table
CREATE TABLE Categories (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL UNIQUE,
    Description NVARCHAR(500) NULL
);

-- Products Table
CREATE TABLE Products (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    Price DECIMAL(18, 2) NOT NULL,
    StockQuantity INT NOT NULL DEFAULT 0,
    CategoryId INT NOT NULL,
    ImageUrl NVARCHAR(500) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (CategoryId) REFERENCES Categories(Id) ON DELETE CASCADE
);

-- ProductSpecs Table (Dynamic Attributes)
CREATE TABLE ProductSpecs (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ProductId INT NOT NULL,
    SpecKey NVARCHAR(50) NOT NULL,
    SpecValue NVARCHAR(100) NOT NULL,
    FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE CASCADE
);

-- Orders Table
CREATE TABLE Orders (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    OrderDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    TotalAmount DECIMAL(18, 2) NOT NULL,
    Status NVARCHAR(50) NOT NULL DEFAULT 'Pending',
    PaymentMethod NVARCHAR(50) NULL,
    ShippingAddress NVARCHAR(255) NULL,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

-- OrderItems Table
CREATE TABLE OrderItems (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    OrderId INT NOT NULL,
    ProductId INT NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18, 2) NOT NULL,
    FOREIGN KEY (OrderId) REFERENCES Orders(Id) ON DELETE CASCADE,
    FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE NO ACTION
);

-- ====================================
-- Seed Data
-- ====================================
INSERT INTO Categories (Name, Description) VALUES
    (N'Laptop', N'Máy tính xách tay các loại'),
    (N'Smartphone', N'Điện thoại thông minh'),
    (N'Tablet', N'Máy tính bảng'),
    (N'Accessories', N'Phụ kiện công nghệ');

-- Create default Admin account (password: admin123)
INSERT INTO Users (Username, Email, PasswordHash, FullName, Role)
VALUES (
    'admin',
    'admin@techstore.com',
    '$2a$11$K7RqFGDSQXKhGCVOqZV9qe5K0bY9zP2tVsR8L1mK7jZxRV44mmPbm',
    'Admin TechStore',
    'Admin'
);
