/********************************************************
 * ADO.NET 2.0 Data Provider for SQLite Version 3.X
 * Written by Robert Simpson (robert@blackcastlesoft.com)
 * 
 * Released to the public domain, use at your own risk!
 ********************************************************/

#define SQLITE_API __declspec(dllexport)

#include "../core/sqlite3.c"

#if defined(INTEROP_EXTENSION_FUNCTIONS)
#include "../contrib/extension-functions.c"
extern int RegisterExtensionFunctions(sqlite3 *db);
#endif

#ifdef SQLITE_OS_WIN
#if defined(INTEROP_CODEC)
#include "crypt.c"
#endif

#define INTEROP_DEBUG_NONE           (0x0000)
#define INTEROP_DEBUG_CLOSE          (0x0001)
#define INTEROP_DEBUG_FINALIZE       (0x0002)
#define INTEROP_DEBUG_BACKUP_FINISH  (0x0004)
#define INTEROP_DEBUG_OPEN           (0x0008)
#define INTEROP_DEBUG_OPEN16         (0x0010)
#define INTEROP_DEBUG_PREPARE        (0x0020)
#define INTEROP_DEBUG_PREPARE16      (0x0040)
#define INTEROP_DEBUG_RESET          (0x0080)
#define INTEROP_DEBUG_CHANGES        (0x0100)
#define INTEROP_DEBUG_BREAK          (0x0200)

#if defined(_MSC_VER) && defined(INTEROP_DEBUG) && \
    (INTEROP_DEBUG & INTEROP_DEBUG_BREAK)
#define sqlite3InteropBreak(a) { sqlite3InteropDebug("%s\n", (a)); __debugbreak(); }
#else
#define sqlite3InteropBreak(a)
#endif

typedef void (*SQLITEUSERFUNC)(sqlite3_context *, int, sqlite3_value **);
typedef void (*SQLITEFUNCFINAL)(sqlite3_context *);

#if defined(INTEROP_DEBUG) || defined(INTEROP_LOG)
SQLITE_PRIVATE void sqlite3InteropDebug(const char *zFormat, ...){
  va_list ap;                         /* Vararg list */
  StrAccum acc;                       /* String accumulator */
  char zMsg[SQLITE_PRINT_BUF_SIZE*3]; /* Complete log message */
  va_start(ap, zFormat);
  sqlite3StrAccumInit(&acc, zMsg, sizeof(zMsg), 0);
  acc.useMalloc = 0;
  sqlite3VXPrintf(&acc, 0, zFormat, ap);
  va_end(ap);
#if SQLITE_VERSION_NUMBER >= 3007013
  sqlite3_win32_write_debug(sqlite3StrAccumFinish(&acc), -1);
#elif defined(SQLITE_WIN32_HAS_ANSI)
  OutputDebugStringA(sqlite3StrAccumFinish(&acc));
#elif defined(SQLITE_WIN32_HAS_WIDE)
  {
    LPWSTR zWideMsg = utf8ToUnicode(sqlite3StrAccumFinish(&acc));
    if( zWideMsg ){
      OutputDebugStringW(zWideMsg);
      sqlite3_free(zWideMsg);
    }
  }
#else
  fprintf(stderr, "%s", sqlite3StrAccumFinish(&acc));
#endif
}
#endif

#if defined(INTEROP_LOG)
SQLITE_PRIVATE int logConfigured = 0;

SQLITE_PRIVATE void sqlite3InteropLogCallback(void *pArg, int iCode, const char *zMsg){
  sqlite3InteropDebug("INTEROP_LOG (%d) %s\n", iCode, zMsg);
}
#endif

#if SQLITE_VERSION_NUMBER < 3007014
SQLITE_PRIVATE void * sqlite3DbMallocZero_interop(sqlite3 *db, int n)
{
  void *p;
  if (db) {
    sqlite3_mutex_enter(db->mutex);
  }
  p = sqlite3DbMallocZero(db,n);
  if (db) {
    sqlite3_mutex_leave(db->mutex);
  }
  return p;
}

SQLITE_PRIVATE void sqlite3DbFree_interop(sqlite3 *db, void *p)
{
  if (db) {
    sqlite3_mutex_enter(db->mutex);
  }
  if (p) {
    sqlite3MemdebugSetType(p, MEMTYPE_DB|MEMTYPE_HEAP);
  }
  sqlite3DbFree(db,p);
  if (db) {
    sqlite3_mutex_leave(db->mutex);
  }
}
#endif

/*
    The goal of this version of close is different than that of sqlite3_close(), and is designed to lend itself better to .NET's non-deterministic finalizers and
    the GC thread.  SQLite will not close a database if statements are open on it -- but for our purposes, we'd rather finalize all active statements
    and forcibly close the database.  The reason is simple -- a lot of people don't Dispose() of their objects correctly and let the garbage collector
    do it.  This leads to unexpected behavior when a user thinks they've closed a database, but it's still open because not all the statements have
    hit the GC yet.

    So, here we have a problem ... .NET has a pointer to any number of sqlite3_stmt objects.  We can't call sqlite3_finalize() on these because
    their memory is freed and can be used for something else.  The GC thread could potentially try and call finalize again on the statement after
    that memory was deallocated.  BAD.  So, what we need to do is make a copy of each statement, and call finalize() on the copy -- so that the original
    statement's memory is preserved, and marked as BAD, but we can still manage to finalize everything and forcibly close the database.  Later when the 
    GC gets around to calling finalize_interop() on the "bad" statement, we detect that and finish deallocating the pointer.
*/
SQLITE_API int WINAPI sqlite3_close_interop(sqlite3 *db)
{
  int ret;
#if SQLITE_VERSION_NUMBER >= 3007014

#if defined(INTEROP_DEBUG) && (INTEROP_DEBUG & INTEROP_DEBUG_CLOSE)
  sqlite3InteropDebug("sqlite3_close_interop(): calling sqlite3_close_v2(%p)...\n", db);
#endif

  ret = sqlite3_close_v2(db);

#if defined(INTEROP_DEBUG) && (INTEROP_DEBUG & INTEROP_DEBUG_CLOSE)
  sqlite3InteropDebug("sqlite3_close_interop(): sqlite3_close_v2(%p) returned %d.\n", db, ret);
#endif

  return ret;
#else
  ret = sqlite3_close(db);

  if (ret == SQLITE_BUSY)
  {
    sqlite3_mutex_enter(db->mutex);

    if (!db->pVdbe)
    {
      sqlite3_mutex_leave(db->mutex);
      return ret;
    }

    while (db->pVdbe)
    {
      /* Make a copy of the first prepared statement */
      Vdbe *p = (Vdbe *)sqlite3DbMallocZero_interop(db, sizeof(Vdbe));
      Vdbe *po = db->pVdbe;

      if (!p) 
      {
        ret = SQLITE_NOMEM;
        break;
      }

      CopyMemory(p, po, sizeof(Vdbe));

      /* Put it on the chain so we can free it */
      db->pVdbe = p;
      ret = sqlite3_finalize((sqlite3_stmt *)p); /* This will also free the copy's memory */
      if (ret)
      {
        /* finalize failed -- so we must put back anything we munged */
        CopyMemory(po, p, sizeof(Vdbe));
        db->pVdbe = po;

        /*
        ** NOTE: Ok, we must free this block that *we* allocated (above) since
        **       finalize did not do so.
        */
        sqlite3DbFree_interop(db, p);
        break;
      }
      else
      {
        ZeroMemory(po, sizeof(Vdbe));
        po->magic = VDBE_MAGIC_DEAD;
      }
    }
    sqlite3_mutex_leave(db->mutex);
    ret = sqlite3_close(db);
  }
  return ret;
#endif
}

SQLITE_API int WINAPI sqlite3_open_interop(const char *filename, int flags, sqlite3 **ppdb)
{
  int ret;

#if defined(INTEROP_LOG)
  if( !logConfigured ){
    ret = sqlite3_config(SQLITE_CONFIG_LOG, sqlite3InteropLogCallback, 0);
    if( ret==SQLITE_OK ){
      logConfigured = 1;
    }else{
      sqlite3InteropDebug("sqlite3_open_interop(): sqlite3_config(SQLITE_CONFIG_LOG) returned %d.\n", ret);
    }
  }
#endif

#if defined(INTEROP_DEBUG) && (INTEROP_DEBUG & INTEROP_DEBUG_OPEN)
  sqlite3InteropDebug("sqlite3_open_interop(): calling sqlite3_open_v2(\"%s\", %d, %p)...\n", filename, flags, ppdb);
#endif

  ret = sqlite3_open_v2(filename, ppdb, flags, NULL);

#if defined(INTEROP_DEBUG) && (INTEROP_DEBUG & INTEROP_DEBUG_OPEN)
  sqlite3InteropDebug("sqlite3_open_interop(): sqlite3_open_v2(\"%s\", %d, %p) returned %d.\n", filename, flags, ppdb, ret);
#endif

#if defined(INTEROP_EXTENSION_FUNCTIONS)
  if (ret == SQLITE_OK)
    RegisterExtensionFunctions(*ppdb);
#endif

  return ret;
}

SQLITE_API int WINAPI sqlite3_open16_interop(const char *filename, int flags, sqlite3 **ppdb)
{
  int ret;

#if defined(INTEROP_LOG)
  if( !logConfigured ){
    ret = sqlite3_config(SQLITE_CONFIG_LOG, sqlite3InteropLogCallback, 0);
    if( ret==SQLITE_OK ){
      logConfigured = 1;
    }else{
      sqlite3InteropDebug("sqlite3_open_interop(): sqlite3_config(SQLITE_CONFIG_LOG) returned %d.\n", ret);
    }
  }
#endif

#if defined(INTEROP_DEBUG) && (INTEROP_DEBUG & INTEROP_DEBUG_OPEN16)
  sqlite3InteropDebug("sqlite3_open16_interop(): calling sqlite3_open_interop(\"%s\", %d, %p)...\n", filename, flags, ppdb);
#endif

  ret = sqlite3_open_interop(filename, flags, ppdb);

#if defined(INTEROP_DEBUG) && (INTEROP_DEBUG & INTEROP_DEBUG_OPEN16)
  sqlite3InteropDebug("sqlite3_open16_interop(): sqlite3_open_interop(\"%s\", %d, %p) returned %d.\n", filename, flags, ppdb, ret);
#endif

  if ((ret == SQLITE_OK) && !DbHasProperty(*ppdb, 0, DB_SchemaLoaded))
    ENC(*ppdb) = SQLITE_UTF16NATIVE;

  return ret;
}

SQLITE_API const char * WINAPI sqlite3_errmsg_interop(sqlite3 *db, int *plen)
{
  const char *pval = sqlite3_errmsg(db);
  *plen = (pval != 0) ? strlen(pval) : 0;
  return pval;
}

SQLITE_API int WINAPI sqlite3_changes_interop(sqlite3 *db)
{
  int result;

#if defined(INTEROP_DEBUG) && (INTEROP_DEBUG & INTEROP_DEBUG_CHANGES)
  sqlite3InteropDebug("sqlite3_changes_interop(): calling sqlite3_changes(%p)...\n", db);
#endif

#ifndef NDEBUG
  if (!db)
      sqlite3InteropBreak("null database handle for sqlite3_changes()");
#endif

  result = sqlite3_changes(db);

#if defined(INTEROP_DEBUG) && (INTEROP_DEBUG & INTEROP_DEBUG_CHANGES)
  sqlite3InteropDebug("sqlite3_changes_interop(): sqlite3_changes(%p) returned %d.\n", db, result);
#endif

  return result;
}

SQLITE_API int WINAPI sqlite3_prepare_interop(sqlite3 *db, const char *sql, int nbytes, sqlite3_stmt **ppstmt, const char **pztail, int *plen)
{
  int n;

#if defined(INTEROP_DEBUG) && (INTEROP_DEBUG & INTEROP_DEBUG_PREPARE)
  sqlite3InteropDebug("sqlite3_prepare_interop(): calling sqlite3_prepare(%p, \"%s\", %d, %p)...\n", db, sql, nbytes, ppstmt);
#endif

  n = sqlite3_prepare(db, sql, nbytes, ppstmt, pztail);

#if defined(INTEROP_DEBUG) && (INTEROP_DEBUG & INTEROP_DEBUG_PREPARE)
  sqlite3InteropDebug("sqlite3_prepare_interop(): sqlite3_prepare(%p, \"%s\", %d, %p) returned %d.\n", db, sql, nbytes, ppstmt, n);
#endif

  *plen = (*pztail != 0) ? strlen(*pztail) : 0;

  return n;
}

SQLITE_API int WINAPI sqlite3_prepare16_interop(sqlite3 *db, const void *sql, int nchars, sqlite3_stmt **ppstmt, const void **pztail, int *plen)
{
  int n;

#if defined(INTEROP_DEBUG) && (INTEROP_DEBUG & INTEROP_DEBUG_PREPARE16)
  sqlite3InteropDebug("sqlite3_prepare_interop(): calling sqlite3_prepare16(%p, \"%s\", %d, %p)...\n", db, sql, nchars, ppstmt);
#endif

  n = sqlite3_prepare16(db, sql, nchars * sizeof(wchar_t), ppstmt, pztail);

#if defined(INTEROP_DEBUG) && (INTEROP_DEBUG & INTEROP_DEBUG_PREPARE16)
  sqlite3InteropDebug("sqlite3_prepare_interop(): sqlite3_prepare16(%p, \"%s\", %d, %p) returned %d.\n", db, sql, nchars, ppstmt, n);
#endif

  *plen = (*pztail != 0) ? wcslen((wchar_t *)*pztail) * sizeof(wchar_t) : 0;

  return n;
}

SQLITE_API int WINAPI sqlite3_bind_double_interop(sqlite3_stmt *stmt, int iCol, double *val)
{
	return sqlite3_bind_double(stmt,iCol,*val);
}

SQLITE_API int WINAPI sqlite3_bind_int64_interop(sqlite3_stmt *stmt, int iCol, sqlite_int64 *val)
{
	return sqlite3_bind_int64(stmt,iCol,*val);
}

SQLITE_API const char * WINAPI sqlite3_bind_parameter_name_interop(sqlite3_stmt *stmt, int iCol, int *plen)
{
  const char *pval = sqlite3_bind_parameter_name(stmt, iCol);
  *plen = (pval != 0) ? strlen(pval) : 0;
  return pval;
}

SQLITE_API const char * WINAPI sqlite3_column_name_interop(sqlite3_stmt *stmt, int iCol, int *plen)
{
  const char *pval = sqlite3_column_name(stmt, iCol);
  *plen = (pval != 0) ? strlen(pval) : 0;
  return pval;
}

SQLITE_API const void * WINAPI sqlite3_column_name16_interop(sqlite3_stmt *stmt, int iCol, int *plen)
{
  const void *pval = sqlite3_column_name16(stmt, iCol);
  *plen = (pval != 0) ? wcslen((wchar_t *)pval) * sizeof(wchar_t) : 0;
  return pval;
}

SQLITE_API const char * WINAPI sqlite3_column_decltype_interop(sqlite3_stmt *stmt, int iCol, int *plen)
{
  const char *pval = sqlite3_column_decltype(stmt, iCol);
  *plen = (pval != 0) ? strlen(pval) : 0;
  return pval;
}

SQLITE_API const void * WINAPI sqlite3_column_decltype16_interop(sqlite3_stmt *stmt, int iCol, int *plen)
{
  const void *pval = sqlite3_column_decltype16(stmt, iCol);
  *plen = (pval != 0) ? wcslen((wchar_t *)pval) * sizeof(wchar_t) : 0;
  return pval;
}

SQLITE_API void WINAPI sqlite3_column_double_interop(sqlite3_stmt *stmt, int iCol, double *val)
{
	*val = sqlite3_column_double(stmt,iCol);
}

SQLITE_API void WINAPI sqlite3_column_int64_interop(sqlite3_stmt *stmt, int iCol, sqlite_int64 *val)
{
	*val = sqlite3_column_int64(stmt,iCol);
}

SQLITE_API void WINAPI sqlite3_last_insert_rowid_interop(sqlite3 *db, sqlite_int64 *rowId)
{
	*rowId = sqlite3_last_insert_rowid(db);
}

SQLITE_API void WINAPI sqlite3_memory_used_interop(sqlite_int64 *nBytes)
{
	*nBytes = sqlite3_memory_used();
}

SQLITE_API void WINAPI sqlite3_memory_highwater_interop(int resetFlag, sqlite_int64 *nBytes)
{
	*nBytes = sqlite3_memory_highwater(resetFlag);
}

SQLITE_API const unsigned char * WINAPI sqlite3_column_text_interop(sqlite3_stmt *stmt, int iCol, int *plen)
{
  const unsigned char *pval = sqlite3_column_text(stmt, iCol);
  *plen = (pval != 0) ? strlen((char *)pval) : 0;
  return pval;
}

SQLITE_API const void * WINAPI sqlite3_column_text16_interop(sqlite3_stmt *stmt, int iCol, int *plen)
{
  const void *pval = sqlite3_column_text16(stmt, iCol);
  *plen = (pval != 0) ? wcslen((wchar_t *)pval) * sizeof(wchar_t): 0;
  return pval;
}

SQLITE_API int WINAPI sqlite3_finalize_interop(sqlite3_stmt *stmt)
{
  int ret;
#if SQLITE_VERSION_NUMBER >= 3007014

#if defined(INTEROP_DEBUG) && (INTEROP_DEBUG & INTEROP_DEBUG_FINALIZE)
  Vdbe *p = (Vdbe *)stmt;
  sqlite3 *db = p->db;
  sqlite3InteropDebug("sqlite3_finalize_interop(): calling sqlite3_finalize(%p, %p)...\n", db, stmt);
#endif

  ret = sqlite3_finalize(stmt);

#if defined(INTEROP_DEBUG) && (INTEROP_DEBUG & INTEROP_DEBUG_FINALIZE)
  sqlite3InteropDebug("sqlite3_finalize_interop(): sqlite3_finalize(%p, %p) returned %d.\n", db, stmt, ret);
#endif

  return ret;
#else
  Vdbe *p;
  int ret = SQLITE_OK;

  p = (Vdbe *)stmt;
  if (p)
  {
    sqlite3 *db = p->db;

    if (db != NULL)
      sqlite3_mutex_enter(db->mutex);

    if ((p->magic == VDBE_MAGIC_DEAD) && (db == NULL))
    {
      sqlite3DbFree_interop(db, p);
    }
    else
    {
      ret = sqlite3_finalize(stmt);
    }

    if (db != NULL)
      sqlite3_mutex_leave(db->mutex);
  }

  return ret;
#endif
}

SQLITE_API int WINAPI sqlite3_backup_finish_interop(sqlite3_backup *p)
{
  int ret;

#if defined(INTEROP_DEBUG) && (INTEROP_DEBUG & INTEROP_DEBUG_BACKUP_FINISH)
  sqlite3* pDestDb = p->pDestDb;
  sqlite3* pSrcDb = p->pSrcDb;
  sqlite3InteropDebug("sqlite3_backup_finish_interop(): calling sqlite3_backup_finish(%p, %p, %p)...\n", pDestDb, pSrcDb, p);
#endif

  ret = sqlite3_backup_finish(p);

#if defined(INTEROP_DEBUG) && (INTEROP_DEBUG & INTEROP_DEBUG_BACKUP_FINISH)
  sqlite3InteropDebug("sqlite3_backup_finish_interop(): sqlite3_backup_finish(%p, %p, %p) returned %d.\n", pDestDb, pSrcDb, p, ret);
#endif

  return ret;
}

SQLITE_API int WINAPI sqlite3_reset_interop(sqlite3_stmt *stmt)
{
  int ret;
#if SQLITE_VERSION_NUMBER >= 3007014

#if defined(INTEROP_DEBUG) && (INTEROP_DEBUG & INTEROP_DEBUG_RESET)
  sqlite3InteropDebug("sqlite3_reset_interop(): calling sqlite3_reset(%p)...\n", stmt);
#endif

  ret = sqlite3_reset(stmt);

#if defined(INTEROP_DEBUG) && (INTEROP_DEBUG & INTEROP_DEBUG_RESET)
  sqlite3InteropDebug("sqlite3_reset_interop(): sqlite3_reset(%p) returned %d.\n", stmt, ret);
#endif

  return ret;
#else
  if (((Vdbe *)stmt)->magic == VDBE_MAGIC_DEAD) return SQLITE_SCHEMA;
  ret = sqlite3_reset(stmt);
  return ret;
#endif
}

SQLITE_API int WINAPI sqlite3_create_function_interop(sqlite3 *psql, const char *zFunctionName, int nArg, int eTextRep, void *pvUser, SQLITEUSERFUNC func, SQLITEUSERFUNC funcstep, SQLITEFUNCFINAL funcfinal, int needCollSeq)
{
  int n;

  if (eTextRep == SQLITE_UTF16)
    eTextRep = SQLITE_UTF16NATIVE;

  n = sqlite3_create_function(psql, zFunctionName, nArg, eTextRep, 0, func, funcstep, funcfinal);
  if (n == SQLITE_OK)
  {
    if (needCollSeq)
    {
      FuncDef *pFunc = sqlite3FindFunction(psql, zFunctionName, strlen(zFunctionName), nArg, eTextRep, 0);
      if( pFunc )
      {
        pFunc->flags |= SQLITE_FUNC_NEEDCOLL;
      }
    }
  }

  return n;
}

SQLITE_API void WINAPI sqlite3_value_double_interop(sqlite3_value *pval, double *val)
{
  *val = sqlite3_value_double(pval);
}

SQLITE_API void WINAPI sqlite3_value_int64_interop(sqlite3_value *pval, sqlite_int64 *val)
{
  *val = sqlite3_value_int64(pval);
}

SQLITE_API const unsigned char * WINAPI sqlite3_value_text_interop(sqlite3_value *val, int *plen)
{
  const unsigned char *pval = sqlite3_value_text(val);
  *plen = (pval != 0) ? strlen((char *)pval) : 0;
  return pval;
}

SQLITE_API const void * WINAPI sqlite3_value_text16_interop(sqlite3_value *val, int *plen)
{
  const void *pval = sqlite3_value_text16(val);
  *plen = (pval != 0) ? wcslen((wchar_t *)pval) * sizeof(wchar_t) : 0;
  return pval;
}

SQLITE_API void WINAPI sqlite3_result_double_interop(sqlite3_context *pctx, double *val)
{
  sqlite3_result_double(pctx, *val);
}

SQLITE_API void WINAPI sqlite3_result_int64_interop(sqlite3_context *pctx, sqlite_int64 *val)
{
  sqlite3_result_int64(pctx, *val);
}

SQLITE_API int WINAPI sqlite3_context_collcompare_interop(sqlite3_context *ctx, const void *p1, int p1len, const void *p2, int p2len)
{
  if ((ctx->pFunc->flags & SQLITE_FUNC_NEEDCOLL) == 0) return 2;
  return ctx->pColl->xCmp(ctx->pColl->pUser, p1len, p1, p2len, p2);
}

SQLITE_API const char * WINAPI sqlite3_context_collseq_interop(sqlite3_context *ctx, int *ptype, int *enc, int *plen)
{
  CollSeq *pColl = ctx->pColl;
  *ptype = 0;
  *plen = 0;
  *enc = 0;

  if ((ctx->pFunc->flags & SQLITE_FUNC_NEEDCOLL) == 0) return NULL;

  if (pColl)
  {
    *enc = pColl->enc;
#if SQLITE_VERSION_NUMBER < 3007010
    *ptype = pColl->type;
#endif
    *plen = (pColl->zName != 0) ? strlen(pColl->zName) : 0;

    return pColl->zName;
  }
  return NULL;
}

SQLITE_API const char * WINAPI sqlite3_column_database_name_interop(sqlite3_stmt *stmt, int iCol, int *plen)
{
  const char *pval = sqlite3_column_database_name(stmt, iCol);
  *plen = (pval != 0) ? strlen(pval) : 0;
  return pval;
}

SQLITE_API const void * WINAPI sqlite3_column_database_name16_interop(sqlite3_stmt *stmt, int iCol, int *plen)
{
  const void *pval = sqlite3_column_database_name16(stmt, iCol);
  *plen = (pval != 0) ? wcslen((wchar_t *)pval) * sizeof(wchar_t) : 0;
  return pval;
}

SQLITE_API const char * WINAPI sqlite3_column_table_name_interop(sqlite3_stmt *stmt, int iCol, int *plen)
{
  const char *pval = sqlite3_column_table_name(stmt, iCol);
  *plen = (pval != 0) ? strlen(pval) : 0;
  return pval;
}

SQLITE_API const void * WINAPI sqlite3_column_table_name16_interop(sqlite3_stmt *stmt, int iCol, int *plen)
{
  const void *pval = sqlite3_column_table_name16(stmt, iCol);
  *plen = (pval != 0) ? wcslen((wchar_t *)pval) * sizeof(wchar_t) : 0;
  return pval;
}

SQLITE_API const char * WINAPI sqlite3_column_origin_name_interop(sqlite3_stmt *stmt, int iCol, int *plen)
{
  const char *pval = sqlite3_column_origin_name(stmt, iCol);
  *plen = (pval != 0) ? strlen(pval) : 0;
  return pval;
}

SQLITE_API const void * WINAPI sqlite3_column_origin_name16_interop(sqlite3_stmt *stmt, int iCol, int *plen)
{
  const void *pval = sqlite3_column_origin_name16(stmt, iCol);
  *plen = (pval != 0) ? wcslen((wchar_t *)pval) * sizeof(wchar_t) : 0;
  return pval;
}

SQLITE_API int WINAPI sqlite3_table_column_metadata_interop(sqlite3 *db, const char *zDbName, const char *zTableName, const char *zColumnName, char **pzDataType, char **pzCollSeq, int *pNotNull, int *pPrimaryKey, int *pAutoinc, int *pdtLen, int *pcsLen)
{
  int n;
  
  n = sqlite3_table_column_metadata(db, zDbName, zTableName, zColumnName, pzDataType, pzCollSeq, pNotNull, pPrimaryKey, pAutoinc);
  *pdtLen = (*pzDataType != 0) ? strlen(*pzDataType) : 0;
  *pcsLen = (*pzCollSeq != 0) ? strlen(*pzCollSeq) : 0;

  return n;
}

SQLITE_API int WINAPI sqlite3_index_column_info_interop(sqlite3 *db, const char *zDb, const char *zIndexName, const char *zColumnName, int *sortOrder, int *onError, char **pzColl, int *plen)
{
  Index *pIdx;
  Table *pTab;
  int n;

  sqlite3_mutex_enter(db->mutex);
  sqlite3BtreeEnterAll(db);

  pIdx = sqlite3FindIndex(db, zIndexName, zDb);

  sqlite3BtreeLeaveAll(db);
  sqlite3_mutex_leave(db->mutex);

  if (!pIdx) return SQLITE_ERROR;

  pTab = pIdx->pTable;
  for (n = 0; n < pIdx->nColumn; n++)
  {
    int cnum = pIdx->aiColumn[n];
    if (sqlite3StrICmp(pTab->aCol[cnum].zName, zColumnName) == 0)
    {
      *sortOrder = pIdx->aSortOrder[n];
      *pzColl = pIdx->azColl[n];
      *plen = strlen(*pzColl);
      *onError = pIdx->onError;

      return SQLITE_OK;
    }
  }
  return SQLITE_ERROR;
}

SQLITE_API int WINAPI sqlite3_table_cursor_interop(sqlite3_stmt *pstmt, int iDb, Pgno tableRootPage)
{
  Vdbe *p = (Vdbe *)pstmt;
  sqlite3 *db = (p == NULL) ? NULL : p->db;
  int n;
  int ret = -1;

  sqlite3_mutex_enter(db->mutex);
  for (n = 0; n < p->nCursor && p->apCsr[n] != NULL; n++)
  {
    if (p->apCsr[n]->isTable == FALSE) continue;
    if (p->apCsr[n]->iDb != iDb) continue;
    if (p->apCsr[n]->pCursor->pgnoRoot == tableRootPage)
    {
      ret = n;
      break;
    }
  }
  sqlite3_mutex_leave(db->mutex);

  return ret;
}

SQLITE_API int WINAPI sqlite3_cursor_rowid_interop(sqlite3_stmt *pstmt, int cursor, sqlite_int64 *prowid)
{
  Vdbe *p = (Vdbe *)pstmt;
  sqlite3 *db = (p == NULL) ? NULL : p->db;
  VdbeCursor *pC;
  int ret = SQLITE_OK;

  sqlite3_mutex_enter(db->mutex);
  while (1)
  {
    if (cursor < 0 || cursor >= p->nCursor)
    {
      ret = SQLITE_ERROR;
      break;
    }
    if (p->apCsr[cursor] == NULL)
    {
      ret = SQLITE_ERROR;
      break;
    }

    pC = p->apCsr[cursor];

    ret = sqlite3VdbeCursorMoveto(pC);
    if(ret)
      break;

    if(pC->rowidIsValid)
    {
      *prowid = pC->lastRowid;
    }
    else if(pC->pseudoTableReg > 0)
    {
      ret = SQLITE_ERROR;
      break;
    }
    else if(pC->nullRow || pC->pCursor==0)
    {
      ret = SQLITE_ERROR;
      break;
    }
    else
    {
      if (pC->pCursor == NULL)
      {
        ret = SQLITE_ERROR;
        break;
      }
      sqlite3BtreeKeySize(pC->pCursor, prowid);
      *prowid = *prowid;
    }
    break;
  }
  sqlite3_mutex_leave(db->mutex);

  return ret;
}
#endif /* SQLITE_OS_WIN */

/*****************************************************************************/

/*
** The INTEROP_TEST_EXTENSION block must be at the end of this source file
** because it includes the "sqlite3ext.h" file, which defines the sqlite3
** public API function names to be macros and that would cause the code
** above this point to malfunction.
*/
#if defined(INTEROP_TEST_EXTENSION)
#include "../core/sqlite3ext.h"
SQLITE_EXTENSION_INIT1

/*
** The interopTest() SQL function returns its first argument or raises an
** error if there are not enough arguments.
*/
SQLITE_PRIVATE void interopTestFunc(
  sqlite3_context *context,
  int argc,
  sqlite3_value **argv
){
  const unsigned char *z;
  if( argc!=1 ){
    sqlite3_result_error(context, "need exactly one argument", -1);
    return;
  }
  z = sqlite3_value_text(argv[0]);
  if( z ){
    sqlite3_result_text(context, (char*)z, -1, SQLITE_STATIC);
  }else{
    sqlite3_result_null(context);
  }
}

/* SQLite invokes this routine once when it loads the extension.
** Create new functions, collating sequences, and virtual table
** modules here.  This is usually the only exported symbol in
** the shared library.
*/
SQLITE_API int interop_test_extension_init(
  sqlite3 *db,
  char **pzErrMsg,
  const sqlite3_api_routines *pApi
){
  SQLITE_EXTENSION_INIT2(pApi)
  return sqlite3_create_function(db, "interopTest", -1, SQLITE_ANY, 0,
      interopTestFunc, 0, 0);
}
#endif
