10 REM *** 3D Tunnel Effect ***
20 SCREEN 9
30 CLS
40 CX = 320: CY = 175
50 FOR R = 10 TO 300 STEP 8
60   C = (R / 8) MOD 15 + 1
70   CIRCLE (CX, CY), R, C
80   FOR A = 0 TO 6.28 STEP 0.5
90     X1 = CX + R * COS(A)
100    Y1 = CY + R * SIN(A) * 0.6
110    X2 = CX + (R+8) * COS(A)
120    Y2 = CY + (R+8) * SIN(A) * 0.6
130    LINE (X1, Y1)-(X2, Y2), C
140  NEXT A
150 NEXT R
160 A$ = INKEY$: IF A$ = "" THEN 160
170 END
