
USE PackageStaging
GO

IF OBJECT_ID('Owner', 'U') IS NOT NULL
	DROP TABLE [Owner]
GO

CREATE TABLE [Owner]
(
	[Key] INT IDENTITY,
	[Name] VARCHAR(256)
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
	[Name] VARCHAR(256),
	[BaseService] VARCHAR(1024)
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
	[Staged] DATETIME,
	[NupkgLocation] VARCHAR(1024),
	[NuspecLocation] VARCHAR(1024)
)
GO

CREATE CLUSTERED INDEX StagePackage_Index_1 ON StagePackage ( [Key] )
GO

IF OBJECT_ID('StagePackageOwner', 'U') IS NOT NULL
DROP TABLE [StagePackageOwner]
GO

CREATE TABLE [StagePackageOwner]
(
	[OwnerKey] INT,
	[PackageKey] INT
)
GO

CREATE CLUSTERED INDEX StagePackageOwner_Index_1 ON StagePackageOwner ( [OwnerKey], [PackageKey] )
GO

