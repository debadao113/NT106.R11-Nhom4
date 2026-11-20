-- Script tao co so du lieu va cac bang cho Do an Quan Ly Khach San (NT106)
-- Chay tren SQL Server Management Studio (SSMS)

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'dbMyHotel')
BEGIN
    CREATE DATABASE dbMyHotel;
END
GO

USE dbMyHotel;
GO

-- Bang quan ly phong (rooms)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'rooms')
BEGIN
    CREATE TABLE rooms (
        roomid INT IDENTITY(1,1) PRIMARY KEY,
        roomNo VARCHAR(50) NOT NULL UNIQUE,
        roomType VARCHAR(100) NOT NULL,
        bed VARCHAR(50) NOT NULL,
        price BIGINT NOT NULL,
        booked VARCHAR(10) DEFAULT 'NO'
    );
END
GO

-- Bang thong tin khach hang (customer)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'customer')
BEGIN
    CREATE TABLE customer (
        cid INT IDENTITY(1,1) PRIMARY KEY,
        cname VARCHAR(250) NOT NULL,
        mobile BIGINT NOT NULL,
        nationality VARCHAR(250) NOT NULL,
        gender VARCHAR(50) NOT NULL,
        dob VARCHAR(50) NOT NULL,
        idproof VARCHAR(250) NOT NULL,
        address VARCHAR(350) NOT NULL,
        checkin VARCHAR(250) NOT NULL,
        checkout VARCHAR(250) DEFAULT 'NO',
        chekout VARCHAR(250) DEFAULT 'NO',
        roomid INT NOT NULL,
        FOREIGN KEY (roomid) REFERENCES rooms(roomid)
    );
END
GO

-- Tai khoan le tan. Tao tai khoan bang cua so HotelServer de bam mat khau.
IF OBJECT_ID('dbo.staff_accounts', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.staff_accounts (
        accountId INT IDENTITY(1,1) PRIMARY KEY,
        username NVARCHAR(50) NOT NULL UNIQUE,
        displayName NVARCHAR(100) NOT NULL,
        passwordHash VARBINARY(32) NOT NULL,
        passwordSalt VARBINARY(16) NOT NULL,
        passwordIterations INT NOT NULL,
        isActive BIT NOT NULL DEFAULT 1,
        createdAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END
GO

-- Ho so nhan vien; tai khoan dang nhap server duoc quan ly rieng.
IF OBJECT_ID('dbo.employee', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.employee (
        eid INT IDENTITY(1,1) PRIMARY KEY,
        ename NVARCHAR(250) NOT NULL,
        mobile BIGINT NOT NULL,
        gender NVARCHAR(50) NOT NULL,
        emailid NVARCHAR(250) NOT NULL,
        username NVARCHAR(50) NOT NULL UNIQUE,
        pass NVARCHAR(250) NOT NULL
    );
END;
GO
