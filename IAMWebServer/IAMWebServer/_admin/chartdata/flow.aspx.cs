using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using System.Data.SqlClient;
using SafeTrend.Data;
using SafeTrend.Data.SqlClient;
using IAM.GlobalDefs;

namespace IAMWebServer._admin._chartdata
{
    public partial class flow : System.Web.UI.Page
    {
       
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!EnterpriseIdentify.Identify(this)) //Se houver falha na identificação da empresa finaliza a resposta
                return;

            if ((Page.Session["enterprise_data"] == null) || !(Page.Session["enterprise_data"] is EnterpriseData))
                return;

            String type = "";
            if (!String.IsNullOrWhiteSpace((String)RouteData.Values["type"]))
                type = (String)RouteData.Values["type"];

            switch (type.ToLower())
            {
                case "context":
                    Retorno.Controls.Add(new LiteralControl(ContextFlow()));
                    break;

                case "user":
                    Retorno.Controls.Add(new LiteralControl(UserFlow()));
                    break;

                case "enterprise":
                    Retorno.Controls.Add(new LiteralControl(ContextFlow()));
                    break;

                case "plugin":
                    Retorno.Controls.Add(new LiteralControl(Plugin()));
                    break;
            }

        }


        public String Plugin()
        {

            String pluginId = "";
            if (!String.IsNullOrWhiteSpace((String)RouteData.Values["id"]))
                pluginId = (String)RouteData.Values["id"];

            EnterpriseData ent = (EnterpriseData)Page.Session["enterprise_data"];

            FlowData flowData = new FlowData();
            
            DataTable dtPlugins = null;
            using (IAMDatabase db = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
                dtPlugins = db.Select("select * from plugin where (enterprise_id = "+ ent.Id +" or enterprise_id = 0) and id = " + pluginId);
            
            if (dtPlugins == null)
                return "";

            Node pNode = flowData.AddNode(dtPlugins.Rows[0]["name"].ToString(), 0, 1);

            using (IAMDatabase db = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
            {
                switch (dtPlugins.Rows[0]["scheme"].ToString().ToLower())
                {

                    case "connector":
                        DataTable dtResources = db.Select("select r.* from resource_plugin rp inner join resource r on r.id = rp.resource_id where rp.plugin_id = " + dtPlugins.Rows[0]["id"]);
                        if ((dtResources == null) && (dtResources.Rows.Count == 0))
                        {
                            Node resNode = flowData.AddNode("Nenhum recurso vinculado a este plugin", 1, 1, true);
                            flowData.AddConnection(pNode, resNode, "");
                        }
                        else
                        {
                            foreach (DataRow drRes in dtResources.Rows)
                            {
                                Node nResource = flowData.AddNode("Recurso: " + drRes["name"], 2, 1, true);
                                flowData.AddConnection(pNode, nResource, "");


                            }
                        }
                        break;

                    case "agent":
                        DataTable dtProxy = db.Select("select * from proxy_plugin pp inner join proxy p on pp.proxy_id = p.id where pp.plugin_id = " + dtPlugins.Rows[0]["id"]);
                        if ((dtProxy == null) && (dtProxy.Rows.Count == 0))
                        {
                            Node errProxyNode = flowData.AddNode("Nenhum proxy vinculado a este plugin", 1, 1, true);
                            flowData.AddConnection(pNode, errProxyNode, "");
                        }
                        else
                        {
                            foreach (DataRow drProxy in dtProxy.Rows)
                            {
                                Node nProxy = flowData.AddNode("Proxy: " + drProxy["name"], 2, 1, true);
                                flowData.AddConnection(pNode, nProxy, "");
                            }
                        }
                        break;

                    default:
                        Node errNode = flowData.AddNode("Tipo de plugin não reconhecido", 1, 1, true);
                        flowData.AddConnection(pNode, errNode, "");
                        break;
                }
            }
            return flowData.ToJson();
        }


        public String UserFlow()
        {

            String userId = "";
            if (!String.IsNullOrWhiteSpace((String)RouteData.Values["id"]))
                userId = (String)RouteData.Values["id"];

            EnterpriseData ent = (EnterpriseData)Page.Session["enterprise_data"];

            FlowData flowData = new FlowData();

            using (IAMDatabase db = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
            {

                DataTable dtEntity = db.Select("select e.*, c.name context_name from entity e inner join context c on e.context_id = c.id where e.id = " + userId);
                if (dtEntity == null)
                    return "";

                Node eNode = flowData.AddNode(dtEntity.Rows[0]["full_name"].ToString(), 0, 1);

                Node ctxNode = flowData.AddNode("Contexto: " + dtEntity.Rows[0]["context_name"].ToString(), 1, 1);
                flowData.AddConnection(eNode, ctxNode, "");

                Node entNode = flowData.AddNode("Entidade", 2, 1);
                flowData.AddConnection(ctxNode, entNode, "");

                DataTable dtIdentity = db.Select("select ROW_NUMBER() OVER (ORDER BY r.name, i.id) AS [row_number], i.id identity_id, r.name resource_name, p.name from [identity] i inner join resource_plugin rp on i.resource_plugin_id = rp.id inner join resource r on rp.resource_id = r.id inner join plugin p on rp.plugin_id = p.id where i.entity_id = " + userId);

                foreach (DataRow drI in dtIdentity.Rows)
                {
                    Node nIdentity = flowData.AddNode("Identidade " + drI["row_number"], 3, 1, true);
                    flowData.AddConnection(entNode, nIdentity, "");

                    Node nSubIdentity = flowData.AddNode(drI["resource_name"].ToString(), 4, 1);
                    flowData.AddConnection(nIdentity, nSubIdentity, "");

                    DataTable dtRole = db.Select("select r.name role_name from identity_role ir inner join role r on ir.role_id = r.id where ir.identity_id = " + drI["identity_id"] + " order by r.name");

                    foreach (DataRow drRole in dtRole.Rows)
                    {
                        Node nRole = flowData.AddNode("Perfil", 5, 1, true);
                        flowData.AddConnection(nSubIdentity, nRole, "");

                        Node nRoleName = flowData.AddNode(drRole["role_name"].ToString(), 6, 1);
                        flowData.AddConnection(nRole, nRoleName, "");

                    }

                }


                Node systemNode = flowData.AddNode("Sistema", 1, 1);
                flowData.AddConnection(eNode, systemNode, "");

                Node nSysRole = flowData.AddNode("Perfis de sistema", 2, 1);
                flowData.AddConnection(systemNode, nSysRole, "");

                DataTable dtSysRole = db.Select("select r.* from sys_entity_role er inner join sys_role r on er.role_id = r.id where er.entity_id = " + userId);

                if ((dtSysRole == null) || (dtSysRole.Rows.Count == 0))
                {
                    Node nRoleName = flowData.AddNode("Nenhum perfil", 3, 1);
                    flowData.AddConnection(nSysRole, nRoleName, "");
                }
                else
                {
                    foreach (DataRow drRole in dtSysRole.Rows)
                    {
                        Node nRoleName = flowData.AddNode(drRole["name"].ToString(), 3, 1);
                        flowData.AddConnection(nSysRole, nRoleName, "");

                        if ((Boolean)drRole["sa"])
                        {
                            nRoleName.name += "\n(Administrador)";
                        }
                        else
                        {

                            DataTable dtSysEnt = db.Select("select * from enterprise e where e.id = " + drRole["enterprise_id"]);

                            foreach (DataRow drEnt in dtSysEnt.Rows)
                            {
                                Node nRoleEntName = flowData.AddNode(drEnt["name"].ToString(), 4, 1);
                                flowData.AddConnection(nRoleName, nRoleEntName, "");

                                if ((Boolean)drRole["ea"])
                                    nRoleEntName.name += "\n(Administrador)";

                            }
                        }
                    }
                }
            }

            return flowData.ToJson();
        }

        public String ContextFlow()
        {

            String contextid = "";
            if (!String.IsNullOrWhiteSpace((String)RouteData.Values["id"]))
                contextid = (String)RouteData.Values["id"];

            EnterpriseData ent = (EnterpriseData)Page.Session["enterprise_data"];

            FlowData flowData = new FlowData();
            Node eNode = flowData.AddNode(ent.Name, 0, 1);

            using (IAMDatabase db = new IAMDatabase(IAMDatabase.GetWebConnectionString()))
            {

                DataTable dtCtx = db.Select("select * from context where enterprise_id = " + ent.Id + (contextid != "" ? " and id = " + contextid : ""));
                if (dtCtx == null)
                    return "";

                foreach (DataRow dr in dtCtx.Rows)
                {
                    Int64 contextID = (Int64)dr["id"];
                    String cName = "Contexto: " + dr["name"];
                    Node cNode = flowData.AddNode(cName, 1, 1);
                    flowData.AddConnection(eNode, cNode, "");

                    Node roleNode = null;

                    /*
                    DataTable dtRoles1 = DB.Select("select * from [role] e where e.context_id = " + contextID);
                    if (dtRoles1 != null)
                    {
                        roleNode = flowData.AddNode("Perfis", 6, dtRoles1.Rows.Count);
                        flowData.AddConnection(cNode, roleNode, "");
                    
                        foreach (DataRow drR in dtRoles1.Rows)
                        {

                            Int64 irId = (Int64)drR["id"];

                            Node roleNameNode = flowData.AddNode("Perfil: " + drR["name"].ToString(), 7, 1);
                            flowData.AddConnection(roleNode, roleNameNode, "");

                        }
                    }*/

                    Node userNode = flowData.AddNode("Usuários", 3, 1, true);
                    flowData.AddConnection(cNode, userNode, "");

                    DataTable dtEntity = db.Select("select count(*) qty from [entity] e where e.context_id = " + contextID);
                    if ((dtEntity == null) || (dtEntity.Rows.Count == 0) || ((Int32)dtEntity.Rows[0]["qty"] == 0))
                    {
                        Node entNode = flowData.AddNode("Nenhuma entidade vinculada a este contexto", 4, 1, true);
                        flowData.AddConnection(userNode, entNode, "");
                    }
                    else
                    {
                        String rpEntName = "Entidades";
                        Node entNode = flowData.AddNode(rpEntName, 4, (Int32)dtEntity.Rows[0]["qty"], true);
                        flowData.AddConnection(userNode, entNode, dtEntity.Rows[0]["qty"] + " entidades");

                        DataTable dtIdentity = db.Select("select COUNT(distinct i.id) qty from [identity] i inner join entity e on i.entity_id = e.id where e.context_id = " + contextID);
                        if ((dtIdentity == null) || (dtIdentity.Rows.Count == 0))
                        {
                            Node identNode = flowData.AddNode("Nenhuma identidade vinculado a esta entidade", 4, 1, true);
                            flowData.AddConnection(entNode, identNode, "");
                        }
                        else
                        {
                            String rpIdentName = "Identidades";
                            Node identNode = flowData.AddNode(rpIdentName, 5, (Int32)dtIdentity.Rows[0]["qty"], true);
                            flowData.AddConnection(entNode, identNode, dtIdentity.Rows[0]["qty"] + " identidades");

                            DataTable dtResources = db.Select("select name, qty = (select COUNT(distinct i.id) from resource r1 inner join resource_plugin rp on r1.id = rp.resource_id inner join [identity] i on i.resource_plugin_id = rp.id inner join entity e on i.entity_id = e.id where r1.name = r.name and r1.context_id = r.context_id) from resource r  where r.context_id = " + contextID + " group by r.name, r.context_id");
                            if (dtResources != null)
                            {
                                foreach (DataRow drR in dtResources.Rows)
                                {

                                    String resourceName = drR["name"].ToString();
                                    Node resNode = flowData.AddNode(resourceName, 6, (Int32)drR["qty"], true);
                                    flowData.AddConnection(identNode, resNode, drR["qty"] + " identidades");

                                }
                            }

                        }

                    }


                    Node confNode = flowData.AddNode("Configuração", 3, 1, true);
                    flowData.AddConnection(cNode, confNode, "");

                    DataTable dtProxy = db.Select("select p.id, p.name from resource r inner join proxy p on r.proxy_id = p.id where r.context_id = " + contextID + " group by p.id, p.name order by p.name");
                    if ((dtProxy == null) || (dtProxy.Rows.Count == 0))
                    {
                        Node pNode = flowData.AddNode("Nenhuma configuração vinculada a este contexto", 4, 1, true);
                        flowData.AddConnection(confNode, pNode, "");
                    }
                    else
                    {

                        //Node proxyNode = flowData.AddNode("Proxy", 2, dtProxy.Rows.Count, false);
                        //flowData.AddConnection(cNode, proxyNode, "");

                        foreach (DataRow drP in dtProxy.Rows)
                        {
                            Int64 pId = (Int64)drP["id"];
                            Node pNode = flowData.AddNode("Proxy: " + drP["name"], 4, 1, true);
                            flowData.AddConnection(confNode, pNode, "");

                            DataTable dtResource = db.Select("select r.*, p.name proxy_name from resource r inner join proxy p on r.proxy_id = p.id where r.context_id = " + contextID + " and p.id = " + pId);
                            if (dtResource != null)
                            {
                                foreach (DataRow drR in dtResource.Rows)
                                {
                                    Int64 rId = (Int64)drR["id"];
                                    Node rNode = flowData.AddNode("Recurso: " + drR["name"], 5, 1, true);
                                    flowData.AddConnection(pNode, rNode, "");

                                    DataTable dtResPlugin = db.Select("select p.name plugin_name, rp.* from resource_plugin rp inner join plugin p on rp.plugin_id = p.id where rp.resource_id = " + rId);
                                    if (dtResPlugin != null)
                                    {
                                        foreach (DataRow drRP in dtResPlugin.Rows)
                                        {
                                            Int64 rpId = (Int64)drRP["id"];
                                            Node rpNode = flowData.AddNode("Plugin: " + drRP["plugin_name"].ToString(), 6, 1, true);
                                            flowData.AddConnection(rNode, rpNode, "");

                                            DataTable dtRoles = db.Select("select r.id, r.name from role r inner join resource_plugin_role rpr on rpr.role_id = r.id where rpr.resource_plugin_id = " + rpId + "  group by r.id, r.name");
                                            if (dtRoles != null)
                                            {

                                                foreach (DataRow drRol in dtRoles.Rows)
                                                {
                                                    String roleName = "Perfil: " + drRol["name"];

                                                    //if (roleNode != null)
                                                    //{

                                                    //Node roleNameNode = flowData.Find(roleNode, roleName, 6);
                                                    Node roleNameNode = flowData.Find(rpNode, roleName, 6);
                                                    if (roleNameNode == null)
                                                        roleNameNode = flowData.AddNode("Perfil: " + drRol["name"].ToString(), 7, 1, true);

                                                    if (roleNameNode != null)
                                                        flowData.AddConnection(rpNode, roleNameNode, "");

                                                    //Int32 roleNameNodeIndex = flowData.AddNode("Perfil: " + drRol["name"].ToString(), true);

                                                    //flowData.AddLink(rpNodeIndex, roleNameNodeIndex, 1, "");
                                                    //}

                                                }
                                            }


                                        }

                                    }

                                }
                            }

                        }
                    }


                }
            }

            return flowData.ToJson();
        }


        [Serializable()]
        class NodeBase
        {
            /*"nodeID": 0,
			"name": "Node 1 name",
			"column": 0,
			"value": 1,
			"cssClass": "class-to-add"*/

            public Int32 nodeID;

            public NodeBase(Int32 nodeID)
            {
                this.nodeID = nodeID;
            }
        }

        [Serializable()]
        class Node : NodeBase
        {
            /*"nodeID": 0,
			"name": "Node 1 name",
			"column": 0,
			"value": 1,
			"cssClass": "class-to-add"*/

            public String name;
            public Int32 column;
            public Int32 value;
            public String cssClass;

            public Node(Int32 nodeID, String name, Int32 column, Int32 value, String cssClass)
                : base(nodeID)
            {
                this.name = name;
                this.column = column;
                this.value = value;
                this.cssClass = cssClass;
            }
        }

        [Serializable()]
        class Connection
        {
            public NodeBase source;
            public NodeBase dest;
            public String title;

            public Connection(Node source, Node destination, String title)
            {
                this.source = new NodeBase(source.nodeID);
                this.dest = new NodeBase(destination.nodeID);
                this.title = title;
            }
        }

        [Serializable()]
        class FlowData
        {

            public List<Node> nodes = new List<Node>();
            public List<Connection> connections = new List<Connection>();

            public Node AddNode(String name, Int32 column, Int32 value)
            {
                return AddNode(name, column, value, false);
            }

            public Node AddNode(String name, Int32 column, Int32 value, Boolean forceNew)
            {
                Node ret = new Node(nodes.Count, name, column, value, "");

                if (!nodes.Exists(n => (n.name == name && n.column == column)) || forceNew)
                    nodes.Add(ret);
                else
                    ret = nodes.Find(n => (n.name == name && n.column == column));

                return ret;
            }

            public Node Find(Node parent, String name, Int32 column)
            {
                foreach (Node node in nodes.FindAll(n => (n.name == name)))
                {
                    if (connections.Exists(c => (c.source.nodeID == parent.nodeID && c.dest.nodeID == node.nodeID)))
                        return node;
                }

                return null;

            }

            public void AddConnection(Node source, Node destination, String title)
            {
                connections.Add(new Connection(source, destination, title));
            }

            public string ToJson()
            {
                try
                {
                    //return SafeTrend.Json.JSON.Serialize<Connection>(connections[0]);
                    return SafeTrend.Json.JSON.Serialize<FlowData>(this);
                }
                catch (Exception ex)
                {
                    return "{ \"error\": \"" + ex.Message + "\"";
                }

            }

        }

    }


}