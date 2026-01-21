10 REM *** Rose Curve ***
20 SCREEN 9
30 CLS
40 CX = 320: CY = 175
50 K = 5
60 FOR T = 0 TO 6.28 * 2 STEP 0.005
70   R = 150 * COS(K * T)
80   X = CX + R * COS(T)
90   Y = CY + R * SIN(T) * 0.6
100  C = INT(T * 2) MOD 15 + 1
110  PSET (X, Y), C
120 NEXT T
130 A$ = INKEY$: IF A$ = "" THEN 130
140 END
