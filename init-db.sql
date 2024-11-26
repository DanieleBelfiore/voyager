-- Create identity database if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'identity')
BEGIN
    CREATE DATABASE [identity];
END
GO

-- Create driver database if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'driver')
BEGIN
    CREATE DATABASE [driver];
END
GO

-- Create ride database if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'ride')
BEGIN
    CREATE DATABASE [ride];
END
GO