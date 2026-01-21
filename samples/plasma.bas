10 REM *** Animated Plasma Effect ***
20 REM *** With Double Buffering ***
30 SCREEN 9,,0,1: CLS
40 T = 0
50 REM Precompute sine table for speed
60 DIM SINTAB(360)
70 FOR I = 0 TO 359
80   SINTAB(I) = SIN(I * 3.14159 / 180) * 7
90 NEXT I
100 PAGE = 0
110 REM Main loop
120 K$ = INKEY$: IF K$ <> "" THEN 330
130 REM Switch to draw on back buffer
140 IF PAGE = 0 THEN SCREEN 9,,0,1 ELSE SCREEN 9,,1,0
150 FOR Y = 0 TO 349 STEP 4
160   FOR X = 0 TO 639 STEP 4
170     REM Plasma formula with multiple sine waves
180     A1 = INT(X / 2 + T * 3) MOD 360
190     A2 = INT(Y / 2 + T * 2) MOD 360
200     A3 = INT((X + Y) / 4 + T * 4) MOD 360
210     IF A1 < 0 THEN A1 = A1 + 360
220     IF A2 < 0 THEN A2 = A2 + 360
230     IF A3 < 0 THEN A3 = A3 + 360
240     V = SINTAB(A1) + SINTAB(A2) + SINTAB(A3)
250     C = INT(V + 8)
260     IF C < 0 THEN C = 0
270     IF C > 15 THEN C = 15
280     LINE (X, Y)-(X + 3, Y + 3), C, BF
290   NEXT X
300 NEXT Y
310 REM Flip pages - show what we just drew
320 IF PAGE = 0 THEN SCREEN 9,,1,0 ELSE SCREEN 9,,0,1
330 PAGE = 1 - PAGE
340 T = T + 2
350 IF T > 360 THEN T = T - 360
360 GOTO 120
370 SCREEN 0: END
