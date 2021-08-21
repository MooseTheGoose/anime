#include "wchar_interop.h"

#define UNICODE_MIN 0
#define UNICODE_LIM 0x110000
#define UTF8_1CHAR_LIM 0x80
#define UTF8_2CHAR_LIM 0x800
#define UTF8_3CHAR_LIM 0x10000
#define UTF8_4CHAR_LIM UNICODE_MAX

#define SURROGATE_MASK 0xFC00
#define SURROGATE_NMASK 0x03FF
#define SURROGATE_SHIFT 10
#define HIGH_SURROGATE 0xD800
#define LOW_SURROGATE 0xDC00

#define W2I(c) (((INT32)c) & 0xFFFF)

static int cp_u8encode(char encode[], int cp) {
  int len = 0;
  if(cp >= 0 && cp < UTF8_1CHAR_LIM) {
    * encode = cp; len = 1;
  } else if(cp >= UTF8_1CHAR_LIM && cp < UTF8_2CHAR_LIM) {
    * encode = ((cp >> 6) & 0x1F) | 0xC0;
    len = 2; 
  } else if(cp >= UTF8_2CHAR_LIM && cp < UTF8_3CHAR_LIM) {
    * encode = ((cp >> 12) & 0x0F) | 0xE0;
    len = 3; 
  } else if(cp >= UTF8_3CHAR_LIM && cp < UTF8_4CHAR_LIM) {
    * encode = ((cp >> 18) & 0x0E) | 0xF0;
    len = 4;
  }
  for(int i = 0; i < len; i += 1) {
    encode[i + 1] = ((cp >> (6 * i)) & 0x3F) | 0x80;
  }
  return len;
}

static int cp_u16encode(wchar_t encode[], int cp) {
  int len = 0;
  if(cp >= 0 && cp < 0x10000) {
    *encode = cp; 
    len = 1;
  } else if (cp < UNICODE_MAX) {
    cp -= 0x10000;
    * encode++ = HIGH_SURROGATE + (cp >> SURROGATE_SHIFT);
    * encode = LOW_SURROGATE + cp % (1 << SURROGATE_SHIFT);
    len = 2;
  }
  return len;
}

static int cp_u8decode(char * decode, int * cp) {
  int final = -1;
  int len = 0;
  unsigned char * points = (unsigned char *)decode;
  char first = * points++;
  if(first > 0x80) {
    int mask = 0x40;
    while((first & mask) && (* points & 0xC0) == 0x80)
      points++;
  } else {
    final = * points;
    len = 1;
  }
  * cp = final;
}

LPCSTR u8wstrncpy(LPSTR dst, LPCWSTR src, SIZE_T n) {
  LPCSTR strval = dst;
  char currpoint[8];
  while(* src) {
    WCHAR c = src[0];
    INT32 cp = c;
    if((c & SURROGATE_MASK) == HIGH_SURROGATE) {
      cp = c;
      if((src[1] & SURROGATE_MASK) == LOW_SURROGATE) {
        INT32 tempresult = ((cp - HIGH_SURROGATE) << SURROGATE_SHIFT)) | (src[1] - LOW_SURROGATE);
        tempresult += 0x10000;
        if(tempresult < UNICODE_LIM) {
          src++;
          cp = tempresult;
        }
      }
    }
    src++;
    int len = cp_u8encode(currpoint, cp);
    if(len >= n)
      break;
    for(int i = 0; i < len; i += 1) {
      *dst++ = currpoint[i];
      n--;
    }
  }
  *dst = 0;
  return strval;
}

LPWSTR wu8strncpy(LPWSTR dst, LPCSTR src, SIZE_T n) {
  LPWSTR strval = dst;
  while(* src) {

  }
  return strval;
}
