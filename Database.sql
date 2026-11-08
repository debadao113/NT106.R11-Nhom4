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
