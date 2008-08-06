using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Objects;

namespace testlinq
{
  class Program
  {
    static void Main(string[] args)
    {
      using (northwindEntities db = new northwindEntities())
      {
        {
          string entitySQL = "SELECT VALUE o FROM Orders AS o WHERE SQLite.DatePart('yyyy', o.OrderDate) = 1997;";
          ObjectQuery<Orders> query = db.CreateQuery<Orders>(entitySQL);

          foreach (Orders o in query)
          {
            Console.WriteLine(o.ShipPostalCode);
          }
        }

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

        {
          var query = db.Customers.Where(cust => cust.Country == "Denmark")
                          .SelectMany(cust => cust.Orders.Where(o => o.Freight > 5));

          foreach (Orders c in query)
          {
            Console.WriteLine(c.Freight);
          }
        }

        {
          var query = from c in db.Customers
                      where c.Orders.Any(o => o.OrderDate.HasValue == true && o.OrderDate.Value.Year == 1997)
                      select c;

          foreach (Customers c in query)
          {
            Console.WriteLine(c.CompanyName);
          }
        }

        {
          string entitySQL = "SELECT VALUE o FROM Orders AS o WHERE o.Customers.Country <> 'UK' AND o.Customers.Country <> 'Mexico' AND Year(o.OrderDate) = 1997;";
          ObjectQuery<Orders> query = db.CreateQuery<Orders>(entitySQL);

          foreach (Orders o in query)
          {
            Console.WriteLine(o.ShipPostalCode);
          }
        }

        {
          string entitySQL = "SELECT VALUE o FROM Orders AS o WHERE NewGuid() <> NewGuid();";
          ObjectQuery<Orders> query = db.CreateQuery<Orders>(entitySQL);

          foreach (Orders o in query)
          {
            Console.WriteLine(o.ShipPostalCode);
          }
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
