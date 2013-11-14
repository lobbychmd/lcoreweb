using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace l.core.web
{
    public class MetaHelper : l.core.MetaHelper
    {
        

        static public object DeserializeObject(string metaType, string data)
        {
            if (metaType == "MetaQuery") return Newtonsoft.Json.JsonConvert.DeserializeObject<l.core.Query>(data);
            else if (metaType == "MetaModule") return Newtonsoft.Json.JsonConvert.DeserializeObject<l.core.Module>(data);
            else if (metaType == "MetaBiz") return Newtonsoft.Json.JsonConvert.DeserializeObject<l.core.Biz>(data);
            else if (metaType == "FieldMeta") return Newtonsoft.Json.JsonConvert.DeserializeObject<l.core.FieldMeta>(data);
            else return null;
        }

        static public void UpdateLocal(string metaType, string data, string[] keyValues){
            l.core.MetaTypeInfo info = l.core.MetaHelper.MetaTypeInfos[metaType];
            (DeserializeObject(metaType, data) as dynamic ).Save();
        }

    }
}
