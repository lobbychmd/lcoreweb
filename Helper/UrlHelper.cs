using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace l.core.web.Helper
{
    public class UrlHelper
    {
        private System.Web.HttpRequestBase request;
        public UrlHelper(System.Web.HttpRequestBase request) {
            this.request = request;
            
        }

        public string SetQueryString(string key, object value) {
            var qs = request.QueryString.AllKeys.Where(p=> p != null).ToDictionary(p=> p, q=>request.QueryString[q]);
            qs[key] = Convert.ToString( value);
            return string.Join("?", new []{
                request.Path.ToString(),
                string.Join("&", qs.Select(p=> p.Key + "=" + p.Value))});
        }

        public string BackUrl(string defaultUrl) {
            return (request.UrlReferrer == null || 
                string.Join("/", request.UrlReferrer.AbsolutePath.Split('/').Take(2)).Equals(string.Join("/", request.Url.AbsolutePath.Split('/').Take(2))))
                ? defaultUrl: 
                request.UrlReferrer.ToString();
        }
    }
}
