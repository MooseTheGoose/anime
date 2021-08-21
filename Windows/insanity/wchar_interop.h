#ifndef __ANIM_EXTRACTOR_WCHAR_INTEROP_H
#define __ANIM_EXTRACTOR_WCHAR_INTEROP_H

/*
 *  Converts wide character strings to UTF8 strings.
 *
 *  First tries to convert using UTF-16 conventions.
 *  If the encoding is illegal for that, it will "encode"
 *  the raw 16-bit values instead (technically, this is
 *  illegal encoding, but this is the most sane we can get).
 *
 *  This has the benefit of the UTF8 strings still being readable
 *  when encoding is good, and also has the benefit of being
 *  able to convert UTF8 strings back to their wide character counterparts.
 */

#include <Windows.h>

/*
 *  strncpy from one type to another.
 *  Note that u8wstrncpy length is in
 *  bytes, not characters.
 */
LPWSTR wu8strncpy(LPWSTR dst, LPCSTR src, SIZE_T n);
LPCSTR u8wstrncpy(LPSTR src, LPCWSTR dst, SIZE_T n);

#endif