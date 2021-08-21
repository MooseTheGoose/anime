#ifndef ANIM_EXTRACTOR_H
#define ANIM_EXTRACTOR_H

#include <SDL.h>

typedef struct _AnimExtractorWindow {
  SDL_Window * win;
  SDL_Renderer * rend;
} AnimExtractorWindow;

typedef SDL_Surface AnimExtractorImage;
typedef struct _AnimExtractorTexture {
  AnimExtractorWindow * w;
  SDL_Texture * tex;
} AnimExtractorTexture;

int AnimExtractorInit();
AnimExtractorWindow * AnimExtractorCreateWindow(const char * name, int x, int y, int w, int h);
void AnimExtractorDestroyWindow(AnimExtractorWindow * win);
AnimExtractorImage * AnimExtractorLoadImage(wchar_t * path);
int AnimExtractorUpdateMainWindow(AnimExtractorWindow * main);
int AnimExtractorUpdateFileDialog(AnimExtractorWindow * dialog);

#endif