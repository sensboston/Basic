10 REM *** Colorful Spiral Pattern ***
20 SCREEN 9
30 CLS
40 PI = 3.14159
50 CX = 320: CY = 175
60 FOR I = 0 TO 1500
70   A = I / 20
80   R = I / 10
90   X = CX + R * COS(A)
100  Y = CY + R * SIN(A) * 0.7
110  C = (I MOD 15) + 1
120  PSET (X, Y), C
130 NEXT I
140 A$ = INKEY$: IF A$ = "" THEN 140
150 END
