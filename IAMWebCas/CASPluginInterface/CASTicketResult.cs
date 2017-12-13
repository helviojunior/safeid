using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Data;
using SafeTrend.Data;

namespace CAS.PluginInterface
{
    [Serializable()]
    public class CASTicketResult: CASJsonBase, ICloneable
    {
        public Boolean Success;
        public Uri Service;
        public String UserId;
        public String UserName;
        public String GrantTicket;
        public String LongTicket;
        public String ProxyTicket;
        public DateTime CreateDate;
        public DateTime Expires;
        public Boolean CreateByCredentials;
        public Boolean ChangePasswordNextLogon;
        public String ErrorText;

        [OptionalField()]
        public Dictionary<String, String> Attributes;

        public CASTicketResult()
        {
            this.CreateDate = DateTime.Now;
            this.Expires = DateTime.Now.AddDays(1);
            this.Success = false; //Definir como falso por padrão pois é usado em outras áreas do sistema
            this.CreateByCredentials = false;
            this.Attributes = new Dictionary<string, string>();
        }

        public CASTicketResult(String errorText)
            :base()
        {
            this.ErrorText = errorText;
        }

        public Object Clone()
        {
            CASTicketResult newItem = new CASTicketResult();
            newItem.Success = this.Success;
            newItem.Service = CASPluginService.Normalize(this.Service);
            newItem.UserId = this.UserId;
            newItem.UserName = this.UserName;
            newItem.GrantTicket = this.GrantTicket;
            newItem.LongTicket = this.LongTicket;
            newItem.ProxyTicket = this.ProxyTicket;
            newItem.CreateDate = this.CreateDate;
            newItem.Expires = this.Expires;
            newItem.CreateByCredentials = this.CreateByCredentials;
            newItem.ChangePasswordNextLogon = this.ChangePasswordNextLogon;
            newItem.ErrorText = this.ErrorText;
            newItem.Attributes = this.Attributes;

            return newItem;
        }

        public void SaveToDb(DbBase database)
        {
            
            //Exclui ticket com o mesmo serviço e grant ticket
            database.ExecuteNonQuery(String.Format("delete from [CAS_Ticket] where [Service_Uri] = '{0}' and Grant_Ticket = '{1}'", CASPluginService.Normalize(this.Service).AbsoluteUri, this.GrantTicket));
            
            if (!this.Success)
                return;

            //Adiciona o ticket
            database.ExecuteNonQuery(String.Format("insert into [CAS_Ticket] ([Service_Uri],[User_Id],[User_Name],[Grant_Ticket],[Long_Ticket],[Proxy_Ticket],[Create_Date],[Expires],[Create_By_Credentials]) VALUES ('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}',{8})", 
                CASPluginService.Normalize(this.Service).AbsoluteUri, 
                this.UserId, 
                this.UserName,
                this.GrantTicket,
                this.LongTicket,
                this.ProxyTicket,
                this.CreateDate.ToString("yyyy-MM-dd HH:mm:ss"),
                this.Expires.ToString("yyyy-MM-dd HH:mm:ss"),
                (this.CreateByCredentials ? 1 : 0)));

        }

        /*
        public void SaveToFile()
        {
            System.Reflection.Assembly asm = System.Reflection.Assembly.GetAssembly(this.GetType());
            SaveToFile(Path.GetFullPath(asm.Location));
            asm = null;
        }

        public void SaveToFile(String basePath)
        {
            if (!Success)
                return;
            
            String jData = Serialize<CASTicketResult>(this);

            //Salva 2 arquivos, um com nome do tiket, outro com nome do usuário

            String tFile = Path.Combine(basePath, Service.Host + (Service.Port != 80 ? "-" + Service.Port : "") + "\\{0}.tgc");
            FileInfo tokenFile = new FileInfo(String.Format(tFile, this.GrantTicket));

            if (!tokenFile.Directory.Exists)
                tokenFile.Directory.Create();

            File.WriteAllText(tokenFile.FullName, jData, Encoding.UTF8);
            File.WriteAllText(String.Format(tFile, this.UserName), jData, Encoding.UTF8);
            
            tokenFile = null;
        }*/

        static public CASTicketResult GetToken(DbBase database, Uri service, String grantTicket)
        {
            return GetToken(database, service, grantTicket, null);
        }

        static public CASTicketResult GetToken(DbBase database, Uri service, String grantTicket, String username)
        {


            CASTicketResult ret = null;
            if (service != null)
            {
                DataTable dtTickets = database.ExecuteDataTable(String.Format("select * from [CAS_Ticket] where [Service_Uri] = '{0}' and (Grant_Ticket = '{1}' or User_Name = '{2}')", CASPluginService.Normalize(service).AbsoluteUri, grantTicket, username));
                if ((dtTickets != null) && (dtTickets.Rows.Count > 0))
                {
                    ret = new CASTicketResult();
                    ret.Success = true;
                    ret.Service = CASPluginService.Normalize(dtTickets.Rows[0]["Service_Uri"].ToString());
                    ret.UserId = dtTickets.Rows[0]["User_Id"].ToString();
                    ret.UserName = dtTickets.Rows[0]["User_Name"].ToString();
                    ret.GrantTicket = dtTickets.Rows[0]["Grant_Ticket"].ToString();
                    ret.LongTicket = dtTickets.Rows[0]["Long_Ticket"].ToString();
                    ret.ProxyTicket = dtTickets.Rows[0]["Proxy_Ticket"].ToString();
                    ret.CreateDate = (DateTime)dtTickets.Rows[0]["Create_Date"];
                    ret.Expires = (DateTime)dtTickets.Rows[0]["Expires"];
                    ret.CreateByCredentials = (Boolean)dtTickets.Rows[0]["Create_By_Credentials"];
                }
            }
                        
            if (ret == null)
                ret = new CASTicketResult();

            if (ret.Success)
            {
                if (service == null)
                {
                    ret.Success = false;
                    return ret;
                }

                //Verifica se o ticket pode ser validado no serviço atual
                if ((ret.Service == null) || (!ret.Service.Equals(service)))
                {
                    if (database.ExecuteScalar<Int64>(String.Format("select COUNT(*) from [CAS_Service] where Uri = '{0}' and Context_Name = (select Context_Name from [CAS_Service] where Uri = '{1}')", CASPluginService.Normalize(service).AbsoluteUri, ret.Service.AbsoluteUri)) > 0)
                        ret.CreateByCredentials = false; //Define que as informações foram copiadas de outro token e não a partir de uma autenticação usuário/senha
                    else
                        ret.Success = false;
                }

                //Define o serviço atual
                ret.Service = service;

                //Salva o token copiado
                //ret.SaveToFile(basePath);
                ret.SaveToDb(database);
            }

            return ret;

        }

        public void Destroy(DbBase database)
        {
            if ((this.Service == null) || (this.GrantTicket == null))
                return;

            database.ExecuteNonQuery(String.Format("delete from [CAS_Ticket] where [Service_Uri] = '{0}' and Grant_Ticket = '{1}'", CASPluginService.Normalize(this.Service).AbsoluteUri, this.GrantTicket));
        }

        public void BuildTokenCodes()
        {
            this.CreateDate = DateTime.Now;
            this.Expires = DateTime.Now.AddDays(1);
            this.Success = true;
            this.GrantTicket = NewToken();
            this.LongTicket = NewToken();
            this.ProxyTicket = NewToken();

        }

        private String NewToken()
        {


            // Define supported password characters divided into groups.
            // You can add (or remove) characters to (from) these groups.
            string PASSWORD_CHARS_LCASE = "abcdefgijkmnopqrstwxyz";
            string PASSWORD_CHARS_UCASE = "ABCDEFGHJKLMNPQRSTWXYZ";
            string PASSWORD_CHARS_NUMERIC = "23456789";


            // Create a local array containing supported password characters
            // grouped by types. You can remove character groups from this
            // array, but doing so will weaken the password strength.
            char[][] charGroups = new char[][] 
            {
                PASSWORD_CHARS_LCASE.ToCharArray(),
                PASSWORD_CHARS_UCASE.ToCharArray(),
                PASSWORD_CHARS_NUMERIC.ToCharArray()
            };

            // Use this array to track the number of unused characters in each
            // character group.
            int[] charsLeftInGroup = new int[charGroups.Length];

            // Initially, all characters in each group are not used.
            for (int i = 0; i < charsLeftInGroup.Length; i++)
                charsLeftInGroup[i] = charGroups[i].Length;

            // Use this array to track (iterate through) unused character groups.
            int[] leftGroupsOrder = new int[charGroups.Length];

            // Initially, all character groups are not used.
            for (int i = 0; i < leftGroupsOrder.Length; i++)
                leftGroupsOrder[i] = i;

            // Because we cannot use the default randomizer, which is based on the
            // current time (it will produce the same "random" number within a
            // second), we will use a random number generator to seed the
            // randomizer.

            // Use a 4-byte array to fill it with random bytes and convert it then
            // to an integer value.
            byte[] randomBytes = new byte[4];

            // Generate 4 random bytes.
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            rng.GetBytes(randomBytes);

            // Convert 4 bytes into a 32-bit integer value.
            int seed = (randomBytes[0] & 0x7f) << 24 |
                        randomBytes[1] << 16 |
                        randomBytes[2] << 8 |
                        randomBytes[3];

            // Now, this is real randomization.
            Random random = new Random(seed);

            // This array will hold password characters.
            char[] ticket = null;

            // Allocate appropriate memory for the password.
            ticket = new char[20];

            // Index of the next character to be added to password.
            int nextCharIdx;

            // Index of the next character group to be processed.
            int nextGroupIdx;

            // Index which will be used to track not processed character groups.
            int nextLeftGroupsOrderIdx;

            // Index of the last non-processed character in a group.
            int lastCharIdx;

            // Index of the last non-processed group.
            int lastLeftGroupsOrderIdx = leftGroupsOrder.Length - 1;

            // Generate password characters one at a time.
            for (int i = 0; i < ticket.Length; i++)
            {
                // If only one character group remained unprocessed, process it;
                // otherwise, pick a random character group from the unprocessed
                // group list. To allow a special character to appear in the
                // first position, increment the second parameter of the Next
                // function call by one, i.e. lastLeftGroupsOrderIdx + 1.
                if (lastLeftGroupsOrderIdx == 0)
                    nextLeftGroupsOrderIdx = 0;
                else
                    nextLeftGroupsOrderIdx = random.Next(0,
                                                         lastLeftGroupsOrderIdx);

                // Get the actual index of the character group, from which we will
                // pick the next character.
                nextGroupIdx = leftGroupsOrder[nextLeftGroupsOrderIdx];

                // Get the index of the last unprocessed characters in this group.
                lastCharIdx = charsLeftInGroup[nextGroupIdx] - 1;

                // If only one unprocessed character is left, pick it; otherwise,
                // get a random character from the unused character list.
                if (lastCharIdx == 0)
                    nextCharIdx = 0;
                else
                    nextCharIdx = random.Next(0, lastCharIdx + 1);

                // Add this character to the password.
                ticket[i] = charGroups[nextGroupIdx][nextCharIdx];

                // If we processed the last character in this group, start over.
                if (lastCharIdx == 0)
                    charsLeftInGroup[nextGroupIdx] =
                                              charGroups[nextGroupIdx].Length;
                // There are more unprocessed characters left.
                else
                {
                    // Swap processed character with the last unprocessed character
                    // so that we don't pick it until we process all characters in
                    // this group.
                    if (lastCharIdx != nextCharIdx)
                    {
                        char temp = charGroups[nextGroupIdx][lastCharIdx];
                        charGroups[nextGroupIdx][lastCharIdx] =
                                    charGroups[nextGroupIdx][nextCharIdx];
                        charGroups[nextGroupIdx][nextCharIdx] = temp;
                    }
                    // Decrement the number of unprocessed characters in
                    // this group.
                    charsLeftInGroup[nextGroupIdx]--;
                }

                // If we processed the last group, start all over.
                if (lastLeftGroupsOrderIdx == 0)
                    lastLeftGroupsOrderIdx = leftGroupsOrder.Length - 1;
                // There are more unprocessed groups left.
                else
                {
                    // Swap processed group with the last unprocessed group
                    // so that we don't pick it until we process all groups.
                    if (lastLeftGroupsOrderIdx != nextLeftGroupsOrderIdx)
                    {
                        int temp = leftGroupsOrder[lastLeftGroupsOrderIdx];
                        leftGroupsOrder[lastLeftGroupsOrderIdx] =
                                    leftGroupsOrder[nextLeftGroupsOrderIdx];
                        leftGroupsOrder[nextLeftGroupsOrderIdx] = temp;
                    }
                    // Decrement the number of unprocessed groups.
                    lastLeftGroupsOrderIdx--;
                }
            }

            TimeSpan ts = DateTime.Now - new DateTime(1970, 1, 1);

            // Convert ticket characters into a string and return the result.
            return "LT-SafeID-" + ((Int64)ts.TotalSeconds).ToString() + "-" + new string(ticket);

        }
    }
}
