/* Parsing-only C runtime header of the binding generator (docs/adr/0026). The headers are parsed
   as C17, where bool is the _Bool macro. */
#ifndef JADE_STDBOOL_H
#define JADE_STDBOOL_H

#define bool _Bool
#define true 1
#define false 0
#define __bool_true_false_are_defined 1

#endif
