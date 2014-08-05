using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.Reflection;


namespace l.core.web
{
    public class MetaPageData {
        protected System.Web.Mvc.ControllerBase context;
        public MetaPageData(System.Web.Mvc.ControllerBase context) {
            this.context = context;
        }

        public void AddDataSet(string queryName, DataSet ds) {
            if (context.ViewData["__data"] == null)
                context.ViewData["__data"] = new Dictionary<string, DataSet>();
            (context.ViewData["__data"] as Dictionary<string, DataSet>)[queryName] = ds;
        } 

        public DataTable GetTable(string key) {
            var kk = (key ?? "").Split('.');
            bool valid = true;
            int idx = 0;
            if (kk.Count() > 2) valid = false;
            else if (kk.Count() > 1) 
                try { idx = Convert.ToInt32(kk[1]); }
                catch { valid = false; }
            if (!valid) throw new Exception(string.Format("\"{0}\" 不是UI控件获取数据表的合法标识.", key));

            DataTable dt = null;
            if (context.ViewData["__data"] != null) {
                var ds = context.ViewData["__data"] as Dictionary<string, DataSet>;
                if (ds.ContainsKey(kk[0]))
                    if (ds[kk[0]].Tables.Count > idx)
                        dt = ds[kk[0]].Tables[idx];
            }
            if (dt == null) throw new Exception(string.Format("未能根据 \"{0}\" 从页面数据中获取数据表.", key));
            else return dt;
        } 
    }

    public class MetaPageUI {
        private List<Assembly> asms;
        private MetaPageHelper helper;
        private l.core.Theme theme;
        public MetaPageData pageData;
        private Dictionary<dynamic, int> uiObjectArray;
        private ModulePage page;
        private Assembly webAssembly;

        protected System.Web.Mvc.ControllerBase context;
        public l.core.Theme Theme { get { return theme; } }

        public MetaPageUI(System.Web.Mvc.ControllerBase context, Assembly webAssembly,
            MetaPageData pageData, ModulePage page, MetaPageHelper owner) {
            asms = new List<Assembly> {
                System.Reflection.Assembly.Load("l.core.web"), 
                webAssembly};
            var helperAsm = System.Configuration.ConfigurationManager.AppSettings["UIHelperAssembly"];
            if (!string.IsNullOrEmpty(helperAsm)) asms.Add(System.Reflection.Assembly.Load(helperAsm));

            this.context = context;
            this.helper = owner;
            this.pageData = pageData;
            this.page = page;
            this.webAssembly = webAssembly;
            if (context.ControllerContext.RequestContext.HttpContext.Request.Headers["X-Requested-With"] == null){
                string themeid = System.Configuration.ConfigurationManager.AppSettings["Layout"];
                if (!string.IsNullOrEmpty(themeid)) {
                    theme = new Theme(themeid).Load();
                }
            }
        }

        private void checkPrepareUI()  {
            if (uiObjectArray == null) throw new Exception("在获取UI渲染数据之前，请先执行 PrepareUI.");
        }

        public void PrepareUI(HtmlHelper html, bool layout, bool refresh, bool partial = false) { 
            if (refresh) uiObjectArray = null;

            if (uiObjectArray == null)  {
                    uiObjectArray = new Dictionary<dynamic, int>();
                prepareUI(html, page.UI, uiObjectArray);

                var layoutEvalResult = new Dictionary<dynamic, int>();
                if (partial) {
                    prepareUI(html, "ModulePage {}", layoutEvalResult);
                }
                else{
                    if (theme != null) prepareUI(html, theme.LayoutUI, layoutEvalResult);
                    else layoutEvalResult[new l.core.web.html.ModulePage(html, "module_page_" + (helper.Account.CurrModule == null?"-1":helper.Account.CurrModule.ModuleID) + "_" + page.PageID, page, helper)] = 0;
                    mergeLayout(layoutEvalResult);

                }
            }
        }

        public void prepareUI(HtmlHelper html, string uiScript, Dictionary<dynamic, int> uiObjectArray)  {
            foreach (var s in (uiScript ?? "").Split('\n'))  {
                var ss = s.TrimEnd(); var sss = ss.TrimStart();
                if (string.IsNullOrEmpty(sss)) continue;
                var indent = ss.Length - sss.Length;

                string cls = string.Empty, param = string.Empty ;
                try {
                    cls = sss.Split(' ')[0];
                    param = (sss.Substring(cls.Length + 1));
                } catch (Exception e){ 
                    throw new Exception(string.Format("解析页面UI 行\"{0}\" 的时候遇到格式错误.\n{1}", sss, e.Message));
                }
                var obj = DeserializeUIHelper(html, cls, param);

                //找父亲
                for (int i = uiObjectArray.Count - 1; i >= 0; i--)
                    if (uiObjectArray.ElementAt(i).Value < indent) {
                        if (obj is HtmlTagHelper)
                            uiObjectArray.ElementAt(i).Key.GetType().GetMethod("Add", new System.Type[] { typeof(HtmlTagHelper) }).Invoke(uiObjectArray.ElementAt(i).Key, new[] { obj });
                        else uiObjectArray.ElementAt(i).Key.GetType().GetMethod("Add", new System.Type[] { typeof(string) }).Invoke(uiObjectArray.ElementAt(i).Key, new[] { obj.Html() });
                        break;
                    }

                uiObjectArray[obj] = indent;
            }
        }

        private void mergeLayout(Dictionary<dynamic, int> layoutUIObjectArray) {
            bool merge = false;
            var newUI = new Dictionary<dynamic, int> ();

            foreach(var i in layoutUIObjectArray){
                newUI[i.Key] = i.Value;
                if (i.Key is l.core.web.html.ModulePage) {
                    int topIndent = -1;
                    foreach (var j in uiObjectArray){
                        if (topIndent == -1) topIndent = j.Value;
                        newUI[j.Key] = i.Value + j.Value + 2;
                        if (topIndent == j.Value) {
                            if (j.Key is HtmlTagHelper)
                                i.Key.GetType().GetMethod("Add", new System.Type[] { typeof(HtmlTagHelper) }).Invoke(i.Key, new[] { j.Key});
                            else i.Key.GetType().GetMethod("Add", new System.Type[] { typeof(string) }).Invoke(i.Key, new[] { j.Key.Html() });
                        };
                    }
                    merge = true;
                }
            }
            if (!merge) throw new Exception("主题布局配置没有找到 ModulePage 的入口。");
            else uiObjectArray = newUI;
        }

        public MvcHtmlString Html(HtmlHelper html, bool layout) {
            checkPrepareUI();
            if (uiObjectArray.Count == 0) return new MvcHtmlString("");
            else {
                int parent_indent = uiObjectArray.First().Value;
                var htm = string.Join("\n", uiObjectArray.Where(p => p.Value == parent_indent).Select(p => p.Key.GetType().GetMethod("Html", new System.Type[] { }).Invoke(p.Key, null) as string));
                return new MvcHtmlString(htm);
            }
        }

        private Dictionary<string, Query> __query {get {
            return context.ViewData["__query"] as Dictionary<string, Query>;}
        }

        private Dictionary<string, FieldMetaHelper> __fms {get {
            return context.ViewData["__fms"] as Dictionary<string, FieldMetaHelper>;}
        }

        private dynamic DeserializeUIHelper(HtmlHelper html, string type, string _params)   {
            if (html == null) throw new Exception("PageHelper.Render 的 Html 参数不能为空." + _params);
            
            var cls = "l.core.web.html." + type;
            Type typ = null;
            Assembly asm = null;
            foreach(var a in asms){
                asm = a;
                typ = asm.GetType(cls);
                if (typ != null) break;
            }
            
            if (typ == null) return new l.core.web.html.NotExistTag(null, cls + "不存在.");
            else   {
                Dictionary<string, object> ui_params = null;
                try { ui_params = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(_params); }
                catch { throw new Exception("解析UI 项参数出错，不符合 Json 格式.\n" + _params); }

                var @params = typ.GetConstructors()[0].GetParameters().Select(p =>  {
                    object obj = null;
                    var str_value = ui_params.ContainsKey(p.Name) ? Convert.ToString( ui_params[p.Name]) : null;
                    if (p.ParameterType == typeof(System.Web.Mvc.HtmlHelper)) obj = html;
                    else if (p.ParameterType == typeof(MetaPageHelper))
                        obj = helper;
                    else if (p.ParameterType == typeof(Theme))
                        obj = theme;
                    else if (p.ParameterType == typeof(System.Data.DataTable))
                        obj = string.IsNullOrEmpty(str_value)? null: pageData.GetTable(str_value);
                    else if (p.ParameterType == typeof(l.core.ModulePage))
                        obj = page;    
                    else if (p.ParameterType == typeof(l.core.IQuery)) {
                        if (str_value != null)
                            if(__query != null && __query.ContainsKey(str_value)) obj = __query[str_value];
                            else throw new Exception(string.Format("构建类型 \"{0}\" 赋值参数\"{1}\"时，未能从页面加载数据中找到\"{2}\"，可能是页面配置未定义", typ.Name, p.Name, str_value));
                    }
                    else if (p.ParameterType == typeof(l.core.FieldMetaHelper)) {
                        if (str_value != null)
                            if (__fms != null && __fms.ContainsKey(str_value)) obj = __fms[str_value];
                            else throw new Exception(string.Format("构建类型 \"{0}\" 赋值参数\"{1}\"时，未能从页面加载数据中找到\"{2}\"，可能是页面配置未定义", typ.Name, p.Name, str_value));
                    }
                    else if (p.ParameterType == typeof(l.core.web.ModulePageHelper))
                        obj = l.core.web.ModulePageHelper.Get(context, "index");
                    else if (p.ParameterType == typeof(string[]) && ui_params.ContainsKey(p.Name) && ui_params[p.Name].GetType() == typeof(string))
                        obj = ui_params[p.Name].ToString().Split(';');
                    else try
                        {
                            obj = ui_params.ContainsKey(p.Name) ?
                            Newtonsoft.Json.JsonConvert.DeserializeObject(Newtonsoft.Json.JsonConvert.SerializeObject(ui_params[p.Name]), p.ParameterType)
                            : null;
                        }
                        catch (Exception e)
                        {
                            throw new Exception(string.Format("构建类型 \"{0}\" 赋值参数\"{1}\"时发生错误.\n{2}", typ.Name, p.Name, e.Message));
                        }

                    //if (obj == null) throw new Exception(string.Format("UI解析工具无法为 \"{0}\"的\"{1}\"类型参数\"{2}\"提供合适的值.", type, p.ParameterType, p.Name));
                    //else return obj;
                    return obj;
                }).ToArray();
                try  {
                    var ins = asm.CreateInstance(cls, false, System.Reflection.BindingFlags.Default, null,
                        @params, null, null);
                    return ins;
                }
                catch (Exception e)  {
                    throw new Exception(string.Format("UI解析工具创建 \"{0}\"对象时发生异常.\n{1}\n{2}\n{3}", type, e.Message, e.InnerException, e.StackTrace));
                }
            }
        }

        public MvcHtmlString Render(HtmlHelper html, string type, string _params) {
            var obj = DeserializeUIHelper(html, type, _params);
            try {
                return new MvcHtmlString( obj.Html());
            }
            catch (Exception e)  {
                throw new Exception(string.Format("UI解析工具渲染 \"{0}\"时发生异常.\n{1}\n{2}\n{3}", type, e.Message, e.InnerException, e.StackTrace));
            }
        }

        public MvcHtmlString UIScript()  {
            checkPrepareUI();
            return new MvcHtmlString(string.Join("\n", uiObjectArray.Select(p =>  {
                var s = p.Key.GetType().GetMethod("Script", new System.Type[] { });
                if (s != null) return s.Invoke(p.Key, null) as string;
                else return "";
            })));
        }

        public MvcHtmlString UIScriptInvoke()  {
            checkPrepareUI();
            int i = 0;
            return new MvcHtmlString(string.Join("\n", uiObjectArray.Select(p => {
                var ts = p.Key.GetType().Name.Split('.');
                var top = i == 0;
                i++;
                var script = string.Empty;// Format("if (console) console.log('{0}');", ts[ts.Length - 1]);
                if (top)  script +=  string.Format("if ($.fn.{0}) $('#{1}').{0}();", ts[ts.Length - 1], uiObjectArray.First().Key.Id);
                else script += string.Format("if ($.fn.{0}) $('#{1} .{0}').{0}();", ts[ts.Length - 1], uiObjectArray.First().Key.Id);
                return script;
            }).Distinct()));
        }

        public MvcHtmlString UIStyle() {
            checkPrepareUI();
            return new MvcHtmlString(string.Join("\n", uiObjectArray.Select(p => {
                var s = p.Key.GetType().GetMethod("StyleSheet", new System.Type[] { });
                if (s != null) return s.Invoke(p.Key, null) as string;
                else return "";
            })));
        }

        public MvcHtmlString UIStyleInclude(bool ajax = false) {
            checkPrepareUI();
            var include = new List<string>();
            foreach (var ui in uiObjectArray) {
                var s = ui.Key.GetType().GetMethod("StyleSheetInclude", new System.Type[] { });
                if (s != null)
                    foreach (string ss in s.Invoke(ui.Key, null) as IEnumerable<string>)
                    {
                        if (!include.Contains(ss)) include.Add(ss);
                    }

            };
            string strInclude = !ajax ? 
                string.Join("\n", include.Select(p =>
                    string.Format("<link href='/Scripts/idc.ui.script/{0}' rel='stylesheet' type='text/css' />", p)
                ))
                :
                "[" + string.Join(",", include.Select(p =>
                    string.Format("'/Scripts/idc.ui.script/{0}'", p)
                )) + "]" ;
            //if (theme != null) strInclude += "\n" + string.Format("<link href='/Theme/Style/{0}.css' rel='stylesheet' type='text/css' />",
            //    context.ControllerContext.HttpContext.Server.UrlEncode(theme.Theme));
            return new MvcHtmlString(strInclude);
        }

        public MvcHtmlString UIScriptInclude(bool ajax = false)
        {
            checkPrepareUI();
            var include = new List<string>();
            foreach (var ui in uiObjectArray) {
                var s = ui.Key.GetType().GetMethod("ScriptInclude", new System.Type[] { });
                if (s != null)
                    foreach (string ss in s.Invoke(ui.Key, null) as IEnumerable<string>) {
                        if (!include.Contains(ss)) include.Add(ss);
                    }

            };
            if (!ajax) 
                return new MvcHtmlString(string.Join("\n", include.Select(p =>
                    string.Format("<script src='/Scripts/idc.ui.script/{0}?{1}' type='text/javascript'></script>", p, System.Configuration.ConfigurationManager.AppSettings["verfix"])
                )));
            else return new MvcHtmlString("[" + string.Join(",", include.Select(p =>
                    string.Format("'/Scripts/idc.ui.script/{0}?{1}'", p, System.Configuration.ConfigurationManager.AppSettings["verfix"])
                )) + "]");
        }
    }

    public class MetaPageHelper {
        private string[] blackList;
        
        private MetaPageUI pageUI;
        private MetaModule module;
        private ModulePage page;
        private l.core.web.Account account;
        private System.Web.HttpRequestBase request;
        private PageFlowItem activeFlow;

        protected System.Web.Mvc.ControllerBase context;
        public MetaPageData pageData { get; set; }
        public MetaPageUI PageUI { get { return pageUI; } }
        public ModulePage Page { get { return page; } }
        public PageFlowItem ActiveFlow { get { return activeFlow; } }
        public string[] BlackList { get { return blackList; } }
        public bool FullBlackList { get; set; }
        public Account Account { get { return Account.Current(context.ControllerContext.Controller); } }

        static public MetaPageHelper Get(System.Web.Mvc.ControllerBase context, string pageId, ModulePage page = null) {
            if (!context.ViewData.ContainsKey("__page")) context.ViewData["__page"] = new MetaPageHelper(context, System.Reflection.Assembly.GetCallingAssembly(), pageId, page);
            return context.ViewData["__page"] as MetaPageHelper;
        }

        public MetaPageHelper(System.Web.Mvc.ControllerBase context, Assembly webAssembly, string pageId, ModulePage page = null)  {
            if (context == null) throw new Exception("MetaPageHelper 初始化出错, controllerContext为空值.");
            else {
                this.context = context;
                request = context.ControllerContext.RequestContext.HttpContext.Request;
                account = l.core.web.Account.Current(context);
                if (account == null) throw new Exception("MetaPageHelper 初始化出错, 当前用户为空值.");
                else {
                    if (page != null) { //无模块页面，例如查询
                        this.page = page;
                        this.module = new MetaModule { ModuleID = "-100", ModulePages = new List<ModulePage> { page } };
                    } 
                    else {
                        var module = account.CurrModule;
                        if (module == null) throw new Exception("MetaPageHelper 初始化出错, module 为空值.");
                        else {
                            this.module = module;
                            this.page = module.ModulePages.Find(p => p.PageID == pageId);
                        }
                    }
                    if (this.page == null) throw new Exception(string.Format("MetaPageHelper 初始化出错,  page \"{0}\"未定义.", pageId));
                    else {
                        pageData = new MetaPageData(context);
                        pageUI = new MetaPageUI(context, webAssembly, pageData, this.page, this);
                    }
                }
            }
        }
        
        public MetaPageHelper(System.Web.Mvc.ControllerBase context, Assembly webAssembly, ModulePage page) {
            this.context = context;
            request = context.ControllerContext.RequestContext.HttpContext.Request;
            account = l.core.web.Account.Current(context);

        }

        private Dictionary<string, Query> __query {get {
            if (context.ViewData["__query"] == null) context.ViewData["__query"] = new Dictionary<string, Query>();
            return context.ViewData["__query"] as Dictionary<string, Query>;}
        }

        public Dictionary<string, FieldMetaHelper> __fms {get {
            if (context.ViewData["__fms"] == null) context.ViewData["__fms"] = new Dictionary<string, FieldMetaHelper>();
            return context.ViewData["__fms"] as Dictionary<string, FieldMetaHelper>;}
        }

        public void GetData() {
            int pageRowCount = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["StdQueryPageRowCount"] ?? "10");
            IEnumerable<string> queryList = page.QueryList ?? new string[]{};
            if (pageUI.Theme != null && pageUI.Theme.QueryList != null && pageUI.Theme.QueryList.Count() > 0)
                queryList = queryList.Union(pageUI.Theme.QueryList);

            //if (page.QueryList == null) throw new Exception(string.Format("Module \"{0}\" 的 page \"{1}\" 查询列表为空.", module.ModuleID, page.PageID));
            
            foreach (var i in queryList) {
                var q = new Query(i).Load();
                q.SysValues = account.SysParamValues();
                __query[i] = q;
                __fms[i + "_p"] = q.GetParamMeta(new Dictionary<string, DBParam> { { "Operator", new DBParam { ParamValue = Account.UserNO } } });
                int c = 0;

                //new l.core.web.ModelBinder(q.Params.Select(p => p.ParamName).ToArray(), q.SmartParams, request.QueryString).Bind(p => account.ParamFilter(p, null));
                foreach (var j in request.QueryString.AllKeys.Union(new[] { "Operator", "OperName", "Where", "LocalStoreNO" }))
                    if (q.Params.Find(p=>p.ParamName == j) != null) {
                        var v = request.QueryString[j];
                        if (!string.IsNullOrEmpty(v)) v = v.Split('.')[0];
                        //if (page.PageType != ModulePageType.ptLookup)
                            q.SmartParams.SetParamValue(j, account.ParamFilter(j, v));
                        c++;
                    }
                bool full_params = q.Params.Count == c;
                Nullable<int> pageno = null;
                string pagesttr = request.QueryString["page"]??(page.PageParam.ContainsKey("page")? page.PageParam["page"]:null);
                if (pagesttr != null)
                    try {pageno = Convert.ToInt32(pagesttr) - 1;} catch{ }
                bool mainQuery = page.MainQuery == i;
                            
                bool exec = (page.PageType == ModulePageType.ptQuery &&  mainQuery && pageno.HasValue && q.QueryType == 0)
                                                                                                        //查询页，主查询
                    ||      (page.PageType == ModulePageType.ptQuery && !mainQuery && full_params)      //查询页，非主查询
                    ||      (page.PageType == ModulePageType.ptDetail && full_params)                   //明细页
                    ||      (page.PageType == ModulePageType.ptLookup)
                    ;

                if (pageno!=null && pageno.Value < 0) pageno = null;
                var ds = exec ? (q.QueryType == 0 ? q.ExecuteQuery(null, pageno.HasValue ? pageno.Value * pageRowCount : 0, pageno.HasValue ? pageRowCount : 0) : q.ExecuteDataObject(null, false))
                    : q.Prepare();
                pageData.AddDataSet(i, ds);
                __fms[i] = new FieldMetaHelper().Ready(ds, i).CheckSQLList(true, new Dictionary<string, DBParam> { { "Operator", new DBParam { ParamValue = Account.UserNO } } });

                ExecuteLookup();
                (__fms[i] as FieldMetaHelper).Ready(ds, i);
                if (mainQuery) {
                    findActiveFlow(ds);
                    findBlackList();
                };
            }
        }

        public void ExecuteLookup()  {
            if (page.Lookups != null)
                foreach (var l in page.Lookups)  {
                    l.Ready(pageData.GetTable(l.Table), __fms[l.Table.Split('.')[0]].All).BindData(pageData.GetTable(l.Table), 
                            new []{"Operator", "OperName", "LocalStoreNO"}.ToDictionary(p=>p, q=> account.ParamFilter(q, null))
                        , ActiveFlow!=null && ActiveFlow.ID 
                        == "New"? true: false);
                }
        }

        static public PageFlowItem FindActiveFlow(ModulePage page, DataSet ds) {
            PageFlowItem activeFlow = null;
            if (page.Flows != null){
                var table = ds.Tables[0];
                var fs = page.Flows.ToList();
                if (table.Rows.Count == 0) activeFlow = fs.Find(p => p.ID == "New");
                else {
                    activeFlow = fs.Find(p => p.ID == "Checked");
                    if (activeFlow != null && table.Columns.Contains("Checked") && Convert.ToBoolean( table.Rows[0]["Checked"])) { }
                    else {
                        activeFlow = null;
                        int i = 1;
                        while(table.Columns.Contains("Checked_" + i.ToString())){
                            if (Convert.ToBoolean(table.Rows[0]["Checked_" + i.ToString()])) {
                                activeFlow = fs.Find(p => p.ID == "Checked_" + i.ToString());
                                if (activeFlow == null) {
                                    throw new Exception("流程配置和数据对象不一致，数据对象的 Checked_" + i.ToString() + " 在未包含在流程定义里面.");
                                }
                                //break;
                            }
                            i++;
                        }
                        if (activeFlow == null) activeFlow = fs.Find(p => p.ID == "Normal");
                    }
                };

                if (activeFlow == null) throw new Exception("未能根据主查询的数据判断当前流程点.");
            }
            return activeFlow;
        }
        private void findActiveFlow(DataSet ds) {

            this.activeFlow = FindActiveFlow(page, ds);
        }

        private void findBlackList() {
            if (activeFlow != null)  {
                var ds = (context.ViewData["__data"] as Dictionary<string, DataSet>)[page.MainQuery];
                var fs = new List<string>();
                int i = 0;
                foreach (DataTable dt in ds.Tables)  {
                    fs = fs.Union(from DataColumn dc in dt.Columns select dc.ColumnName).ToList();
                    fs.Add("+" + dt.TableName.Replace("_", "."));
                    fs.Add("-" + dt.TableName.Replace("_", "."));
                    i++;
                }

                if (activeFlow.BlackList != null) {
                    if (activeFlow.WhiteList != null) throw new Exception(string.Format("页面\"{0}\"的流程项\"{1}\"不能同时设置黑白名单.", page.PageID, activeFlow.ID));
                    foreach (string s in activeFlow.BlackList)
                        if (fs.IndexOf(s) < 0) throw new Exception(string.Format("页面\"{0}\"的流程项\"{1}\"黑名单设置字段\"{2}\"没有在主数据表存在.", page.PageID, activeFlow.ID, s));
                    blackList = activeFlow.BlackList;
                }
                else if (activeFlow.WhiteList != null) {
                    foreach (string s in activeFlow.WhiteList)
                        if (fs.IndexOf(s) < 0) throw new Exception(string.Format("页面\"{0}\"的流程项\"{1}\"白名单设置字段\"{2}\"没有在主数据表存在.", page.PageID, activeFlow.ID, s));
                    blackList = fs.Except(activeFlow.WhiteList).ToArray();
                    FullBlackList = activeFlow.WhiteList.Count() == 0;
                }
            }
        }

        public MvcHtmlString Render(HtmlHelper html, string type, string _params) {
            return PageUI.Render(html, type, _params);
        }

        public MvcHtmlString Render(HtmlHelper html, bool layout, bool partial = false) {
            PageUI.PrepareUI(html, true, false, partial);
            return PageUI.Html(html, layout);
        }
    }
}
