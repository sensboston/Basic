10 REM *** Barnsley Fern Fractal ***
20 REM *** Iterated Function System ***
30 SCREEN 9: CLS
40 RANDOMIZE TIMER
50 REM Starting point
60 X = 0: Y = 0
70 REM Draw 50000 points
80 FOR I = 1 TO 50000
90   REM Random transformation selection
100  R = RND * 100
110  IF R >= 1 THEN 160
120  REM Transformation 1: Stem (1%)
130  NEWX = 0
140  NEWY = 0.16 * Y
150  GOTO 310
160  IF R >= 86 THEN 210
170  REM Transformation 2: Main leaflets (85%)
180  NEWX = 0.85 * X + 0.04 * Y
190  NEWY = -0.04 * X + 0.85 * Y + 1.6
200  GOTO 310
210  IF R >= 93 THEN 260
220  REM Transformation 3: Left leaflet (7%)
230  NEWX = 0.2 * X - 0.26 * Y
240  NEWY = 0.23 * X + 0.22 * Y + 1.6
250  GOTO 310
260  REM Transformation 4: Right leaflet (7%)
270  NEWX = -0.15 * X + 0.28 * Y
280  NEWY = 0.26 * X + 0.24 * Y + 0.44
310  REM Update position
320  X = NEWX: Y = NEWY
330  REM Map to screen coordinates
340  PX = 320 + X * 60
350  PY = 349 - Y * 35
360  REM Draw point with gradient color
370  C = INT(Y * 1.5) MOD 6 + 2
380  IF PX < 0 OR PX > 639 OR PY < 0 OR PY > 349 THEN 400
390  PSET (PX, PY), C
400 NEXT I
410 LOCATE 1, 1: PRINT "Barnsley Fern - Press any key"
420 A$ = INKEY$: IF A$ = "" THEN 420
430 END
