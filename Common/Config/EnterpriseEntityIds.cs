using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Security.Cryptography;
using System.Text;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Data;
using System.Data.SqlClient;
using IAM.GlobalDefs;
using SafeTrend.Data;
using SafeTrend.Data.SqlClient;

namespace IAM.Config
{
    public class EnterpriseEntityIDItem: IDisposable
    {
        public Int64 EntityId { get; internal set; }
        public String Value { get; internal set; }
        public Boolean IsLogin { get; internal set; }
        public Boolean IsMail { get; internal set; }

        public EnterpriseEntityIDItem(Int64 EntityId, String Value, Boolean IsLogin, Boolean IsMail)
        {
            this.EntityId = EntityId;
            this.Value = Value;
            this.IsLogin = IsLogin;
            this.IsMail = IsMail;
        }

        public void Dispose()
        {
            //Value = null;
        }
    }

    public class EnterpriseEntityIds :  SqlBase, IDisposable
    {
        Dictionary<Int64, List<EnterpriseEntityIDItem>> items;

        public EnterpriseEntityIds()
        {
            items = new Dictionary<Int64, List<EnterpriseEntityIDItem>>();
        }

        public void GetDBConfig(SqlConnection conn)
        {
            this.Connection = conn;
            DataTable dt = ExecuteDataTable("select * from vw_entity_ids with(nolock)");

            if ((dt == null) || (dt.Rows.Count == 0))
                return;

            foreach (DataRow dr in dt.Rows)
                AddItem((Int64)dr["context_id"], (Int64)dr["id"], dr["value"].ToString().ToLower(), (Boolean)dr["is_login"], (Boolean)dr["is_mail"]);
        }

        public void AddItem(Int64 contextId, Int64 EntityId, String Value, Boolean IsLogin, Boolean IsMail)
        {
            if (!items.ContainsKey(contextId))
                items.Add(contextId, new List<EnterpriseEntityIDItem>());

            if (!items[contextId].Exists(u => (u.EntityId == EntityId && u.IsLogin == IsLogin && u.IsMail == IsMail && u.Value == Value.ToLower().Trim())))
                items[contextId].Add(new EnterpriseEntityIDItem(EntityId, Value.ToLower().Trim(), IsLogin, IsMail));
        }

        public List<EnterpriseEntityIDItem> GetItem(Int64 contextId)
        {
            if (items == null)
                return null;

            if (!items.ContainsKey(contextId))
                return null;

            return items[contextId];
        }

        public EnterpriseEntityIDItem GetEntityByIds(Int64 contextId, String value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return null;

            if (items == null)
                return null;

            if (!items.ContainsKey(contextId))
                return null;

            value = value.ToLower().Trim();

            return items[contextId].Find(u => (u.Value == value));
            /*
            return items[contextId].Find(delegate(EnterpriseEntityIDItem u)
            {
                return u.Value == value;
            });*/
        }


        public EnterpriseEntityIDItem GetEntityByMail(Int64 contextId, String value)
        {
            if (items == null)
                return null;

            if (!items.ContainsKey(contextId))
                return null;

            value = value.ToLower().Trim();

            return items[contextId].Find(u => (u.Value == value && u.IsMail));
        }


        public EnterpriseEntityIDItem GetEntityByLogin(Int64 contextId, String value)
        {
            if (items == null)
                return null;

            if (!items.ContainsKey(contextId))
                return null;

            value = value.ToLower().Trim();

            return items[contextId].Find(u => (u.Value == value && u.IsLogin));
        }

        public Boolean HasOtherLogin(Int64 contextId, Int64 entityId, String login)
        {
            if (items == null)
                return false;

            if (!items.ContainsKey(contextId))
                return false;

            login = login.ToLower().Trim();

            return items[contextId].Exists(u => (u.Value == login && u.IsLogin && u.EntityId != entityId));
        }


        public Boolean HasOtherMail(Int64 contextId, Int64 entityId, String mail)
        {
            if (items == null)
                return false;

            if (!items.ContainsKey(contextId))
                return false;

            mail = mail.ToLower().Trim();

            return items[contextId].Exists(u => (u.Value == mail && u.IsMail && u.EntityId != entityId));
        }

        public List<String> GetMails(Int64 contextId, Int64 entityId)
        {
            List<String> mails = new List<String>();

            if (items == null)
                return mails;

            if (!items.ContainsKey(contextId))
                return mails;

            foreach (EnterpriseEntityIDItem i in items[contextId].FindAll(u => (u.EntityId == entityId && u.IsMail)))
                mails.Add(i.Value);

            return mails;
        }


        public List<String> GetMailsInDomain(Int64 contextId, Int64 entityId, String domain)
        {
            List<String> mails = new List<String>();

            if (items == null)
                return mails;

            if (!items.ContainsKey(contextId))
                return mails;

            foreach (EnterpriseEntityIDItem i in items[contextId].FindAll(u => (u.EntityId == entityId && u.IsMail && u.Value.IndexOf(domain.ToLower().Trim()) > 0)))
                mails.Add(i.Value);

            return mails;
        }

        public void Dispose()
        {
            foreach (List<EnterpriseEntityIDItem> li in items.Values)
            {
                foreach (EnterpriseEntityIDItem i in li)
                    i.Dispose();

                li.Clear();
            }

            items.Clear();
        }
    }
}
