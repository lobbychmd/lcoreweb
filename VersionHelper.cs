﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Net;
using l.core;

namespace l.core.web
{
    public class VersionHelper: l.core.IVersionHelper
    {
        public string FitSite { get; set; }
        public string UserNO { get; set; }
        public string Password { get; set; }
        public string projectCode { get; set; }
        public string ProjectName { get; set; }
        public string syncPassword { get; set; }
        public bool Suspend { get; set; }
        public List<string> Action { get; set; }

        public string ProjectCode { get { return Project.Current != null && !string.IsNullOrEmpty(Project.Current.SimulateCode) ? Project.Current.ProjectCode : projectCode; } }
        public string SyncPassword { get { return Project.Current != null && !string.IsNullOrEmpty(Project.Current.SimulateCode) ? Project.Current.SyncPassword : syncPassword; } }

        public   VersionHelper() {
            Action = new List<string>();
        }

        static public void Register(string config = null) {
            string FitSite = string.IsNullOrEmpty(config)? ConfigurationManager.AppSettings["FitSite"] : config;
            if (!string.IsNullOrEmpty(FitSite))
            {
                var ll = FitSite.Split('?');
                Dictionary<string, string> lll = null;
                try{
                    lll = ll[1].Split(';').ToDictionary(p => p.Split('=')[0], q => q.Split('=')[1]);
                }catch{
                    throw new Exception ("FitSite 连接字符串格式错误.");
                }
                l.core.VersionHelper.Helper = new l.core.web.VersionHelper {
                    FitSite = ll[0],
                    UserNO = lll.ContainsKey("UserNO") ? lll["UserNO"] : "",
                    Password = lll.ContainsKey("Password") ? lll["Password"] : "",
                    syncPassword = lll.ContainsKey("SyncPassword") ? lll["SyncPassword"] : "",
                    projectCode = lll.ContainsKey("ProjectCode") ? lll["ProjectCode"] : "",
                    ProjectName = lll.ContainsKey("ProjectName") ? lll["ProjectName"] : "",
                    Action = (ConfigurationManager.AppSettings["FitSiteAction"]??"").Split(' ').ToList()
                };
            }
        }

        private string getData(string url) {
            string r = l.core.web.HttpHelper.Execute(url, null, false, 300);
            if (r.IndexOf("Login failed") > 0) return l.core.web.HttpHelper.Execute(url);
            else return r;
        }

        public string GetStr(string metaType, string keyValues)
        {
            
            string url = string.Format("{0}/Sync/Export/{1}?UserNO={2}&Password={3}&ProjectCode={4}&ProjectName={5}&SyncPassword={6}&{7}",
                        FitSite, metaType, UserNO, Password, ProjectCode, ProjectName, SyncPassword, keyValues);
            string data = getData(url);
            return  data;
        }

        public string GetStr(string metaType, Dictionary<string, string> keyValues)  {
            return GetStr(metaType, keyValues == null ? "" : string.Join("&", keyValues.Select(p => p.Key + "=" + p.Value)));
        }

        public object GetAs<T>(string metaType, Dictionary<string, string> keyValues) {
            //return null;
            string remoteObj = GetStr(metaType, keyValues);
            if (remoteObj.GetType() == typeof(string))
                try { return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(remoteObj); }
                catch (Exception e) {
                    throw new Exception( e.Message + "\n" + remoteObj);
                }
            else return null;
        }

        public void InvokeRec<T>(object obj, string metaType, string[] keyFields, string ParamsValue,  int timeCost) { 
            var t = typeof(T);
            var url = string.Format("{0}/Expim/timeCost/{1}?{2}&time={3}&UserNO={4}&Password={5}&ProjectCode={6}&ProjectName={7}&SyncPassword={8}&ParamValues={9}", FitSite, metaType,
                string.Join("&",keyFields.Select(p=> p + "=" + t.GetProperty(p.ToString()).GetValue(obj, null).ToString())), timeCost,
                UserNO, Password, ProjectCode, ProjectName, SyncPassword, Convert.ToBase64String(System.Text.Encoding.Default.GetBytes(ParamsValue)).Replace("+", "`").Replace("=", "~"));
            Uri HttpSite = new Uri(url);

            // 创建请求对象
            HttpWebRequest wreq = WebRequest.Create(HttpSite) as HttpWebRequest;
            //wreq.Method = "POST";
            // 创建状态对象
            //RequestState rs = new RequestState();
            //rs.Request = wreq;
            IAsyncResult ar = wreq.BeginGetResponse(null, null);//new AsyncCallback(RespCallback), rs);

        }


        public bool CheckNewAs<T>(object obj, string metaType, string[] keyFields, bool updateLocal)  {
            if (Suspend) return true;

            var t = typeof(T);
            dynamic remoteObj = GetAs<T>(metaType, keyFields.ToDictionary(p => p, q => t.GetProperty(q.ToString()).GetValue(obj, null).ToString()));
            
            if (remoteObj != null)  {
                if (metaType == "MetaField") if ((remoteObj != null) && (remoteObj.Context == null)) remoteObj.Context = "";

                var r = remoteObj.Version == (obj as dynamic).Version;
                var r1 = ((obj as dynamic).Version == null || !r) && updateLocal;
                if (r1) remoteObj.Save();
                return !r1;
            }
            else return true;
        }

        public List<string[]> GetList(string MetaType) {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<List<string[]>>(
                        getData(string.Format("{0}/Sync/MetaList/{1}", FitSite, MetaType)));
        }

        public string GetMetaDDL() {
            return HttpHelper.Execute(string.Format("{0}/Sync/metaDDL", FitSite));
        }
        public string GetBizDDL()
        {
            return HttpHelper.Execute(string.Format("{0}/Sync/bizDDL?UserNO={1}&Password={2}&ProjectCode={3}&ProjectName={4}&SyncPassword={5}",
                        FitSite, UserNO, Password, ProjectCode, ProjectName, SyncPassword), null, true);
        }

        public string GetInitDDL()
        {
            return HttpHelper.Execute(string.Format("{0}/Sync/initDDL?UserNO={1}&Password={2}&ProjectCode={3}&ProjectName={4}&SyncPassword={5}",
                        FitSite, UserNO, Password, ProjectCode, ProjectName, SyncPassword), null, true);
        }

        public string GetInitFrmDDL()
        {
            return HttpHelper.Execute(string.Format("{0}/Sync/initFrmDDL?UserNO={1}&Password={2}&ProjectCode={3}&ProjectName={4}&SyncPassword={5}",
                        FitSite, UserNO, Password, ProjectCode, ProjectName, SyncPassword), null, true);
        }
    }
}
