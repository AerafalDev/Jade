#ifndef JADE_NATIVE_H
#define JADE_NATIVE_H

#include <stdint.h>

#if defined(_WIN32)
#  if defined(JADE_NATIVE_BUILD)
#    define JADE_API __declspec(dllexport)
#  else
#    define JADE_API __declspec(dllimport)
#  endif
#elif defined(__GNUC__)
#  define JADE_API __attribute__((visibility("default")))
#else
#  define JADE_API
#endif

#ifdef __cplusplus
extern "C" {
#endif

/**
 * ABI version of jade_native: the jade_* shims and the set of bundled upstream APIs.
 *
 * Bumped by every change that breaks bindings generated against the previous value, so managed
 * code can refuse a mismatched library at load time instead of failing on a missing or changed
 * export later.
 */
#define JADE_NATIVE_ABI_VERSION 1

/** Returns JADE_NATIVE_ABI_VERSION as compiled into the loaded library. */
JADE_API uint32_t jade_native_abi_version(void);

#ifdef __cplusplus
}
#endif

#endif
