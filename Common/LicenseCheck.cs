using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Reflection;
using System.Data.SqlClient;
using IAM.CA;
using IAM.GlobalDefs;
using SafeTrend.Data;
using SafeTrend.Data.SqlClient;

namespace IAM.License
{

    public class LicenseControl : IDisposable
    {
        public Int32 Count { get; set; }
        public Boolean Notified { get; set; }
        public Boolean Valid { get; internal set; }
        public Int32 Entities { get; internal set; }
        public String Error { get; internal set; }
        public String InstallationKey { get; internal set; }

        public LicenseControl(Int32 Entities, Int32 count, String installationKey) :
            this(true, Entities, count, null, installationKey) { }

        public LicenseControl(Int32 Entities, String Error, String installationKey) :
            this(false, Entities, 0, Error, installationKey) { }


        public LicenseControl(Boolean valid, Int32 entities, Int32 count, String error, String installationKey)
        {
            this.Valid = valid;
            this.Entities = entities;
            this.Count = count;
            this.Error = error;
            this.InstallationKey = installationKey;

            if ((!this.Valid) && (String.IsNullOrWhiteSpace(Error)))
                throw new Exception("Error text not found");
        }

        public void Dispose()
        {

        }
    }

    public static class LicenseChecker
    {
        public static LicenseControl GetLicenseData(SqlConnection conn, SqlTransaction trans, Int64 enterpriseId)
        {
            //Retorna zero para ilimitado
            
            
            
            String installKey = "";
            try
            {
                using (IAMDatabase db = new IAMDatabase(conn))
                {
                    //Server installation key
                    using (IAM.Config.ServerKey2 sk = new IAM.Config.ServerKey2(db.Connection))
                        installKey = sk.ServerInstallationKey.AbsoluteUri;

                    //Resgata todas as licenças desta empresa e de servidor
                    DataTable dtLic = db.ExecuteDataTable("select * from license where enterprise_id in (0, " + enterpriseId + ")", trans);
                    if (dtLic == null)
                        return new LicenseControl(1, "Error on get licenses on server", installKey);

                    if (dtLic.Rows.Count == 0)
                        return new LicenseControl(1, "License not found", installKey);

                    //Localiza a licença menos restrita
                    IAMKeyData key = null;
                    foreach (DataRow dr in dtLic.Rows)
                    {
                        try
                        {
                            IAMKeyData k = IAMKey.ExtractFromCert(dr["license_data"].ToString());

                            //Checa a validade da licença
                            if ((k.IsTemp) && (k.TempDate.Value.CompareTo(DateTime.Now) < 0))
                                continue;

                            if (key == null)
                                key = k;

                            if (k.NumLic > key.NumLic)
                                key = k;
                        }
                        catch { }
                    }

                    if (key == null)
                        return new LicenseControl(1, "License not found", installKey);

                    //Resgata do banco a contagem atual de entidades
                    Int32 count = db.ExecuteScalar<Int32>(conn, "select count(e.id) from entity e with(nolock) inner join context c with(nolock) on c.id = e.context_id where e.deleted = 0 and c.enterprise_id = " + enterpriseId, CommandType.Text, null, trans);

                    LicenseControl lc = new LicenseControl((Int32)key.NumLic, count, installKey);

                    return lc;
                }
            }
            catch (Exception ex)
            {
                return new LicenseControl(0, ex.Message, installKey);
            }

            
        }


    }
}
