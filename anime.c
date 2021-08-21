#include "anime.h"
#include <stdlib.h>
#include <string.h>
#include <stdint.h>

int AnimExtractorInit() {
  if(SDL_Init(SDL_INIT_VIDEO) < 0)
    return 0;
  FreeImage_Initialise();
  return 1;
}

AnimExtractorWindow * AnimExtractorCreateWindow(const char * name, int x, int y, int w, int h) {
  SDL_Window * sdlwin = 0;
  SDL_Renderer * sdlrend = 0;
  AnimExtractorWindow * win = 0;
  char * wintitle;
  int success = 0;
  do {
    win = malloc(sizeof(*win));
    if(! win)
      break;
    // sdlwin = SDL_CreateWindow();
    success = 1; 
  } while(0);

  if(! success) {
    return 0;
  }
  return win;
}