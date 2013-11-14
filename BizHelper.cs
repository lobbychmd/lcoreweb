using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace l.core.web
{
    public class BizHelper
    {
        static public l.core.BizResult ModelState2BizResult(ModelStateDictionary ModelState)
        {
            l.core.BizResult r = new l.core.BizResult();
            //有时候不检查模型,因为是共用模型 
            if (!ModelState.IsValid)
            {
                foreach (var i in ModelState.Where(p => p.Value.Errors.Count > 0))
                    foreach (var j in i.Value.Errors)
                        r.Errors.Add(new ValidationResult(j.ErrorMessage, new[] { i.Key }));
            }


            return r;
        }
    }
}
