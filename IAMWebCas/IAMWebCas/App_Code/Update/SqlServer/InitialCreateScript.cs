using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SafeTrend.Data.Update.SqlServer
{
    public class InitialCreateScript : IUpdateScript
    {
        public string Command
        {
            get
            {
                return @"

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'CAS_Context'))
                    BEGIN
                            CREATE TABLE [CAS_Context] (
                                [Name] VarChar(255) Not Null,
                                [Host] VarChar(255) Not Null,
                                Constraint [PK_CAS_Context] Primary Key ([Name]),
                                Constraint [UNQ_Context_1] Unique ([Name], [Host])
                            );
                    END
                    
                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'CAS_Service'))
                    BEGIN
                        CREATE TABLE [CAS_Service] (
                            [Context_Name] VarChar(255) Not Null,
                            [Uri] VarChar(2000) Not Null,
                            [Plugin_Assembly] VarChar(255) Not Null,
                            [Permit_Password_Recover] Bit Not Null,
                            [External_Password_Recover] Bit Not Null,
                            [Password_RecoverUri] VarChar(2000) Null,
                            [Permit_Change_Password] Bit Not Null,
                            [Admin] VarChar(255) Not Null,
                            Constraint [PK_CAS_Service] Primary Key ([Uri]),
                            Foreign Key ([Context_Name]) References [CAS_Context]([Name])
                        );
                    END
                    
                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'CAS_Service_Attributes'))
                    BEGIN
                        CREATE TABLE [CAS_Service_Attributes] (
                            [Service_Uri] VarChar(2000) Not Null,
                            [Key] VarChar(255) Not Null,
                            [Value] VarChar(500) Not Null,
                            Foreign Key ([Service_Uri]) References [CAS_Service]([Uri])
                        );
                    END
                    
                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'CAS_Role'))
                    BEGIN

                        CREATE TABLE [CAS_Role] (
                            [Name] VarChar(255) Not Null,
                            [Description] VarChar(255) Null,
                            Constraint [PK_CAS_Role] Primary Key ([Name])
                        );
                    END
                    
                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'CAS_User'))
                    BEGIN

                        CREATE TABLE [CAS_User] (
                            [Name] VarChar(255) Not Null,
                            [Surname] VarChar(255) Not Null,
                            [Username] VarChar(255) Not Null,
                            [Password] VarChar(255) Not Null,
                            [Email] VarChar(255) Not Null,
                            Constraint [PK_CAS_User] Primary Key ([Username])
                        );
                    END
                    
                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'CAS_UserRole_InRole'))
                    BEGIN

                        CREATE TABLE [CAS_UserRole_InRole] (
                            [User_Username] VarChar(255) Not Null,
                            [Role_Name] VarChar(255) Not Null,
                            Constraint [UNQ_UserRole_InRole_1] Unique ([User_Username], [Role_Name]),
                            Foreign Key ([User_Username]) References [CAS_User]([Username]),
                            Foreign Key ([Role_Name]) References [CAS_Role]([Name])
                        );
                    END
                    

                    IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'CAS_Ticket'))
                    BEGIN

                        CREATE TABLE [CAS_Ticket] (
                            [Service_Uri] VarChar(2000) Not Null,
                            [User_Id] VarChar(255) Not Null,
                            [User_Name] VarChar(255) Not Null,
                            [Grant_Ticket] VarChar(255) Not Null,
                            [Long_Ticket] VarChar(255) Not Null,
                            [Proxy_Ticket] VarChar(255) Not Null,
                            [Create_Date] Datetime Not Null,
                            [Expires] Datetime Not Null,
                            [Create_By_Credentials] Bit Not Null,
                            Foreign Key ([Service_Uri]) References [CAS_Service]([Uri])
                        );
                    END

                    ";
            }
        }

        public string Precondition
        {
            get { return null; }
        }

        public double Serial { get { return 0; } }
        public string Provider { get { return "System.Data.SqlClient"; } }
    }
}
