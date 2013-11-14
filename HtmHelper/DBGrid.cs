using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;


namespace l.core.web.html
{
    public interface ISmartDataSet {
        IEnumerable<object> Items();
    }

    public class NewBsonDocument: ISmartDataSet{
        public BsonDocument Doc {get;set;}
        public IEnumerable<object> Items() {
            return Doc;
        }
    }
    static public class MonHelper
    {
        static public ISmartDataSet AsDataSet(this BsonDocument doc)
        {
            return new NewBsonDocument{ Doc = doc};
        }
    }

    public class NewDataSet : ISmartDataSet
    {
        public System.Data.DataSet Doc { get; set; }
        public IEnumerable<object> Items()
        {
            return from System.Data.DataTable dt in Doc.Tables select dt;
        }
    }
    static public class DataSetHelper
    {
        static public ISmartDataSet AsDataSet(this System.Data.DataSet ds)
        {
            return new NewDataSet { Doc = ds };
        }
    }

    static public class DsHelper{
        static public IEnumerable<object> Items(this System.Data.DataSet ds)
        {
            return from System.Data.DataTable dt in ds.Tables select dt;
        }

        static public void test() {
            var mondoc = new BsonDocument();
            UIGrid.Draw(mondoc.AsDataSet());

            var ds = new System.Data.DataSet();
            UIGrid.Draw(ds.AsDataSet());
        }
    }

    public class UIGrid {
        static public string Draw(ISmartDataSet data)
        {
            return string.Join(";", data.Items().Select(p=> p.GetType().Name));
        }

         
    }
}
