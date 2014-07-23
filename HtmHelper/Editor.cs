using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc.Html;
using System.Web.Mvc;

namespace l.core.web.html
{
    static public class EditorHelper {
        static public MvcHtmlString DatePicker(this HtmlHelper html, string name,  object value, l.core.MetaField mf, object htmlAttributes) {
            MvcHtmlString result = new MvcHtmlString(new l.core.web.html.DateEditor(html, name, value, mf, htmlAttributes).Html());
            return result;
        }
        static public MvcHtmlString NumberEditor(this HtmlHelper html, string name, object value, l.core.MetaField mf, object htmlAttributes)
        {
            MvcHtmlString result = new MvcHtmlString(new l.core.web.html.NumberEditor(html, name, value, mf, htmlAttributes).Html());
            return result;
        }
        static public MvcHtmlString PasswordEditor(this HtmlHelper html, string name, object value, l.core.MetaField mf, object htmlAttributes)
        {
            MvcHtmlString result = new MvcHtmlString(new l.core.web.html.PasswordEditor(html, name, value, mf, htmlAttributes).Html());
            return result;
        }
        static public MvcHtmlString PropEditor(this HtmlHelper html, string name, object value, l.core.MetaField mf, object htmlAttributes)
        {
            MvcHtmlString result = new MvcHtmlString(new l.core.web.html.PropEditor(html, name, value, mf, htmlAttributes).Html());
            return result;
        }
    }

    public class Editor : IHtml {
        private string strHtml;

        public static void SetSizeAttr(HtmlAttr attr, MetaField mf) {
            int w = 0;
            int editorSize = 0;
            if (mf.CharLength > 0) {
                editorSize = mf.CharLength;
                attr["maxlength"] = (mf.CharLength).ToString();
                attr["size"] = (mf.CharLength).ToString();
            }
            else if ((mf.EditorType??"").ToUpper().Equals("DATETIME") ) {
                editorSize = 16;
            }
            if (attr.ContainsKey("editorSize") ) 
                editorSize = Convert.ToInt32( attr[ "editorSize" ]);
            w = (editorSize == 0?14: editorSize) * 8;
            attr.Css("width",  w.ToString() + "px");
        }

        public Editor(System.Web.Mvc.HtmlHelper html, string name, object value, l.core.MetaField mf, object htmlAttributes) {
            if (mf != null) value = mf.Format(value);
            
            var attr = new HtmlAttr(htmlAttributes);
            attr["editor"] = "true";
            attr["fn"] = name.IndexOf(".") < 0 ? name : name.Split('.').Last();
            attr["class"] = attr.ContainsKey("class") ? attr["class"] + " editor" : "editor";
            if (!string.IsNullOrEmpty(mf.Regex)) { //支持正则表达式的注释，就是 # 后面的所有文字
                attr["format"] = mf.Regex.IndexOf('#') > 0 ? mf.Regex.Split('#')[0].Trim() : mf.Regex;
                if (mf.Regex.IndexOf('#') > 0) attr["formatErrorMsg"] = mf.Regex.Split('#')[1];
            }

            if (mf != null) {
                var dropdownlist = ((mf.EditorType ?? "").ToUpper().Equals("DROPDOWNLIST"));
                var dic = !string.IsNullOrEmpty(mf.DicNO);
                if ((mf.EditorType ?? "").ToUpper().Equals("PROPEDIT"))
                    strHtml = html.PropEditor(name, value, mf, attr).ToHtmlString();
                else if (dic || dropdownlist)
                {
                    bool idprefix = (mf.FieldName.Length > 2) && (mf.FieldName.IndexOf("ID") == mf.FieldName.Length - 2 || mf.FieldName.IndexOf("NO") == mf.FieldName.Length - 2);
                    var strvalue = Convert.ToString(value);
                    //if (!ParialMatch) if (!mf.List().ContainsKey(Convert.ToString(value))) d[Convert.ToString(value)] =Convert.ToString(value);
                    if ((mf.EditorType ?? "").ToUpper().Equals("DROPDOWNLIST")) {
                        var items = mf.List().Union(new Dictionary<string, string> { { "", "" } })
                            .Select(p => new System.Web.Mvc.SelectListItem
                            {
                                Value = idprefix || !dic? p.Key : p.Value,
                                Text = p.Value,
                                Selected = strvalue == p.Key || p.Key.IndexOf(strvalue + ".") == 0 || p.Key.IndexOf("." + strvalue) > 0
                            });

                        strHtml = html.DropDownList(name, items, attr).ToHtmlString();
                    } else {
                        attr["list"] = string.Join(";", mf.List().Select(p=> p.Key + "." + p.Value));
                        strHtml = html.TextBox(name, value, attr).ToHtmlString();
                    }
                    if (dic) strHtml +=  html.TextBox(name + "@type", "dic", new { style = "display:none" }).ToHtmlString();
                } else if ((mf.EditorType ?? "").ToUpper().Equals("BOOLEAN")) {
                    if (value is string) value = value.ToString() == "true,false"; else if (value == DBNull.Value ) value = null;
                    strHtml = html.CheckBox(name, Convert.ToBoolean( value), attr).ToHtmlString();
                } else if ((mf.EditorType ?? "").ToUpper().Equals("DATE")){
                    attr["type"] = "_date";
                    strHtml = html.DatePicker(name, value, mf, attr).ToHtmlString();
                }
                else if ((mf.EditorType ?? "").ToUpper().Equals("DATETIME"))  {//编辑框暂不支持日期时间
                    attr["type"] = "datetime";
                    strHtml = html.DatePicker(name, value, mf, attr).ToHtmlString();
                }
                else if ((mf.EditorType ?? "").ToUpper().Equals("TIME")){
                    attr["type"] = "_time";
                    strHtml = html.DatePicker(name, value, mf, attr).ToHtmlString();
                }
                else if ((mf.EditorType ?? "").ToUpper().Equals("NUMBER"))
                    strHtml = html.NumberEditor(name, value, mf, attr).ToHtmlString();
                else if ((mf.EditorType ?? "").ToUpper().Equals("LIST"))
                    strHtml = new ListEditor(html, name, (value ?? "").ToString(), mf, attr).Html();
                else if ((mf.EditorType ?? "").ToUpper().Equals("TEXTAREA")) {
                    //SetSizeAttr(attr, mf);
                    strHtml = html.TextArea(name, (value ?? "").ToString(), attr).ToHtmlString();
                }

                else if ((mf.EditorType ?? "").ToUpper().Equals("PASSWORD"))
                    strHtml = html.PasswordEditor(name, value, mf, attr).ToHtmlString();
                else
                {
                    SetSizeAttr(attr, mf);
                    strHtml = html.TextBox(name, value, attr).ToHtmlString();
                }
            }
            else strHtml = html.TextBox(name, value, attr).ToHtmlString();
        }

        public string Html() {
            return strHtml;
        }

        static public string StyleSheet()
        {
            return @"
                ";
        }

        
    }

    public class DateEditor : IHtml
    {
        private string strHtml;

        public DateEditor(System.Web.Mvc.HtmlHelper html, string name, object value, l.core.MetaField mf, object htmlAttributes) {
            var attr = new HtmlAttr(htmlAttributes);
            Editor.SetSizeAttr(attr, mf);
            if(!attr.ContainsKey("type")) attr["type"] = "date";
            attr["class"] = (attr.ContainsKey("class") ? (attr["class"].ToString() + " ") : "") + "DateEditor";
            if (!attr.ContainsKey("format"))
            { }//attr["format"] = @"^(^(\d{4}|\d{2})(\-|\/|\.)\d{1,2}\3\d{1,2}$)|(^\d{4}年\d{1,2}月\d{1,2}日$)$";
            strHtml = html.TextBox(name, mf.Render(value), attr).ToHtmlString();
        }

        public string Html()
        {
            return strHtml;
        }
    }

    //其实没有必要做一个 NumberEditor，用 Format 设定就可以了，这里只是一个例子
    public class NumberEditor : IHtml {
        private string strHtml;

        public NumberEditor(System.Web.Mvc.HtmlHelper html, string name, object value, l.core.MetaField mf, object htmlAttributes)
        {
            var attr = new HtmlAttr(htmlAttributes);
            Editor.SetSizeAttr(attr, mf);
            attr["class"] = (attr.ContainsKey("class")? (attr["class"].ToString() + " ") :"") +  "NumberEditor";
            if (!attr.ContainsKey("format")) attr["format"] = @"^\\d+$";
            strHtml = html.TextBox(name, value, attr).ToHtmlString();
        }

        public string Html()
        {
            return strHtml;
        }
    }

    public class PasswordEditor : IHtml
    {
        private string strHtml;

        public PasswordEditor(System.Web.Mvc.HtmlHelper html, string name, object value, l.core.MetaField mf, object htmlAttributes)
        {
            var attr = new HtmlAttr(htmlAttributes);
            Editor.SetSizeAttr(attr, mf);
            attr["class"] = (attr.ContainsKey("class") ? (attr["class"].ToString() + " ") : "") + "PasswordEditor";
            attr["type"] = "password";
            strHtml = html.TextBox(name, value, attr).ToHtmlString() +
                html.TextBox(name + "@type", "password", new {style="display:none" }).ToHtmlString();
        }

        public string Html()
        {
            return strHtml;
        }
    }
    
    public class PropEditor : IHtml
    {
        private string strHtml;

        public PropEditor(System.Web.Mvc.HtmlHelper html, string name, object value, l.core.MetaField mf, object htmlAttributes)
        {
            var attr = new HtmlAttr(htmlAttributes);
            Editor.SetSizeAttr(attr, mf);
            attr["class"] = (attr.ContainsKey("class") ? (attr["class"].ToString() + " ") : "") + "PropEditor";
            strHtml = html.TextBox(name, value, attr).ToHtmlString();
            strHtml += "<div class='propTable'>";
            foreach(var i in mf.List()){
                strHtml += "<div><input type=checkbox idx='" + i.Key.Split('.')[0] + "' /><span>" + i.Value + "</span></div>";
            }
            strHtml += "</div>";
        }

        public string Html()
        {
            return strHtml;
        }
    }

    public class ListEditor :HtmlTagHelper, IHtml{
        public ListEditor(HtmlHelper html, string name, object value, l.core.MetaField mf, object htmlAttributes) :base(null, "fieldset", htmlAttributes){
            Attr("class", "ListEditor");
            Attr("fieldName", name);
            Add(html.TextBox("_" + name, "", htmlAttributes).ToHtmlString());
            Add(null, "a").AddClass("add");
            Add("<br/>");
            Add(html.DropDownList(name, value.ToString().Split('\n').Where(p=> p.Trim()!="").Select(p => new SelectListItem { Text= p }), new { Size = 5 }).ToHtmlString());
            Add("<br/>");
            Add(html.TextArea(name, value.ToString(), htmlAttributes).ToHtmlString());
        }
    }
}
