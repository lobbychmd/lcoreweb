using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using System.Web.Mvc;

namespace l.core.web
{
    public class ObjectIdModelBinder : DefaultModelBinder
    {
        public override object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            //string value = controllerContext.RequestContext.HttpContext.Request.Form[bindingContext.ModelName] as string;
            var value = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            if ((value == null) || (String.IsNullOrEmpty(value.ToString())) || ((value.RawValue as string[])[0] == ""))  {
                return null;//return ObjectId.Empty; //不要返回 ObjectId.Empty， 用null 区分 00000000000000000
            }
            return ObjectId.Parse((string)value.ConvertTo(typeof(string)));
        }
    }
    
    public class DBRefModelBinder : DefaultModelBinder
    {
        public override object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            //string value = controllerContext.RequestContext.HttpContext.Request.Form[bindingContext.ModelName] as string;
            var value = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            if ((value == null) || (String.IsNullOrEmpty(value.ToString())))
            {
                return null;
            }
            else
            {
                var refv = ((string)value.ConvertTo(typeof(string))).Split('-');
                return new MongoDBRef(refv[0], ObjectId.Parse(refv[1]));
            }
        }
    }

    //暂时用不上
    public class BizParamsBinder : DefaultModelBinder
    {
        public override object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            //string value = controllerContext.RequestContext.HttpContext.Request.Form[bindingContext.ModelName] as string;
            var value = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            if ((value == null) || (String.IsNullOrEmpty(value.ToString())))
            {
                return null;
            }
            else {
                var refv = ((string)value.ConvertTo(typeof(string))).Split('-');
                return new MongoDBRef(refv[0], ObjectId.Parse(refv[1]));
            }
        }
    }
    
}