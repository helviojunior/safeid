using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IAM.GlobalDefs
{

    public enum UserLogLevel
    {
        Debug = 0,
        Trace = 100,
        Info = 200,
        Warning = 300,
        Error = 400,
        Fatal = 500
    }


    public enum LogKey
    {
        Undefined = 0,
        Certificate_Error = 1,
        Encrypt_Error = 2,
        Dencrypt_Error = 3,
        Licence_error = 4,
        Debug = 5,
        Deploy = 6,
        Inbound = 7,
        Engine = 8,
        Watchdog = 9,
        Workflow = 10,
        Report = 11,
        Backup = 12,

        /* User logs 1000 */
        User_Logged = 1001,
        User_NewRecoveryCode = 1002,
        User_PasswordChanged = 1003,
        User_PasswordReseted = 1004,
        User_WrongUserAndPassword = 1005,
        User_WrongPassword = 1006,
        User_Locked = 1007,
        User_Unlocked = 1008,
        User_Deploy = 1009,
        User_DeployMark = 1010,
        User_Deleted = 1011,
        User_Undeleted = 1012,
        User_AccessDenied = 1013,
        User_Update = 1014,
        User_ImportError = 1015,
        User_Added = 1016,
        User_IdentityRoleBind = 1017,
        User_IdentityRoleUnbind = 1018,
        User_ImportInfo = 1019,
        User_WrongTicket = 1020,
        User_IdentityNew = 1021,
        User_IdentityDeleted = 1022,
        User_SystemRoleBind = 1023,
        User_SystemRoleUnbind = 1024,
        User_TempLocked = 1025,
        User_TempUnlocked = 1026,
        User_PasswordCreated = 1027,
        User_PropertyChanged = 1028,
        User_ContainerRoleBind = 1017,
        User_ContainerRoleUnbind = 1018,


        /* API logs 2000 */
        API_Error = 2001,
        Proxy_Event = 2002,
        Plugin_Event = 2003,
        API_Log = 2004,

        /* Autoservice logs 3000 */

        /* Role 4000 */
        Role_Deploy = 4001,
        Role_Deleted = 4002,
        Role_Inserted = 4003,
        Role_Changed = 4004,

        /* Import 5000 */
        Import = 5001,

        /* Role 6000 */
        Context_Deleted = 6001,
        Context_Inserted = 6002,
        Context_Changed = 6003,

        /* Plugin 7000 */
        Plugin_Deleted = 7001,
        Plugin_Inserted = 7002,
        Plugin_Changed = 7003,

        /* Proxy 8000 */
        Proxy_Deleted = 8001,
        Proxy_Inserted = 8002,
        Proxy_Changed = 8003,
        Proxy_ResetRequest = 8004,

        /* Resource 9000 */
        Resource_Deleted = 9001,
        Resource_Inserted = 9002,
        Resource_Changed = 9003,

        /* Resource 9000 */
        ResourcePlugin_Deleted = 10001,
        ResourcePlugin_Inserted = 10002,
        ResourcePlugin_Changed = 10003,
        ResourcePluginParameters_Changed = 10004,
        ResourcePluginMapping_Changed = 10005,
        ResourcePluginRole_Changed = 10006,
        ResourcePluginIdentity_Changed = 10007,
        ResourcePluginLockExpression_Changed = 10008,
        ResourcePluginLockSchedule_Changed = 10009,
        ResourcePluginDeploy = 10009,

        /* Field 11000 */
        Field_Deleted = 11001,
        Field_Inserted = 11002,
        Field_Changed = 11003,

        /* System roles 12000 */
        SystemRole_Deploy = 12001,
        SystemRole_Deleted = 12002,
        SystemRole_Inserted = 12003,
        SystemRole_Changed = 12004,
        SystemRolePermission_Changed = 12005,


        /* Filter 13000 */
        Filter_Deleted = 13001,
        Filter_Inserted = 13002,
        Filter_Changed = 13003,


    }

}
