    using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text ;
using System.Data;

namespace System.Web.Mvc.Html
{
    // HtmlTagHelper的作用 1） 构造Html  2) 维护树状结构  3) 树状子节点也可以是实现 IHtml 的类，不一定是 HtmlTagHelper 类型

    // 实现一个新 Help.helper 几种方式
    // 1) 直接 extend HtmlHelper (例如 DataGrid)
    // 2) 做一个类实现 
    // 3) 做一个类，构造 HtmlTagHelper 辅助实现 (例如 Paginate)
    // 4) 做一个类，继承 HtmlTagHelper 实现 (FlowPanel)
    // 5) HtmlTagHelper.Add 可以是tag，可以是对象(实现 IHtml接口的)，也可以是 HtmlTagHelper

    public interface IHtml{
        string Html();
    }

    public class HtmlTagHelper :IHtml {
        private string tagName, name;
        private HtmlAttr attrs;
        private string text;
        private string html;
        private List<IHtml> children;
 
        public HtmlTagHelper Parent { get; set; }
        //public HtmlHelper Html { get; set; }
        public bool Xml { get; set; }
        public List<IHtml> Children { get { return children; } }

        virtual protected HtmlTagHelper ChildContainer { get; set; }
        virtual protected void OnAddChild() { 
        }

        public int Indent { get; set; }
        protected int ChildIndex {get;set;}
        private string indentStr() {
            string s = ""; for (int i = 0; i < Indent; i++) s += " "; return s;
        }

        public HtmlTagHelper() {
        }

        public string Id { get { return getName(); } }
        //自动计算id 和name（未完成序号）
        private string getName() {
            try
            {
                return name ?? //null; 
                    (Parent == null ? "" : (Parent.Id??"") + "_") + ((this.GetType().Name == "HtmlTagHelper" && (tagName != null)) ? tagName : this.GetType().Name) + ChildIndex.ToString();
            }
            catch { return null; };
        }

        public HtmlTagHelper(string name, string tagName, object htmlAttributes = null):base() {
            this.tagName = tagName;
            this.name = name;
            children = new List<IHtml>();
            attrs = new HtmlAttr(htmlAttributes);
        }

        public HtmlTagHelper Attr(string attr, string value){
            attrs[attr] = value;
            return this;
        }

        public HtmlTagHelper Css(string key, string value)
        {
            attrs.Css(key, value);
            return this;
        }

        public HtmlTagHelper Attr(object htmlAttributes) {
            foreach (var i in new HtmlAttr( htmlAttributes)) {
                Attr(i.Key, i.Value.ToString());
            }
            return this;
        }

        //自动生成jquery 插件的调用（目前是顺序，也没有包含依赖关系），还需要完善
        public HtmlTagHelper JQueryPlugIn(HtmlHelper html, string className)
        {
            var JQueryPlugIn = html.ViewData["JQueryPlugIn"] ==null?new List<string>():html.ViewData["JQueryPlugIn"]  as List<string>;
            if (JQueryPlugIn.IndexOf(className) < 0) JQueryPlugIn.Add(className);
            html.ViewData["JQueryPlugIn"] = JQueryPlugIn ;
            return this;
        }

        public HtmlTagHelper AddClass(string className)
        {
            attrs["class"] = attrs.ContainsKey("class") ? attrs["class"] + " " + className : className;
            return this;
        }


        public HtmlTagHelper Text(string text) {
            this.text = text;
            return this;
        }

        public HtmlTagHelper Html(string html) {
            this.html = html;
            return this;
        }

        private HtmlTagHelper add(string name, string tagName) {
            return new HtmlTagHelper(name, tagName, null) { Parent = this, Indent = Indent + 1, Xml = Xml };
        }
        public HtmlTagHelper Add(string name, string tagName) {
            var child = add(name, tagName);
            (ChildContainer??this).children.Add(child);
            return child;
        }

        public HtmlTagHelper Insert(string name, string tagName) {
            var child = add(name, tagName);
            (ChildContainer ?? this).children.Insert(0, child);
            return child;
        }

        public IHtml Add(IHtml child) {
            (ChildContainer??this).children.Add(child);
            return child;
        }

        public HtmlTagHelper Add(HtmlTagHelper child){
            (ChildContainer??this).children.Add(child);
            child.Parent = this;
            child.ChildIndex = (ChildContainer??this).children.Count- 1;
            OnAddChild();
            return child;
        }

        public HtmlTagHelper Add(string html)  {
            var child = new HtmlTagHelper { Parent = this, Indent = Indent  + 1, html = html};
            (ChildContainer??this).children.Add(child);
            return child;
        }

        static public HtmlTagHelper Tag(string name, string tagName) {
            return new HtmlTagHelper(name, tagName, null);
        }

        public string XML() {
            StringBuilder sb = new StringBuilder();
            bool selClose = string.IsNullOrEmpty(text) && (children == null || children.Count == 0);
            if (tagName != null) {
                var l = string.Format("<{0}", tagName);
                if (attrs.Count != 0) l += " " + string.Join(" ", attrs.Select(p =>
                    p.Value == null ? "" : (p.Key + "='" + p.Value + "'"))
                    );
                if (selClose) {
                    l += "/>";
                    sb.AppendLine(l);
                }
                else {
                    if (string.IsNullOrEmpty(text)) {
                        l += ">" + text + string.Format("</{0}>", tagName);
                        sb.AppendLine(l);
                    }
                    else if (children != null)
                    {
                        foreach (var c in children) sb.AppendLine(c.Html());
                        sb.AppendLine(string.Format("</{0}>", tagName));
                    }
                }
                
            }
 
            return sb.ToString();
        }

        private string getHtml(bool end) {
            StringBuilder sb = new StringBuilder();
            //tagName =null 就只构造children。也就是一个空容器
            bool selClose = string.IsNullOrEmpty(text) && (children == null || children.Count == 0) && Xml;
            if (tagName != null)
            {
                if (!Xml) attrs["id"] = getName();
                //不是录入框不要乱写 name
                //attrs["name"] = attrs["id"];
                sb.Append(string.Format("{0}<{1}{2}{3}>", Xml?"": indentStr(), tagName, (attrs.Count==0?"": " ") + string.Join(" ", attrs.Select(p =>
                    p.Value == null ? "" : (p.Key + "='" + p.Value + "'"))
                    ), (selClose   )? "/" : ""));
            }
            if (children!=null) foreach (var c in children) sb.AppendLine(c.Html());
            if (tagName != null) sb.Append(string.Format("{0}{1}", Xml?"": indentStr(), text));
            if (!selClose && end &&tagName != null) sb.Append(string.Format("</{0}>", tagName));
            return sb.ToString();
        }

        public string Html() {
            if (html != null) return html;
            else return  getHtml(true);
        }
        public string BeginHtml() {
            return getHtml(false);
        }
        public string EndHtml(){
            return tagName == null? "":string.Format("</{0}>", tagName);
        }
        public void WriteBeginHtml(HtmlHelper html, params string[] htmlStr)
        {
            if (htmlStr.Count() == 0) html.ViewContext.Writer.Write(BeginHtml());
            foreach (string h in htmlStr) html.ViewContext.Writer.Write(h);
        }
        public void WriteEndHtml(HtmlHelper html, params string[] htmlStr)
        {
            html.ViewContext.Writer.Write(EndHtml());
            foreach (string h in htmlStr) html.ViewContext.Writer.Write(h);
        }
    }

    static public class HtmHelper
    {
        static public MvcHtmlString Paginating1(this HtmlHelper html, string name, ITable table, ITable sumTable, string reqPageKey = "page", int countPerPage = 10){
            MvcHtmlString result = new MvcHtmlString(new l.core.web.html.Paginating1(html, name, table, sumTable, reqPageKey, countPerPage ).Html());
            return result;
        }

        //static public MvcHtmlString Paginating(this HtmlHelper html, string name, DataTable table, DataTable sumTable, string reqPageKey = "page", int countPerPage = 10){
        //    MvcHtmlString result = new MvcHtmlString(new l.core.web.html.Paginating(html, name, table, table, sumTable, reqPageKey, countPerPage).Html());
        //    return result;
        //}

        //static public MvcHtmlString QueryParams(this HtmlHelper html, string name, l.core.Query mq , List<l.core.SmartLookup> lookups, l.core.FieldMetaHelper fm, string grid, string button, object htmlAttributes) {
        //    MvcHtmlString result = new MvcHtmlString(new l.core.web.html.QueryParams(html, name, mq, null, lookups, fm, grid, button, htmlAttributes, null).Html());
            
        //    return result;
        //}

         
        //static public MvcHtmlString FlowPanel(this HtmlHelper html, string name) {
        //    MvcHtmlString result = new MvcHtmlString(new l.core.web.html.FlowPanel(name).Html());
            
        //    return result;
        //}

        static public MvcHtmlString Editor(this HtmlHelper html, string name, object value, l.core.MetaField mf, object htmlAttributes) {
            MvcHtmlString result = new MvcHtmlString(new l.core.web.html.Editor(html, name,  value, mf,  htmlAttributes).Html());

            return result;
        }
 

        //static public MvcHtmlString QueryGrid(this HtmlHelper html, string name, System.Data.DataTable table, string[] fields = null, string href = null){
        //    MvcHtmlString result = new MvcHtmlString(new l.core.web.html.QueryGrid(html, name, table, fields, href).Html());
        //    return result;
        //}

        //static public MvcHtmlString Grid<T>(this HtmlHelper html, string name, System.Data.DataTable table, l.core.FieldMetaHelper fieldMeta, 
        //    string namePrefix, Dictionary<string, object> defaultValue, string[] fields = null, bool editable = true) {
        //    MvcHtmlString result = new MvcHtmlString(new l.core.web.html.Grid<T>(html, name, table, fieldMeta, namePrefix, defaultValue, fields, editable).Html());
        //    return result;
        //}

        //static public MvcHtmlString Grid<T>(this HtmlHelper html, string name, MongoDB.Bson.BsonArray data, l.core.FieldMetaHelper fieldMeta,
        //    string namePrefix, Dictionary<string, object> defaultValue, string[] fields = null, bool editable = true) {
        //    MvcHtmlString result = new MvcHtmlString(new l.core.web.html.Grid<T>(html, name, data, fieldMeta, namePrefix, defaultValue, fields, editable).Html());
        //    return result;
        //}

        static public MvcHtmlString EditorsGrid(this HtmlHelper html, string name, MongoDB.Bson.BsonDocument data, l.core.FieldMetaHelper fieldMeta, 
            object htmlAttributes , string namePrefix, string[] fields = null) {
                MvcHtmlString result = new MvcHtmlString(new l.core.web.html.EditorsGrid(html, name, data, fieldMeta, htmlAttributes, namePrefix, fields).Html());
            return result;
        }

        static public l.core.web.html.Accordion Accordion(this HtmlHelper html, string name) {
            return new l.core.web.html.Accordion(html, name, true);
        }
        static public l.core.web.html.AccordionSheet AccordionSheet(this HtmlHelper html, string name, string caption){
            return new l.core.web.html.AccordionSheet(html, name, caption, true);
        }

        

        //todo ，待实现
        public class ScriptHelper : IDisposable {
            HtmlHelper html;
            public ScriptHelper(HtmlHelper html) {
                html.ViewContext.Writer.Write("<script>");
                this.html = html;
            }

            public void Dispose() {
                html.ViewContext.Writer.Write("</script>");
            }
        }
        static public ScriptHelper Script(this HtmlHelper html) {
            return new ScriptHelper(html);
        }
    }

    public class HtmlAttr : Dictionary<string, object> {
        public HtmlAttr(object htmlAttributes)
        {
            if (htmlAttributes != null)
            {
                if (htmlAttributes is System.Collections.IDictionary)
                    //this.Concat(htmlAttributes as Dictionary<string, object>);
                    foreach (var p in (htmlAttributes as Dictionary<string, object>))
                        this[p.Key] = p.Value;
                else
                {
                    var t = htmlAttributes.GetType();
                    foreach (var p in t.GetProperties())
                        this[p.Name] = p.GetValue(htmlAttributes, null);
                }
            }


        }

        public HtmlAttr Css(string key, string value) {
            var styles = (from i in this where i.Key.ToUpper() == "STYLE" select i);
            if (styles.Count() != 0) {
                Dictionary<string, string> style = styles.First().Value.ToString().Split(';').ToDictionary(p => p.Split(':').First(), q => q.Split(':').Last());
                style[key] = value;
                this[styles.First().Key] = string.Join(";", style.Select(p=> p.Key + ":" + p.Value));
            }
            else this["style"] = key + ":" + value;
            return this;
        }
    }
}
