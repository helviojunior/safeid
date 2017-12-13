using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace AkAuthAgent
{
    public class AuthServer
    {
        private Int32 port;
        private TcpListener listener;
        //private AuthUsers _users;
        private Dictionary<IPAddress, String> _ips;

        //public AuthUsers users { get { return _users; } }
        public Dictionary<IPAddress, String> ips { get { return _ips; } }

        public delegate void Error(IPEndPoint client, String text);
        public event Error OnError;

        public delegate void Info(IPEndPoint client, String text);
        public event Info OnInfo;

        public delegate void UserValidate(IPEndPoint client, String username, String password, ref AuthUserResult result);
        public event UserValidate OnUserValidate;

        public delegate void ListUsers(IPEndPoint client, ref List<String> users);
        public event ListUsers OnListUsers;

        public delegate void ListGroups(IPEndPoint client, ref List<String> groups);
        public event ListGroups OnListGroups;

        public delegate void ConnectionStarted(IPEndPoint client, ref Int32 usersCount);
        public event ConnectionStarted OnConnectionStarted;

        public AuthServer(Int32 port)
        {
            this.port = port;
            listener = new TcpListener(IPAddress.Any, port);
            //this._users = new AuthUsers();
            this._ips = new Dictionary<IPAddress, String>();
        }

        public void Listen()
        {
            listener.Start(500);
            listener.BeginAcceptSocket(new AsyncCallback(OnSocketAccept), listener);
        }

        public void Stop()
        {
            listener.Stop();
        }

        private void OnSocketAccept(IAsyncResult ar)
        {
            try
            {
                Socket NewSocket = listener.EndAcceptSocket(ar);

                //Inicia nova escuta
                listener.BeginAcceptSocket(new AsyncCallback(OnSocketAccept), listener);

                if (NewSocket != null)
                {
                    IPEndPoint ipEP = (IPEndPoint)NewSocket.RemoteEndPoint;
                    //IPAddress ip = ((IPEndPoint)NewSocket.RemoteEndPoint).Address;
                    NewSocket.ReceiveTimeout = 0;

                    if (_ips.ContainsKey(ipEP.Address))
                    {
                        AuthServerItem newItem = new AuthServerItem(NewSocket, _ips[ipEP.Address]);
                        newItem.OnConnectionStarted += new ConnectionStarted(newItem_OnConnectionStarted);
                        newItem.OnListGroups += new ListGroups(newItem_OnListGroups);
                        newItem.OnListUsers += new ListUsers(newItem_OnListUsers);
                        newItem.OnUserValidate += new UserValidate(newItem_OnUserValidate);
                        newItem.OnInfo += new Info(newItem_OnInfo);


                        if (OnInfo != null)
                            OnInfo(ipEP, "Connected " + ipEP.ToString());

                        try
                        {

                            //newItem.users = _users;
                            Thread newThread = new Thread(new ThreadStart(newItem.Start));
                            newThread.Start();

                        }
                        catch(Exception ex)
                        {
                            if (OnError != null)
                                OnError(ipEP, "Falha autenticando o cliente '" + ipEP.Address.ToString() + "': " + ex.Message);
                        }
                    }
                    else
                    {
                        if (OnError != null)
                            OnError(ipEP, "Tentativa de acesso não autorizado pelo IP '" + ipEP.Address.ToString() + "'");

                        NewSocket.Shutdown(SocketShutdown.Both);
                        NewSocket.Close();
                    }
                }
            }
            catch
            {

            }
        }

        void newItem_OnInfo(IPEndPoint client, string text)
        {
            if (OnInfo != null)
                OnInfo(client, text);
        }

        void newItem_OnUserValidate(IPEndPoint client, string username, string password, ref AuthUserResult result)
        {
            if (OnUserValidate != null)
                OnUserValidate(client, username, password, ref result);
        }

        void newItem_OnListUsers(IPEndPoint client, ref List<string> users)
        {
            if (OnListUsers != null)
                OnListUsers(client, ref users);
        }

        void newItem_OnListGroups(IPEndPoint client, ref List<string> groups)
        {
            if (OnListGroups != null)
                OnListGroups(client, ref groups);
        }

        void newItem_OnConnectionStarted(IPEndPoint client, ref int usersCount)
        {
            if (OnConnectionStarted != null)
                OnConnectionStarted(client, ref usersCount);
        }

    }
}
