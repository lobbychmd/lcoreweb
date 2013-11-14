using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.Data;
using MongoDB.Bson;

namespace l.core.web.html.mobile
{
    

    public class QueryGrid : HtmlTagHelper {
        public QueryGrid(HtmlHelper html, string name, System.Data.DataTable table, string[] fields = null, string href = null)
            : base(name, "div", null) {
            Attr("class", "QueryGrid").JQueryPlugIn(html, "QueryGrid");
            if (table.Columns.Count == 1 && table.Columns[0].ColumnName == "error") Attr("SqlError", table.Columns[0].Caption); //用来弹错误框
            var fs = fields ??  from DataColumn c in table.Columns select c.ColumnName;

            foreach (System.Data.DataRow dr in table.Rows){
                var tr = Add(null, "div").Attr("data-role", "collapsible");
                tr.Add(null, "h3").Text(table.Columns[2].Caption + ":" + dr[2].ToString());
                foreach (string s in fs){
                    System.Data.DataColumn dc = table.Columns[s];
                    tr.Add(null, "p").Text(( dc.Caption??dc.ColumnName ) + ":" + ( dc.DataType == typeof(Boolean) ? ( Convert.ToBoolean( dr[dc])?"√":"×") : dr[dc].ToString()));
                }
            }
        }

        static public string StyleSheet() {
            return @"
                table.QueryGrid {border-collapse: collapse;margin: 0em 0;awidth: 100%;}
                table.QueryGrid thead tr{height:30px}
                table.QueryGrid td, table.QueryGrid th { border: 1px solid #EEEEEE;padding: 0.6em 10px;text-align: left;}
                table.QueryGrid a{color:blue;}
                a.newRow{color:#000; text-decoration:none}
                table.QueryGrid thead td {}
                table.QueryGrid tfoot tr{background-color: #eeeeee}
                table.QueryGrid tfoot tr.sum{border-top:2px solid #cccccc;
                    border-bottom:2px solid #cccccc;height:3px;}
                ";
        }
    }
}
