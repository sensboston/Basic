10 REM *** Mandala Pattern ***
20 SCREEN 9
30 CLS
40 CX = 320: CY = 175
50 PI = 3.14159
60 FOR L = 1 TO 8
70   R = L * 20
80   N = L * 6
90   FOR I = 0 TO N - 1
100    A1 = I * 2 * PI / N
110    A2 = (I + 1) * 2 * PI / N
120    X1 = CX + R * COS(A1)
130    Y1 = CY + R * SIN(A1) * 0.6
140    X2 = CX + R * COS(A2)
150    Y2 = CY + R * SIN(A2) * 0.6
160    LINE (X1, Y1)-(X2, Y2), L
170    LINE (CX, CY)-(X1, Y1), L + 8
180  NEXT I
190 NEXT L
200 A$ = INKEY$: IF A$ = "" THEN 200
210 END
