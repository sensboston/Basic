10 REM *** Bouncing Balls with Physics ***
20 REM *** With Double Buffering ***
30 SCREEN 9,,0,1: CLS
40 NUMBALLS = 8
50 DIM BX(8), BY(8), VX(8), VY(8), BC(8), BR(8)
60 RANDOMIZE TIMER
70 REM Initialize balls
80 FOR I = 1 TO NUMBALLS
90   BX(I) = 100 + RND * 440
100  BY(I) = 50 + RND * 200
110  VX(I) = (RND - 0.5) * 10
120  VY(I) = RND * 5
130  BC(I) = INT(RND * 14) + 1
140  BR(I) = 10 + INT(RND * 20)
150 NEXT I
160 GRAVITY = 0.3
170 BOUNCE = 0.85
180 FRICTION = 0.99
190 PAGE = 0
200 REM Main loop
210 K$ = INKEY$: IF K$ <> "" THEN 550
220 REM Switch to draw on back buffer
230 IF PAGE = 0 THEN SCREEN 9,,0,1 ELSE SCREEN 9,,1,0
240 REM Clear back buffer
250 CLS
260 REM Update and draw each ball
270 FOR I = 1 TO NUMBALLS
280   REM Apply gravity
290   VY(I) = VY(I) + GRAVITY
300   REM Apply friction
310   VX(I) = VX(I) * FRICTION
320   REM Update position
330   BX(I) = BX(I) + VX(I)
340   BY(I) = BY(I) + VY(I)
350   REM Floor bounce
360   IF BY(I) + BR(I) <= 340 THEN 390
370   BY(I) = 340 - BR(I)
380   VY(I) = -VY(I) * BOUNCE
390   REM Ceiling bounce
400   IF BY(I) - BR(I) >= 0 THEN 430
410   BY(I) = BR(I)
420   VY(I) = -VY(I) * BOUNCE
430   REM Right wall bounce
440   IF BX(I) + BR(I) <= 630 THEN 470
450   BX(I) = 630 - BR(I)
460   VX(I) = -VX(I) * BOUNCE
470   REM Left wall bounce
480   IF BX(I) - BR(I) >= 10 THEN 510
490   BX(I) = 10 + BR(I)
500   VX(I) = -VX(I) * BOUNCE
510   REM Draw ball
520   CIRCLE (BX(I), BY(I)), BR(I), BC(I)
530   PAINT (BX(I), BY(I)), BC(I), BC(I)
540   REM Highlight
550   CIRCLE (BX(I) - BR(I) / 3, BY(I) - BR(I) / 3), BR(I) / 4, 15
560 NEXT I
570 REM Draw border
580 LINE (5, 5)-(634, 344), 7, B
590 LOCATE 1, 30: PRINT "Bouncing Balls"
600 REM Flip pages - show what we just drew
610 IF PAGE = 0 THEN SCREEN 9,,1,0 ELSE SCREEN 9,,0,1
620 PAGE = 1 - PAGE
630 GOTO 210
640 SCREEN 0: END
