/********************************************************
 * ADO.NET 2.0 Data Provider for SQLite Version 3.X
 * Written by Robert Simpson (robert@blackcastlesoft.com)
 * 
 * Released to the public domain, use at your own risk!
 ********************************************************/

namespace System.Data.SQLite
{
  using System;
  using System.Collections.Generic;
  using System.Threading;

  /// <summary>
  /// This class should not be used by applications that make use of WPF
  /// (either directly or indirectly) due to possible deadlocks that can occur
  /// during finalization of some WPF objects.
  /// </summary>
  internal static class SQLiteConnectionPool
  {
    /// <summary>
    /// Keeps track of connections made on a specified file.  The PoolVersion dictates whether old objects get
    /// returned to the pool or discarded when no longer in use.
    /// </summary>
    internal class Pool
    {
      internal readonly Queue<WeakReference> Queue = new Queue<WeakReference>();
      internal int PoolVersion;
      internal int MaxPoolSize;

      internal Pool(int version, int maxSize)
      {
        PoolVersion = version;
        MaxPoolSize = maxSize;
      }
    }

    /// <summary>
    /// The connection pool object
    /// </summary>
    private static SortedList<string, Pool> _connections = new SortedList<string, Pool>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The default version number new pools will get
    /// </summary>
    private static int _poolVersion = 1;

    /// <summary>
    /// The number of connections successfully opened from any pool.
    /// This value is incremented by the Remove method.
    /// </summary>
    private static int _poolOpened = 0;

    /// <summary>
    /// The number of connections successfully closed from any pool.
    /// This value is incremented by the Add method.
    /// </summary>
    private static int _poolClosed = 0;

    /// <summary>
    /// Counts the number of pool entries matching the specified file name.
    /// </summary>
    /// <param name="fileName">The file name to match or null to match all files.</param>
    /// <param name="counts">The pool entry counts for each matching file.</param>
    /// <param name="openCount">The total number of connections successfully opened from any pool.</param>
    /// <param name="closeCount">The total number of connections successfully closed from any pool.</param>
    /// <param name="totalCount">The total number of pool entries for all matching files.</param>
    internal static void GetCounts(
        string fileName,
        ref Dictionary<string, int> counts,
        ref int openCount,
        ref int closeCount,
        ref int totalCount
        )
    {
        lock (_connections)
        {
            openCount = _poolOpened;
            closeCount = _poolClosed;

            if (counts == null)
            {
                counts = new Dictionary<string, int>(
                    StringComparer.OrdinalIgnoreCase);
            }

            if (fileName != null)
            {
                Pool queue;

                if (_connections.TryGetValue(fileName, out queue))
                {
                    Queue<WeakReference> poolQueue = queue.Queue;
                    int count = (poolQueue != null) ? poolQueue.Count : 0;

                    counts.Add(fileName, count);
                    totalCount += count;
                }
            }
            else
            {
                foreach (KeyValuePair<string, Pool> pair in _connections)
                {
                    if (pair.Value == null)
                        continue;

                    Queue<WeakReference> poolQueue = pair.Value.Queue;
                    int count = (poolQueue != null) ? poolQueue.Count : 0;

                    counts.Add(pair.Key, count);
                    totalCount += count;
                }
            }
        }
    }

    /// <summary>
    /// Attempt to pull a pooled connection out of the queue for active duty
    /// </summary>
    /// <param name="fileName">The filename for a desired connection</param>
    /// <param name="maxPoolSize">The maximum size the connection pool for the filename can be</param>
    /// <param name="version">The pool version the returned connection will belong to</param>
    /// <returns>Returns NULL if no connections were available.  Even if none are, the poolversion will still be a valid pool version</returns>
    internal static SQLiteConnectionHandle Remove(string fileName, int maxPoolSize, out int version)
    {
        int localVersion;
        Queue<WeakReference> poolQueue;

        //
        // NOTE: This lock cannot be held while checking the queue for available
        //       connections because other methods of this class are called from
        //       the GC finalizer thread and we use the WaitForPendingFinalizers
        //       method (below).  Holding this lock while calling that method
        //       would therefore result in a deadlock.  Instead, this lock is
        //       held only while a temporary copy of the queue is created, and
        //       if necessary, when committing changes back to that original
        //       queue prior to returning from this method.
        //
        lock (_connections)
        {
            Pool queue;

            // Default to the highest pool version
            version = _poolVersion;

            // If we didn't find a pool for this file, create one even though it will be empty.
            // We have to do this here because otherwise calling ClearPool() on the file will not work for active connections
            // that have never seen the pool yet.
            if (_connections.TryGetValue(fileName, out queue) == false)
            {
                queue = new Pool(_poolVersion, maxPoolSize);
                _connections.Add(fileName, queue);

                return null;
            }

            // We found a pool for this file, so use its version number
            version = localVersion = queue.PoolVersion;
            queue.MaxPoolSize = maxPoolSize;

            ResizePool(queue, false);

            // Try and get a pooled connection from the queue
            poolQueue = queue.Queue;
            if (poolQueue == null) return null;

            //
            // NOTE: Temporarily tranfer the queue for this file into a local
            //       variable.  The queue for this file will be modified and
            //       then committed back to the real pool list (below) prior
            //       to returning from this method.
            //
            _connections.Remove(fileName);
            poolQueue = new Queue<WeakReference>(poolQueue);
        }

        try
        {
            while (poolQueue.Count > 0)
            {
                WeakReference cnn = poolQueue.Dequeue();
                if (cnn == null) continue;

                SQLiteConnectionHandle hdl = cnn.Target as SQLiteConnectionHandle;
                if (hdl == null) continue;

                //
                // BUGFIX: For ticket [996d13cd87], step #1.  After this point,
                //         make sure that the finalizer for the connection
                //         handle just obtained from the queue cannot START
                //         running (i.e. it may still be pending but it will no
                //         longer start after this point).
                //
                GC.SuppressFinalize(hdl);

                try
                {
                    //
                    // BUGFIX: For ticket [996d13cd87], step #2.  Now, we must wait
                    //         for all pending finalizers which have STARTED running
                    //         and have not yet COMPLETED.  This must be done just
                    //         in case the finalizer for the connection handle just
                    //         obtained from the queue has STARTED running at some
                    //         point before SuppressFinalize was called on it.
                    //
                    //         After this point, checking properties of the
                    //         connection handle (e.g. IsClosed) should work
                    //         reliably without having to worry that they will
                    //         (due to the finalizer) change out from under us.
                    //
                    GC.WaitForPendingFinalizers();

                    //
                    // BUGFIX: For ticket [996d13cd87], step #3.  Next, verify that
                    //         the connection handle is actually valid and [still?]
                    //         not closed prior to actually returning it to our
                    //         caller.
                    //
                    if (!hdl.IsInvalid && !hdl.IsClosed)
                    {
                        Interlocked.Increment(ref _poolOpened);
                        return hdl;
                    }
                }
                finally
                {
                    //
                    // BUGFIX: For ticket [996d13cd87], step #4.  Next, we must
                    //         re-register the connection handle for finalization
                    //         now that we have a strong reference to it (i.e. the
                    //         finalizer will not run at least until the connection
                    //         is subsequently closed).
                    //
                    GC.ReRegisterForFinalize(hdl);
                }

                GC.KeepAlive(hdl);
            }
        }
        finally
        {
            //
            // BUGFIX: For ticket [996d13cd87], step #5.  Finally, commit any
            //         changes to the pool/queue for this database file.
            //
            lock (_connections)
            {
                //
                // NOTE: We must check [again] if a pool exists for this file
                //       because one may have been added while the search for
                //       an available connection was in progress (above).
                //
                Pool queue;
                Queue<WeakReference> newPoolQueue;
                bool addPool;

                if (_connections.TryGetValue(fileName, out queue))
                {
                    addPool = false;
                }
                else
                {
                    addPool = true;
                    queue = new Pool(localVersion, maxPoolSize);
                }

                newPoolQueue = queue.Queue;

                while (poolQueue.Count > 0)
                    newPoolQueue.Enqueue(poolQueue.Dequeue());

                ResizePool(queue, false);

                if (addPool)
                    _connections.Add(fileName, queue);
            }
        }

        return null;
    }

    /// <summary>
    /// Clears out all pooled connections and rev's up the default pool version to force all old active objects
    /// not in the pool to get discarded rather than returned to their pools.
    /// </summary>
    internal static void ClearAllPools()
    {
      lock (_connections)
      {
        foreach (KeyValuePair<string, Pool> pair in _connections)
        {
          if (pair.Value == null)
            continue;

          Queue<WeakReference> poolQueue = pair.Value.Queue;

          while (poolQueue.Count > 0)
          {
            WeakReference cnn = poolQueue.Dequeue();
            if (cnn == null) continue;
            SQLiteConnectionHandle hdl = cnn.Target as SQLiteConnectionHandle;
            if (hdl != null)
            {
              hdl.Dispose();
            }
            GC.KeepAlive(hdl);
          }
          
          // Keep track of the highest revision so we can go one higher when we're finished
          if (_poolVersion <= pair.Value.PoolVersion)
            _poolVersion = pair.Value.PoolVersion + 1;
        }
        // All pools are cleared and we have a new highest version number to force all old version active items to get discarded
        // instead of going back to the queue when they are closed.
        // We can get away with this because we've pumped up the _poolVersion out of range of all active connections, so they
        // will all get discarded when they try to put themselves back in their pool.
        _connections.Clear();
      }
    }

    /// <summary>
    /// Clear a given pool for a given filename.  Discards anything in the pool for the given file, and revs the pool
    /// version so current active objects on the old version of the pool will get discarded rather than be returned to the pool.
    /// </summary>
    /// <param name="fileName">The filename of the pool to clear</param>
    internal static void ClearPool(string fileName)
    {
      lock (_connections)
      {
        Pool queue;
        if (_connections.TryGetValue(fileName, out queue) == true)
        {
          queue.PoolVersion++;

          Queue<WeakReference> poolQueue = queue.Queue;
          if (poolQueue == null) return;

          while (poolQueue.Count > 0)
          {
            WeakReference cnn = poolQueue.Dequeue();
            if (cnn == null) continue;
            SQLiteConnectionHandle hdl = cnn.Target as SQLiteConnectionHandle;
            if (hdl != null)
            {
              hdl.Dispose();
            }
            GC.KeepAlive(hdl);
          }
        }
      }
    }

    /// <summary>
    /// Return a connection to the pool for someone else to use.
    /// </summary>
    /// <param name="fileName">The filename of the pool to use</param>
    /// <param name="hdl">The connection handle to pool</param>
    /// <param name="version">The pool version the handle was created under</param>
    /// <remarks>
    /// If the version numbers don't match between the connection and the pool, then the handle is discarded.
    /// </remarks>
    internal static void Add(string fileName, SQLiteConnectionHandle hdl, int version)
    {
      lock (_connections)
      {
        // If the queue doesn't exist in the pool, then it must've been cleared sometime after the connection was created.
        Pool queue;
        if (_connections.TryGetValue(fileName, out queue) == true && version == queue.PoolVersion)
        {
          ResizePool(queue, true);

          Queue<WeakReference> poolQueue = queue.Queue;
          if (poolQueue == null) return;

          poolQueue.Enqueue(new WeakReference(hdl, false));
          Interlocked.Increment(ref _poolClosed);
        }
        else
        {
          hdl.Close();
        }
        GC.KeepAlive(hdl);
      }
    }

    /// <summary>
    /// We don't have to thread-lock anything in this function, because it's only called by other functions above
    /// which already have a thread-safe lock.
    /// </summary>
    /// <param name="queue">The queue to resize</param>
    /// <param name="forAdding">If a function intends to add to the pool, this is true, which forces the resize
    /// to take one more than it needs from the pool</param>
    private static void ResizePool(Pool queue, bool forAdding)
    {
      int target = queue.MaxPoolSize;

      if (forAdding && target > 0) target--;

      Queue<WeakReference> poolQueue = queue.Queue;
      if (poolQueue == null) return;

      while (poolQueue.Count > target)
      {
        WeakReference cnn = poolQueue.Dequeue();
        if (cnn == null) continue;
        SQLiteConnectionHandle hdl = cnn.Target as SQLiteConnectionHandle;
        if (hdl != null)
        {
          hdl.Dispose();
        }
        GC.KeepAlive(hdl);
      }
    }
  }
}
