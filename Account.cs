using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using l.core;
using System.Data;

namespace l.core.web
{
    public class Account {
        private Dictionary<string, bool> Powers { get; set; }
        
        public string UserNO { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Where { get; set; }
        public bool Signin { get; set; }
        public MetaModule CurrModule { get; set; }
        private MenuItem menu;
        public MenuItem Menu { get { if (menu == null) getMenu(); return menu; } }
        public Dictionary<string, string> Positions { get; set; }
        private List<MetaModule> _modules;
        public List<MetaModule> Modules { get {
            if (_modules == null) getMenu();
            return _modules;
        } }

        public string ConversationTime { get; set; }

        private void checkProject() {
            if (l.core.Project.Current == null) throw new Exception("Project 全局变量尚未初始化");
        }

        public IDbConnection GetConn(string connAlias = null) {
            checkProject();
            return l.core.Project.Current.GetConn(/*Where,*/ connAlias);
        }

        public IDbConnection GetFrmConn() {
            checkProject();
            return l.core.Project.Current.GetConn(/*Where,*/ "frm");
        }

        //一个会话要用到的权限不会很多，首先在 Filter里面就把Module 相关的取出来（不一定够）
        public void Module(string moduleID) {
            checkProject();
            CurrModule = new Module(moduleID).Load();
            CurrModule.Functions.ForEach(p=> new Function(p.FuncID).Load());
            using (var conn = l.core.Project.Current.GetConn()) {
                DataTable dtModulePower = DBHelper.ExecuteQuery(conn, @"select distinct rf.FuncID 
                        from tAccountRoles ar join tRoleFunc rf on ar.RoleID = rf.RoleID
                        join tModuleFunc mf on rf.FuncID = mf.FuncID
                        where ar.UserNO = :UserNO and ar.PlaceNO = :Where
                          and mf.ModuleID = :ModuleID ", new Dictionary<string, DBParam>{
                                    {"UserNO", new DBParam{ ParamValue = UserNO}},
                                    {"ModuleID", new DBParam{ ParamValue = CurrModule.ModuleID}},
                                    {"Where", new DBParam{ ParamValue = Where}}});
                foreach (DataRow dr in dtModulePower.Rows){
                    if (!Powers.ContainsKey(dr[0].ToString()))
                        Powers[dr[0].ToString()] = true;
                }
            }
        }

        //一个会话要用到的权限不会很多，因此需要再取
        public bool CheckPower(string FuncID){
            checkProject();
            if (Powers == null) return false;
            else {
                if (Powers.ContainsKey(FuncID)) return Powers[FuncID];
                else {
                    using (var conn = l.core.Project.Current.GetConn()) {
                        DataTable dtModulePower = DBHelper.ExecuteQuery(conn, @"select distinct rf.FuncID 
                            from tAccountRoles ar join tRoleFunc rf on ar.RoleID = rf.RoleID
                            where ar.UserNO = :UserNO and ar.PlaceNO = :Where
                                and rf.FuncID = :FuncID", new Dictionary<string, DBParam>{
                                        {"UserNO", new DBParam{ ParamValue = UserNO}},
                                        {"FuncID", new DBParam{ ParamValue = FuncID}},{"Where", new DBParam{ ParamValue = Where}}});
                        Powers[FuncID] = dtModulePower.Rows.Count > 0;
                        return Powers[FuncID];
                    }
                }
            }
        }

        public object ParamFilter(string paramName, object paramValue) {
            return paramName == "Operator" ? UserNO 
                : (paramName == "LangID" ? "936" 
                    : (paramName == "OperName" ?  UserName
                        : (paramName == "LocalStoreNO" ? Where
                            : (paramName == "Where" ? Where
                                :  (paramName =="LastUpdateTime"? DateTime.Now : paramValue)))));
        }
        public Dictionary<string, object> SysParamValues() {
            return "Operator;LangID;OperName;LastUpdateTime;LocalStoreNO".Split(';').ToDictionary(p => p, q => ParamFilter(q, null));
        }

        public void UpdateModules( DataTable dtModule) {
            #region 自动更新新模块
            if (dtModule == null) dtModule = getMetaModule( Where);
            using (var conn = Project.Current.GetConn()) {
                if (l.core.VersionHelper.Helper != null && l.core.VersionHelper.Helper.Action.IndexOf("update") >= 0)   {
                    l.core.MetaModule[] modules = l.core.VersionHelper.Helper.GetAs<l.core.MetaModule[]>("MetaModule.all", null) as l.core.MetaModule[];
                    //var new_modules = from m in modules where dtModule.DefaultView.Find(m.ModuleID) < 0 select m;
                    if (modules != null)  {
                        foreach (var m in modules) {
                            var f = dtModule.DefaultView.Find(m.ModuleID);
                            if (f < 0)  {
                                new Module(m.ModuleID).Load();
                                DBHelper.ExecuteSql(conn, //这一句是为了自动往菜单表加一个，以及往权限表插记录（为了调试方便，在权限管理完成后可以去掉）
                                    @"  declare @iidx int
                                        declare @tt varchar(1)
                                        select @tt = type from sysobjects where name='tRoleFunc'
                                        
                                          insert into tRoleFunc(RoleID, FuncID, TimeStamp) select RoleID, :FuncID, getdate() from tRoles 
                                                where RoleID not in(select RoleID from tRoleFunc where FuncID = :FuncID) and @tt = 'U'
                                        
                                        select @iidx = isnull(max(Idx), 0) + 1 from tMenu where ParentID = :ParentID
                                        insert into tMenu(ParentID, Idx, ModuleID) values(:ParentID, @iidx, :ModuleID)",
                                        new Dictionary<string, DBParam> { 
                                                    {"FuncID", new DBParam{ ParamValue= m.ModuleID}}, 
                                                    {"ModuleID", new DBParam{ ParamValue= m.ModuleID}}, 
                                                    {"ParentID", new DBParam{ ParamValue= m.ParentID??""}}, 
                                                });
                            }
                            else if (Convert.ToString(dtModule.DefaultView[f]["Path"]).Trim() != Convert.ToString(m.Path ?? "").Trim()){
                                //路径变化了也要更新,要不然模块永远进不去正确的地方,没法单独更新
                                new Module(m.ModuleID).Load();
                                dtModule.DefaultView[f]["Path"] = Convert.ToString(m.Path ?? "").Trim();
                            }
                        }
                    }
                }
            }
            #endregion
        }

        private DataTable getMetaModule(string where){
            using (var conn1 = Project.Current.GetFrmConn()) {
                DataTable dtModule = null;
                dtModule = DBHelper.ExecuteQuery(conn1, @"select * from metaModule where rtrim(isnull(Subsystems, '')) = ''  or ( ' '  + Subsystems +  ' ') like :where",
                    new Dictionary<string, DBParam> { {
                            "where", new DBParam{ ParamValue = "% " + where + " %"}                              
                        }});
                dtModule .DefaultView.Sort = "ModuleID";
                return dtModule;
            }
        }

        public Account() {
            checkProject();
            var session = System.Web.HttpContext.Current.Session;
            UserNO = session["UserNO" + l.core.Project.Current.SimulateCode] == null ? null : session["UserNO" + l.core.Project.Current.SimulateCode].ToString();
            Signin = UserNO != null;
            UserName = session["UserName" + l.core.Project.Current.SimulateCode] == null ? null : session["UserName" + l.core.Project.Current.SimulateCode].ToString();
            Password = session["Password" + l.core.Project.Current.SimulateCode] == null ? null : session["Password" + l.core.Project.Current.SimulateCode].ToString();
            Where = session["Where" + l.core.Project.Current.SimulateCode] == null ? null : session["Where" + l.core.Project.Current.SimulateCode].ToString();
            if(Signin){
                Powers = new Dictionary<string, bool>();
                using (var conn = l.core.Project.Current.GetConn())  {
                    DataTable dtPosition = DBHelper.ExecuteQuery(conn, @"select distinct ar.PlaceNO, Place 
                                from tAccountRoles ar join tSysPlace sp on ar.PlaceNO = sp.PlaceNO 
                                where ar.UserNO= :UserNO", new Dictionary<string, DBParam>{
                                    {"UserNO", new DBParam{ ParamValue = UserNO}},});
                    Positions = new Dictionary<string, string>();
                    foreach (System.Data.DataRow dr in dtPosition.Rows) {
                        Positions.Add(dr[0].ToString(), dr[1].ToString());
                    }
                    //重新评估上次的位置是否有效
                    if ((Where != null) && (!Positions.ContainsKey(Where))) Where = null;
                    if (Where == null) if (Positions.Count > 0) Where = Positions.First().Key;
                }
            }
        }

        private void getMenu(){
            menu = new MenuItem();
            if(Signin){
                using (var conn = l.core.Project.Current.GetConn())
                {
                    DataTable dtModule = getMetaModule(Where);
                    if (System.Configuration.ConfigurationManager.AppSettings["ManualUpdateModules"] != "true")
                        UpdateModules(dtModule);

                    DataTable dtModulePower = DBHelper.ExecuteQuery(conn, @"select distinct rf.FuncID 
                            from tAccountRoles ar join tRoleFunc rf on ar.RoleID = rf.RoleID
                            join tMenu m on rf.FuncID = m.ModuleID
                            where ar.UserNO = :UserNO and ar.PlaceNO = :Where", new Dictionary<string, DBParam>{
                                {"UserNO", new DBParam{ ParamValue = UserNO}},
                                {"Where", new DBParam{ ParamValue = Where}}});

                    DataTable dtMenu = DBHelper.ExecuteQuery(conn, @"select * from tMenu order by ParentID, Idx, ModuleID", null);
                    _modules = new List<MetaModule>();

                    menu.Load(dtModule, dtModulePower, dtMenu, _modules);
                }
            }
        }

        public static void Refresh(System.Web.Mvc.ViewDataDictionary viewData) {
            viewData.Remove("Account"); 
        }

        public static Account Current2(System.Web.Mvc.ViewDataDictionary viewData) {
            if (viewData == null || viewData["Account"] == null)
            {
                var acc = new Account(){
                    ConversationTime = DateTime.Now.ToString()
                };
                if (viewData != null) viewData["Account"] = acc;
                else return acc;
            }
            return viewData["Account"] as Account;
        }

        public static Account Current(System.Web.Mvc.ControllerBase context) {
            return Current2(context==null?null:context.ViewData);
        }
    }

 
}
 