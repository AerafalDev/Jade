/*
 * Allocation of the miniaudio structures whose size and layout depend on the platform or on the
 * MA_* defines (docs/adr/0006). The managed side never declares their members: it only holds the
 * pointers returned here and passes them to the miniaudio functions that initialize them.
 *
 * The list must contain every structure that the binding generator makes opaque.
 */
#ifndef JADE_MINIAUDIO_H
#define JADE_MINIAUDIO_H

#include "miniaudio.h"

#ifdef __cplusplus
extern "C" {
#endif

/* Each function returns zeroed memory, or NULL when the allocation fails. */
MA_API ma_context* jade_ma_context_alloc(void);
MA_API void jade_ma_context_free(ma_context* context);

MA_API ma_device* jade_ma_device_alloc(void);
MA_API void jade_ma_device_free(ma_device* device);

MA_API ma_engine* jade_ma_engine_alloc(void);
MA_API void jade_ma_engine_free(ma_engine* engine);

MA_API ma_sound* jade_ma_sound_alloc(void);
MA_API void jade_ma_sound_free(ma_sound* sound);

#ifdef __cplusplus
}
#endif

#endif
