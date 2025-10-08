USE [DigitalContract]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- 合約分類
CREATE TABLE [dbo].[ContractCategory](
	[ContractCategoryID] [int] IDENTITY(1,1) NOT NULL,
	[Code] [nvarchar](256) NOT NULL,
	[CompanyID] [int] NOT NULL,
	[CategoryName] [nvarchar](512) NOT NULL,
	[CreateUID] [int] NOT NULL,
	[CreateDate] [datetime] NOT NULL,
	[ModifyUID] [int] NULL,
	[ModifyDate] [datetime] NULL,
 CONSTRAINT [PK_ContractCategory] PRIMARY KEY CLUSTERED 
(
	[ContractCategoryID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[ContractCategory] ADD  CONSTRAINT [DF_ContractCategory_CreateDate]  DEFAULT (getdate()) FOR [CreateDate]
GO

-- 合約權限
CREATE TABLE [dbo].[ContractCategoryPermission](
	[ContractCategoryPermissionID] [int] IDENTITY(1,1) NOT NULL,
	[ContractCategoryID] [int] NOT NULL,
	[UID] [int] NOT NULL,
	[CreateUID] [int] NOT NULL,
	[CreateDate] [datetime] NOT NULL,
 CONSTRAINT [PK_ContractCategoryPermission] PRIMARY KEY CLUSTERED 
(
	[ContractCategoryPermissionID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[ContractCategoryPermission] ADD  CONSTRAINT [DF_ContractCategoryPermission_CreateDate]  DEFAULT (getdate()) FOR [CreateDate]
GO

--常用簽署人
CREATE TABLE [dbo].[FavoriteSigner](
	[FavoriteSignerID] [int] IDENTITY(1,1) NOT NULL,
	[SignerUID] [int] NOT NULL,
	[CreateUID] [int] NOT NULL,
 CONSTRAINT [PK_FavoriteSigner] PRIMARY KEY CLUSTERED 
(
	[FavoriteSignerID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO


--公司建立人ID
ALTER TABLE [dbo].[Organization] ADD CreateUID INT NULL;
GO
--分類ID
ALTER TABLE [dbo].[Contract] ADD ContractCategoryID INT NOT NULL CONSTRAINT DF_Contract_ContractCategoryID DEFAULT (0);
GO