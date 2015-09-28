
IF OBJECT_ID('CreateStage', 'P') IS NOT NULL
	DROP PROCEDURE CreateStage
GO

CREATE PROCEDURE CreateStage
@OwnerName VARCHAR(1024),
@StageId VARCHAR(1024)
AS
	IF EXISTS (
		SELECT *
		FROM Stage
		INNER JOIN [StageOwner] ON [Stage].[Key] = [StageOwner].[StageKey]
		INNER JOIN [Owner] ON [Owner].[Key] = [StageOwner].[OwnerKey]
		WHERE [Owner].[Name] = @OwnerName
	      AND [Stage].[Id] = @StageId
		)
	BEGIN
		SELECT 0
	END
	ELSE
	BEGIN
		BEGIN TRAN

		DECLARE @OwnerKey INT
		SELECT @OwnerKey = [Key] FROM [Owner] WHERE [Name] = @OwnerName

		INSERT Stage ( Id ) VALUES ( @StageId )
		DECLARE @StageKey INT = SCOPE_IDENTITY()

		INSERT StageOwner ( OwnerKey, StageKey ) VALUES ( @OwnerKey, @StageKey )

		COMMIT TRAN

		SELECT 1
	END
GO

IF OBJECT_ID('DeleteStage', 'P') IS NOT NULL
	DROP PROCEDURE DeleteStage
GO

CREATE PROCEDURE DeleteStage
@OwnerName VARCHAR(1024),
@StageId VARCHAR(1024)
AS
BEGIN
	DECLARE @StageKey INT
	DECLARE @OwnerKey INT
		
	SELECT @StageKey = [StageOwner].[StageKey], @OwnerKey = [StageOwner].[OwnerKey]
	FROM Stage
	INNER JOIN [StageOwner] ON [Stage].[Key] = [StageOwner].[StageKey]
	INNER JOIN [Owner] ON [Owner].[Key] = [StageOwner].[OwnerKey]
	WHERE [Owner].[Name] = @OwnerName
	    AND [Stage].[Id] = @StageId

	IF (@@ROWCOUNT > 0)
	BEGIN
		CREATE TABLE #DeletedPackagesTempTable (
			[Id] VARCHAR(1024),
			[Version] VARCHAR(1024),
			[NupkgLocation] VARCHAR(1024),
			[NuspecLocation] VARCHAR(1024)
		)

		/* always return at least one row to show we were successful, NULLs are ignored in the code */
		INSERT #DeletedPackagesTempTable VALUES ( NULL, NULL, NULL, NULL )

		INSERT INTO #DeletedPackagesTempTable
		SELECT 
			[Id],
			[Version],
			NupkgLocation,
			NuspecLocation
		FROM [StagePackage]
		WHERE StageKey = @StageKey

		BEGIN TRAN
			DELETE [Stage] WHERE [Key] = @StageKey
			DELETE [StageOwner] WHERE StageKey = @StageKey AND OwnerKey = @OwnerKey
			DELETE [StagePackage] WHERE StageKey = @StageKey
		COMMIT TRAN

		/* return a list of packages we have deleted */
		SELECT * FROM #DeletedPackagesTempTable
	END
END
GO

IF OBJECT_ID('CreatePackage', 'P') IS NOT NULL
	DROP PROCEDURE CreatePackage
GO

CREATE PROCEDURE CreatePackage
@OwnerName VARCHAR(1024),
@StageId VARCHAR(1024),
@Id VARCHAR(1024),
@Version VARCHAR(1024),
@NupkgLocation VARCHAR(1024),
@NuspecLocation VARCHAR(1024)
AS
	IF EXISTS (
		SELECT *
		FROM StagePackage
		INNER JOIN [Stage] ON [Stage].[Key] = [StagePackage].[StageKey]
		INNER JOIN [StageOwner] ON [Stage].[Key] = [StageOwner].[StageKey]
		INNER JOIN [Owner] ON [Owner].[Key] = [StageOwner].[OwnerKey]
		WHERE [Owner].[Name] = @OwnerName
	      AND [Stage].[Id] = @StageId
	      AND [StagePackage].[Id] = @Id
	      AND [StagePackage].[Version] = @Version
		)
	BEGIN
		SELECT 0
	END
	ELSE
	BEGIN
		INSERT INTO StagePackage ( [Id], [Version], StageKey, NupkgLocation, NuspecLocation )
		SELECT @Id, @Version, StageKey, @NupkgLocation, @NuspecLocation
		FROM Stage
		INNER JOIN [StageOwner] ON [Stage].[Key] = [StageOwner].[StageKey]
		INNER JOIN [Owner] ON [Owner].[Key] = [StageOwner].[OwnerKey]
		WHERE [Owner].[Name] = @OwnerName
			AND [Stage].[Id] = @StageId

		SELECT 1
	END
GO

IF OBJECT_ID('DeletePackage', 'P') IS NOT NULL
	DROP PROCEDURE DeletePackage
GO

CREATE PROCEDURE DeletePackage
@OwnerName VARCHAR(1024),
@StageId VARCHAR(1024),
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
    INNER JOIN [Stage] ON [Stage].[Key] = [StagePackage].[StageKey]
    INNER JOIN [StageOwner] ON [Stage].[Key] = [StageOwner].[StageKey]
    INNER JOIN [Owner] ON [Owner].[Key] = [StageOwner].[OwnerKey]
    WHERE [Owner].[Name] = @OwnerName
        AND [Stage].[Id] = @StageId
        AND [StagePackage].[Id] = @Id
        AND [StagePackage].[Version] = @Version

    DELETE StagePackage WHERE [Key] = @StagePackageKey

	IF (@@ROWCOUNT > 0)
	BEGIN
		SELECT @NupkgLocation, @NuspecLocation
	END
END
GO


