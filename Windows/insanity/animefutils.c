
#include "..\animefutils.h"
#include <Windows.h>

#define __WIN32_MAX_PATH 32800

static int _aefutils_errno;

int AnimExtractorFutilsGetError() {
  return _aefutils_errno;
}


#define SIZE
wchar_t * AnimExtractorAllocString(unsigned int nmemb) {
  if(nmemb > (~(SIZE_T)0) / 2 + 1)
    return 0; 
  return (wchar_t *)HeapAlloc(GetProcessHeap(), 0, (SIZE_T)nmemb * 2);
}

wchar_t * AnimExtractorGetCwd() {
  wchar_t buffer[__WIN32_MAX_PATH];
  wchar_t * path;
  size_t pathlen;
  DWORD status = GetFullPathNameW(L".", __WIN32_MAX_PATH, buffer, 0);
  if(status == 0) {
    _aefutils_errno = AEFUTILS_CHECKSYS;
    return 0;
  }
  pathlen = wcslen(buffer) + 1;
  path = AnimExtractorAllocString(pathlen);
  if(! path) {
    _aefutils_errno = AEFUTILS_NOMEM;
    return 0;
  }
  wmemcpy(path, buffer, pathlen);
  return path;
}

int AnimExtractorChdir(wchar_t * pathname) {
  wchar_t buffer[__WIN32_MAX_PATH];
  DWORD status = GetFullPathNameW(pathname, __WIN32_MAX_PATH, buffer, 
  if(! SetCurrentDirectoryW(pathname)) {
    _aefutils_errno = AEFUTILS_CHECKSYS;
    return 0;
  }
  return 1;
}

int AnimExtractorMkdir(wchar_t * pathname) {
  if(! CreateDirectoryW(pathname, 0)) {
    switch(GetLastError()) {
      case ERROR_ALREADY_EXISTS:
        _aefutils_errno = AEFUTILS_EXISTS;
        break;
      case ERROR_PATH_NOT_FOUND:
        _aefutils_errno = AEFUTILS_NOPATH;
        break;
      default:
        _aefutils_errno = AEFUTILS_CHECKSYS;
    }
    return 0;
  }
  return 1;
}

int AnimExtractorRmdir(wchar_t * pathname) {
  if(! RemoveDirectoryW(pathname)) {
    _aefutils_errno = AEFUTILS_CHECKSYS;
    return 0;
  }
  return 1;
}