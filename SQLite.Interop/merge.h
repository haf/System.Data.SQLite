// This code was automatically generated from assembly
// C:\src\SQLite.NET\System.Data.SQLite\bin\CompactFramework\System.Data.SQLite.dll

#include <windef.h>

#pragma data_seg(".clr")
#pragma comment(linker, "/SECTION:.clr,ER")
  char __ph[85184] = {0}; // The number of bytes to reserve
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
