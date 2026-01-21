10 REM *** Lissajous Curves ***
20 SCREEN 9
30 CLS
40 CX = 320: CY = 175
50 A = 3: B = 4: D = 3.14159 / 2
60 FOR T = 0 TO 6.28 * 10 STEP 0.01
70   X = CX + 250 * SIN(A * T + D)
80   Y = CY + 150 * SIN(B * T)
90   C = INT(T) MOD 15 + 1
100  PSET (X, Y), C
110 NEXT T
120 A$ = INKEY$: IF A$ = "" THEN 120
130 END
