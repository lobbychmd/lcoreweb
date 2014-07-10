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
    //存放页面数据
    //参数类型的匹配。 例如 Helper 里面是 DataTable 参数，但是 Script 肯定是 string 的，就需要按照约定提取。
    //用Script 反射Helper 和参数
    //
    //public interface ModulePageHelperFactory {
      //  ModulePageHelper Get(System.Web.Mvc.ControllerBase context, string pageID = null);
    //}
    public class ModulePageHelper  {
        private string pageID;
        protected System.Web.Mvc.ControllerBase context;
        private Dictionary<dynamic, int> uiEvalResult;
        public l.core.Theme theme;

        public l.core.ModulePage MetaPage { get; set; }
        public l.core.web.Account Account { get { return l.core.web.Account.Current(context); } }
        public l.core.MetaModule Module { get { return l.core.web.Account.Current(context).CurrModule; } }
        public l.core.PageFlowItem ActiveState { get; set; }

        public l.core.ModulePage CurrPage { get {
            return Module == null? null:
                Module.ModulePages.Find(p => p.PageID.ToUpper() == Convert.ToString(pageID ?? context.ControllerContext.RouteData.Values["action"]).ToUpper());
        } 
        }

        static public ModulePageHelper Get(System.Web.Mvc.ControllerBase context, string pageID = null){
            if (!context.ViewData.ContainsKey("page")) context.ViewData["page"] = new ModulePageHelper(context, pageID);
            return context.ViewData["page"] as ModulePageHelper;
        }

        public ModulePageHelper(System.Web.Mvc.ControllerBase context, string pageID = null) {
            this.context = context;
            this.pageID = pageID;
            var themeid = System.Configuration.ConfigurationManager.AppSettings["Theme"];
            if (!string.IsNullOrEmpty(themeid)) {
                theme = new Theme(themeid).Load();
            }
        }

        public DataSet MainDataSet { get {
            return context.ViewData["data"] == null ? null : (
                context.ViewData["data"] is Dictionary<string, DataSet> && (context.ViewData["data"] as Dictionary<string, DataSet>).Count != 0 ?
                (context.ViewData["data"] as Dictionary<string, DataSet>).First().Value : null);
        }}
        private System.Web.HttpRequestBase request {get{
            return context.ControllerContext.RequestContext.HttpContext.Request;
        }}

        virtual public void LoadFromStdReceipt() { 
            //
        }

        virtual public void GetActiveState (){
            //
        }

        virtual public string[] GetDisableleFields()
        {
            return null;
        }
        virtual public l.core.DataEditType[] GetEditType(string table)
        {
            return null;
        }
        private void setNewState(){
            if (CurrPage.Flows != null){
                var fs = (from f in CurrPage.Flows where f.ID == "New" select f);
                if (fs.Count() == 1) ActiveState = fs.First();
            }
        }
        public ModulePageHelper ExecuteQuery() {
            int pageRowCount = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["StdQueryPageRowCount"] ?? "10");

            var qs = (CurrPage != null && theme != null) ? CurrPage.QueryList.Union(theme.QueryList) : (CurrPage != null ? CurrPage.QueryList : theme.QueryList);
            foreach (var q in qs) {
                if (!string.IsNullOrEmpty(q.Trim())) {
                    l.core.Query query = new Query(q.Trim()).Load();
                    context.ViewData["mq-" + q.Trim()] = query;

                    //主查询的参数约等于页面的参数
                    if (CurrPage!= null && CurrPage.Params == null) CurrPage.Params = query.Params.Select(p => p.ParamName).ToArray();

                    DataSet ds = null;
                    bool allParamsProvide = true;
                    bool paramsEmpty = false;
                    foreach (var par in query.Params) {
                        var vv = request.QueryString[par.ParamName];
                        if ((vv == null) && ((CurrPage != null) && (CurrPage.PageParam != null) && CurrPage.PageParam.ContainsKey(par.ParamName)))
                            vv = CurrPage.PageParam[par.ParamName];
                        var v = Account.ParamFilter(par.ParamName,vv);
                        if ((CurrPage != null && CurrPage.PageType != ModulePageType.ptQuery) && (v == null))
                        { allParamsProvide = false; break; }
                        else if (v == "") paramsEmpty = true;

                        query.SmartParams.SetParamValue(par.ParamName, v);
                    }
                    if (CurrPage != null && CurrPage.PageType == ModulePageType.ptQuery) {
                        var page = request.QueryString["page"];
                        var exec = (page != null) || (theme.LayoutQueries != null && (from i in theme.QueryList where i== q select i).Count() == 1);
                        if (exec){        
                            var p = Convert.ToInt32(page) - 1;
                            ds = query.ExecuteQuery(null, p < 0 ? 0 : p * pageRowCount, p < 0 ? 0 : pageRowCount);
                        }
                        else ds = query.Prepare();
                    }
                    else {
                        var exec = allParamsProvide;
                        if (exec) {
                            if (paramsEmpty) setNewState();
                            if (query.QueryType == 0) ds = query.ExecuteQuery(null, 0, 0, false);
                            else ds = query.ExecuteDataObject();
                        }
                        else {
                            setNewState();
                            ds = query.Prepare();
                            foreach (DataTable table in ds.Tables) table.Rows.Add(table.NewRow());
                        }
                    }
                    var fms = query.GetParamMeta(new Dictionary<string,DBParam>{{"Operator", new DBParam{ParamValue = Account.UserNO}}});
                    context.ViewData["fms-" + query.QueryName + "_p"] = fms;
                    context.ViewData["lookups"] = l.core.SmartLookup.GetLookupFromFieldMeta(fms.All);
                    if (ds != null) context.ViewData["fms-" + query.QueryName] = new l.core.FieldMetaHelper().Ready(ds, query.QueryName).CheckSQLList(true, new Dictionary<string, DBParam> { { "Operator", new DBParam { ParamValue = Account.UserNO } } }); 
                    AddDataSet(q.Trim(), ds);
                }

            }
            return this;
        }

        public ModulePageHelper ExecuteLookup()
        {
            if (CurrPage.Lookups !=null)
                foreach (var l in CurrPage.Lookups) {
                    l.Ready(GetTable(l.Table), (context.ViewData["fms-" + l.Table.Split('.')[0]] as l.core.FieldMetaHelper).All);
                }
            return this;
        }
        public void AddDataSet(string key, DataSet ds) {
            Dictionary<string, DataSet> data = context.ViewData.ContainsKey("data") ? context.ViewData["data"] as Dictionary<string, DataSet> : new Dictionary<string, DataSet>();
            data.Add(key, ds);
            context.ViewData["data"] = data;
        }

        public DataTable GetTable(string name) {
            var d = (context.ViewData["data"] as Dictionary<string, DataSet>);
            var names = name.Split('.');
            var idx = names.Count() == 1 ? 0 : Convert.ToInt32(names[1]);
            return d.ContainsKey(names[0])?(d[names[0]].Tables.Count > idx ? d[names[0]].Tables[idx]:null):null;
        }

        //自动生成jquery 插件的调用（顺序），还需要完善
        public string JQueryPlugIn() {
            return context.ViewData["JQueryPlugIn"] == null ? "" :
                string.Join("\n", (context.ViewData["JQueryPlugIn"] as List<string>).Select(p => string.Format("if($.fn.{0}) {{$('.{0}').{0}();}}", p)));
        }

        private dynamic DeserializeHelper(HtmlHelper html, string script) {
            var cls = script.Split(' ')[0];
            var param = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(script.Substring(cls.Length + 1));
            var asm = System.Reflection.Assembly.Load("l.core.web");
            cls = "l.core.web.html." + cls;
            var typ = asm.GetType(cls);
            if (typ == null) {
                try { asm = System.Reflection.Assembly.Load("l.core.web.pub"); }
                catch { asm = System.Reflection.Assembly.Load("l.publish.web"); }
                typ = asm.GetType(cls);
                if (typ == null) {
                    asm = System.Reflection.Assembly.Load("l.net");
                    typ = asm.GetType(cls);
                }
            }
            if (typ == null) //throw new Exception(cls + "不存在.");
                return new l.core.web.html.NotExistTag(null, cls + "不存在.");
            else {
                var @params = typ.GetConstructors()[0].GetParameters().Select(
                        p => {
                            if (p.ParameterType == typeof(System.Web.Mvc.HtmlHelper))  return html;
                            else if (param.ContainsKey(p.Name)){
                                if (p.ParameterType == typeof(l.core.ModulePage))
                                    return (Module.ModulePages.Find(pp => pp.PageID == param[p.Name].ToString()) ?? new l.core.ModulePage { PageID = param[p.Name].ToString() });
                                else if (p.ParameterType == typeof(System.Data.DataTable))
                                    return GetTable(param[p.Name].ToString());
                                else if (p.ParameterType == typeof(ModulePageHelper))
                                    return this;
                                else if (p.ParameterType == typeof(l.core.IQuery))
                                    return context.ViewData["mq-" + param[p.Name]] as l.core.IQuery;
                                else if (p.ParameterType == typeof(int))
                                    return Convert.ToInt32(param[p.Name]);
                                else if (p.ParameterType == typeof(Dictionary<string, string>))
                                {
                                    var pv = param[p.Name] as Newtonsoft.Json.Linq.JObject;
                                    var pv1 = new Dictionary<string, string>();
                                    foreach (var i in pv)
                                        pv1[i.Key] = i.Value.ToString();
                                    return pv1;
                                }
                                else if (p.ParameterType == typeof(List<string>))
                                {
                                    var pv = param[p.Name] as Newtonsoft.Json.Linq.JArray;
                                    var pv1 = new List<string>();
                                    foreach (var i in pv) pv1.Add(i.ToString());
                                    return pv1;
                                }
                                else if (p.ParameterType == typeof(string[]))
                                {
                                    var pv = param[p.Name] as Newtonsoft.Json.Linq.JArray;
                                    return pv.Select(p1 => p1.ToString()).ToArray();
                                }
                                else if (p.ParameterType == typeof(l.core.FieldMetaHelper))
                                    return context.ViewData["fms-" + param[p.Name]];
                                else
                                    return param[p.Name];
                            }
                            else {
                                if (p.ParameterType == typeof(l.core.FieldMetaHelper)) return new l.core.FieldMetaHelper();
                                else if (p.ParameterType == typeof(ModulePageHelper))
                                    return this;
                                else if (p.ParameterType == typeof(l.core.ModulePage))
                                    return CurrPage;
                                else return null;
                            }
                        }).ToArray();
                var ins = asm.CreateInstance(cls, false, System.Reflection.BindingFlags.Default, null,
                    @params, null, null);
                //var m = html.GetType().GetMethod(cls);
                //var result = m.Invoke(html, m.GetParameters().Select(p => param.ContainsKey(p.Name) ? param[p.Name] : null).ToArray());
                return ins ;
            }
        }

        public void EvalUI(HtmlHelper html, bool ajax = false) {
            if(uiEvalResult == null) uiEvalResult = new Dictionary<dynamic, int>();
            EvalUI(html, CurrPage==null?"":CurrPage.UI);
            
            var layoutEvalResult = new Dictionary<dynamic, int>();
            if (theme != null)
                EvalUI(html, theme.LayoutUI, layoutEvalResult);
            else layoutEvalResult[new l.core.web.html.ModulePage(html, null, CurrPage, null)] = 0;
            if (!ajax) mergeUI(layoutEvalResult);
        }

        private void mergeUI(Dictionary<dynamic, int> layoutEvalResult ) {
            bool merge = false;
            var newUI = new Dictionary<dynamic, int> ();

            foreach(var i in layoutEvalResult){
                newUI[i.Key] = i.Value;
                if (i.Key is l.core.web.html.ModulePage) {
                    int topIndent = -1;
                    foreach (var j in uiEvalResult){
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
            else uiEvalResult = newUI;
        }

        public void EvalUI(HtmlHelper html, string script, Dictionary<dynamic, int> evalResult = null) {
            if (evalResult == null) {
                if (uiEvalResult == null) uiEvalResult = new Dictionary<dynamic, int>();
                evalResult = uiEvalResult;
            }

            //if (wrapModulePage) uiEvalResult[new l.core.web.html.ModulePage(html, null, CurrPage)] = -1;
            foreach (var s in script.Split('\n'))  {
                var ss = s.TrimEnd(); var sss = ss.TrimStart();
                if (string.IsNullOrEmpty(sss)) continue;
                var indent = ss.Length - sss.Length;

                var obj = DeserializeHelper(html, sss);

                //找父亲
                for (int i = evalResult.Count - 1; i >= 0; i--)
                    if (evalResult.ElementAt(i).Value < indent) {
                        if (obj is HtmlTagHelper)
                            evalResult.ElementAt(i).Key.GetType().GetMethod("Add", new System.Type[] { typeof(HtmlTagHelper) }).Invoke(evalResult.ElementAt(i).Key, new[] { obj });
                        else evalResult.ElementAt(i).Key.GetType().GetMethod("Add", new System.Type[] { typeof(string) }).Invoke(evalResult.ElementAt(i).Key, new[] { obj.Html() });
                        break;
                    }

                evalResult[obj] = indent;
            }
        }

        private void checkEvalResult() {
            if (uiEvalResult == null) throw new Exception("先 EvalUI.");
        }

        public MvcHtmlString UIContent(HtmlHelper html, string script) {
            EvalUI(html, script);
            return UIContent();
        }

        public MvcHtmlString UIContent(){
            checkEvalResult();
            if (uiEvalResult.Count == 0) return new MvcHtmlString("");
            else {
                int parent_indent = uiEvalResult.First().Value;
                //return new MvcHtmlString(string.Join("\n", uiEvalResult.Where(p => l.core.ScriptHelper.GetDynamicProp(p.Key, "Parent") == null).Select(p => p.Key.GetType().GetMethod("Html", new System.Type[] { }).Invoke(p.Key, null) as string)));
                var htm = string.Join("\n", uiEvalResult.Where(p => p.Value == parent_indent).Select(p => p.Key.GetType().GetMethod("Html", new System.Type[] { }).Invoke(p.Key, null) as string));
                return new MvcHtmlString(htm);
            }
        }

        public MvcHtmlString UIScript()  {
            checkEvalResult();
            return new MvcHtmlString(string.Join("\n", uiEvalResult.Select(p => {
                var s = p.Key.GetType().GetMethod("Script", new System.Type[] { });
                if (s != null) return s.Invoke(p.Key, null) as string;
                else return "";
            })));
        }

        public MvcHtmlString UIScriptInvoke() {
            checkEvalResult();
            return new MvcHtmlString(string.Join("\n", uiEvalResult.Select(p =>{
                var ts = p.Key.GetType().Name.Split('.');
                return string.Format("if ($.fn.{0}) $('.{0}').{0}();", ts[ts.Length - 1]);
            }).Distinct()));
        }

        public MvcHtmlString UIStyle()
        {
            checkEvalResult();
            return new MvcHtmlString(string.Join("\n", uiEvalResult.Select(p => {
                var s = p.Key.GetType().GetMethod("StyleSheet", new System.Type[] { });
                if (s != null) return s.Invoke(p.Key, null) as string;
                else return "";
            })));
        }

        public MvcHtmlString UIStyleInclude() {
            checkEvalResult();
            var include = new List<string>();
            foreach(var ui in uiEvalResult) {
                var s = ui.Key.GetType().GetMethod("StyleSheetInclude", new System.Type[] { });
                if (s != null) 
                    foreach(string ss in s.Invoke(ui.Key, null) as IEnumerable<string>){
                        if (!include.Contains(ss)) include.Add(ss);
                    }

            };
            string strInclude = string.Join("\n", include.Select(p =>
                string.Format("<link href='/Scripts/helper/{0}' rel='stylesheet' type='text/css' />", p)
            ));
            if (theme != null) strInclude += "\n" + string.Format("<link href='/Home/ThemeStyle/{0}' rel='stylesheet' type='text/css' />", 
                context.ControllerContext.HttpContext.Server.UrlEncode(theme.Theme));
            return new MvcHtmlString(strInclude);
        }

        public MvcHtmlString UIScriptInclude() {
            checkEvalResult();
            var include = new List<string>();
            foreach(var ui in uiEvalResult) {
                var s = ui.Key.GetType().GetMethod("ScriptInclude", new System.Type[] { });
                if (s != null) 
                    foreach(string ss in s.Invoke(ui.Key, null) as IEnumerable<string>){
                        if (!include.Contains(ss)) include.Add(ss);
                    }

            };
            return new MvcHtmlString(string.Join("\n", include.Select(p =>
                string.Format("<script src='/Scripts/helper/{0}?{1}' type='text/javascript'></script>", p, System.Configuration.ConfigurationManager.AppSettings["verfix"])
            )));
        }
    }
}

