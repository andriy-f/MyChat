CREATE DATABASE [ChatServer]
GO
USE [ChatServer]
GO
/****** Object:  Table [dbo].[Logins]    Script Date: 01/24/2012 21:56:41 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Logins](
	[Logins] [nvarchar](max) NOT NULL,
	[Passwords] [nvarchar](max) NOT NULL
) ON [PRIMARY]
GO
INSERT [dbo].[Logins] ([Logins], [Passwords]) VALUES (N'admin', N'admpass123')
INSERT [dbo].[Logins] ([Logins], [Passwords]) VALUES (N'user1', N'qwe`123')
INSERT [dbo].[Logins] ([Logins], [Passwords]) VALUES (N'user2', N'qwe`123')
INSERT [dbo].[Logins] ([Logins], [Passwords]) VALUES (N'user4', N'qwe123')
