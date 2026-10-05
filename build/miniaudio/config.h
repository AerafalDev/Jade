/*
 * The MA_* defines that configure miniaudio for Jade. The native build and the binding generator
 * both force-include this file, so the compiled library and the parsed headers always agree
 * (docs/adr/0006, docs/adr/0026). Changing a define means rebuilding the natives and regenerating
 * the bindings together. The choices are recorded in docs/adr/0031.
 */
#ifndef JADE_MINIAUDIO_CONFIG_H
#define JADE_MINIAUDIO_CONFIG_H

/*
 * The library hides every symbol by default, so the public API is exported explicitly. MA_DLL
 * would do the same, but miniaudio.h documents it as unsupported, while MA_API is a documented
 * build option.
 */
#if defined(_WIN32)
#define MA_API __declspec(dllexport)
#else
#define MA_API __attribute__((visibility("default")))
#endif

/*
 * Loading the Apple audio frameworks at runtime can make an application fail notarization
 * (miniaudio.h, section 2.2); linking them at build time avoids it.
 */
#if defined(__APPLE__)
#define MA_NO_RUNTIME_LINKING
#endif

#endif
