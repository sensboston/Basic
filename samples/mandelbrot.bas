10 REM *** Mandelbrot Set Fractal ***
20 REM *** GW-BASIC / EGA 640x350 ***
30 SCREEN 9
40 CLS
50 REM Screen dimensions
60 XMAX = 639: YMAX = 349
70 REM Mandelbrot region
80 XMIN2 = -2.5: XMAX2 = 1
90 YMIN2 = -1.25: YMAX2 = 1.25
100 REM Calculate scale
110 XSCALE = (XMAX2 - XMIN2) / XMAX
120 YSCALE = (YMAX2 - YMIN2) / YMAX
130 MAXITER = 32
140 REM Main loop
150 FOR PY = 0 TO YMAX
160   FOR PX = 0 TO XMAX STEP 2
170     REM Map pixel to complex plane
180     X0 = XMIN2 + PX * XSCALE
190     Y0 = YMIN2 + PY * YSCALE
200     REM Mandelbrot iteration
210     X = 0: Y = 0
220     ITER = 0
230     REM WHILE loop replacement
240     IF (X * X + Y * Y >= 4) OR (ITER >= MAXITER) THEN 290
250     XTEMP = X * X - Y * Y + X0
260     Y = 2 * X * Y + Y0
270     X = XTEMP
280     ITER = ITER + 1: GOTO 240
290     REM Color based on iterations
300     IF ITER = MAXITER THEN C = 0 ELSE C = (ITER MOD 15) + 1
310     PSET (PX, PY), C
320   NEXT PX
330 NEXT PY
340 REM Wait for keypress
350 LOCATE 1, 1: PRINT "Mandelbrot Set - Press any key"
360 A$ = INKEY$: IF A$ = "" THEN 360
370 END
