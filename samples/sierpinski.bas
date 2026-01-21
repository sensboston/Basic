10 REM *** Sierpinski Triangle - Chaos Game ***
20 SCREEN 9
30 CLS
40 REM Define triangle vertices
50 X1 = 320: Y1 = 20
60 X2 = 100: Y2 = 330
70 X3 = 540: Y3 = 330
80 REM Start point
90 PX = 320: PY = 175
100 RANDOMIZE TIMER
110 FOR I = 1 TO 30000
120   V = INT(RND * 3) + 1
130   IF V = 1 THEN PX = (PX + X1) / 2: PY = (PY + Y1) / 2
140   IF V = 2 THEN PX = (PX + X2) / 2: PY = (PY + Y2) / 2
150   IF V = 3 THEN PX = (PX + X3) / 2: PY = (PY + Y3) / 2
160   PSET (PX, PY), (I MOD 15) + 1
170 NEXT I
180 A$ = INKEY$: IF A$ = "" THEN 180
190 END
