using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;

namespace l.core.web
{
    public class UrlHelper
    {
        static public string QueryString(HttpRequestBase request, string key, string value) { 
            
            //这种做法会把 path 重新组合 成逗号分隔的格式 （因为多个key）
            //return request.QueryString.AllKeys.Contains(key)?
            //    string.Join("&", request.QueryString.AllKeys.Select(p=>p + "=" + (p==key? value :request.QueryString[p])))
            //    : request.QueryString.ToString() + "&" + key + "=" + value;
            return request.QueryString.AllKeys.Contains(key) ?
                request.QueryString.ToString().Replace(key + "=" + request[key], key + "=" + value)
                : request.QueryString.ToString() + "&" + key + "=" + value;
        }

        static public string rel(HttpRequestBase request) {
            return request.QueryString["rel"] == null ? null :
                request.Url.ToString().Substring(request.Url.ToString().IndexOf("rel=") + 4);
        }
    }
}
