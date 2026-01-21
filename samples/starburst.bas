10 REM *** Starburst Animation ***
20 SCREEN 9
30 CX = 320: CY = 175
40 FOR F = 1 TO 100
50   CLS
60   FOR I = 0 TO 72
70     A = I * 3.14159 / 36 + F / 10
80     R = 150 + 50 * SIN(F / 5)
90     X = CX + R * COS(A)
100    Y = CY + R * SIN(A) * 0.6
110    C = (I + F) MOD 15 + 1
120    LINE (CX, CY)-(X, Y), C
130  NEXT I
140  FOR D = 1 TO 500: NEXT D
150 NEXT F
160 END
