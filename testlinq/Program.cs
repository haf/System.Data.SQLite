using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NorthwindModel;

namespace testlinq
{
  class Program
  {
    static void Main(string[] args)
    {
      using (SQLiteNorthwind db = new SQLiteNorthwind())
      {
        {
          var query = from c in db.Customers
                      where c.City == "London"
                      orderby c.CompanyName
                      select c;

          foreach (Customers c in query)
          {
            Console.WriteLine(c.CompanyName);
          }
        }

        {
          DateTime dt = new DateTime(1997, 1, 1);
          var query = from order in db.Orders
                      where order.OrderDate < dt
                      select order;

          foreach (Orders o in query)
          {
            Console.WriteLine(o.OrderDate.ToString());
          }
        }

        {
          Categories c = new Categories();
          c.CategoryName = "Test Category";
          c.Description = "My Description";
          db.AddToCategories(c);
          db.SaveChanges();

          Console.WriteLine(c.CategoryID);

          c.Description = "My modified description";
          db.SaveChanges();

          db.DeleteObject(c);
          db.SaveChanges();
        }

        // This query fails due to a SQLite core issue.  Currently pending review by Dr. Hipp
        //{
        //  var query = from p in db.Products
        //              where p.Order_Details.Count(od => od.Orders.Customers.Country == p.Suppliers.Country) > 2
        //              select p;

        //  foreach (Products p in query)
        //  {
        //    Console.WriteLine(p.ProductName);
        //  }
        //}
      }
      Console.ReadKey();
    }
  }
}
