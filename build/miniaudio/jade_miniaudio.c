#include "jade_miniaudio.h"

#include <string.h>

/*
 * MA_SIMD_ALIGNMENT is the largest alignment miniaudio asks for, so it covers every structure
 * whatever its members are on the target.
 */
#define JADE_MA_DEFINE_ALLOCATOR(type)                                                    \
    type* jade_##type##_alloc(void)                                                       \
    {                                                                                     \
        type* object = (type*)ma_aligned_malloc(sizeof(type), MA_SIMD_ALIGNMENT, NULL);   \
        if (object != NULL) {                                                             \
            memset(object, 0, sizeof(type));                                              \
        }                                                                                 \
        return object;                                                                    \
    }                                                                                     \
                                                                                          \
    void jade_##type##_free(type* object)                                                 \
    {                                                                                     \
        ma_aligned_free(object, NULL);                                                    \
    }

JADE_MA_DEFINE_ALLOCATOR(ma_async_notification_event)
JADE_MA_DEFINE_ALLOCATOR(ma_context)
JADE_MA_DEFINE_ALLOCATOR(ma_device)
JADE_MA_DEFINE_ALLOCATOR(ma_device_job_thread)
JADE_MA_DEFINE_ALLOCATOR(ma_engine)
JADE_MA_DEFINE_ALLOCATOR(ma_event)
JADE_MA_DEFINE_ALLOCATOR(ma_fence)
JADE_MA_DEFINE_ALLOCATOR(ma_job_queue)
JADE_MA_DEFINE_ALLOCATOR(ma_log)
JADE_MA_DEFINE_ALLOCATOR(ma_mutex)
JADE_MA_DEFINE_ALLOCATOR(ma_resource_manager)
JADE_MA_DEFINE_ALLOCATOR(ma_semaphore)
JADE_MA_DEFINE_ALLOCATOR(ma_sound)
