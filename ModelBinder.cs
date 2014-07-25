using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using System.Text.RegularExpressions;

namespace l.core.web
{
    public class ModelBinder {
        private l.core.ParamsHelper paramsHelper;
        private NameValueCollection values;
        private List<string> paramsName;

        public ModelBinder(string[] paramsName, l.core.ParamsHelper paramsHelper, NameValueCollection values) {
            this.paramsHelper = paramsHelper;
            this.values = values;
            this.paramsName = paramsName.ToList();
        }

        public string GetDic(string dic, bool key) {
            return getDic(dic, key);
        }

        private string getDic(string dic, bool key) {
            var idx = dic.IndexOf('.');
            if (idx < 0) return dic;
            else if (key) return dic.Substring(0, idx);
            else return dic.Substring(idx + 1, dic.Length - idx - 1);
        }

        private string getValue(string key) {
            if (key == "Old_LastUpdateTime")
                return values["LastUpdateTime"];
            else if (values[key + "@type"] == "password"   && values[key] != string.Empty )  {//写了这个旧密码就不加密了，
                var userno = values["UserNO"] ?? paramsHelper.GetParamValue("Operator");
                return l.core.Crypt.Encode(userno +  values[key]);
            }
            else {
                bool dic = (values[key + "@type"] == "dic");
                bool dicvalue = (values[key + "ID"] != null && values[key + "ID@type"] == "dic");

                bool idprefix = ((key.Length > 2) && key.IndexOf("ID") == key.Length - 2);

                if (idprefix || dicvalue)
                    return getDic(idprefix ? values[key] : values[key + "ID"], idprefix);
                else return values[key];
            } 

        }
        //Func<string, object>, string 是参数， object 是返回值

        public void Bind(Func<string, object> predicate) {
            var sysps = new List<string> { "Old_LastUpdateTime", "LastUpdateTime", "Operator", "OperName", "LocalStoreNO" };
            foreach (string s in sysps.Union(values.AllKeys)) {
                var ms = new Regex(@"[\w_]+\[\d+\]\.([\w_]+)$").Match(s);
                if (ms.Success) {
                    if (paramsName.IndexOf(ms.Groups[1].Value) >= 0 
                            && sysps.IndexOf(ms.Groups[1].Value) < 0) //系统参数不能用作重复参数
                        paramsHelper.AddParamValue(ms.Groups[1].Value, predicate(s) ?? getValue(s));
                }
                else if (paramsName.IndexOf(s) >= 0) 
                    paramsHelper.SetParamValue(s, predicate(s) ?? getValue(s));
            }
        }
    }
}
