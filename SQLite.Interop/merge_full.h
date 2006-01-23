// This code was automatically generated from assembly
// D:\src\SQLite.NET.Test\System.Data.SQLite\bin\System.Data.SQLite.dll

#include <windef.h>

#pragma data_seg(".clr")
#pragma comment(linker, "/SECTION:.clr,ER")
  char __ph[96452] = {0}; // The number of bytes to reserve
#pragma data_seg()

typedef BOOL (WINAPI *DLLMAIN)(HANDLE, DWORD, LPVOID);
extern BOOL WINAPI _DllMainCRTStartup(HANDLE, DWORD, LPVOID);

__declspec(dllexport) BOOL WINAPI _CorDllMainStub(HANDLE hModule, DWORD dwReason, LPVOID pvReserved)
{
  HANDLE hMod;
  DLLMAIN proc;

  hMod = GetModuleHandle(_T("mscoree"));
  if (hMod)
    proc = (DLLMAIN)GetProcAddress(hMod, _T("_CorDllMain"));
  else
    proc = _DllMainCRTStartup;

  return proc(hModule, dwReason, pvReserved);
}
