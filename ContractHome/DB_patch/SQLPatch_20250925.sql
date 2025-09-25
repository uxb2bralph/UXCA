USE [DigitalContract]
GO

/****** Object:  Table [dbo].[Contract]    Script Date: 2025/9/25 ¤U¤È 04:09:38 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

ALTER TABLE Contract
ADD CreateSourceType INT NOT NULL
CONSTRAINT DF_Contract_CreateSourceType DEFAULT (0);

