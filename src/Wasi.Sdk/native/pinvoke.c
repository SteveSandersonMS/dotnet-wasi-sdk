#include <stdio.h>
#include <string.h>
#include <mono/metadata/assembly.h>
#include <pinvoke.h>
#include "generated-pinvokes.h"

void*
wasm_dl_lookup_pinvoke_table (const char *name)
{
	for (int i = 0; i < sizeof(pinvoke_tables) / sizeof(void*); ++i) {
		if (!strcmp (name, pinvoke_names [i]))
			return pinvoke_tables [i];
	}
	return NULL;
}
