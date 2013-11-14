using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc.Html;
using System.Web.Mvc;

namespace l.core.web.html.mobile
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

    }

    public class Editor : IHtml {
        private string strHtml;

        public Editor(System.Web.Mvc.HtmlHelper html, string name, object value, l.core.MetaField mf, object htmlAttributes) {
            var attr = new HtmlAttr(htmlAttributes);
            attr["class"] = attr.ContainsKey("class") ? attr["class"] + " editor" : "editor";
            if (!string.IsNullOrEmpty(mf.Regex)) { //支持正则表达式的注释，就是 # 后面的所有文字
                attr["format"] = mf.Regex.IndexOf('#') > 0 ? mf.Regex.Split('#')[0].Trim() : mf.Regex;
                if (mf.Regex.IndexOf('#') > 0) attr["formatErrorMsg"] = mf.Regex.Split('#')[1];
            }

            if (mf != null) {
                if (!string.IsNullOrEmpty(mf.DicNO) || ((mf.EditorType ?? "").ToUpper().Equals("DROPDOWNLIST"))){
                    //是否部分匹配。
                    bool ParialMatch = (mf.FieldName.Length > 6) && mf.FieldName.IndexOf("TypeID") == mf.FieldName.Length - 6;
                    ParialMatch = ParialMatch || (!string.IsNullOrEmpty(mf.DicNO));

                    var d= new Dictionary<string, string> { {"",""}};
                    if (!ParialMatch) if (!mf.List().ContainsKey(Convert.ToString(value))) d[Convert.ToString(value)] =Convert.ToString(value);
                    strHtml = html.DropDownList(name, mf.List().Union(d).Select(p => new System.Web.Mvc.SelectListItem { Value = p.Key, Text = p.Value, 
                        Selected = ParialMatch?(p.Key.IndexOf(Convert.ToString(value) + ".")==0) :p.Key.Equals(Convert.ToString(value)) }), 
                        attr).ToHtmlString();
                }else if ((mf.EditorType ?? "").ToUpper().Equals("BOOLEAN")) {
                    if (value is string) value = value.ToString() == "true,false"; else if (value == DBNull.Value ) value = null;
                    strHtml = html.CheckBox(name, Convert.ToBoolean( value), attr).ToHtmlString();
                }else if ((mf.EditorType ?? "").ToUpper().Equals("DATE"))
                    strHtml = html.DatePicker(name, value, mf, attr).ToHtmlString();
                else if ((mf.EditorType ?? "").ToUpper().Equals("DATETIME"))
                    strHtml = html.DatePicker(name, value, mf, attr).ToHtmlString();
                else if ((mf.EditorType ?? "").ToUpper().Equals("NUMBER"))
                    strHtml = html.NumberEditor(name, value, mf, attr).ToHtmlString();
                else if ((mf.EditorType ?? "").ToUpper().Equals("LIST"))
                    strHtml = new ListEditor(html, name, (value ?? "").ToString(), mf, attr).Html();
                else if ((mf.EditorType ?? "").ToUpper().Equals("TEXTAREA"))
                    strHtml = html.TextArea(name, (value ?? "").ToString(), attr).ToHtmlString();
                else {
                    if (mf.CharLength > 0) {
                        attr["size"] = mf.CharLength;
                        attr["maxlength"] = mf.CharLength;
                    }
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
            attr["class"] = (attr.ContainsKey("class") ? (attr["class"].ToString() + " ") : "") + "DateEditor";
            if (!attr.ContainsKey("format")) attr["format"] = @"^(^(\d{4}|\d{2})(\-|\/|\.)\d{1,2}\3\d{1,2}$)|(^\d{4}年\d{1,2}月\d{1,2}日$)$";
            strHtml = html.TextBox(name, (value is DateTime && mf.EditorType.ToUpper() == "DATE")?((DateTime)value ).ToString("d"):value, attr).ToHtmlString();
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
            attr["class"] = (attr.ContainsKey("class")? (attr["class"].ToString() + " ") :"") +  "NumberEditor";
            if (!attr.ContainsKey("format")) attr["format"] = @"^\\d+$";
            strHtml = html.TextBox(name, value, attr).ToHtmlString();
        }

        public string Html()
        {
            return strHtml;
        }
    }
    
    public class ListEditor :HtmlTagHelper, IHtml{
        public ListEditor(HtmlHelper html, string name, object value, l.core.MetaField mf, object htmlAttributes) :base(null, "fieldset", htmlAttributes){
            var attr = new  HtmlAttr(htmlAttributes);
            attr["class"] = "ListEditor";
            Attr(attr).Attr("fieldName", name);
            Add(html.TextBox("_" + name, "", htmlAttributes).ToHtmlString());
            Add(null, "a").AddClass("add");
            Add("<br/>");
            Add(html.DropDownList(name, value.ToString().Split('\n').Where(p=> p.Trim()!="").Select(p => new SelectListItem { Text= p }), new { Size = 5 }).ToHtmlString());
            Add("<br/>");
            Add(html.TextArea(name, value.ToString(), htmlAttributes).ToHtmlString());
        }
    }
}
