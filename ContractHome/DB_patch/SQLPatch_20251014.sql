USE [DigitalContract]
GO

/****** Object:  Table [dbo].[Contract]    Script Date: 2025/9/25 ¤U¤È 04:09:38 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

ALTER TABLE UserProfile
ADD IsEnabled bit NOT NULL DEFAULT 1;

