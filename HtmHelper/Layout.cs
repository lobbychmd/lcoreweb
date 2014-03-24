using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc.Html;
using System.Web.Mvc;

namespace l.core.web.html
{
    public class NotExistTag: HtmlTagHelper {
        public NotExistTag(string name, string caption) :base(name, "fieldset", null){ 
            AddClass("NotExistTag").Add(null, "legend").Text(caption);
        }
    }
    public class Layout : HtmlTagHelper {
        public Layout(HtmlHelper html, string name, l.core.ModulePage page)
            : base(name, "div", null) {
                ChildContainer =
                    AddClass("Layout").AddClass("page")
                        .Add("header", "div")
                            .Add("sitelink", "div").Add(null, "a").Attr("href", "/").Text("MAHATTAN").Parent.Parent
                        .Add("title", "div")
                            .Add(null, "h1").Text( new ModulePageHelper(html.ViewContext.Controller).Module.Caption ).Parent.Parent
                        .Add("main", "div");
        }

        static public string[] StyleSheetInclude() {
            return new[] { "manhattan.css" };
        }
        
        
    }

    public class ModulePage: HtmlTagHelper  //这个类降作为模块的缺省容器. 介于模块配置 和 布局配置之间,系统自动生成.
    {
        public ModulePage(HtmlHelper html, string name, l.core.ModulePage page, MetaPageHelper pageHelper1)
            : base(name, "div", null)
        {
            var m = l.core.web.Account.Current(html.ViewContext.Controller).CurrModule;
            //if (m != null && page != null) //这里没用，会被后面覆盖
            //    name= "module_page_" + m.ModuleID + "_" + page.PageID.ToString();
            Attr("href", html.ViewContext.HttpContext.Request.Url.ToString());
            AddClass("ModulePage").Attr("mid", m == null? null : m.ModuleID)
                .Attr("pageid", page != null? page.PageID: null)
                .Attr("pageType", page == null? null:page.PageType.ToString());

            if ((page != null) &&(page.Lookups!=null))
                Add("lookupConfig", "div").Text(Newtonsoft.Json.JsonConvert.SerializeObject(page.Lookups) ).Attr("style", "display:none");
            var pageHelper = l.core.web.ModulePageHelper.Get(html.ViewContext.Controller);
           // var pageHelper1 = l.core.web.MetaPageHelper.Get(html.ViewContext.Controller, null);
            if (pageHelper1!=null) {
                if (pageHelper1.BlackList != null) Attr("blacklist", string.Join(";", pageHelper1.BlackList));
                if (pageHelper1.ActiveFlow != null) Attr("activeflow", pageHelper1.ActiveFlow.ID);
                if (pageHelper1.Page.Lookups != null) Add(null, "span").AddClass("lookup").Text(
                    Newtonsoft.Json.JsonConvert.SerializeObject(pageHelper1.Page.Lookups));
                if (pageHelper1.FullBlackList) Attr("fullBlackList", "true");
            }
            var ds = pageHelper.MainDataSet ;
            if (ds != null){
                var fs = pageHelper.GetDisableleFields();
                if (fs != null) {
                    Add(null, "input").Attr("type", "hidden").AddClass("flowconfig_readonly").Attr("value", Newtonsoft.Json.JsonConvert.SerializeObject(fs));
                }

                var das = new Dictionary<string, l.core.DataEditType[]>();
                for(int i = 1; i< ds.Tables.Count ; i++){
                    var tbName = ds.Tables[i].TableName;
                    das[tbName] = pageHelper.GetEditType(tbName);
                }
                Add(null, "input").Attr("type", "hidden").AddClass("flowconfig_edittype").Attr("value", Newtonsoft.Json.JsonConvert.SerializeObject(das));
            }
            if (pageHelper1!=null) 
                if (page.PageType == ModulePageType.ptDetail) {
                    ChildContainer =  Add(null, "form").Attr("method", "post");
                }
        }

        static public string StyleSheet()
        {
            return @"
                .ModulePage>span.lookup{ display:none}
                .ModulePage a.lookup.ui-button-text-only .ui-button-text {padding: 0.1em 0.3em;}";
        }
        static public string[] ScriptInclude()
        {
            return new[] { "modulepage.js", "lookup.js" };
        }
        static public string[] StyleSheetInclude()
        {
            return new[] { "modulepage.css" };
        }
    }

    public class AjaxPanel : HtmlTagHelper  
    {
        public AjaxPanel(HtmlHelper html, string name)
            : base(name, "div", null)
        {
            AddClass("AjaxPanel").JQueryPlugIn(html, "AjaxPanel");
            
        }

        static public string StyleSheet()
        {
            return @".AjaxPanel {border:0px solid gray;}";
        }
    }

    public class AlignPanel : HtmlTagHelper
    {
        public AlignPanel(HtmlHelper html, string name, int width, bool left)
            : base(name, "div", null)
        {
            AddClass("AlignPanel").JQueryPlugIn(html, "AlignPanel");
            if (left)
                Attr("width", width.ToString() + "px").Attr("style", "float: left;");
            else Attr("width", "auto").Attr("style", "margin-left: " + width.ToString() + "px");
        }

        static public string StyleSheet()
        {
            return @".AjaxPanel {border:1px solid gray;}";
        }

        static public string[] ScriptInclude() {
            return new[] { "modulepage.js" };
        }
    }

     

    

    public class Menu : HtmlTagHelper {
        private l.core.web.Account acc;
        public Menu(HtmlHelper html, string name, bool execCS)
            : base(name, "div", null)
        {
            acc = l.core.web.Account.Current(html.ViewContext.Controller);
            var ul =AddClass("MenuContainer").Add(null, "DIV").Attr("id", "main").Attr("class", "Menu").Attr("STYLE", "border:0px solid red")
                 .Add(null, "UL").Attr("CLASS", "sf-menu").Attr("STYLE", "height:20px;margin-left:20px");
            if (acc.Signin) addMenu(ul, acc.Menu, execCS);

            //ChildContainer = Add(null, "div");
        }

        private void addMenu(HtmlTagHelper parent, l.core.MenuItem mi, bool execCS) {
            foreach (l.core.MenuItem m in mi.Children)
            {
                var li = parent.Add(null, "LI");//.Attr("CLASS", "current");
                var a = li.Add(null, "A").Text(m.Caption);
                bool leaf = m.Children.Count == 0;
                if (!leaf)
                    addMenu(li.Add(null, "UL"), m, execCS);
                else a.Attr("HREF", (!string.IsNullOrEmpty(m.Path) ? m.Path : ("/Module/Index/" + m.ModuleID)))
                    .Attr("mis", !execCS ? null : ((!string.IsNullOrEmpty(m.Path)) ? null : ("mis://" + //Crypt.Encode(
                        //"Projects=金珏测试总部JYHOADB&ModuleID=80002&UserNO=SX&Password=e4H1YNAb+OLv5Dwg1rfB6A=="
                        string.Format("Projects={0}&ModuleID={1}&UserNO={2}&Password={3}/",
                        //System.Web.HttpContext.Current.Server.UrlEncode( 
                            System.Configuration.ConfigurationManager.AppSettings["ProjectName"]
                        //    )
                        , m.ModuleID, (!acc.Signin ?"":acc.UserNO), (Crypt.Encode(!acc.Signin?"":(acc.UserNO + acc.Password))))
                        )));//);

            }
        }

        static public string StyleSheet() {
            return @"";
        }
    }

    

    public class Accordion : HtmlTagHelper, IDisposable
    {
        private HtmlHelper html;
        bool block;
        public Accordion(HtmlHelper html, string name, bool block)
            : base(name, "div", null)
        {
            Attr("class", "Accordion");

            this.html = html; this.block = block;
            if (block) WriteBeginHtml(html);
        }
        public void Dispose()
        {
            if (block) WriteEndHtml(html);
        }
        static public MvcHtmlString StyleSheet()
        {
            return new MvcHtmlString(@"
                ");
        }
        static public string[] ScriptInclude()
        {
            return new[] { "modulepage.js" };
        }
    }

    public class AccordionSheet : HtmlTagHelper, IDisposable
    {
        private HtmlHelper html;
        bool block;
        public AccordionSheet(HtmlHelper html, string name, string caption, bool block)
            : base(name, null, null)
        {
            this.html = html; this.block = block;
            var h3 = Add(null, "h3").Add(null, "a").Attr("href", "#").Text(caption).Parent;

            //将子节点的容器重定向
            ChildContainer = Add(null, "div");

            if (block) WriteBeginHtml(html, h3.Html(), ChildContainer.BeginHtml());
        }

        public void Dispose()
        {
            if (block) WriteEndHtml(html, ChildContainer.EndHtml());
        }

        static public MvcHtmlString StyleSheet()
        {
            return new MvcHtmlString(@"
                ");
        }

    }
}
