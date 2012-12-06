/********************************************************
 * ADO.NET 2.0 Data Provider for SQLite Version 3.X
 * Written by Robert Simpson (robert@blackcastlesoft.com)
 * 
 * Released to the public domain, use at your own risk!
 ********************************************************/

using System;
using System.Diagnostics;
using System.Linq;
using System.Data.Objects;
using System.Text;
using System.Transactions;

namespace testlinq
{
  class Program
  {
      private static int Main(string[] args)
      {
          if (Environment.GetEnvironmentVariable("BREAK") != null)
          {
              Console.WriteLine(
                  "Attach a debugger to process {0} and press any key to continue.",
                  Process.GetCurrentProcess().Id);

              try
              {
                  Console.ReadKey(true); /* throw */
              }
              catch (InvalidOperationException) // Console.ReadKey
              {
                  // do nothing.
              }

              Debugger.Break();
          }

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
              case "endswith":
                  {
                      string value = null;

                      if (args.Length > 1)
                      {
                          value = args[1];

                          if (value != null)
                              value = value.Trim();
                      }

                      return EndsWithTest(value);
                  }
              case "startswith":
                  {
                      string value = null;

                      if (args.Length > 1)
                      {
                          value = args[1];

                          if (value != null)
                              value = value.Trim();
                      }

                      return StartsWithTest(value);
                  }
              case "eftransaction":
                  {
                      bool value = false;

                      if (args.Length > 1)
                      {
                          if (!bool.TryParse(args[1], out value))
                          {
                              Console.WriteLine(
                                  "cannot parse \"{0}\" as boolean",
                                  args[1]);

                              return 1;
                          }
                      }

                      return EFTransactionTest(value);
                  }
              default:
                  {
                      Console.WriteLine("unknown test \"{0}\"", arg);
                      return 1;
                  }
          }
      }

      //
      // NOTE: Used to test the fix for ticket [8b7d179c3c].
      //
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

      //
      // NOTE: Used to test the fix for ticket [59edc1018b].
      //
      private static int EndsWithTest(string value)
      {
          using (northwindEFEntities db = new northwindEFEntities())
          {
              bool once = false;
              var query = from c in db.Customers
                          where c.City.EndsWith(value)
                          orderby c.CustomerID
                          select c;

              foreach (Customers customers in query)
              {
                  if (once)
                      Console.Write(' ');

                  Console.Write(customers.CustomerID);

                  once = true;
              }
          }

          return 0;
      }

      //
      // NOTE: Used to verify the behavior from ticket [00f86f9739].
      //
      private static int StartsWithTest(string value)
      {
          using (northwindEFEntities db = new northwindEFEntities())
          {
              bool once = false;
              var query = from c in db.Customers
                          where c.City.StartsWith(value)
                          orderby c.CustomerID
                          select c;

              foreach (Customers customers in query)
              {
                  if (once)
                      Console.Write(' ');

                  Console.Write(customers.CustomerID);

                  once = true;
              }
          }

          return 0;
      }

      //
      // NOTE: Used to test the fix for ticket [ccfa69fc32].
      //
      private static int EFTransactionTest(bool add)
      {
          //
          // NOTE: Some of these territories already exist and should cause
          //       an exception to be thrown when we try to INSERT them.
          //
          long[] territoryIds = new long[] {
                 1,    2,    3,    4,    5, // NOTE: Success
                 6,    7,    8,    9,   10, // NOTE: Success
              1576, 1577, 1578, 1579, 1580, // NOTE: Success
              1581, 1730, 1833, 2116, 2139, // NOTE: Fail (1581)
              2140, 2141                    // NOTE: Skipped
          };

          if (add)
          {
              using (northwindEFEntities db = new northwindEFEntities())
              {
                  using (TransactionScope scope = new TransactionScope())
                  {
                      //
                      // NOTE: *REQUIRED* This is required so that the
                      //       Entity Framework is prevented from opening
                      //       multiple connections to the underlying SQLite
                      //       database (i.e. which would result in multiple
                      //       IMMEDIATE transactions, thereby failing [later
                      //       on] with locking errors).
                      //
                      db.Connection.Open();

                      foreach (int id in territoryIds)
                      {
                          Territories territories = new Territories();

                          territories.TerritoryID = id;
                          territories.TerritoryDescription = String.Format(
                              "Test Territory #{0}", id);
                          territories.Regions = db.Regions.First();

                          db.AddObject("Territories", territories);
                      }

                      try
                      {
#if NET_40 || NET_45
                          db.SaveChanges(SaveOptions.None);
#else
                          db.SaveChanges(false);
#endif
                      }
                      catch (Exception e)
                      {
                          Console.WriteLine(e);
                      }
                      finally
                      {
                          scope.Complete();
                          db.AcceptAllChanges();
                      }
                  }
              }
          }
          else
          {
              using (northwindEFEntities db = new northwindEFEntities())
              {
                  bool once = false;
#if NET_40 || NET_45
                  var query = from t in db.Territories
                    where territoryIds.AsQueryable<long>().Contains<long>(t.TerritoryID)
                    orderby t.TerritoryID
                    select t;

                  foreach (Territories territories in query)
                  {
                      if (once)
                          Console.Write(' ');

                      Console.Write(territories.TerritoryID);

                      once = true;
                  }
#else
                  //
                  // HACK: We cannot use the Contains extension method within a
                  //       LINQ query with the .NET Framework 3.5.
                  //
                  var query = from t in db.Territories
                    orderby t.TerritoryID
                    select t;

                  foreach (Territories territories in query)
                  {
                      if (Array.IndexOf(territoryIds, territories.TerritoryID) == -1)
                          continue;

                      if (once)
                          Console.Write(' ');

                      Console.Write(territories.TerritoryID);

                      once = true;
                  }
#endif
              }
          }

          return 0;
      }

    private static int OldTests()
    {
      using (northwindEFEntities db = new northwindEFEntities())
      {
        {
          string entitySQL = "SELECT VALUE o FROM Orders AS o WHERE SQLite.DatePart('yyyy', o.OrderDate) = 1997 ORDER BY o.OrderID;";
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
                      orderby order.OrderID
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
                          .SelectMany(cust => cust.Orders.Where(o => o.Freight > 5))
                          .OrderBy(o => o.Customers.CustomerID);

          foreach (Orders c in query)
          {
            Console.WriteLine(c.Freight);
          }
        }

        {
          var query = from c in db.Customers
                      where c.Orders.Any(o => o.OrderDate.HasValue == true && o.OrderDate.Value.Year == 1997)
                      orderby c.CustomerID
                      select c;

          foreach (Customers c in query)
          {
            Console.WriteLine(c.CompanyName);
          }
        }

        {
          string entitySQL = "SELECT VALUE o FROM Orders AS o WHERE o.Customers.Country <> 'UK' AND o.Customers.Country <> 'Mexico' AND Year(o.OrderDate) = 1997 ORDER BY o.OrderID;";
          ObjectQuery<Orders> query = db.CreateQuery<Orders>(entitySQL);

          foreach (Orders o in query)
          {
            Console.WriteLine(o.ShipPostalCode);
          }
        }

        {
          string entitySQL = "SELECT VALUE o FROM Orders AS o WHERE NewGuid() <> NewGuid() ORDER BY o.OrderID;";
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
                      orderby p.ProductID
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
