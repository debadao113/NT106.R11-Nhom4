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
