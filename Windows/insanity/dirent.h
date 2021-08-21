#ifndef __ANIM_EXTRACTOR_DIRENT_H
#define __ANIM_EXTRACTOR_DIRENT_H

/* 
 * dirent.h compatibility layer for windows
 * with the plus that it works with wide character
 * filenames.
 */

#include <Windows.h>

struct dirent {
  char d_name[MAX_PATH * 4];
};

typedef struct _wdir {
  HANDLE dh;
  struct dirent dir;
} DIR;

#endif