using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace IAM.MongoDB
{
    //http://docs.mongodb.org/ecosystem/tutorial/use-csharp-driver/
    public class MongoBaseDB : IDisposable
    {
        protected String ConnectionString;
        protected String DatabaseName;


        private MongoClient client;
        private MongoServer server;
        private MongoDatabase databaseConn;

        /*
        public MongoBaseDB(String server, String username, String password)
        {
            / *
            mongodb://[username:password@]hostname[:port][/[database][?options]]
            * /

            this.ConnectionString = "mongodb://" + username + ":" + password + "@" + server;

            /*
            MongoClient client = new MongoClient(); // connect to localhost
            MongoServer server = client.GetServer();
            MongoDatabase test = server.GetDatabase("test");
            MongoCredentials credentials = new MongoCredentials("username", "password");
            MongoDatabase salaries = server.GetDatabase("salaries", credentials);* /
        }*/

        public MongoBaseDB(String server)
            : this(server, "admin") { }

        public MongoBaseDB(String server, String databaseName)
        {
            /*
            mongodb://[username:password@]hostname[:port][/[database][?options]]
            */
            this.DatabaseName = databaseName;
            this.ConnectionString = "mongodb://" + server;
        }

        public void AddObject<T>(String Collection, T data)
        {
            /*
            using (MemoryStream s = new MemoryStream())
            using (BsonWriter w = BsonWriter.Create(s))
                BsonSerializer.Serialize<T>(w, data);*/

            MongoCollection<T> col = this.databaseConn.GetCollection<T>(Collection);
            col.Save(data);

        }

        public void UpdateObject<T>(String Collection, T data, String where)
        {
            
            RemoveObject<T>(Collection, where);
            AddObject<T>(Collection, data);

        }


        public T GetObject<T>(String Collection, String where)
        {
            MongoCollection<T> col = this.databaseConn.GetCollection<T>(Collection);

            var query = Query.Where(where);
            return col.FindOne(query);
        }

        public List<T> GetObjects<T>(String Collection, String where)
        {
            MongoCollection<T> col = this.databaseConn.GetCollection<T>(Collection);

            var query = Query.Where(where);
            List<T> tmp = new List<T>();
            tmp.AddRange(col.Find(query));

            return tmp;
        }


        public void RemoveObject<T>(String Collection, String where)
        {
            MongoCollection<T> col = this.databaseConn.GetCollection<T>(Collection);

            var query = Query.Where(where);
            col.Remove(query);
        }

        public void RemoveObject<T>(String Collection, String queryName, String queryValue)
        {
            MongoCollection<T> col = this.databaseConn.GetCollection<T>(Collection);
            
            var query = Query.EQ(queryName, BsonValue.Create(queryValue));
            col.Remove(query);
        }

        public void CreateAdminUser(String username, String password)
        {

            MongoClient client = new MongoClient(this.ConnectionString);
            MongoServer server = client.GetServer();
            MongoDatabase admin = server.GetDatabase("admin");

            //MongoUser user = new MongoUser(username, new PasswordEvidence(password), false);

            //CommandResult res = admin.RunCommand("db.addUser( { user: \"st_admin\", pwd: \"t0FtaQgrpmK2ZDc7Qr30\", roles: [ \"userAdminAnyDatabase\" ] } )");

            //admin.AddUser(user);
            

            //MongoCredentials credentials = new MongoCredentials("username", "password");
            //MongoDatabase salaries = server.GetDatabase("salaries", credentials);
        }

        public Boolean DataBaseExists(String databaseName)
        {
           return this.server.DatabaseExists(databaseName);
        }

        public void CreateDatabase(String databaseName)
        {
            // O MongoDB cria a base em sua primeira utilização
            server.GetDatabase(databaseName);
        }

        public void Connect()
        {
            this.client = new MongoClient(this.ConnectionString);
            this.server = client.GetServer();
            this.databaseConn = server.GetDatabase(this.DatabaseName);
        }

        public void Disconnect()
        {
            this.databaseConn = null;
            if (this.server != null)
                this.server.Disconnect();
            this.server = null;
            this.client = null;
        }

        public void Dispose()
        {
            this.Disconnect();
            this.DatabaseName = null;
            this.ConnectionString = null;
        }
    }
}
