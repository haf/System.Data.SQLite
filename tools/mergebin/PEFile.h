/********************************************************
 * mergebin
 * Written by Robert Simpson (robert@blackcastlesoft.com)
 * 
 * Released to the public domain, use at your own risk!
 ********************************************************/

#pragma once

class CPEFile
{
public:
  CPEFile();
  virtual ~CPEFile(void);

protected:
  HANDLE            m_hMap;
  HANDLE            m_hFile;
  PIMAGE_DOS_HEADER m_pBase;
  PIMAGE_NT_HEADERS m_pNTHeader;

public:
  HRESULT Open  (LPCTSTR pszFile, BOOL bReadOnly = TRUE);
  void    Close (void);

  PIMAGE_SECTION_HEADER GetEnclosingSectionHeader (DWORD rva) const;
  LPVOID                GetPtrFromRVA             (DWORD rva) const;
  PIMAGE_SECTION_HEADER GetSectionHeader          (LPCSTR name) const;
  
  operator PIMAGE_DOS_HEADER   () const;
  operator PIMAGE_NT_HEADERS   () const;
  operator PIMAGE_COR20_HEADER () const;
};
