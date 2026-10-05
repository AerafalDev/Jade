/*
 * Allocation of the miniaudio structures whose size and layout depend on the platform or on the
 * MA_* defines (docs/adr/0006). The managed side never declares their members: it only holds the
 * pointers returned here and passes them to the miniaudio functions that initialize them.
 *
 * The list must contain every structure that the binding generator makes opaque: the generator
 * fails when one lacks its two functions (opaque and opaqueAllocators in
 * interop/Jade.MiniAudio/bindings.json).
 */
#ifndef JADE_MINIAUDIO_H
#define JADE_MINIAUDIO_H

#include "miniaudio.h"

#ifdef __cplusplus
extern "C" {
#endif

/* Each function returns zeroed memory, or NULL when the allocation fails. */
MA_API ma_async_notification_event* jade_ma_async_notification_event_alloc(void);
MA_API void jade_ma_async_notification_event_free(ma_async_notification_event* object);

MA_API ma_context* jade_ma_context_alloc(void);
MA_API void jade_ma_context_free(ma_context* object);

MA_API ma_device* jade_ma_device_alloc(void);
MA_API void jade_ma_device_free(ma_device* object);

MA_API ma_device_job_thread* jade_ma_device_job_thread_alloc(void);
MA_API void jade_ma_device_job_thread_free(ma_device_job_thread* object);

MA_API ma_engine* jade_ma_engine_alloc(void);
MA_API void jade_ma_engine_free(ma_engine* object);

MA_API ma_event* jade_ma_event_alloc(void);
MA_API void jade_ma_event_free(ma_event* object);

MA_API ma_fence* jade_ma_fence_alloc(void);
MA_API void jade_ma_fence_free(ma_fence* object);

MA_API ma_job_queue* jade_ma_job_queue_alloc(void);
MA_API void jade_ma_job_queue_free(ma_job_queue* object);

MA_API ma_log* jade_ma_log_alloc(void);
MA_API void jade_ma_log_free(ma_log* object);

MA_API ma_mutex* jade_ma_mutex_alloc(void);
MA_API void jade_ma_mutex_free(ma_mutex* object);

MA_API ma_resource_manager* jade_ma_resource_manager_alloc(void);
MA_API void jade_ma_resource_manager_free(ma_resource_manager* object);

MA_API ma_semaphore* jade_ma_semaphore_alloc(void);
MA_API void jade_ma_semaphore_free(ma_semaphore* object);

MA_API ma_sound* jade_ma_sound_alloc(void);
MA_API void jade_ma_sound_free(ma_sound* object);

#ifdef __cplusplus
}
#endif

#endif
