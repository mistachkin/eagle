/*
 * trimspace.c -- Eagle Package for Tcl (Garuda)
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#include <assert.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#ifndef c_isspc
#  define c_isspc(c)		(((c)=='\t') || ((c)==' '))
#endif
#ifndef c_iseol
#  define c_iseol(c)		(((c)=='\r') || ((c)=='\n'))
#endif
#ifndef s_eol
#  define s_eol			("\n")
#endif

#ifndef RC_OK
#  define RC_OK			(0)
#endif
#ifndef RC_ERROR
#  define RC_ERROR		(1)
#endif
#ifndef RC_SYNTAX
#  define RC_SYNTAX		(255)
#endif

static int		CopyLinesWithoutTrailing(FILE *pIn, FILE *pOut,
			    size_t bufSize, char *pBuf);

/*
 *------------------------------------------------------------------------
 *
 * main --
 *
 *	This utility program copies all characters from the input file to
 *	the output file.  Trailing space and tab characters at the end of
 *	each logical line are removed in the process.
 *
 * Results:
 *	Zero on success, non-zero otherwise.
 *
 * Side effects:
 *	None.
 *
 *------------------------------------------------------------------------
 */

int main(int argc, char *argv[])
{
    assert( sizeof(char)==1 );
    if( argc==3 ){
	int rc = RC_OK, rc2;
	FILE *pIn = NULL;
	FILE *pOut = NULL;
	size_t bufSize = 8192; /* TODO: Reasonable? */
	char *pBuf = NULL;

	pIn = fopen(argv[1], "r");
	if( pIn==NULL ){
	    printf("could not open input file \"%s\"\n", argv[1]);
	    rc = RC_ERROR; goto done;
	}
	pOut = fopen(argv[2], "w");
	if( pOut==NULL ){
	    printf("could not open output file \"%s\"\n", argv[2]);
	    rc = RC_ERROR; goto done;
	}
	pBuf = malloc(bufSize);
	if( pBuf==NULL ){
	    printf("unable to allocate %d bytes\n", (int)bufSize);
	    rc = RC_ERROR; goto done;
	}
	rc = CopyLinesWithoutTrailing(pIn, pOut, bufSize, pBuf);
	assert( rc!=RC_OK || !ferror(pIn) );
	assert( rc!=RC_OK || !ferror(pOut) );

    done:

	if( pBuf!=NULL ){
	    free(pBuf); pBuf = NULL;
	}
	if( pOut!=NULL ){
	    rc2 = fclose(pOut);
	    assert( "fclose(pOut)" && rc2==0 );
	    pOut = NULL;
	}
	if (pIn != NULL) {
	    rc2 = fclose(pIn);
	    assert( "fclose(pIn)" && rc2==0 );
	    pIn = NULL;
	}
	return rc;
    }
    printf("\"%s\" inFile outFile\n", argv[0]);
    return RC_SYNTAX;
}

/*
 *------------------------------------------------------------------------
 *
 * CopyLinesWithoutTrailing --
 *
 *	Copies all chars from the input file to the output file, with
 *	the exception of trailing space and tab characters at the end
 *	of each logical line.
 *
 * Results:
 *	Zero on success, non-zero otherwise.
 *
 * Side effects:
 *	None.
 *
 *------------------------------------------------------------------------
 */

static int
CopyLinesWithoutTrailing(
    FILE *pIn,		/* Input file, must be opened for reading. */
    FILE *pOut,		/* Output file, must be opened for writing. */
    size_t bufSize,	/* Size of temporary buffer, in chars. */
    char *pBuf)		/* Temporary file I/O buffer. */
{
    size_t nLineSep = strlen(s_eol);
    assert( nLineSep>0 );
    while( 1 ){
	size_t nLine;
	memset(pBuf, 0, bufSize);
	if( fgets(pBuf, bufSize, pIn)==NULL ){
	    if( !ferror(pIn) ){
		assert( feof(pIn) );
		break;
	    }
	    printf("failed to read input line\n");
	    return RC_ERROR;
	}
	nLine = strlen(pBuf);
	while( nLine>0 && c_iseol(pBuf[nLine-1]) ) nLine--;
	assert( nLine==0 || !c_iseol(pBuf[nLine-1]) );
	while( nLine>0 && c_isspc(pBuf[nLine-1]) ) nLine--;
	assert( nLine==0 || !c_isspc(pBuf[nLine-1]) );
	if( nLine>0 ){
	    if( fwrite(pBuf, 1, nLine, pOut)!=nLine ){
		printf("failed to write output characters\n");
		return RC_ERROR;
	    }
	}
	if( fwrite(s_eol, 1, nLineSep, pOut)!=nLineSep ){
	    printf("failed to write line separator\n");
	    return RC_ERROR;
	}
    }
    assert( !ferror(pIn) );
    assert( !ferror(pOut) );
    return RC_OK;
}
