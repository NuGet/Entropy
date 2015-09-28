
USE PackageStaging
GO

IF OBJECT_ID('Owner', 'U') IS NOT NULL
	DROP TABLE [Owner]
GO

CREATE TABLE [Owner]
(
	[Key] INT IDENTITY,
	[Name] VARCHAR(1024)
)
GO

CREATE CLUSTERED INDEX Owner_Index_1 ON [Owner] ( [Key] )
GO

IF OBJECT_ID('StageOwner', 'U') IS NOT NULL
DROP TABLE [StageOwner]
GO

CREATE TABLE [StageOwner]
(
	[OwnerKey] INT,
	[StageKey] INT
)
GO

CREATE CLUSTERED INDEX StageOwner_Index_1 ON StageOwner ( [OwnerKey], [StageKey] )
GO

IF OBJECT_ID('Stage', 'U') IS NOT NULL
DROP TABLE [Stage]
GO

CREATE TABLE [Stage]
(
	[Key] INT IDENTITY,
	[Id] VARCHAR(64)
)
GO

CREATE CLUSTERED INDEX Stage_Index_1 ON Stage ( [Key] )
GO

IF OBJECT_ID('StagePackage', 'U') IS NOT NULL
DROP TABLE [StagePackage]
GO

CREATE TABLE [StagePackage]
(
	[Key] INT IDENTITY,
	[Id] VARCHAR(1024),
	[Version] VARCHAR(1024),
	[StageKey] INT,
	[NupkgLocation] VARCHAR(1024),
	[NuspecLocation] VARCHAR(1024)
)
GO

CREATE CLUSTERED INDEX StagePackage_Index_1 ON StagePackage ( [Key] )
GO
