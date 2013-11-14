using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.Data;

namespace l.core.web.html
{
    public class Paginating1 : IHtml {
        private System.Web.Mvc.Html.HtmlTagHelper tag;
        private ITable dataTable, sumTable;
        public int CountPerPage {get;set;}
        public string ReqPageKey { get; set; }
        public string ReqRecordCountKey { get; set; }

        private class PageNode {
            public int PageNO; public string Text; public bool Href;

            public string HrefStr(string ReqPageKey) { 
                var req = HttpContext.Current.Request;
                var term = ReqPageKey + "=" + (PageNO ).ToString();
                return req.QueryString[ReqPageKey] != null ?
                    req.Url.ToString().Replace(ReqPageKey + "=" + req[ReqPageKey], term) :
                    (req.Url.ToString() + (req.QueryString.Count == 0 ? "?" : "&") + term);
            }
        }
         
        private int[] pageRange(int pageCount, int currPage)   {
            var r = new int[] { 1, pageCount == 0 ? currPage : pageCount };
            r[0] = Math.Max(1, currPage - (CountPerPage/2 -1));
            r[1] = Math.Min(r[0] + (CountPerPage -1), r[1]);
            r[0] = Math.Max(1, r[1] - (CountPerPage - 1));
            return r;
        }

        public Paginating1(HtmlHelper html, string name, ITable dataTable, ITable sumTable, string reqPageKey = "page", int countPerPage = 10){
            this.dataTable = dataTable;
            this.sumTable = sumTable;
            this.CountPerPage = countPerPage;
            this.ReqPageKey = reqPageKey;
            this.ReqRecordCountKey = "recordCount";
        }

        public string Html() {
            if (HttpContext.Current.Request[ReqPageKey] == null) return "";
            else
            {
                tag = new System.Web.Mvc.Html.HtmlTagHelper(null, "div", null).Attr("class", "paginating");
                bool isLastPage = dataTable.Count() < CountPerPage;

                int recordCount = (sumTable != null ? (sumTable.Keys.Contains("RecordCount") && (sumTable.Count() == 1) ? Convert.ToInt32(sumTable[0]["RecordCount"]) : 0) : 0);
                if (recordCount == 0) recordCount = Convert.ToInt32(HttpContext.Current.Request[ReqRecordCountKey] ?? "0");

                int pageCount = (recordCount / CountPerPage) + (((recordCount / CountPerPage) * CountPerPage) == recordCount ? 0 : 1);
                int currPage = HttpContext.Current.Request[ReqPageKey] == null ? 1 : Convert.ToInt32(HttpContext.Current.Request[ReqPageKey]) ;

                //算页码范围
                var r = pageRange(pageCount, currPage);

                //将范围变为节点数组
                List<PageNode> nodes = new List<PageNode>();
                for (int i = r[0]; i <= r[1]; i++) nodes.Add(new PageNode { Text = i.ToString(), Href = i != currPage, PageNO = i });
                //将范围变为节点数组 end;

                //加上首尾页和前后页
                nodes.Insert(0, new PageNode { PageNO = 1, Href = 1 != currPage, Text = "首页" });
                nodes.Insert(1, new PageNode { PageNO = currPage - 1, Href = currPage > 1 , Text = "上一页" });
                nodes.Add(new PageNode { PageNO = currPage+1 , Href = !isLastPage, Text = "下一页" });
                nodes.Add(new PageNode
                {
                    PageNO = recordCount == 0? 0 : (recordCount /CountPerPage + 1),
                    Href = !isLastPage,
                    Text = "尾页" +
                        (isLastPage ? "(共" + currPage.ToString() + "页)" : "")
                });
                //加上首尾页和前后页 end;


                //渲染节点
                foreach (var n in nodes)
                {
                    if (n.Href) tag.Add(null, "a").Attr("href", n.HrefStr(ReqPageKey)).Text(n.Text);
                    else tag.Add(null, "span").Text(n.Text);
                }

                return tag.Html();
            }
        }

        static public string StyleSheet()
        {
            return @"
                .paginating {padding :10px;}
                .paginating a, .Paginate span{text-decoration:none; border:1px #dddddd solid; padding:2px 4px; margin: 0px 2px 0px 2px;}
                ";
        }

    }
}
