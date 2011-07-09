using System;
using System.Linq;
using System.Data.Objects;
using System.Text;

namespace testlinq
{
  class Program
  {
      private static int Main(string[] args)
      {
          string arg = null;

          if ((args != null) && (args.Length > 0))
              arg = args[0];

          if (arg == null)
              arg = "";

          arg = arg.Trim().TrimStart('-', '/').ToLowerInvariant();

          switch (arg)
          {
              case "": // String.Empty
              case "old":
                  {
                      return OldTests();
                  }
              case "skip":
                  {
                      int pageSize = 0;

                      if (args.Length > 1)
                      {
                          arg = args[1];

                          if (arg != null)
                              pageSize = int.Parse(arg.Trim());
                      }

                      return SkipTest(pageSize);
                  }
              default:
                  {
                      Console.WriteLine("unknown test \"{0}\"", arg);
                      return 1;
                  }
          }
      }

      private static int SkipTest(int pageSize)
      {
          using (northwindEFEntities db = new northwindEFEntities())
          {
              bool once = false;
              int count = db.Customers.Count();

              int PageCount = (pageSize != 0) ?
                  (count / pageSize) + ((count % pageSize) == 0 ? 0 : 1) : 1;

              for (int pageIndex = 0; pageIndex < PageCount; pageIndex++)
              {
                  var query = db.Customers.OrderBy(p => p.City).
                      Skip(pageSize * pageIndex).Take(pageSize);

                  foreach (Customers customers in query)
                  {
                      if (once)
                          Console.Write(' ');

                      Console.Write(customers.CustomerID);

                      once = true;
                  }
              }
          }

          return 0;
      }

    private static int OldTests()
    {
      using (northwindEFEntities db = new northwindEFEntities())
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

          int cc = query.Count();

          foreach (Customers c in query)
          {
            Console.WriteLine(c.CompanyName);
          }
        }

        {
          string scity = "London";
          Customers c = db.Customers.FirstOrDefault(cd => cd.City == scity);
          Console.WriteLine(c.CompanyName);
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
          Customers cust = new Customers();
          cust.CustomerID = "MTMTM";
          cust.ContactName = "My Name";
          cust.CompanyName = "SQLite Company";
          cust.Country = "Netherlands";
          cust.City = "Amsterdam";
          cust.Phone = "012345677";
          db.AddToCustomers(cust);
          db.SaveChanges();

          db.DeleteObject(cust);
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

        // This query requires SQLite 3.6.2 to function correctly
        {
          var query = from p in db.Products
                      where p.OrderDetails.Count(od => od.Orders.Customers.Country == p.Suppliers.Country) > 2
                      select p;

          foreach (Products p in query)
          {
            Console.WriteLine(p.ProductName);
          }
        }
      }

      //
      // NOTE: (JJM) Removed on 2011/07/06, makes it harder to run this EXE via
      //       the new unit test suite.
      //
      // Console.ReadKey();

      return 0;
    }
  }
}
