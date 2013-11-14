using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc.Html;
using System.Web.Mvc;
using MongoDB.Bson;
using System.Data;

namespace l.core.web.html
{
    public class EditorsGrid : HtmlTagHelper
    {
        public EditorsGrid(HtmlHelper html, string name, MongoDB.Bson.BsonDocument data, l.core.FieldMetaHelper fieldMeta, object htmlAttributes, 
            string namePrefix, string[] fields = null)
            : base(name, "table",htmlAttributes)  {
            AddClass("EditorsGrid ");
            var fs = fields ?? from i in data select i.Name;           //必须是纯值
            var hidefs = fields == null ? null : (from i in data where !i.Value.IsBsonArray select i.Name ).Except(fs);

            foreach (var i in fs)  {
                var fm = fieldMeta.Get(i);
                var v = data[i].IsBsonNull ? null : data[i];
                Add(null, "tr").Add("null", "td").Attr("fn", i).Text(fm.DisplayLabel).Parent.Add(null, "td").Attr("fn", i).Add(new Editor(html, (namePrefix ?? "") + i, v, fm, null));
            }

            if (hidefs != null){
                var last = Add(null, "tr").Add("null", "td").Attr("cols", "2");
                foreach (var s in hidefs) last.Add(html.Hidden((namePrefix ?? "") + s, data[s].ToString()).ToString());
            }

        }
    }

    public class EditorsPanel : HtmlTagHelper {
        public EditorsPanel(HtmlHelper html, string name, string caption, ITable table, l.core.FieldMetaHelper fieldMeta, object htmlAttributes,
            string namePrefix, string[] fields = null)
            : base(name, "fieldset", htmlAttributes)
        {
            Attr("dataSource", table.TableName);
            AddClass("EditorsPanel");
            Add(null, "legend").Text(caption);
            var fs = fields ?? table.Keys ;           //必须是纯值
            var hidefs = fields == null ? null :table.Keys .Except(fs);

            foreach (var i in fs)
            {
                var fm = fieldMeta.Get(i);
                var v = table[0][i];
                Add(null, "div").Add("null", "label").Text(fm.DisplayLabel).Parent.Add("<br />").Parent.Add(new Editor(html, (namePrefix ?? "") + i, v, fm, null));
            }

            if (hidefs != null)
            {
                foreach (var s in hidefs) Add(html.Hidden((namePrefix ?? "") + s, table[0][s]).ToString());
            }

        }

        static public MvcHtmlString StyleSheet() {
            return new MvcHtmlString( @"
                fieldset.EditorsPanel {width:auto; padding:5px;}
                fieldset.EditorsPanel>div{ display:inline-block}
            ");
        }
    }

    public class QuickRecPanel : HtmlTagHelper
    {
        public QuickRecPanel(HtmlHelper html, string name, string caption, DataTable table, string grid, l.core.FieldMetaHelper fieldMeta, object htmlAttributes,
            string namePrefix, string[] keyFields , string[] displayFields)
            : base(name, "fieldset", htmlAttributes)
        {
            Attr("dataSource", table.TableName).Attr("keyFields", string.Join(";", keyFields)).Attr("grid", grid);
            AddClass("EditorsPanel").AddClass("QuickRecPanel");
            Add(null, "legend").Text(caption);
            var fs = keyFields.Union(displayFields);

            foreach (var i in fs)
            {
                var fm = fieldMeta.Get(i);
                var v = table.Rows[0][i];
                Add(null, "div").Add("null", "label").Text(fm.DisplayLabel).Parent.Add("<br />").Parent.Add(new Editor(html, (namePrefix ?? "") + i, v, fm, null));
            }

        }

    }

}
