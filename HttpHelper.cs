using System.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Text; 

namespace l.core.web
{
    public class HttpHelper  {
        static public string Execute(string url, string data=null, bool unicode = false, int timeout = 0) {
            //url = "http://192.168.1.123:84/login";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            if (data !=null){
                //byte[] requestBytes = Encoding.Default.GetBytes(data);
                byte[] requestBytes = (unicode? Encoding.UTF8 : Encoding.Default).GetBytes(data);
                request.Method = "post";
                request.ContentType = "application/x-www-form-urlencoded;";
                //request.ContentType = "text/plain";
                //request.ContentType = "text/xml;charset=UTF-8";
                request.ContentLength = requestBytes.Length;
                if (timeout > 0) request.Timeout = timeout;
                Stream requestStream = request.GetRequestStream();
                requestStream.Write(requestBytes, 0, requestBytes.Length);
                requestStream.Close();
            }

            HttpWebResponse response = null;
            try {
                response = (HttpWebResponse)request.GetResponse();
            }
            catch (Exception e) {
                throw new Exception(e.Message + "\n\nurl:" + url);
            }
            StreamReader stream = new StreamReader(response.GetResponseStream(), System.Text.Encoding.UTF8);
            //StreamReader stream = new StreamReader(response.GetResponseStream(), System.Text.Encoding.GetEncoding("GB2312"));
            string responseBody = stream.ReadToEnd();
            stream.Close();
            response.Close();
            return responseBody;
        }

        public static Dictionary<string, string> DExecute(string url) {
            return Execute(url).Split('&').ToDictionary(p=> 
                p.Split('=')[0], p=>p.Split('=')[1]);
        }
    }
}