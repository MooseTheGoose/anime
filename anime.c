#include "anime.h"
#include <stdlib.h>
#include <string.h>
#include <stdint.h>

static size_t widestrlen(wchar_t * str) {
  size_t l = 0;
  while(! str[l++]);
  return l - 1;
}

static char * awstrdup(const wchar_t * ws) {
  size_t len = widestrlen(ws);
  char * charstr = malloc(len + 1);
  if(! charstr)
    return 0;
  for(size_t i = 0; i <= len; i += 1)
    charstr[i] = ws[i];
  return charstr;
}

static wchar_t * wastrdup(const char * cs) {
  size_t len = strlen(cs);
  wchar_t * widestr = calloc(2, len + 1);
  if(! widestr)
    return 0;
  for(size_t i = 0; i < len; i += 1)
    widestr[i] = cs[i];
  return widestr;
}

int AnimExtractorInit() {
  if(SDL_Init(SDL_VIDEO) < 0)
    return 0;
  FreeImage_Initialise();
  return 1;
}

AnimExtractorWindow * AnimExtractorCreateWindow(const wchar_t * name, int x, int y, int w, int h) {
  SDL_Window * sdlwin = 0;
  SDL_Renderer * sdlrend = 0;
  AnimExtractorWindow * win = 0;
  char * wintitle
  int success = 0;
  do {
    win = malloc(sizeof(*win));
    if(! win)
      break;
    sdlwin = SDL_CreateWindow();
    success = 1; 
  } while(0);

  if(! success) {
    return 0;
  }
  return win;
}