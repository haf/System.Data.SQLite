/********************************************************
 * ADO.NET 2.0 Data Provider for SQLite Version 3.X
 * Written by Robert Simpson (robert@blackcastlesoft.com)
 * 
 * Released to the public domain, use at your own risk!
 ********************************************************/

namespace System.Data.SQLite
{
  using System;

  /// <summary>
  /// A simple custom attribute to enable us to easily find user-defined functions in
  /// the loaded assemblies and initialize them in SQLite as connections are made.
  /// </summary>
  [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
  public sealed class SQLiteFunctionAttribute : Attribute
  {
    private string       _name;
    private int          _argumentCount;
    private FunctionType _functionType;
    private Type         _instanceType;

    /// <summary>
    /// Default constructor, initializes the internal variables for the function.
    /// </summary>
    public SQLiteFunctionAttribute()
        : this(String.Empty, -1, FunctionType.Scalar)
    {
        // do nothing.
    }

    /// <summary>
    /// Constructs an instance of this class.
    /// </summary>
    /// <param name="name">
    /// The name of the function, as seen by the SQLite core library.
    /// </param>
    /// <param name="argumentCount">
    /// The number of arguments that the function will accept.
    /// </param>
    /// <param name="functionType">
    /// The type of function being declared.  This will either be Scalar,
    /// Aggregate, or Collation.
    /// </param>
    public SQLiteFunctionAttribute(
        string name,
        int argumentCount,
        FunctionType functionType
        )
    {
        _name = name;
        _argumentCount = argumentCount;
        _functionType = functionType;
        _instanceType = null;
    }

    /// <summary>
    /// The function's name as it will be used in SQLite command text.
    /// </summary>
    public string Name
    {
      get { return _name; }
      set { _name = value; }
    }

    /// <summary>
    /// The number of arguments this function expects.  -1 if the number of arguments is variable.
    /// </summary>
    public int Arguments
    {
      get { return _argumentCount; }
      set { _argumentCount = value; }
    }

    /// <summary>
    /// The type of function this implementation will be.
    /// </summary>
    public FunctionType FuncType
    {
      get { return _functionType; }
      set { _functionType = value; }
    }

    /// <summary>
    /// The <see cref="System.Type" /> object instance that describes the class
    /// containing the implementation for the associated function.
    /// </summary>
    internal Type InstanceType
    {
        get { return _instanceType; }
        set { _instanceType = value; }
    }
  }
}
