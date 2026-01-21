10 REM *** 3D Starfield ***
20 REM *** With Double Buffering ***
30 SCREEN 9,,0,1
40 DIM SX(100), SY(100), SZ(100)
50 RANDOMIZE TIMER
60 FOR I = 1 TO 100
70   SX(I) = RND * 640 - 320
80   SY(I) = RND * 350 - 175
90   SZ(I) = RND * 100 + 1
100 NEXT I
110 PAGE = 0
120 REM Main loop
130 K$ = INKEY$: IF K$ <> "" THEN 300
140 REM Switch to draw on back buffer
150 IF PAGE = 0 THEN SCREEN 9,,0,1 ELSE SCREEN 9,,1,0
160 CLS
170 FOR I = 1 TO 100
180   SZ(I) = SZ(I) - 2
190   IF SZ(I) < 1 THEN SZ(I) = 100: SX(I) = RND * 640 - 320: SY(I) = RND * 350 - 175
200   X = 320 + SX(I) * 100 / SZ(I)
210   Y = 175 + SY(I) * 100 / SZ(I)
220   IF X > 0 AND X < 639 AND Y > 0 AND Y < 349 THEN
230     C = 15 - INT(SZ(I) / 10)
240     IF C < 1 THEN C = 1
250     PSET (X, Y), C
260   END IF
270 NEXT I
280 REM Flip pages - show what we just drew
290 IF PAGE = 0 THEN SCREEN 9,,1,0 ELSE SCREEN 9,,0,1
300 PAGE = 1 - PAGE
310 GOTO 130
320 SCREEN 0: END
