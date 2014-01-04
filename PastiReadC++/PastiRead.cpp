#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#include "pasti.h"

int main(int argc, char **argv) {
	Floppy* fd = new Floppy;
	char filename[128];
	FILE* in = 0;
	FILE* out = stdout;

	argc--; argv++;		// skip program
	// read options flags
	while ((argc > 0) && (*argv[0]) == '-') {
		char x;
		while(x = *++argv[0]) switch (x) {
			case 'd':  break;
			case 'n':  break;
			default: 
				fprintf(stderr, "invalid line option");
				exit(1);
		}
		argc--; argv++;
	} // line option(s)

	// open and read input file	 	
	if (argc == 0) {
		fprintf(stderr, "You must specify an input file");
		exit(1);
	}
	strcpy(filename, *argv);
	in = fopen(filename, "rb");
	if (in) {
		argc--; argv++;
	}
	else {
		fprintf(stderr, "Could not open input file '%s'\n",*argv); 
		exit(2);
	}

	// open output file if specified 
	if (argc == 0) out = stdout;
	else {
		if ((out = fopen(*argv,"w")) ==  NULL) {
			fprintf(stderr,"Could not open output file '%s'\n",*argv);
			exit(2);
		} // oops
	} // output specified

	fprintf(out, "Working on file %s\n", filename);

	readPasti(in, out, fd);

	delete fd;
	return 0;
}