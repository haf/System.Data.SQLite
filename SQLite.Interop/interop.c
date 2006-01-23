/*
   This interop file must be included at or near the top of the select.c file of the SQLite3 source distribution.

   generateColumnNames() in the select.c must be renamed to _generateColumnNames

*/

#include "src/sqliteint.h"
#include "src\os.h"
#include <tchar.h>

#if NDEBUG
#if _WIN32_WCE
#include "merge.h"
#else
#include "merge_full.h"
#endif // _WIN32_WCE
#endif // NDEBUG

// Forward declare this function, we're implementing it later
static void generateColumnNames(
  Parse *pParse,      /* Parser context */
  SrcList *pTabList,  /* List of tables */
  ExprList *pEList    /* Expressions defining the result set */
);

#include "src\select.c"

/*
** Generate code that will tell the VDBE the names of columns
** in the result set.  This information is used to provide the
** azCol[] values in the callback.
*/
static void generateColumnNames(
  Parse *pParse,      /* Parser context */
  SrcList *pTabList,  /* List of tables */
  ExprList *pEList    /* Expressions defining the result set */
){
  Vdbe *v = pParse->pVdbe;
  int i, j;
  sqlite3 *db = pParse->db;
  int fullNames, shortNames;
  int realNames;                                     /*** ADDED - SQLite.Interop ***/

  realNames = (db->flags & 0x01000000)!=0;           /*** ADDED - SQLite.Interop ***/
  if (!realNames) // Default to normal Sqlite3       /*** ADDED - SQLite.Interop ***/
  {                                                  /*** ADDED - SQLite.Interop ***/
    _generateColumnNames(pParse, pTabList, pEList);  /*** ADDED - SQLite.Interop ***/
    return;                                          /*** ADDED - SQLite.Interop ***/
  }                                                  /*** ADDED - SQLite.Interop ***/

#ifndef SQLITE_OMIT_EXPLAIN
  /* If this is an EXPLAIN, skip this step */
  if( pParse->explain ){
    return;
  }
#endif

  assert( v!=0 );
  if( pParse->colNamesSet || v==0 || sqlite3MallocFailed() ) return;
  pParse->colNamesSet = 1;
  fullNames = (db->flags & SQLITE_FullColNames)!=0;
  shortNames = (db->flags & SQLITE_ShortColNames)!=0;
  if (realNames) fullNames = 1;                      /*** ADDED - SQLite.Interop ***/

  sqlite3VdbeSetNumCols(v, pEList->nExpr);
  for(i=0; i<pEList->nExpr; i++){
    Expr *p;
    p = pEList->a[i].pExpr;
    if( p==0 ) continue;
    if( pEList->a[i].zName && (realNames == 0 || p->op != TK_COLUMN)){   /*** CHANGED - SQLite.Interop ***/
      char *zName = pEList->a[i].zName;
      sqlite3VdbeSetColName(v, i, zName, strlen(zName));
      continue;
    }
    if( p->op==TK_COLUMN && pTabList ){
      Table *pTab;
      char *zCol;
      int iCol = p->iColumn;
      for(j=0; j<pTabList->nSrc && pTabList->a[j].iCursor!=p->iTable; j++){}
      assert( j<pTabList->nSrc );
      pTab = pTabList->a[j].pTab;
      if( iCol<0 ) iCol = pTab->iPKey;
      assert( iCol==-1 || (iCol>=0 && iCol<pTab->nCol) );
      if( iCol<0 ){
        zCol = "rowid";
      }else{
        zCol = pTab->aCol[iCol].zName;
      }
      if( !shortNames && !fullNames && p->span.z && p->span.z[0] ){
        sqlite3VdbeSetColName(v, i, (char*)p->span.z, p->span.n);
      }else if( fullNames || (!shortNames && pTabList->nSrc>1) ){
        char *zName = 0;
        char *zTab;
        char *zDb = 0;                                                          /*** ADDED - SQLite.Interop ***/
        int iDb;

        iDb = sqlite3SchemaToIndex(pParse->db, pTab->pSchema);

        zTab = pTabList->a[j].zAlias;
        if( fullNames || zTab==0 ){
          if (iDb > 1) zDb = db->aDb[iDb].zName;                    /*** ADDED - SQLite.Interop ***/
          zTab = pTab->zName;
        }
        if (!zDb || !realNames) sqlite3SetString(&zName, zTab, "\x01", zCol, 0);   /*** CHANGED - SQLite.Interop ***/
        else sqlite3SetString(&zName, zDb, "\x01", zTab, "\x01", zCol, 0);            /*** ADDED - SQLite.Interop ***/
        sqlite3VdbeSetColName(v, i, zName, P3_DYNAMIC);
      }else{
        sqlite3VdbeSetColName(v, i, zCol, strlen(zCol));
      }
    }else if( p->span.z && p->span.z[0] ){
      sqlite3VdbeSetColName(v, i, (char*)p->span.z, p->span.n);
      /* sqlite3VdbeCompressSpace(v, addr); */
    }else{
      char zName[30];
      assert( p->op!=TK_COLUMN || pTabList==0 );
      sprintf(zName, "column%d", i+1);
      sqlite3VdbeSetColName(v, i, zName, 0);
    }
  }
  generateColumnTypes(pParse, pTabList, pEList);
}

#ifdef OS_WIN

#include <tchar.h>

typedef void (__stdcall *SQLITEUSERFUNC)(void *, int, void **);
typedef int  (__stdcall *SQLITECOLLATION)(int, const void *, int, const void*);

typedef int (__stdcall *ENCRYPTFILEW)(const wchar_t *);
typedef int (__stdcall *ENCRYPTEDSTATUSW)(const wchar_t *, unsigned long *);
typedef int (__stdcall *DECRYPTFILEW)(const wchar_t *, unsigned long);

typedef HANDLE (__stdcall *CREATEFILEW)(
    LPCWSTR,
    DWORD,
    DWORD,
    LPSECURITY_ATTRIBUTES,
    DWORD,
    DWORD,
    HANDLE);

// Callback wrappers
int sqlite3_interop_collationfunc(void *pv, int len1, const void *pv1, int len2, const void *pv2)
{
  SQLITECOLLATION *p = (SQLITECOLLATION *)pv;
  return p[0](len1, pv1, len2, pv2);
}

void sqlite3_interop_func(sqlite3_context *pctx, int n, sqlite3_value **pv)
{
  SQLITEUSERFUNC *pf = (SQLITEUSERFUNC *)sqlite3_user_data(pctx);
  pf[0](pctx, n, (void **)pv);
}

void sqlite3_interop_step(sqlite3_context *pctx, int n, sqlite3_value **pv)
{
  SQLITEUSERFUNC *pf = (SQLITEUSERFUNC *)sqlite3_user_data(pctx);
  pf[1](pctx, n, (void **)pv);
}

void sqlite3_interop_final(sqlite3_context *pctx)
{
  SQLITEUSERFUNC *pf = (SQLITEUSERFUNC *)sqlite3_user_data(pctx);
  pf[2](pctx, 0, 0);
}

__declspec(dllexport) void __stdcall sqlite3_sleep_interop(int milliseconds)
{
  Sleep(milliseconds);
}

__declspec(dllexport) int sqlite3_encryptfile(const wchar_t *pwszFilename)
{
  HMODULE hMod = LoadLibrary(_T("ADVAPI32"));
  ENCRYPTFILEW pfunc;
  int n;

  if (hMod == NULL)
  {
    SetLastError(ERROR_NOT_SUPPORTED);
    return 0;
  }
  
  pfunc = (ENCRYPTFILEW)GetProcAddress(hMod, _T("EncryptFileW"));
  if (pfunc == NULL)
  {
    SetLastError(ERROR_NOT_SUPPORTED);
    return 0;
  }

  n = pfunc(pwszFilename);

  FreeLibrary(hMod);

  return n;
}

__declspec(dllexport) int sqlite3_decryptfile(const wchar_t *pwszFilename)
{
  HMODULE hMod = LoadLibrary(_T("ADVAPI32"));
  DECRYPTFILEW pfunc;
  int n;

  if (hMod == NULL)
  {
    SetLastError(ERROR_NOT_SUPPORTED);
    return 0;
  }

  pfunc = (DECRYPTFILEW)GetProcAddress(hMod, _T("DecryptFileW"));
  if (pfunc == NULL)
  {
    SetLastError(ERROR_NOT_SUPPORTED);
    return 0;
  }

  n = pfunc(pwszFilename, 0);

  FreeLibrary(hMod);

  return n;
}

__declspec(dllexport) unsigned long sqlite3_encryptedstatus(const wchar_t *pwszFilename, unsigned long *pdwStatus)
{
  HMODULE hMod = LoadLibrary(_T("ADVAPI32"));
  ENCRYPTEDSTATUSW pfunc;
  int n;

  if (hMod == NULL)
  {
    SetLastError(ERROR_NOT_SUPPORTED);
    return 0;
  }

  pfunc = (ENCRYPTEDSTATUSW)GetProcAddress(hMod, _T("FileEncryptionStatusW"));
  if (pfunc == NULL)
  {
    SetLastError(ERROR_NOT_SUPPORTED);
    return 0;
  }

  n = pfunc(pwszFilename, pdwStatus);

  FreeLibrary(hMod);

  return n;
}

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

__declspec(dllexport) int __stdcall sqlite3_compressfile(const wchar_t *pwszFilename)
{
  return SetCompression(pwszFilename, COMPRESSION_FORMAT_DEFAULT);
}

__declspec(dllexport) int __stdcall sqlite3_decompressfile(const wchar_t *pwszFilename)
{
  return SetCompression(pwszFilename, COMPRESSION_FORMAT_NONE);
}

__declspec(dllexport) void __stdcall sqlite3_function_free_callbackcookie(void *pCookie)
{
  if (pCookie)
    free(pCookie);
}

// sqlite3 wrappers
__declspec(dllexport) const char * __stdcall sqlite3_libversion_interop(int *plen)
{
  const char *val = sqlite3_libversion();
  *plen = (val != 0) ? strlen(val) : 0;

  return val;
}

__declspec(dllexport) int __stdcall sqlite3_libversion_number_interop(void)
{
  return sqlite3_libversion_number();
}

__declspec(dllexport) int __stdcall sqlite3_close_interop(sqlite3 *db)
{
  return sqlite3_close(db);
}

__declspec(dllexport) int __stdcall sqlite3_exec_interop(sqlite3 *db, const char *sql, sqlite3_callback cb, void *pv, char **errmsg, int *plen)
{
  int n = sqlite3_exec(db, sql, cb, pv, errmsg);
  *plen = (*errmsg != 0) ? strlen(*errmsg) : 0;
  return n;
}

__declspec(dllexport) sqlite_int64 __stdcall sqlite3_last_insert_rowid_interop(sqlite3 *db)
{
  return sqlite3_last_insert_rowid(db);
}

__declspec(dllexport) int __stdcall sqlite3_changes_interop(sqlite3 *db)
{
  return sqlite3_changes(db);
}

__declspec(dllexport) int __stdcall sqlite3_total_changes_interop(sqlite3 *db)
{
  return sqlite3_total_changes(db);
}

__declspec(dllexport) void __stdcall sqlite3_interrupt_interop(sqlite3 *db)
{
  sqlite3_interrupt(db);
}

__declspec(dllexport) int __stdcall sqlite3_complete_interop(const char *sql)
{
  return sqlite3_complete(sql);
}

__declspec(dllexport) int __stdcall sqlite3_complete16_interop(const void *sql)
{
  return sqlite3_complete16(sql);
}

__declspec(dllexport) int __stdcall sqlite3_busy_handler_interop(sqlite3 *db, int(*cb)(void *, int), void *pv)
{
  return sqlite3_busy_handler(db, cb, pv);
}

__declspec(dllexport) int __stdcall sqlite3_busy_timeout_interop(sqlite3 *db, int ms)
{
  return sqlite3_busy_timeout(db, ms);
}

__declspec(dllexport) int __stdcall sqlite3_get_table_interop(sqlite3 *db, const char *sql, char ***resultp, int *nrow, int *ncolumn, char **errmsg, int *plen)
{
  int n = sqlite3_get_table(db, sql, resultp, nrow, ncolumn, errmsg);
  *plen = (*errmsg != 0) ? strlen((char *)*errmsg) : 0;
  return n;
}

__declspec(dllexport) void __stdcall sqlite3_free_table_interop(char **result)
{
  sqlite3_free_table(result);
}

__declspec(dllexport) void __stdcall sqlite3_free_interop(char *z)
{
  sqlite3_free(z);
}

__declspec(dllexport) int __stdcall sqlite3_open_interop(const char*filename, sqlite3 **ppdb)
{
  return sqlite3_open(filename, ppdb);
}

__declspec(dllexport) int __stdcall sqlite3_open16_interop(const void *filename, sqlite3 **ppdb)
{
  return sqlite3_open16(filename, ppdb);
}

__declspec(dllexport) int __stdcall sqlite3_errcode_interop(sqlite3 *db)
{
  return sqlite3_errcode(db);
}

__declspec(dllexport) const char * __stdcall sqlite3_errmsg_interop(sqlite3 *db, int *plen)
{
  const char *pval = sqlite3_errmsg(db);
  *plen = (pval != 0) ? strlen(pval) : 0;
  return pval;
}

__declspec(dllexport) const void * __stdcall sqlite3_errmsg16_interop(sqlite3 *db, int *plen)
{
  const void *pval = sqlite3_errmsg16(db);
  *plen = (pval != 0) ? wcslen((wchar_t *)pval) * sizeof(wchar_t): 0;
  return pval;
}

__declspec(dllexport) int __stdcall sqlite3_prepare_interop(sqlite3 *db, const char *sql, int nbytes, sqlite3_stmt **ppstmt, const char **pztail, int *plen)
{
  int n = sqlite3_prepare(db, sql, nbytes, ppstmt, pztail);
  *plen = (*pztail != 0) ? strlen(*pztail) : 0;
  return n;
}

__declspec(dllexport) int __stdcall sqlite3_prepare16_interop(sqlite3 *db, const void *sql, int nchars, sqlite3_stmt **ppstmt, const void **pztail, int *plen)
{
  int n = sqlite3_prepare16(db, sql, nchars * sizeof(wchar_t), ppstmt, pztail);
  *plen = (*pztail != 0) ? wcslen((wchar_t *)*pztail) * sizeof(wchar_t) : 0;
  return n;
}

__declspec(dllexport) int __stdcall sqlite3_bind_blob_interop(sqlite3_stmt *stmt, int iCol, const void *pv, int n, void(*cb)(void*))
{
  return sqlite3_bind_blob(stmt, iCol, pv, n, cb);
}

__declspec(dllexport) int __stdcall sqlite3_bind_double_interop(sqlite3_stmt *stmt, int iCol, double *val)
{
	return sqlite3_bind_double(stmt,iCol,*val);
}

__declspec(dllexport) int __stdcall sqlite3_bind_int_interop(sqlite3_stmt *stmt, int iCol, int val)
{
  return sqlite3_bind_int(stmt, iCol, val);
}

__declspec(dllexport) int __stdcall sqlite3_bind_int64_interop(sqlite3_stmt *stmt, int iCol, sqlite_int64 *val)
{
	return sqlite3_bind_int64(stmt,iCol,*val);
}

__declspec(dllexport) int __stdcall sqlite3_bind_null_interop(sqlite3_stmt *stmt, int iCol)
{
  return sqlite3_bind_null(stmt, iCol);
}

__declspec(dllexport) int __stdcall sqlite3_bind_text_interop(sqlite3_stmt *stmt, int iCol, const char *val, int n, void(*cb)(void *))
{
  return sqlite3_bind_text(stmt, iCol, val, n, cb);
}

__declspec(dllexport) int __stdcall sqlite3_bind_text16_interop(sqlite3_stmt *stmt, int iCol, const void *val, int n, void(*cb)(void *))
{
  return sqlite3_bind_text16(stmt, iCol, val, n, cb);
}

__declspec(dllexport) int __stdcall sqlite3_bind_parameter_count_interop(sqlite3_stmt *stmt)
{
  return sqlite3_bind_parameter_count(stmt);
}

__declspec(dllexport) const char * __stdcall sqlite3_bind_parameter_name_interop(sqlite3_stmt *stmt, int iCol, int *plen)
{
  const char *pval = sqlite3_bind_parameter_name(stmt, iCol);
  *plen = (pval != 0) ? strlen(pval) : 0;
  return pval;
}

__declspec(dllexport) int __stdcall sqlite3_bind_parameter_index_interop(sqlite3_stmt *stmt, const char *zName)
{
  return sqlite3_bind_parameter_index(stmt, zName);
}

__declspec(dllexport) int __stdcall sqlite3_column_count_interop(sqlite3_stmt *stmt)
{
  return sqlite3_column_count(stmt);
}

__declspec(dllexport) const char * __stdcall sqlite3_column_name_interop(sqlite3_stmt *stmt, int iCol, int *plen)
{
  const char *pval = sqlite3_column_name(stmt, iCol);
  *plen = (pval != 0) ? strlen(pval) : 0;
  return pval;
}

__declspec(dllexport) const void * __stdcall sqlite3_column_name16_interop(sqlite3_stmt *stmt, int iCol, int *plen)
{
  const void *pval = sqlite3_column_name16(stmt, iCol);
  *plen = (pval != 0) ? wcslen((wchar_t *)pval) * sizeof(wchar_t) : 0;
  return pval;
}

__declspec(dllexport) const char * __stdcall sqlite3_column_decltype_interop(sqlite3_stmt *stmt, int iCol, int *plen)
{
  const char *pval = sqlite3_column_decltype(stmt, iCol);
  *plen = (pval != 0) ? strlen(pval) : 0;
  return pval;
}

__declspec(dllexport) const void * __stdcall sqlite3_column_decltype16_interop(sqlite3_stmt *stmt, int iCol, int *plen)
{
  const void *pval = sqlite3_column_decltype16(stmt, iCol);
  *plen = (pval != 0) ? wcslen((wchar_t *)pval) * sizeof(wchar_t) : 0;
  return pval;
}

__declspec(dllexport) int __stdcall sqlite3_step_interop(sqlite3_stmt *stmt)
{
  return sqlite3_step(stmt);
}

__declspec(dllexport) int __stdcall sqlite3_data_count_interop(sqlite3_stmt *stmt)
{
  return sqlite3_data_count(stmt);
}

__declspec(dllexport) const void * __stdcall sqlite3_column_blob_interop(sqlite3_stmt *stmt, int iCol)
{
  return sqlite3_column_blob(stmt, iCol);
}

__declspec(dllexport) int __stdcall sqlite3_column_bytes_interop(sqlite3_stmt *stmt, int iCol)
{
  return sqlite3_column_bytes(stmt, iCol);
}

__declspec(dllexport) int __stdcall sqlite3_column_bytes16_interop(sqlite3_stmt *stmt, int iCol)
{
  return sqlite3_column_bytes16(stmt, iCol);
}

__declspec(dllexport) void __stdcall sqlite3_column_double_interop(sqlite3_stmt *stmt, int iCol, double *val)
{
	*val = sqlite3_column_double(stmt,iCol);
}

__declspec(dllexport) int __stdcall sqlite3_column_int_interop(sqlite3_stmt *stmt, int iCol)
{
  return sqlite3_column_int(stmt, iCol);
}

__declspec(dllexport) void __stdcall sqlite3_column_int64_interop(sqlite3_stmt *stmt, int iCol, sqlite_int64 *val)
{
	*val = sqlite3_column_int64(stmt,iCol);
}

__declspec(dllexport) const unsigned char * __stdcall sqlite3_column_text_interop(sqlite3_stmt *stmt, int iCol, int *plen)
{
  const unsigned char *pval = sqlite3_column_text(stmt, iCol);
  *plen = (pval != 0) ? strlen((char *)pval) : 0;
  return pval;
}

__declspec(dllexport) const void * __stdcall sqlite3_column_text16_interop(sqlite3_stmt *stmt, int iCol, int *plen)
{
  const void *pval = sqlite3_column_text16(stmt, iCol);
  *plen = (pval != 0) ? wcslen((wchar_t *)pval) * sizeof(wchar_t): 0;
  return pval;
}

__declspec(dllexport) int __stdcall sqlite3_column_type_interop(sqlite3_stmt *stmt, int iCol)
{
  return sqlite3_column_type(stmt, iCol);
}

__declspec(dllexport) int __stdcall sqlite3_finalize_interop(sqlite3_stmt *stmt)
{
  return sqlite3_finalize(stmt);
}

__declspec(dllexport) int __stdcall sqlite3_reset_interop(sqlite3_stmt *stmt)
{
  return sqlite3_reset(stmt);
}

__declspec(dllexport) int __stdcall sqlite3_create_function_interop(sqlite3 *psql, const char *zFunctionName, int nArg, int eTextRep, SQLITEUSERFUNC func, SQLITEUSERFUNC funcstep, SQLITEUSERFUNC funcfinal, void **ppCookie)
{
  int n;
  SQLITEUSERFUNC *p = (SQLITEUSERFUNC *)malloc(sizeof(SQLITEUSERFUNC) * 3);

  p[0] = func;
  p[1] = funcstep;
  p[2] = funcfinal;

  *ppCookie = 0;

  n = sqlite3_create_function(psql, zFunctionName, nArg, eTextRep, p, (func != 0) ? sqlite3_interop_func : 0, (funcstep != 0) ? sqlite3_interop_step : 0, (funcfinal != 0) ? sqlite3_interop_final : 0);
  if (n != 0)
    free(p);
  else
    *ppCookie = p;

  return n;
}

__declspec(dllexport) int __stdcall sqlite3_create_function16_interop(sqlite3 *psql, void *zFunctionName, int nArg, int eTextRep, SQLITEUSERFUNC func, SQLITEUSERFUNC funcstep, SQLITEUSERFUNC funcfinal, void **ppCookie)
{
  int n;
  SQLITEUSERFUNC *p = (SQLITEUSERFUNC *)malloc(sizeof(SQLITEUSERFUNC) * 3);

  p[0] = func;
  p[1] = funcstep;
  p[2] = funcfinal;

  *ppCookie = 0;

  n = sqlite3_create_function16(psql, zFunctionName, nArg, eTextRep, p, (func != 0) ? sqlite3_interop_func : 0, (funcstep != 0) ? sqlite3_interop_step : 0, (funcfinal != 0) ? sqlite3_interop_final : 0);
  if (n != 0)
    free(p);
  else
    *ppCookie = p;

  return n;
}

__declspec(dllexport) int __stdcall sqlite3_create_collation_interop(sqlite3* db, const char *zName, int eTextRep, void* pvUser, SQLITECOLLATION func, void **ppCookie)
{
  int n;
  SQLITECOLLATION *p = (SQLITECOLLATION *)malloc(sizeof(SQLITECOLLATION));
  
  p[0] = func;

  *ppCookie = 0;

  n = sqlite3_create_collation(db, zName, eTextRep, p, sqlite3_interop_collationfunc);
  if (n != 0)
    free(p);
  else
    *ppCookie = p;

  return n;
}

__declspec(dllexport) int __stdcall sqlite3_create_collation16_interop(sqlite3* db, const void *zName, int eTextRep, void* pvUser, SQLITECOLLATION func, void **ppCookie)
{
  int n;
  SQLITECOLLATION *p = (SQLITECOLLATION *)malloc(sizeof(SQLITECOLLATION));
  
  p[0] = func;

  *ppCookie = 0;

  n = sqlite3_create_collation16(db, (const char *)zName, eTextRep, p, sqlite3_interop_collationfunc);
  if (n != 0)
    free(p);
  else
    *ppCookie = p;

  return n;
}

__declspec(dllexport) int __stdcall sqlite3_aggregate_count_interop(sqlite3_context *pctx)
{
  return sqlite3_aggregate_count(pctx);
}

__declspec(dllexport) const void * __stdcall sqlite3_value_blob_interop(sqlite3_value *val)
{
  return sqlite3_value_blob(val);
}

__declspec(dllexport) int __stdcall sqlite3_value_bytes_interop(sqlite3_value *val)
{
  return sqlite3_value_bytes(val);
}

__declspec(dllexport) int __stdcall sqlite3_value_bytes16_interop(sqlite3_value *val)
{
  return sqlite3_value_bytes16(val);
}

__declspec(dllexport) void __stdcall sqlite3_value_double_interop(sqlite3_value *pval, double *val)
{
  *val = sqlite3_value_double(pval);
}

__declspec(dllexport) int __stdcall sqlite3_value_int_interop(sqlite3_value *val)
{
  return sqlite3_value_int(val);
}

__declspec(dllexport) void __stdcall sqlite3_value_int64_interop(sqlite3_value *pval, sqlite_int64 *val)
{
  *val = sqlite3_value_int64(pval);
}

__declspec(dllexport) const unsigned char * __stdcall sqlite3_value_text_interop(sqlite3_value *val, int *plen)
{
  const unsigned char *pval = sqlite3_value_text(val);
  *plen = (pval != 0) ? strlen((char *)pval) : 0;
  return pval;
}

__declspec(dllexport) const void * __stdcall sqlite3_value_text16_interop(sqlite3_value *val, int *plen)
{
  const void *pval = sqlite3_value_text16(val);
  *plen = (pval != 0) ? wcslen((wchar_t *)pval) * sizeof(wchar_t) : 0;
  return pval;
}

__declspec(dllexport) int __stdcall sqlite3_value_type_interop(sqlite3_value *val)
{
  return sqlite3_value_type(val);
}

__declspec(dllexport) void * __stdcall sqlite3_aggregate_context_interop(sqlite3_context *pctx, int n)
{
  return sqlite3_aggregate_context(pctx, n);
}

__declspec(dllexport) void __stdcall sqlite3_result_blob_interop(sqlite3_context *ctx, const void *pv, int n, void(*cb)(void *))
{
  sqlite3_result_blob(ctx, pv, n, cb);
}

__declspec(dllexport) void __stdcall sqlite3_result_double_interop(sqlite3_context *pctx, double *val)
{
  sqlite3_result_double(pctx, *val);
}

__declspec(dllexport) void __stdcall sqlite3_result_int_interop(sqlite3_context *pctx, int val)
{
  sqlite3_result_int(pctx, val);
}

__declspec(dllexport) void __stdcall sqlite3_result_int64_interop(sqlite3_context *pctx, sqlite_int64 *val)
{
  sqlite3_result_int64(pctx, *val);
}

__declspec(dllexport) void __stdcall sqlite3_result_null_interop(sqlite3_context *pctx)
{
  sqlite3_result_null(pctx);
}

__declspec(dllexport) void __stdcall sqlite3_result_error_interop(sqlite3_context *ctx, const char *pv, int n)
{
  sqlite3_result_error(ctx, pv, n);
}

__declspec(dllexport) void __stdcall sqlite3_result_error16_interop(sqlite3_context *ctx, const void *pv, int n)
{
  sqlite3_result_error16(ctx, pv, n);
}

__declspec(dllexport) void __stdcall sqlite3_result_text_interop(sqlite3_context *ctx, const char *pv, int n, void(*cb)(void *))
{
  sqlite3_result_text(ctx, pv, n, cb);
}

__declspec(dllexport) void __stdcall sqlite3_result_text16_interop(sqlite3_context *ctx, const void *pv, int n, void(*cb)(void *))
{
  sqlite3_result_text16(ctx, pv, n, cb);
}

__declspec(dllexport) void __stdcall sqlite3_realcolnames(sqlite3 *db, int bOn)
{
  if (bOn)
    db->flags |= 0x01000000;
  else
    db->flags &= (~0x01000000);
}

#endif // OS_WIN
