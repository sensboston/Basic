10 REM *** Classic Fire Effect ***
20 REM *** Simplified for SCREEN 9 ***
30 SCREEN 9: CLS
40 DIM FIRE(320, 50)
50 RANDOMIZE TIMER
60 REM Main fire loop
70 K$ = INKEY$: IF K$ <> "" THEN 300
80 REM Set random heat at bottom
90 FOR X = 0 TO 319
100   FIRE(X, 49) = INT(RND * 2) * 15
110 NEXT X
120 REM Propagate fire upward with averaging
130 FOR Y = 0 TO 48
140   FOR X = 1 TO 318
150     REM Average surrounding pixels
160     SUM = FIRE(X - 1, Y + 1) + FIRE(X, Y + 1) + FIRE(X + 1, Y + 1) + FIRE(X, Y)
170     FIRE(X, Y) = INT(SUM / 4.2)
180     IF FIRE(X, Y) < 0 THEN FIRE(X, Y) = 0
190     REM Map to EGA colors
200     C = INT(FIRE(X, Y))
210     IF C > 15 THEN C = 15
220     IF C > 0 THEN PSET (X, Y + 300), C ELSE PSET (X, Y + 300), 0
230   NEXT X
240 NEXT Y
250 GOTO 70
300 SCREEN 0: END
