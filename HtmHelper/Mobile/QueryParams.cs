using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;

namespace l.core.web.html.mobile
{
    public class QueryParams : HtmlTagHelper
    {
        private HtmlHelper html;

        public QueryParams(HtmlHelper html, string name, l.core.MetaQuery mq, List<l.core.SmartLookup> lookup, l.core.FieldMetaHelper fieldMeta, string grid, string button, object htmlAttributes)
            : base(name, "div", htmlAttributes)
        {
            this.html = html;
            var request = HttpContext.Current.Request;
            Attr("Class", "QueryParams").Attr("grid", grid);

            Attr("paramAsString", mq.ParamsAsString(fieldMeta));

            HtmlTagHelper form = null;
            
            form = Add(null, "form").Attr("method", "get");
                
            mq.Params.ForEach(p => {
                    var fm = fieldMeta.Get(p.ParamName);
                    var div = form.Add(null, "div").Attr("data-role", "fieldcontain")./*AddClass("ui-hide-label").*/Attr("style", p.ParamName.Equals("Operator") ? "display:none" :null);
                    div.Add(null, "label").Attr("for", p.ParamName).Text(fm.DisplayLabel);

                    div.Add(new Editor(html, p.ParamName,request.QueryString[p.ParamName] ?? p.Default(),
                        fm, new { id = p.ParamName, placeholder = fm.DisplayLabel}));
                }
            );

            form.Add(html.Hidden("page", 1).ToString());
            //form.Add(html.Hidden("ParamGroup", request.QueryString["ParamGroup"]).ToString());
            //form.Add(html.Hidden("pgi", request.QueryString["pgi"]).ToString());
            foreach (var p in (from s in request.QueryString.AllKeys where (s != "page") && (s != "recordCount") && (mq.Params.Find(p => p.ParamName == s) == null) select s))
                form.Add(html.Hidden(p, request.QueryString[p]).ToString());

            //如果没指定查询按钮就加一个
            if (button == null) form.Add("", "button").Attr("type", "submit").Attr("data-theme", "a").AddClass("ui-btn-hidden").Text("查询");
            else Attr("button", button);
            //form.Add(null, "div").Attr("style", "clear:both");
        }

        static public MvcHtmlString StyleSheet()
        {
            return new MvcHtmlString(@"
                fieldset.QueryParams{width:auto; min-height:100px;}
                fieldset.QueryParams>*{}
                ");
        }
    }
    
    public class DialogQueryParams : HtmlTagHelper
    {
        private HtmlHelper html;

        public DialogQueryParams(HtmlHelper html, string name, l.core.MetaQuery mq, l.core.FieldMetaHelper fieldMeta, string grid, string ajaxContainer, object htmlAttributes) :
            base(name, "div", htmlAttributes) {
            this.html = html;
            var request = HttpContext.Current.Request;
            Attr("Class", "DialogQueryParams").Attr("grid", grid).Attr("ajaxContainer", ajaxContainer);

            var acc = Add(null, "fieldset").Add(null, "legend").Parent;
            HtmlTagHelper form = null;
            form = acc.Add(null, "form").Attr("method", "get").Attr("action", string.Format("{0}://{1}{2}", request.Url.Scheme, request.Url.Authority, request.Url.AbsolutePath));

            mq.Params.ForEach(p => 
                {   var fm = fieldMeta.Get(p.ParamName);
                    var div = form.Add(null, "div").Attr("style", "display:" + (p.ParamName.Equals("Operator")?"none":"inline-block"));
                    div.Add(null, "label").Text(fm.DisplayLabel);
                    div.Add("<br />");
                    
                    div.Add(new Editor(html, p.ParamName,
                            (p.ParamName.Equals("Operator") && l.core.web.Account.Current(html.ViewContext.Controller) != null) ? l.core.web.Account.Current(html.ViewContext.Controller).UserNO : request.QueryString[p.ParamName] ?? p.Default(),
                        fm, null));

                });
                form.Add(null, "input").Attr("type", "submit").Attr("value", "查询");
                form.Add(html.Hidden("page", 1).ToString());
                foreach(var p in (from s in request.QueryString.AllKeys where (s!= "page") && (mq.Params.Find(p=>p.ParamName == s) == null) select s ))
                    form.Add(html.Hidden(p, request.QueryString[p]).ToString());
        }
        
        
        static public MvcHtmlString StyleSheet() {
            return new MvcHtmlString( @"
                ");
        }
    }

    public class Accordion : HtmlTagHelper, IDisposable 
    {
        private HtmlHelper html;
        bool block;
        public Accordion(HtmlHelper html, string name, bool block) : base(name, "div", null) {
            Attr("class", "Accordion");

            this.html = html; this.block = block;
            if (block) WriteBeginHtml(html);
        }
        public void Dispose(){
            if (block) WriteEndHtml(html);
        }
        static public MvcHtmlString StyleSheet() {
            return new MvcHtmlString(@"
                ");
        }

    }

    public class AccordionSheet : HtmlTagHelper, IDisposable {
        private HtmlHelper html;
        bool block;
        public AccordionSheet(HtmlHelper html, string name, string caption, bool block) : base(name, null, null) {
            this.html = html; this.block = block;
            var h3 = Add(null, "h3").Add(null, "a").Attr("href", "#").Text(caption).Parent;
            
            //将子节点的容器重定向
            ChildContainer = Add(null, "div");

            if (block) WriteBeginHtml(html, h3.Html(), ChildContainer.BeginHtml());
        }

        public void Dispose() {
            if (block) WriteEndHtml(html, ChildContainer.EndHtml());
        }

        static public MvcHtmlString StyleSheet() {
            return new MvcHtmlString(@"
                ");
        }

    }
}
