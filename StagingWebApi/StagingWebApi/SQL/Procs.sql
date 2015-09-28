
IF OBJECT_ID('GetOwner', 'P') IS NOT NULL
	DROP PROCEDURE GetOwner
GO

CREATE PROCEDURE GetOwner
@OwnerName VARCHAR(256)
AS
    SELECT
        [Owner].Name, 
        Stage.Name,
        StagePackage.[Id],
        StagePackage.[Version],
        StagePackage.Staged,
        StagePackage.NuspecLocation,
        PackageOwner.Name
    FROM Stage
    LEFT OUTER JOIN StagePackage ON Stage.[Key] = StagePackage.StageKey 
    LEFT OUTER JOIN StagePackageOwner ON StagePackage.[Key] = StagePackageOwner.PackageKey 
    LEFT OUTER JOIN [Owner] PackageOwner ON PackageOwner.[Key] = StagePackageOwner.OwnerKey 
    INNER JOIN StageOwner ON StageOwner.StageKey = Stage.[Key]
    INNER JOIN [Owner] ON [Owner].[Key] = StageOwner.OwnerKey
    WHERE [Owner].Name = @OwnerName
    ORDER BY Stage.Name, [Id]
GO

IF OBJECT_ID('GetStage', 'P') IS NOT NULL
	DROP PROCEDURE GetStage
GO

CREATE PROCEDURE GetStage
@OwnerName VARCHAR(256),
@StageName VARCHAR(256)
AS
    SELECT
        [Owner].Name, 
        Stage.Name,
        StagePackage.[Id],
        StagePackage.[Version],
        StagePackage.Staged,
        StagePackage.NuspecLocation,
        PackageOwner.Name
    FROM Stage
    LEFT OUTER JOIN StagePackage ON Stage.[Key] = StagePackage.StageKey 
    LEFT OUTER JOIN StagePackageOwner ON StagePackage.[Key] = StagePackageOwner.PackageKey 
    LEFT OUTER JOIN [Owner] PackageOwner ON PackageOwner.[Key] = StagePackageOwner.OwnerKey 
    INNER JOIN StageOwner ON StageOwner.StageKey = Stage.[Key]
    INNER JOIN [Owner] ON [Owner].[Key] = StageOwner.OwnerKey
    WHERE [Owner].Name = @OwnerName
        AND Stage.[Name] = @StageName
    ORDER BY Stage.Name, [Id]
GO

IF OBJECT_ID('GetPackage', 'P') IS NOT NULL
	DROP PROCEDURE GetPackage
GO

CREATE PROCEDURE GetPackage
@OwnerName VARCHAR(256),
@StageName VARCHAR(256),
@Id VARCHAR(1024)
AS
    SELECT
        StagePackage.[Id],
        StagePackage.[Version],
        StagePackage.Staged,
        StagePackage.NuspecLocation,
        PackageOwner.Name
    FROM Stage
    INNER JOIN StagePackage ON Stage.[Key] = StagePackage.StageKey 
    INNER JOIN StagePackageOwner ON StagePackage.[Key] = StagePackageOwner.PackageKey 
    INNER JOIN [Owner] PackageOwner ON PackageOwner.[Key] = StagePackageOwner.OwnerKey 
    INNER JOIN StageOwner ON StageOwner.StageKey = Stage.[Key]
    INNER JOIN [Owner] ON [Owner].[Key] = StageOwner.OwnerKey
    WHERE [Owner].Name = @OwnerName
        AND Stage.[Name] = @StageName
        AND StagePackage.[Id] = @Id
GO

IF OBJECT_ID('GetPackageVersion', 'P') IS NOT NULL
	DROP PROCEDURE GetPackageVersion
GO

CREATE PROCEDURE GetPackageVersion
@OwnerName VARCHAR(256),
@StageName VARCHAR(256),
@Id VARCHAR(1024),
@Version VARCHAR(1024)
AS
	SELECT
		StagePackage.[Version],
		StagePackage.Staged,
		StagePackage.NuspecLocation
	FROM Stage
	INNER JOIN StagePackage ON Stage.[Key] = StagePackage.StageKey 
	INNER JOIN StageOwner ON StageOwner.StageKey = Stage.[Key]
	INNER JOIN [Owner] ON [Owner].[Key] = StageOwner.OwnerKey
	WHERE [Owner].Name = @OwnerName
		AND Stage.[Name] = @StageName
		AND StagePackage.[Id] = @Id
		AND StagePackage.[Version] = @Version
GO

IF OBJECT_ID('ExistsStage', 'P') IS NOT NULL
	DROP PROCEDURE ExistsStage
GO

CREATE PROCEDURE ExistsStage
@OwnerName VARCHAR(256),
@StageName VARCHAR(256)
AS
    SELECT 1
    FROM Stage
    INNER JOIN StageOwner ON Stage.[Key] = StageOwner.StageKey
    INNER JOIN [Owner] ON [Owner].[Key] = StageOwner.OwnerKey
    WHERE [Owner].Name = @OwnerName
        AND Stage.Name = @StageName
GO

IF OBJECT_ID('CreateOwner', 'P') IS NOT NULL
	DROP PROCEDURE CreateOwner
GO

CREATE PROCEDURE CreateOwner
@OwnerName VARCHAR(256)
AS
	IF EXISTS (
		SELECT *
		FROM [Owner]
		INNER JOIN StageOwner ON [Owner].[Key] = StageOwner.OwnerKey
		WHERE [Owner].[Name] = @OwnerName
		)
	BEGIN
		SELECT NULL
	END
	ELSE
	BEGIN
		DECLARE @ApiKey AS VARCHAR(64)
		SELECT @ApiKey = CONVERT(VARCHAR(64), NEWID())

		INSERT [Owner] ( Name, ApiKey ) VALUES ( @OwnerName, @ApiKey )

		SELECT @ApiKey
	END
GO

IF OBJECT_ID('CreateStage', 'P') IS NOT NULL
	DROP PROCEDURE CreateStage
GO

CREATE PROCEDURE CreateStage
@OwnerName VARCHAR(256),
@StageName VARCHAR(256),
@BaseService VARCHAR(1024)
AS
	IF EXISTS (
		SELECT *
		FROM Stage
		INNER JOIN StageOwner ON [Stage].[Key] = StageOwner.StageKey
		INNER JOIN [Owner] ON [Owner].[Key] = StageOwner.OwnerKey
		WHERE [Owner].[Name] = @OwnerName
	      AND [Stage].[Name] = @StageName
		)
	BEGIN
		SELECT 0
	END
	ELSE
	BEGIN
		DECLARE @OwnerKey INT
		SELECT @OwnerKey = [Key] FROM [Owner] WHERE [Name] = @OwnerName

		IF @OwnerKey IS NULL
		BEGIN
			SELECT 2
		END
		ELSE
		BEGIN
			BEGIN TRAN
			INSERT Stage ( Name, BaseService ) VALUES ( @StageName, @BaseService )
			DECLARE @StageKey INT = SCOPE_IDENTITY()
			INSERT StageOwner ( OwnerKey, StageKey ) VALUES ( @OwnerKey, @StageKey )
			COMMIT TRAN
	
			SELECT 1
		END
	END
GO

IF OBJECT_ID('CreatePackage', 'P') IS NOT NULL
	DROP PROCEDURE CreatePackage
GO

CREATE PROCEDURE CreatePackage
@OwnerName VARCHAR(256),
@StageName VARCHAR(256),
@Id VARCHAR(1024),
@Version VARCHAR(1024),
@PackageOwner VARCHAR(256),
@NupkgLocation VARCHAR(1024),
@NuspecLocation VARCHAR(1024),
@Staged DATETIME
AS
	IF EXISTS (
		SELECT *
		FROM StagePackage
		INNER JOIN Stage ON Stage.[Key] = StagePackage.StageKey
		INNER JOIN StageOwner ON Stage.[Key] = StageOwner.StageKey
		INNER JOIN [Owner] ON [Owner].[Key] = StageOwner.OwnerKey
		WHERE [Owner].[Name] = @OwnerName
	      AND Stage.[Name] = @StageName
	      AND StagePackage.[Id] = @Id
	      AND StagePackage.[Version] = @Version
		)
	BEGIN
		SELECT 1
	END
	ELSE
	BEGIN
		DECLARE @OwnerKey INT
		SELECT @OwnerKey = [Key] FROM [Owner] WHERE [Name] = @PackageOwner

		IF @OwnerKey IS NULL
		BEGIN
			SELECT 2
		END

		BEGIN TRAN

			INSERT INTO StagePackage ( [Id], [Version], StageKey, Staged, NupkgLocation, NuspecLocation )
			SELECT @Id, @Version, StageKey, @Staged, @NupkgLocation, @NuspecLocation
			FROM Stage
			INNER JOIN StageOwner ON Stage.[Key] = StageOwner.[StageKey]
			INNER JOIN [Owner] ON [Owner].[Key] = StageOwner.[OwnerKey]
			WHERE [Owner].[Name] = @OwnerName
			  AND Stage.[Name] = @StageName

			DECLARE @PackageKey INT = SCOPE_IDENTITY()

			INSERT StagePackageOwner (OwnerKey, PackageKey) VALUES (@OwnerKey, @PackageKey)

		COMMIT TRAN

		SELECT 0
	END
GO

IF OBJECT_ID('DeleteStage', 'P') IS NOT NULL
	DROP PROCEDURE DeleteStage
GO

CREATE PROCEDURE DeleteStage
@OwnerName VARCHAR(256),
@StageName VARCHAR(256)
AS
BEGIN
	DECLARE @StageKey INT
	DECLARE @OwnerKey INT
		
	SELECT @StageKey = StageOwner.StageKey, @OwnerKey = StageOwner.OwnerKey
	FROM Stage
	INNER JOIN StageOwner ON Stage.[Key] = StageOwner.StageKey
	INNER JOIN [Owner] ON [Owner].[Key] = StageOwner.OwnerKey
	WHERE [Owner].[Name] = @OwnerName
	    AND [Stage].[Name] = @StageName

	IF (@@ROWCOUNT > 0)
	BEGIN
		CREATE TABLE #DeletedPackagesTempTable (
			[Id] VARCHAR(1024),
			[Version] VARCHAR(1024),
			NupkgLocation VARCHAR(1024),
			NuspecLocation VARCHAR(1024)
		)

		/* always return at least one row to show we were successful, NULLs are ignored in the code */
		INSERT #DeletedPackagesTempTable VALUES ( NULL, NULL, NULL, NULL )

		INSERT INTO #DeletedPackagesTempTable
		SELECT 
			[Id],
			[Version],
			NupkgLocation,
			NuspecLocation
		FROM StagePackage
		WHERE StageKey = @StageKey

		BEGIN TRAN
			DELETE [Stage] WHERE [Key] = @StageKey
			DELETE StageOwner WHERE StageKey = @StageKey AND OwnerKey = @OwnerKey
			DELETE StagePackage WHERE StageKey = @StageKey
		COMMIT TRAN

		/* return a list of packages we have deleted */
		SELECT * FROM #DeletedPackagesTempTable
	END
END
GO

IF OBJECT_ID('DeletePackage', 'P') IS NOT NULL
	DROP PROCEDURE DeletePackage
GO

CREATE PROCEDURE DeletePackage
@OwnerName VARCHAR(256),
@StageName VARCHAR(256),
@Id VARCHAR(1024)
AS
BEGIN
	DECLARE @StageKey INT
	DECLARE @OwnerKey INT
		
	SELECT @StageKey = StageOwner.StageKey, @OwnerKey = StageOwner.OwnerKey
	FROM Stage
	INNER JOIN StageOwner ON Stage.[Key] = StageOwner.StageKey
	INNER JOIN [Owner] ON [Owner].[Key] = StageOwner.OwnerKey
	WHERE [Owner].[Name] = @OwnerName
	  AND Stage.[Name] = @StageName

	IF (@@ROWCOUNT > 0)
	BEGIN
		CREATE TABLE #DeletedPackagesTempTable (
			[Id] VARCHAR(1024),
			[Version] VARCHAR(1024),
			NupkgLocation VARCHAR(1024),
			NuspecLocation VARCHAR(1024)
		)

		/* always return at least one row to show we were successful, NULLs are ignored in the code */
		INSERT #DeletedPackagesTempTable VALUES ( NULL, NULL, NULL, NULL )

		INSERT INTO #DeletedPackagesTempTable
		SELECT 
			[Id],
			[Version],
			NupkgLocation,
			NuspecLocation
		FROM StagePackage
		WHERE StageKey = @StageKey
		  AND Id = @Id

		BEGIN TRAN
			DELETE StagePackage WHERE StageKey = @StageKey AND Id = @Id
		COMMIT TRAN

		/* return a list of packages we have deleted */
		SELECT * FROM #DeletedPackagesTempTable
	END
END
GO

IF OBJECT_ID('DeletePackageVersion', 'P') IS NOT NULL
	DROP PROCEDURE DeletePackageVersion
GO

CREATE PROCEDURE DeletePackageVersion
@OwnerName VARCHAR(256),
@StageName VARCHAR(256),
@Id VARCHAR(1024),
@Version VARCHAR(1024)
AS
BEGIN
	DECLARE @StagePackageKey INT
	DECLARE @NupkgLocation VARCHAR(1024)
	DECLARE @NuspecLocation VARCHAR(1024)

	SELECT 
		@StagePackageKey = StagePackage.[Key],
		@NupkgLocation = NupkgLocation,
		@NuspecLocation = NuspecLocation
	FROM StagePackage
    INNER JOIN Stage ON Stage.[Key] = StagePackage.StageKey
    INNER JOIN StageOwner ON Stage.[Key] = StageOwner.StageKey
    INNER JOIN [Owner] ON [Owner].[Key] = StageOwner.OwnerKey
    WHERE [Owner].[Name] = @OwnerName
        AND Stage.[Name] = @StageName
        AND StagePackage.[Id] = @Id
        AND StagePackage.[Version] = @Version

    DELETE StagePackage WHERE [Key] = @StagePackageKey

	IF (@@ROWCOUNT > 0)
	BEGIN
		SELECT @NupkgLocation, @NuspecLocation
	END
END
GO

IF OBJECT_ID('CheckAccess', 'P') IS NOT NULL
	DROP PROCEDURE CheckAccess
GO

CREATE PROCEDURE CheckAccess
@StageName VARCHAR(256),
@ApiKey VARCHAR(64)
AS
	SELECT [Owner].Name
	FROM [Owner]
	INNER JOIN StageOwner ON StageOwner.OwnerKey = [Owner].[Key]
	INNER JOIN Stage ON StageOwner.StageKey = Stage.[Key]
	WHERE Stage.Name = @StageName
	  AND [Owner].ApiKey = @ApiKey
GO

IF OBJECT_ID('AddStageOwner', 'P') IS NOT NULL
	DROP PROCEDURE AddStageOwner
GO

CREATE PROCEDURE AddStageOwner
@OwnerName VARCHAR(256),
@StageName VARCHAR(256),
@NewOwnerName VARCHAR(256)
AS
	DECLARE @NewOwnerKey INT
	SELECT @NewOwnerKey = [Key]
	FROM [Owner]
	WHERE Name = @NewOwnerName
	
	IF @NewOwnerKey IS NULL
	BEGIN
		SELECT 2
	END

	IF NOT EXISTS (SELECT [Key] FROM [Owner] WHERE [Owner].Name = @OwnerName)
	BEGIN
		SELECT 2
	END

	INSERT StageOwner ( OwnerKey, StageKey )
	SELECT @NewOwnerKey, Stage.[Key]
	FROM Stage
	INNER JOIN StageOwner ON [Stage].[Key] = StageOwner.StageKey 
	INNER JOIN [Owner] ON [Owner].[Key] = StageOwner.OwnerKey
	WHERE [Owner].Name = @OwnerName
	  AND Stage.Name = @StageName

	IF (@@ROWCOUNT = 1)
	BEGIN
		SELECT 1
	END

	SELECT 0
GO
