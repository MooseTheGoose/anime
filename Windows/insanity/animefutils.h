#ifndef ANIM_EXTRACTOR_FUTILS_H
#define ANIM_EXTRACTOR_FUTILS_H

/*
 * Implementations are platform-specific.
 */
#include <wchar.h>

enum AnimExtractorFUtilErr {
  AEFUTILS_NOMEM,
  AEFUTILS_CHECKSYS,
  AEFUTILS_NOPATH,
  AEFUTILS_EXISTS
};


typedef void * AnimExtractorFHandle;

int AnimExtractorFutilsGetError();
wchar_t * AnimExtractorGetCwd();
wchar_t * AnimExtractorAllocString(unsigned int nmemb);
int AnimExtractorCatString(wchar_t * left, wchar_t * right)
void AnimExtractorFreeString(wchar_t *);
int AnimExtractorChdir(wchar_t *);
int AnimExtractorMkdir(wchar_t *);
int AnimExtractorRmdir(wchar_t *);
AnimExtractorFHandle AnimExtractorMkfile(wchar_t *);
void AnimExtractorFreeHandle(AnimExtractorFHandle);
unsigned int AnimExtractorRead(void *, unsigned int, unsigned int, AnimExtractorFHandle);
unsigned int AnimExtractorWrite(void *, unsigned int, unsigned int, AnimExtractorFHandle);
int AnimExtractorSeek(AnimExtractorFHandle, long, int);
long AnimExtractorTell(AnimExtractorFHandle);
int AnimExtractorRmfile(wchar_t *);

#endif