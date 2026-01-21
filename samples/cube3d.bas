10 REM *** 3D Rotating Wireframe Cube ***
20 REM *** With Double Buffering ***
30 SCREEN 9,,0,1: CLS
40 DIM PX(8), PY(8), PZ(8)
50 DIM SX(8), SY(8)
60 REM Cube vertices - explicit assignment
70 PX(1) = -50: PY(1) = -50: PZ(1) = -50
80 PX(2) = 50: PY(2) = -50: PZ(2) = -50
90 PX(3) = 50: PY(3) = 50: PZ(3) = -50
100 PX(4) = -50: PY(4) = 50: PZ(4) = -50
110 PX(5) = -50: PY(5) = -50: PZ(5) = 50
120 PX(6) = 50: PY(6) = -50: PZ(6) = 50
130 PX(7) = 50: PY(7) = 50: PZ(7) = 50
140 PX(8) = -50: PY(8) = 50: PZ(8) = 50
150 REM Rotation angles
160 AX = 0: AY = 0: AZ = 0
170 CX = 320: CY = 175: DIST = 250
180 PAGE = 0
190 REM Main animation loop
200 K$ = INKEY$: IF K$ <> "" THEN GOTO 600
210 REM Switch to draw on back buffer
220 IF PAGE = 0 THEN SCREEN 9,,0,1 ELSE SCREEN 9,,1,0
230 REM Clear back buffer
240 CLS
250 REM Rotate and project all vertices
260 FOR I = 1 TO 8
270   REM Rotate around X axis
280   Y1 = PY(I) * COS(AX) - PZ(I) * SIN(AX)
290   Z1 = PY(I) * SIN(AX) + PZ(I) * COS(AX)
300   X1 = PX(I)
310   REM Rotate around Y axis
320   X2 = X1 * COS(AY) + Z1 * SIN(AY)
330   Z2 = -X1 * SIN(AY) + Z1 * COS(AY)
340   Y2 = Y1
350   REM Rotate around Z axis
360   X3 = X2 * COS(AZ) - Y2 * SIN(AZ)
370   Y3 = X2 * SIN(AZ) + Y2 * COS(AZ)
380   Z3 = Z2
390   REM Perspective projection
400   SC = DIST / (DIST + Z3)
410   SX(I) = CX + X3 * SC
420   SY(I) = CY + Y3 * SC
430 NEXT I
440 REM Draw edges - front face (green)
450 LINE (SX(1), SY(1))-(SX(2), SY(2)), 10
460 LINE (SX(2), SY(2))-(SX(3), SY(3)), 10
470 LINE (SX(3), SY(3))-(SX(4), SY(4)), 10
480 LINE (SX(4), SY(4))-(SX(1), SY(1)), 10
490 REM Back face (red)
500 LINE (SX(5), SY(5))-(SX(6), SY(6)), 12
510 LINE (SX(6), SY(6))-(SX(7), SY(7)), 12
520 LINE (SX(7), SY(7))-(SX(8), SY(8)), 12
530 LINE (SX(8), SY(8))-(SX(5), SY(5)), 12
540 REM Connecting edges (yellow)
550 LINE (SX(1), SY(1))-(SX(5), SY(5)), 14
560 LINE (SX(2), SY(2))-(SX(6), SY(6)), 14
570 LINE (SX(3), SY(3))-(SX(7), SY(7)), 14
580 LINE (SX(4), SY(4))-(SX(8), SY(8)), 14
590 REM Flip pages - show what we just drew
600 IF PAGE = 0 THEN SCREEN 9,,1,0 ELSE SCREEN 9,,0,1
610 PAGE = 1 - PAGE
620 REM Update rotation angles
630 AX = AX + 0.05
640 AY = AY + 0.07
650 AZ = AZ + 0.03
660 GOTO 200
670 SCREEN 0: END
