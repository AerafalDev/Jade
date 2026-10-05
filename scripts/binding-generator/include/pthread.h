/* Parsing-only C runtime header of the binding generator (docs/adr/0026). miniaudio embeds these
   types in structs that are opaque on the C# side (docs/adr/0006), so their size does not
   matter. */
#ifndef JADE_PTHREAD_H
#define JADE_PTHREAD_H

typedef unsigned long pthread_t;
typedef union { char data[64]; long long alignment; } pthread_mutex_t;
typedef union { char data[64]; long long alignment; } pthread_cond_t;

#endif
