10 REM *** Colorful Circles - 24-bit RGB ***
20 SCREEN 15: CLS
30 RANDOMIZE TIMER
40 REM Draw random colorful circles
50 FOR I = 1 TO 200
60   X = INT(RND * 640)
70   Y = INT(RND * 480)
80   R = INT(RND * 100) + 10
90   CR = INT(RND * 256)
100  CG = INT(RND * 256)
110  CB = INT(RND * 256)
120  CIRCLE (X, Y), R, RGB(CR, CG, CB)
130 NEXT I
140 LOCATE 1, 1: COLOR 15
150 PRINT "24-bit Random Circles - Press key";
160 A$ = INKEY$: IF A$ = "" THEN 160
170 SCREEN 0: END
