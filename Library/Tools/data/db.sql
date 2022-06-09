/*
 * db.sql --
 *
 * Extensible Adaptable Generalized Logic Engine (Eagle)
 * Database CLR Assembly Setup Script
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

EXECUTE sp_configure 'clr enabled', 1;
GO

RECONFIGURE;
GO

IF NOT EXISTS(SELECT name FROM sys.databases
              WHERE name = '$(DatabaseName)')
BEGIN
  CREATE DATABASE $(DatabaseName);
END
GO

ALTER DATABASE $(DatabaseName) SET TRUSTWORTHY ON;
GO

USE $(DatabaseName);
GO

CREATE ASSEMBLY [$(AssemblyName)] FROM '$(FileName)'
    WITH PERMISSION_SET = UNSAFE;
GO

CREATE PROCEDURE $(DatabaseName)_EvaluateScript
    @input NVARCHAR(MAX),
    @output NVARCHAR(MAX) OUTPUT
WITH EXECUTE AS CALLER
AS EXTERNAL NAME
    [$(AssemblyName)].[$(ClassName)].EvaluateOneScript;
GO

CREATE PROCEDURE $(DatabaseName)_SubstituteString
    @input NVARCHAR(MAX),
    @output NVARCHAR(MAX) OUTPUT
WITH EXECUTE AS CALLER
AS EXTERNAL NAME
    [$(AssemblyName)].[$(ClassName)].SubstituteOneString;
GO
