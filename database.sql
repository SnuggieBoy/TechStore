-- Database: TechStore
CREATE DATABASE TechStore;
GO
USE TechStore;
GO

-- Table: Users
CREATE TABLE Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL UNIQUE,
    Email NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    FullName NVARCHAR(100),
    [Role] NVARCHAR(20) NOT NULL DEFAULT 'Customer', -- Admin, Customer
    [Address] NVARCHAR(255),
    CreatedAt DATETIME DEFAULT GETDATE()
);

-- Table: Categories
CREATE TABLE Categories (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    [Description] NVARCHAR(500)
);

-- Table: Products
CREATE TABLE Products (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    [Description] NVARCHAR(MAX),
    Price DECIMAL(18, 2) NOT NULL,
    StockQuantity INT NOT NULL DEFAULT 0,
    CategoryId INT NOT NULL,
    ImageUrl NVARCHAR(MAX),
    CreatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (CategoryId) REFERENCES Categories(Id) ON DELETE CASCADE
);

-- Table: ProductSpecs
CREATE TABLE ProductSpecs (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ProductId INT NOT NULL,
    SpecKey NVARCHAR(50) NOT NULL, -- e.g., RAM, CPU, Color
    SpecValue NVARCHAR(100) NOT NULL, -- e.g., 16GB, i7, Red
    FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE CASCADE
);

-- Table: Orders
CREATE TABLE Orders (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    OrderDate DATETIME DEFAULT GETDATE(),
    TotalAmount DECIMAL(18, 2) NOT NULL,
    [Status] NVARCHAR(50) DEFAULT 'Pending', -- Pending, Processing, Shipped, Delivered, Cancelled
    PaymentMethod NVARCHAR(50), -- COD, Momo, ZaloPay
    ShippingAddress NVARCHAR(255),
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

-- Table: OrderItems
CREATE TABLE OrderItems (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    OrderId INT NOT NULL,
    ProductId INT NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18, 2) NOT NULL,
    FOREIGN KEY (OrderId) REFERENCES Orders(Id) ON DELETE CASCADE,
    FOREIGN KEY (ProductId) REFERENCES Products(Id) -- No Cascade Delete here to preserve order history
);

-- Seed Data (Optional, but good for testing)
INSERT INTO Categories (Name, Description) VALUES ('Laptop', 'Gaming and Office Laptops'), ('Smartphone', 'Latest Smartphones'), ('Tablet', 'Portable Tablets');
