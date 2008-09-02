#ifndef SQLITE_DEBUG
#include "src/sqlite3.c"
#else
#include "splitsource\btreeint.h"
#include "splitsource\vdbeint.h"
#include "splitsource\sqliteInt.h"
#endif

#include "extension-functions.c"
#include "crypt.c"
#include <tchar.h>

#ifdef NDEBUG

#if _WIN32_WCE
#include "merge.h"
#else
#include "merge_full.h"
#endif // _WIN32_WCE
#endif // NDEBUG

extern int RegisterExtensionFunctions(sqlite3 *db);

#ifdef SQLITE_OS_WIN

// Additional open flags, we use this one privately
//#define SQLITE_OPEN_SHAREDCACHE      0x01000000

typedef void (*SQLITEUSERFUNC)(sqlite3_context *, int, sqlite3_value **);
typedef void (*SQLITEFUNCFINAL)(sqlite3_context *);

typedef HANDLE (WINAPI *CREATEFILEW)(
    LPCWSTR,
    DWORD,
    DWORD,
    LPSECURITY_ATTRIBUTES,
    DWORD,
    DWORD,
    HANDLE);

int SetCompression(const wchar_t *pwszFilename, unsigned short ufLevel)
{
#ifdef FSCTL_SET_COMPRESSION
  HMODULE hMod = GetModuleHandle(_T("KERNEL32"));
  CREATEFILEW pfunc;
  HANDLE hFile;
  unsigned long dw = 0;
  int n;

  if (hMod == NULL)
  {
    SetLastError(ERROR_NOT_SUPPORTED);
    return 0;
  }

  pfunc = (CREATEFILEW)GetProcAddress(hMod, _T("CreateFileW"));
  if (pfunc == NULL)
  {
    SetLastError(ERROR_NOT_SUPPORTED);
    return 0;
  }

  hFile = pfunc(pwszFilename, GENERIC_READ|GENERIC_WRITE, 0, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
  if (hFile == NULL)
    return 0;

  n = DeviceIoControl(hFile, FSCTL_SET_COMPRESSION, &ufLevel, sizeof(ufLevel), NULL, 0, &dw, NULL);

  CloseHandle(hFile);

  return n;
#else
  SetLastError(ERROR_NOT_SUPPORTED);
  return 0;
#endif
}

__declspec(dllexport) int WINAPI sqlite3_compressfile(const wchar_t *pwszFilename)
{
  return SetCompression(pwszFilename, COMPRESSION_FORMAT_DEFAULT);
}

__declspec(dllexport) int WINAPI sqlite3_decompressfile(const wchar_t *pwszFilename)
{
  return SetCompression(pwszFilename, COMPRESSION_FORMAT_NONE);
}

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
__declspec(dllexport) int WINAPI sqlite3_close_interop(sqlite3 *db)
{
  int ret;
  
  ret = sqlite3_close(db);

  if (ret == SQLITE_BUSY && db->pVdbe)
  {
    while (db->pVdbe)
    {
      // Make a copy of the first prepared statement
      Vdbe *p = (Vdbe *)sqlite3_malloc(sizeof(Vdbe));
      Vdbe *po = db->pVdbe;

      if (!p) 
      {
        ret = SQLITE_NOMEM;
        break;
      }

      CopyMemory(p, po, sizeof(Vdbe));

      // Put it on the chain so we can free it
      db->pVdbe = p;
      ret = sqlite3_finalize((sqlite3_stmt *)p); // This will also free the copy's memory
      if (ret)
      {
        // finalize failed -- so we must put back anything we munged
        CopyMemory(po, p, sizeof(Vdbe));
        db->pVdbe = po;
        break;
      }
      else
      {
        ZeroMemory(po, sizeof(Vdbe));
        po->magic = VDBE_MAGIC_DEAD;
      }
    }
    ret = sqlite3_close(db);
  }

  return ret;
}

__declspec(dllexport) int WINAPI sqlite3_open_interop(const char*filename, int flags, sqlite3 **ppdb)
{
  int ret;
  //int sharedcache = ((flags & SQLITE_OPEN_SHAREDCACHE) != 0);
  //flags &= ~SQLITE_OPEN_SHAREDCACHE;

  //sqlite3_enable_shared_cache(sharedcache);
  ret = sqlite3_open_v2(filename, ppdb, flags, NULL);
  //sqlite3_enable_shared_cache(0);

  if (ret == 0)
    RegisterExtensionFunctions(*ppdb);

  return ret;
}

__declspec(dllexport) int WINAPI sqlite3_open16_interop(const char *filename, int flags, sqlite3 **ppdb)
{
  int ret = sqlite3_open_interop(filename, flags, ppdb);
  if (!ret)
  {
    if(!DbHasProperty(*ppdb, 0, DB_SchemaLoaded))
      ENC(*ppdb) = SQLITE_UTF16NATIVE;
  }
  return ret;
}

__declspec(dllexport) const char * WINAPI sqlite3_errmsg_interop(sqlite3 *db, int *plen)
{
  const char *pval = sqlite3_errmsg(db);
  *plen = (pval != 0) ? strlen(pval) : 0;
  return pval;
}

__declspec(dllexport) int WINAPI sqlite3_prepare_interop(sqlite3 *db, const char *sql, int nbytes, sqlite3_stmt **ppstmt, const char **pztail, int *plen)
{
  int n;

  n = sqlite3_prepare(db, sql, nbytes, ppstmt, pztail);
  *plen = (*pztail != 0) ? strlen(*pztail) : 0;

  return n;
}

__declspec(dllexport) int WINAPI sqlite3_prepare16_interop(sqlite3 *db, const void *sql, int nchars, sqlite3_stmt **ppstmt, const void **pztail, int *plen)
{
  int n;

  n = sqlite3_prepare16(db, sql, nchars * sizeof(wchar_t), ppstmt, pztail);
  *plen = (*pztail != 0) ? wcslen((wchar_t *)*pztail) * sizeof(wchar_t) : 0;

  return n;
}

__declspec(dllexport) int WINAPI sqlite3_bind_double_interop(sqlite3_stmt *stmt, int iCol, double *val)
{
	return sqlite3_bind_double(stmt,iCol,*val);
}

__declspec(dllexport) int WINAPI sqlite3_bind_int64_interop(sqlite3_stmt *stmt, int iCol, sqlite_int64 *val)
{
	return sqlite3_bind_int64(stmt,iCol,*val);
}

__declspec(dllexport) const char * WINAPI sqlite3_bind_parameter_name_interop(sqlite3_stmt *stmt, int iCol, int *plen)
{
  const char *pval = sqlite3_bind_parameter_name(stmt, iCol);
  *plen = (pval != 0) ? strlen(pval) : 0;
  return pval;
}

__declspec(dllexport) const char * WINAPI sqlite3_column_name_interop(sqlite3_stmt *stmt, int iCol, int *plen)
{
  const char *pval = sqlite3_column_name(stmt, iCol);
  *plen = (pval != 0) ? strlen(pval) : 0;
  return pval;
}

__declspec(dllexport) const void * WINAPI sqlite3_column_name16_interop(sqlite3_stmt *stmt, int iCol, int *plen)
{
  const void *pval = sqlite3_column_name16(stmt, iCol);
  *plen = (pval != 0) ? wcslen((wchar_t *)pval) * sizeof(wchar_t) : 0;
  return pval;
}

__declspec(dllexport) const char * WINAPI sqlite3_column_decltype_interop(sqlite3_stmt *stmt, int iCol, int *plen)
{
  const char *pval = sqlite3_column_decltype(stmt, iCol);
  *plen = (pval != 0) ? strlen(pval) : 0;
  return pval;
}

__declspec(dllexport) const void * WINAPI sqlite3_column_decltype16_interop(sqlite3_stmt *stmt, int iCol, int *plen)
{
  const void *pval = sqlite3_column_decltype16(stmt, iCol);
  *plen = (pval != 0) ? wcslen((wchar_t *)pval) * sizeof(wchar_t) : 0;
  return pval;
}

__declspec(dllexport) void WINAPI sqlite3_column_double_interop(sqlite3_stmt *stmt, int iCol, double *val)
{
	*val = sqlite3_column_double(stmt,iCol);
}

__declspec(dllexport) void WINAPI sqlite3_column_int64_interop(sqlite3_stmt *stmt, int iCol, sqlite_int64 *val)
{
	*val = sqlite3_column_int64(stmt,iCol);
}

__declspec(dllexport) const unsigned char * WINAPI sqlite3_column_text_interop(sqlite3_stmt *stmt, int iCol, int *plen)
{
  const unsigned char *pval = sqlite3_column_text(stmt, iCol);
  *plen = (pval != 0) ? strlen((char *)pval) : 0;
  return pval;
}

__declspec(dllexport) const void * WINAPI sqlite3_column_text16_interop(sqlite3_stmt *stmt, int iCol, int *plen)
{
  const void *pval = sqlite3_column_text16(stmt, iCol);
  *plen = (pval != 0) ? wcslen((wchar_t *)pval) * sizeof(wchar_t): 0;
  return pval;
}

__declspec(dllexport) int WINAPI sqlite3_finalize_interop(sqlite3_stmt *stmt)
{
  Vdbe *p;
  sqlite3 *db;
  int ret;

  p = (Vdbe *)stmt;
  db = (p == NULL) ? NULL : p->db;

  if (p->magic == VDBE_MAGIC_DEAD)
  {
    if (db == NULL)
    {
      sqlite3_free(p);
      ret = SQLITE_OK;
    }
  }
  else
    ret = sqlite3_finalize(stmt);

  return ret;
}

__declspec(dllexport) int WINAPI sqlite3_reset_interop(sqlite3_stmt *stmt)
{
  int ret;

  if (((Vdbe *)stmt)->magic == VDBE_MAGIC_DEAD) return SQLITE_SCHEMA;
  ret = sqlite3_reset(stmt);
  return ret;
}

__declspec(dllexport) int WINAPI sqlite3_create_function_interop(sqlite3 *psql, const char *zFunctionName, int nArg, int eTextRep, void *pvUser, SQLITEUSERFUNC func, SQLITEUSERFUNC funcstep, SQLITEFUNCFINAL funcfinal, int needCollSeq)
{
  int n;

  if (eTextRep == SQLITE_UTF16)
    eTextRep = SQLITE_UTF16NATIVE;

  n = sqlite3_create_function(psql, zFunctionName, nArg, eTextRep, 0, func, funcstep, funcfinal);
  if (n == 0)
  {
    if (needCollSeq)
    {
      FuncDef *pFunc = sqlite3FindFunction(psql, zFunctionName, strlen(zFunctionName), nArg, eTextRep, 0);
      if( pFunc )
      {
        pFunc->needCollSeq = 1;
      }
    }
  }

  return n;
}

__declspec(dllexport) void WINAPI sqlite3_value_double_interop(sqlite3_value *pval, double *val)
{
  *val = sqlite3_value_double(pval);
}

__declspec(dllexport) void WINAPI sqlite3_value_int64_interop(sqlite3_value *pval, sqlite_int64 *val)
{
  *val = sqlite3_value_int64(pval);
}

__declspec(dllexport) const unsigned char * WINAPI sqlite3_value_text_interop(sqlite3_value *val, int *plen)
{
  const unsigned char *pval = sqlite3_value_text(val);
  *plen = (pval != 0) ? strlen((char *)pval) : 0;
  return pval;
}

__declspec(dllexport) const void * WINAPI sqlite3_value_text16_interop(sqlite3_value *val, int *plen)
{
  const void *pval = sqlite3_value_text16(val);
  *plen = (pval != 0) ? wcslen((wchar_t *)pval) * sizeof(wchar_t) : 0;
  return pval;
}

__declspec(dllexport) void WINAPI sqlite3_result_double_interop(sqlite3_context *pctx, double *val)
{
  sqlite3_result_double(pctx, *val);
}

__declspec(dllexport) void WINAPI sqlite3_result_int64_interop(sqlite3_context *pctx, sqlite_int64 *val)
{
  sqlite3_result_int64(pctx, *val);
}

__declspec(dllexport) int WINAPI sqlite3_context_collcompare(sqlite3_context *ctx, const void *p1, int p1len, const void *p2, int p2len)
{
  if (ctx->pFunc->needCollSeq == 0) return 2;
  return ctx->pColl->xCmp(ctx->pColl->pUser, p1len, p1, p2len, p2);
}

__declspec(dllexport) const char * WINAPI sqlite3_context_collseq(sqlite3_context *ctx, int *ptype, int *enc, int *plen)
{
  CollSeq *pColl = ctx->pColl;
  *ptype = 0;
  *plen = 0;
  *enc = 0;

  if (ctx->pFunc->needCollSeq == 0) return NULL;

  if (pColl)
  {
    *enc = pColl->enc;
    *ptype = pColl->type;
    *plen = (pColl->zName != 0) ? strlen(pColl->zName) : 0;

    return pColl->zName;
  }
  return NULL;
}

__declspec(dllexport) const char * WINAPI sqlite3_column_database_name_interop(sqlite3_stmt *stmt, int iCol, int *plen)
{
  const char *pval = sqlite3_column_database_name(stmt, iCol);
  *plen = (pval != 0) ? strlen(pval) : 0;
  return pval;
}

__declspec(dllexport) const void * WINAPI sqlite3_column_database_name16_interop(sqlite3_stmt *stmt, int iCol, int *plen)
{
  const void *pval = sqlite3_column_database_name16(stmt, iCol);
  *plen = (pval != 0) ? wcslen((wchar_t *)pval) * sizeof(wchar_t) : 0;
  return pval;
}

__declspec(dllexport) const char * WINAPI sqlite3_column_table_name_interop(sqlite3_stmt *stmt, int iCol, int *plen)
{
  const char *pval = sqlite3_column_table_name(stmt, iCol);
  *plen = (pval != 0) ? strlen(pval) : 0;
  return pval;
}

__declspec(dllexport) const void * WINAPI sqlite3_column_table_name16_interop(sqlite3_stmt *stmt, int iCol, int *plen)
{
  const void *pval = sqlite3_column_table_name16(stmt, iCol);
  *plen = (pval != 0) ? wcslen((wchar_t *)pval) * sizeof(wchar_t) : 0;
  return pval;
}

__declspec(dllexport) const char * WINAPI sqlite3_column_origin_name_interop(sqlite3_stmt *stmt, int iCol, int *plen)
{
  const char *pval = sqlite3_column_origin_name(stmt, iCol);
  *plen = (pval != 0) ? strlen(pval) : 0;
  return pval;
}

__declspec(dllexport) const void * WINAPI sqlite3_column_origin_name16_interop(sqlite3_stmt *stmt, int iCol, int *plen)
{
  const void *pval = sqlite3_column_origin_name16(stmt, iCol);
  *plen = (pval != 0) ? wcslen((wchar_t *)pval) * sizeof(wchar_t) : 0;
  return pval;
}

__declspec(dllexport) int WINAPI sqlite3_table_column_metadata_interop(sqlite3 *db, const char *zDbName, const char *zTableName, const char *zColumnName, char **pzDataType, char **pzCollSeq, int *pNotNull, int *pPrimaryKey, int *pAutoinc, int *pdtLen, int *pcsLen)
{
  int n;
  
  n = sqlite3_table_column_metadata(db, zDbName, zTableName, zColumnName, pzDataType, pzCollSeq, pNotNull, pPrimaryKey, pAutoinc);
  *pdtLen = (*pzDataType != 0) ? strlen(*pzDataType) : 0;
  *pcsLen = (*pzCollSeq != 0) ? strlen(*pzCollSeq) : 0;

  return n;
}

__declspec(dllexport) int WINAPI sqlite3_index_column_info_interop(sqlite3 *db, const char *zDb, const char *zIndexName, const char *zColumnName, int *sortOrder, int *onError, char **pzColl, int *plen)
{
  Index *pIdx;
  Table *pTab;
  char *zErrMsg = 0;
  int n;
  pIdx = sqlite3FindIndex(db, zIndexName, zDb);
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

__declspec(dllexport) int WINAPI sqlite3_table_cursor(sqlite3_stmt *pstmt, int iDb, Pgno tableRootPage)
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

__declspec(dllexport) int WINAPI sqlite3_cursor_rowid(sqlite3_stmt *pstmt, int cursor, sqlite_int64 *prowid)
{
  Vdbe *p = (Vdbe *)pstmt;
  sqlite3 *db = (p == NULL) ? NULL : p->db;
  int rc = 0;
  Cursor *pC;
  int ret = 0;

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
    else if(pC->pseudoTable)
    {
      *prowid = keyToInt(pC->iKey);
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
      *prowid = keyToInt(*prowid);
    }
    break;
  }
  sqlite3_mutex_leave(db->mutex);

  return ret;
}

#endif // SQLITE_OS_WIN

