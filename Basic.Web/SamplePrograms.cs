namespace Basic.Web;

public static class SamplePrograms
{
    public record SampleProgram(string Name, string Content);

    public static IEnumerable<SampleProgram> GetAll()
    {
        yield return new SampleProgram("HELLO.BAS", Hello);
        yield return new SampleProgram("LOOP.BAS", Loop);
        yield return new SampleProgram("DEMO.BAS", Demo);
        yield return new SampleProgram("GRAPHICS.BAS", Graphics);
        yield return new SampleProgram("GRAPHICS_DEMO.BAS", GraphicsDemo);
        yield return new SampleProgram("BOUNCE.BAS", Bounce);
        yield return new SampleProgram("CHART.BAS", Chart);
        yield return new SampleProgram("CUBE3D.BAS", Cube3D);
        yield return new SampleProgram("FERN.BAS", Fern);
        yield return new SampleProgram("FIRE.BAS", Fire);
        yield return new SampleProgram("FIREWORKS.BAS", Fireworks);
        yield return new SampleProgram("LISSAJOUS.BAS", Lissajous);
        yield return new SampleProgram("MANDALA.BAS", Mandala);
        yield return new SampleProgram("MANDELBROT.BAS", Mandelbrot);
        yield return new SampleProgram("MATRIX.BAS", Matrix);
        yield return new SampleProgram("MOIRE.BAS", Moire);
        yield return new SampleProgram("PLASMA.BAS", Plasma);
        yield return new SampleProgram("ROSE.BAS", Rose);
        yield return new SampleProgram("SIERPINSKI.BAS", Sierpinski);
        yield return new SampleProgram("SPIRAL.BAS", Spiral);
        yield return new SampleProgram("STARBURST.BAS", Starburst);
        yield return new SampleProgram("STARFIELD.BAS", Starfield);
        yield return new SampleProgram("TUNNEL.BAS", Tunnel);
        yield return new SampleProgram("WAVES.BAS", Waves);
    }

    private const string Hello = """
10 REM Hello World program
20 PRINT "Hello, World!"
30 A = 10
40 B = 20
50 PRINT "A + B = "; A + B
60 NAME$ = "SharpBasic"
70 PRINT "Welcome to "; NAME$
""";

    private const string Loop = """
10 REM Count from 1 to 5 using GOTO and IF
20 I = 1
30 PRINT "Count: "; I
40 I = I + 1
50 IF I <= 5 THEN 30
60 PRINT "Done!"
""";

    private const string Demo = """
10 REM SharpBasic Interpreter Demo
20 REM Demonstrates all major features
30 PRINT "=== SharpBasic Interpreter Demo ==="
40 PRINT
50 GOSUB 1000
60 GOSUB 2000
70 GOSUB 3000
80 GOSUB 4000
90 GOSUB 5000
100 PRINT
110 PRINT "Demo complete!"
120 END

1000 REM === FOR/NEXT Demo ===
1010 PRINT "FOR/NEXT Loop:"
1020 FOR I = 1 TO 5
1030 PRINT "  Count: "; I
1040 NEXT I
1050 PRINT
1060 RETURN

2000 REM === Arrays Demo ===
2010 PRINT "Arrays:"
2020 DIM NUMS(5)
2030 FOR I = 1 TO 5
2040 NUMS(I) = I * I
2050 NEXT I
2060 PRINT "  Squares: ";
2070 FOR I = 1 TO 5
2080 PRINT NUMS(I); " ";
2090 NEXT I
2100 PRINT
2110 PRINT
2120 RETURN

3000 REM === String Functions Demo ===
3010 PRINT "String Functions:"
3020 S$ = "Hello, World!"
3030 PRINT "  Original: "; S$
3040 PRINT "  Length: "; LEN(S$)
3050 PRINT "  Left 5: "; LEFT$(S$, 5)
3060 PRINT "  Right 6: "; RIGHT$(S$, 6)
3070 PRINT "  Mid 8,5: "; MID$(S$, 8, 5)
3080 PRINT "  Upper: "; UCASE$(S$)
3090 PRINT
3100 RETURN

4000 REM === Math Functions Demo ===
4010 PRINT "Math Functions:"
4020 PRINT "  SQR(16) = "; SQR(16)
4030 PRINT "  ABS(-42) = "; ABS(-42)
4040 PRINT "  INT(3.7) = "; INT(3.7)
4050 PRINT "  2^10 = "; 2^10
4060 PRINT "  17 MOD 5 = "; 17 MOD 5
4070 PRINT
4080 RETURN

5000 REM === DATA/READ Demo ===
5010 PRINT "DATA/READ:"
5020 DATA 10, 20, 30, 40, 50
5030 SUM = 0
5040 FOR I = 1 TO 5
5050 READ N
5060 SUM = SUM + N
5070 NEXT I
5080 PRINT "  Sum of data: "; SUM
5090 RETURN
""";

    private const string Graphics = """
10 REM SharpBasic Graphics Demo
20 SCREEN 1
30 COLOR 15, 1
40 CLS
50 REM Draw a border
60 LINE (0, 0)-(319, 199), 14, B
70 REM Draw some lines
80 FOR I = 0 TO 50 STEP 10
90 LINE (10+I, 10)-(310-I, 190), 2
100 NEXT I
110 REM Draw a circle
120 CIRCLE (160, 100), 50, 4
130 REM Draw a filled box
140 LINE (20, 140)-(80, 180), 3, BF
150 REM Draw pixels
160 FOR X = 200 TO 280 STEP 5
170 PSET (X, 150), 14
180 NEXT X
190 BEEP
200 PRINT "Graphics Demo Complete!"
210 END
""";

    private const string GraphicsDemo = """
10 REM Graphics Demo
20 SCREEN 1
30 REM Draw a circle
40 CIRCLE (160, 100), 50, 14
50 REM Draw a line
60 LINE (10, 10)-(310, 190), 15
70 REM Draw a box
80 LINE (50, 50)-(100, 80), 13, B
90 REM Draw a filled box
100 LINE (200, 50)-(280, 80), 12, BF
110 REM Set some pixels
120 FOR I = 1 TO 50
130 PSET (160 + I, 150), 11
140 NEXT I
150 PRINT "Graphics demo complete!"
""";

    private const string Bounce = """
10 REM *** Bouncing Balls with Physics ***
20 REM *** With Double Buffering ***
30 SCREEN 9,,0,1: CLS
40 NUMBALLS = 8
50 DIM BX(8), BY(8), VX(8), VY(8), BC(8), BR(8)
60 RANDOMIZE TIMER
70 REM Initialize balls
80 FOR I = 1 TO NUMBALLS
90   BX(I) = 100 + RND * 440
100  BY(I) = 50 + RND * 200
110  VX(I) = (RND - 0.5) * 10
120  VY(I) = RND * 5
130  BC(I) = INT(RND * 14) + 1
140  BR(I) = 10 + INT(RND * 20)
150 NEXT I
160 GRAVITY = 0.3
170 BOUNCE = 0.85
180 FRICTION = 0.99
190 PAGE = 0
200 REM Main loop
210 K$ = INKEY$: IF K$ <> "" THEN 550
220 REM Switch to draw on back buffer
230 IF PAGE = 0 THEN SCREEN 9,,0,1 ELSE SCREEN 9,,1,0
240 REM Clear back buffer
250 CLS
260 REM Update and draw each ball
270 FOR I = 1 TO NUMBALLS
280   REM Apply gravity
290   VY(I) = VY(I) + GRAVITY
300   REM Apply friction
310   VX(I) = VX(I) * FRICTION
320   REM Update position
330   BX(I) = BX(I) + VX(I)
340   BY(I) = BY(I) + VY(I)
350   REM Floor bounce
360   IF BY(I) + BR(I) <= 340 THEN 390
370   BY(I) = 340 - BR(I)
380   VY(I) = -VY(I) * BOUNCE
390   REM Ceiling bounce
400   IF BY(I) - BR(I) >= 0 THEN 430
410   BY(I) = BR(I)
420   VY(I) = -VY(I) * BOUNCE
430   REM Right wall bounce
440   IF BX(I) + BR(I) <= 630 THEN 470
450   BX(I) = 630 - BR(I)
460   VX(I) = -VX(I) * BOUNCE
470   REM Left wall bounce
480   IF BX(I) - BR(I) >= 10 THEN 510
490   BX(I) = 10 + BR(I)
500   VX(I) = -VX(I) * BOUNCE
510   REM Draw ball
520   CIRCLE (BX(I), BY(I)), BR(I), BC(I)
530   PAINT (BX(I), BY(I)), BC(I), BC(I)
540   REM Highlight
550   CIRCLE (BX(I) - BR(I) / 3, BY(I) - BR(I) / 3), BR(I) / 4, 15
560 NEXT I
570 REM Draw border
580 LINE (5, 5)-(634, 344), 7, B
590 LOCATE 1, 30: PRINT "Bouncing Balls"
600 REM Flip pages - show what we just drew
610 IF PAGE = 0 THEN SCREEN 9,,1,0 ELSE SCREEN 9,,0,1
620 PAGE = 1 - PAGE
630 GOTO 210
640 SCREEN 0: END
""";

    private const string Chart = """
10 REM *** Trigonometric Functions Chart ***
20 REM *** SIN, COS, TAN, COT, SEC, CSC ***
30 SCREEN 9: CLS
40 PI = 3.14159265
50 REM Screen layout
60 XMIN = 50: XMAX = 590
70 YMIN = 30: YMAX = 320
80 XCENTER = 320: YCENTER = 175
90 REM Mathematical range: -2*PI to 2*PI
100 MATHXMIN = -2 * PI
110 MATHXMAX = 2 * PI
120 MATHYMIN = -3
130 MATHYMAX = 3
140 REM Scale factors
150 XSCALE = (XMAX - XMIN) / (MATHXMAX - MATHXMIN)
160 YSCALE = (YMAX - YMIN) / (MATHYMAX - MATHYMIN)
170 REM Draw axes
180 GOSUB 1000
190 REM Draw grid
200 GOSUB 1200
210 REM Draw functions
220 GOSUB 2000
230 REM Draw legend
240 GOSUB 3000
250 REM Wait for key
260 LOCATE 1, 1: COLOR 15
270 PRINT "Trigonometric Functions - Press any key";
280 A$ = INKEY$: IF A$ = "" THEN 280
290 SCREEN 0: END
1000 REM === Draw Axes ===
1010 COLOR 15
1020 REM X axis
1030 LINE (XMIN, YCENTER)-(XMAX, YCENTER), 15
1040 REM Arrow on X axis
1050 LINE (XMAX, YCENTER)-(XMAX - 10, YCENTER - 5), 15
1060 LINE (XMAX, YCENTER)-(XMAX - 10, YCENTER + 5), 15
1070 REM Y axis
1080 LINE (XCENTER, YMIN)-(XCENTER, YMAX), 15
1090 REM Arrow on Y axis
1100 LINE (XCENTER, YMIN)-(XCENTER - 5, YMIN + 10), 15
1110 LINE (XCENTER, YMIN)-(XCENTER + 5, YMIN + 10), 15
1120 REM Axis labels
1130 LOCATE 2, 42: PRINT "Y";
1140 LOCATE 12, 75: PRINT "X";
1150 RETURN
1200 REM === Draw Grid and Scale Marks ===
1210 COLOR 8
1220 REM Vertical grid lines at PI intervals
1230 FOR I = -2 TO 2
1240   IF I = 0 THEN 1300
1250   MX = I * PI
1260   SX = XCENTER + MX * XSCALE
1270   LINE (SX, YMIN)-(SX, YMAX), 8
1280   LOCATE 23, (SX / 8): COLOR 7
1290   IF I = -2 THEN PRINT "-2p";
1291   IF I = -1 THEN PRINT "-p";
1292   IF I = 1 THEN PRINT "p";
1293   IF I = 2 THEN PRINT "2p";
1300 NEXT I
1310 REM Horizontal grid lines
1320 FOR I = -3 TO 3
1330   IF I = 0 THEN 1370
1340   SY = YCENTER - I * YSCALE
1350   LINE (XMIN, SY)-(XMAX, SY), 8
1360   LOCATE (SY / 14) + 1, 38: COLOR 7: PRINT USING "+#"; I
1370 NEXT I
1380 RETURN
2000 REM === Draw Functions ===
2010 REM Loop through X values
2020 FOR SX = XMIN TO XMAX
2030   REM Convert screen X to math X
2040   MX = MATHXMIN + (SX - XMIN) / XSCALE
2050   REM --- SIN (Yellow) ---
2060   MY = SIN(MX)
2070   IF MY < MATHYMIN OR MY > MATHYMAX THEN 2100
2080   SY = YCENTER - MY * YSCALE
2090   IF SX = XMIN THEN PSET (SX, SY), 14 ELSE LINE -(SX, SY), 14
2100   REM --- COS (Cyan) ---
2110   MY = COS(MX)
2120   IF MY < MATHYMIN OR MY > MATHYMAX THEN 2150
2130   SY = YCENTER - MY * YSCALE
2140   PSET (SX, SY), 11
2150   REM --- TAN (Green) ---
2160   C = COS(MX)
2170   IF ABS(C) < 0.05 THEN 2210
2180   MY = TAN(MX)
2190   IF MY < MATHYMIN OR MY > MATHYMAX THEN 2210
2200   SY = YCENTER - MY * YSCALE: PSET (SX, SY), 10
2210   REM --- COT (Magenta) ---
2220   S = SIN(MX)
2230   IF ABS(S) < 0.05 THEN 2270
2240   MY = COS(MX) / S
2250   IF MY < MATHYMIN OR MY > MATHYMAX THEN 2270
2260   SY = YCENTER - MY * YSCALE: PSET (SX, SY), 13
2270   REM --- SEC (Red) ---
2280   C = COS(MX)
2290   IF ABS(C) < 0.1 THEN 2330
2300   MY = 1 / C
2310   IF MY < MATHYMIN OR MY > MATHYMAX THEN 2330
2320   SY = YCENTER - MY * YSCALE: PSET (SX, SY), 12
2330   REM --- CSC (Blue) ---
2340   S = SIN(MX)
2350   IF ABS(S) < 0.1 THEN 2390
2360   MY = 1 / S
2370   IF MY < MATHYMIN OR MY > MATHYMAX THEN 2390
2380   SY = YCENTER - MY * YSCALE: PSET (SX, SY), 9
2390 NEXT SX
2400 RETURN
3000 REM === Draw Legend ===
3010 LX = 500: LY = 40
3020 LINE (LX - 5, LY - 5)-(LX + 90, LY + 85), 7, B
3030 REM SIN
3040 LINE (LX, LY)-(LX + 20, LY), 14
3050 LOCATE 4, 67: COLOR 14: PRINT "SIN";
3060 REM COS
3070 LINE (LX, LY + 12)-(LX + 20, LY + 12), 11
3080 LOCATE 5, 67: COLOR 11: PRINT "COS";
3090 REM TAN
3100 LINE (LX, LY + 24)-(LX + 20, LY + 24), 10
3110 LOCATE 6, 67: COLOR 10: PRINT "TAN";
3120 REM COT
3130 LINE (LX, LY + 36)-(LX + 20, LY + 36), 13
3140 LOCATE 7, 67: COLOR 13: PRINT "COT";
3150 REM SEC
3160 LINE (LX, LY + 48)-(LX + 20, LY + 48), 12
3170 LOCATE 8, 67: COLOR 12: PRINT "SEC";
3180 REM CSC
3190 LINE (LX, LY + 60)-(LX + 20, LY + 60), 9
3200 LOCATE 9, 67: COLOR 9: PRINT "CSC";
3210 RETURN
""";

    private const string Cube3D = """
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
""";

    private const string Fern = """
10 REM *** Barnsley Fern Fractal ***
20 REM *** Iterated Function System ***
30 SCREEN 9: CLS
40 RANDOMIZE TIMER
50 REM Starting point
60 X = 0: Y = 0
70 REM Draw 50000 points
80 FOR I = 1 TO 50000
90   REM Random transformation selection
100  R = RND * 100
110  IF R >= 1 THEN 160
120  REM Transformation 1: Stem (1%)
130  NEWX = 0
140  NEWY = 0.16 * Y
150  GOTO 310
160  IF R >= 86 THEN 210
170  REM Transformation 2: Main leaflets (85%)
180  NEWX = 0.85 * X + 0.04 * Y
190  NEWY = -0.04 * X + 0.85 * Y + 1.6
200  GOTO 310
210  IF R >= 93 THEN 260
220  REM Transformation 3: Left leaflet (7%)
230  NEWX = 0.2 * X - 0.26 * Y
240  NEWY = 0.23 * X + 0.22 * Y + 1.6
250  GOTO 310
260  REM Transformation 4: Right leaflet (7%)
270  NEWX = -0.15 * X + 0.28 * Y
280  NEWY = 0.26 * X + 0.24 * Y + 0.44
310  REM Update position
320  X = NEWX: Y = NEWY
330  REM Map to screen coordinates
340  PX = 320 + X * 60
350  PY = 349 - Y * 35
360  REM Draw point with gradient color
370  C = INT(Y * 1.5) MOD 6 + 2
380  IF PX < 0 OR PX > 639 OR PY < 0 OR PY > 349 THEN 400
390  PSET (PX, PY), C
400 NEXT I
410 LOCATE 1, 1: PRINT "Barnsley Fern - Press any key"
420 A$ = INKEY$: IF A$ = "" THEN 420
430 END
""";

    private const string Fire = """
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
""";

    private const string Fireworks = """
10 REM *** Fireworks Animation ***
20 SCREEN 9: CLS
30 MAXPARTS = 50
40 DIM PX(50), PY(50), VX(50), VY(50), PC(50), LIFE(50), ACTIVE(50)
50 RANDOMIZE TIMER
60 REM Initialize particles as inactive
70 FOR I = 1 TO MAXPARTS: ACTIVE(I) = 0: NEXT I
80 GRAVITY = 0.15
90 LAUNCHTIME = 0
100 REM Main loop
110 K$ = INKEY$: IF K$ <> "" THEN 500
120 REM Erase old particles
130 FOR I = 1 TO MAXPARTS
140   IF ACTIVE(I) = 0 THEN 160
150   PSET (PX(I), PY(I)), 0
160 NEXT I
170 REM Launch new firework periodically
180 LAUNCHTIME = LAUNCHTIME + 1
190 IF LAUNCHTIME <= 30 THEN 310
200 LAUNCHTIME = 0
210 REM Explosion center
220 EX = 100 + INT(RND * 440)
230 EY = 50 + INT(RND * 150)
240 EC = INT(RND * 14) + 1
250 REM Create particles
260 FOR I = 1 TO MAXPARTS
270   IF ACTIVE(I) = 1 THEN 305
280   ACTIVE(I) = 1
290   PX(I) = EX: PY(I) = EY
295   REM Random velocity in all directions
296   ANGLE = RND * 6.28318
297   SPEED = 2 + RND * 5
298   VX(I) = COS(ANGLE) * SPEED
299   VY(I) = SIN(ANGLE) * SPEED
300   PC(I) = EC
301   LIFE(I) = 40 + INT(RND * 40)
305 NEXT I
310 REM Update and draw particles
320 FOR I = 1 TO MAXPARTS
330   IF ACTIVE(I) = 0 THEN 440
340   REM Apply gravity
350   VY(I) = VY(I) + GRAVITY
360   REM Update position
370   PX(I) = PX(I) + VX(I)
380   PY(I) = PY(I) + VY(I)
390   REM Decrease life
400   LIFE(I) = LIFE(I) - 1
410   REM Deactivate if dead or off screen
420   IF LIFE(I) <= 0 OR PY(I) > 340 OR PX(I) < 0 OR PX(I) > 639 THEN ACTIVE(I) = 0: GOTO 440
430   REM Draw with fade
435   C = PC(I): IF LIFE(I) <= 30 THEN C = 8: IF LIFE(I) <= 15 THEN C = 7
437   PSET (PX(I), PY(I)), C
440 NEXT I
450 REM Ground line
460 LINE (0, 345)-(639, 345), 6
470 GOTO 110
500 SCREEN 0: END
""";

    private const string Lissajous = """
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
""";

    private const string Mandala = """
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
""";

    private const string Mandelbrot = """
10 REM *** Mandelbrot Set Fractal ***
20 REM *** GW-BASIC / EGA 640x350 ***
30 SCREEN 9
40 CLS
50 REM Screen dimensions
60 XMAX = 639: YMAX = 349
70 REM Mandelbrot region
80 XMIN2 = -2.5: XMAX2 = 1
90 YMIN2 = -1.25: YMAX2 = 1.25
100 REM Calculate scale
110 XSCALE = (XMAX2 - XMIN2) / XMAX
120 YSCALE = (YMAX2 - YMIN2) / YMAX
130 MAXITER = 32
140 REM Main loop
150 FOR PY = 0 TO YMAX
160   FOR PX = 0 TO XMAX STEP 2
170     REM Map pixel to complex plane
180     X0 = XMIN2 + PX * XSCALE
190     Y0 = YMIN2 + PY * YSCALE
200     REM Mandelbrot iteration
210     X = 0: Y = 0
220     ITER = 0
230     REM WHILE loop replacement
240     IF (X * X + Y * Y >= 4) OR (ITER >= MAXITER) THEN 290
250     XTEMP = X * X - Y * Y + X0
260     Y = 2 * X * Y + Y0
270     X = XTEMP
280     ITER = ITER + 1: GOTO 240
290     REM Color based on iterations
300     IF ITER = MAXITER THEN C = 0 ELSE C = (ITER MOD 15) + 1
310     PSET (PX, PY), C
320   NEXT PX
330 NEXT PY
340 REM Wait for keypress
350 LOCATE 1, 1: PRINT "Mandelbrot Set - Press any key"
360 A$ = INKEY$: IF A$ = "" THEN 360
370 END
""";

    private const string Matrix = """
10 REM *** Matrix Digital Rain ***
20 SCREEN 9: CLS
30 NUMCOLS = 40
40 DIM COLY(40), COLSPD(40), COLLEN(40)
50 RANDOMIZE TIMER
60 REM Initialize columns
70 FOR I = 1 TO NUMCOLS
80   COLY(I) = -INT(RND * 25)
90   COLSPD(I) = 1 + INT(RND * 2)
100  COLLEN(I) = 5 + INT(RND * 15)
110 NEXT I
120 REM Define characters
130 CHARS$ = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ@#$%&*+=<>?"
140 REM Main loop
150 K$ = INKEY$: IF K$ <> "" THEN 400
160 FOR I = 1 TO NUMCOLS
170   COL = I * 2
180   REM Draw trail
190   FOR J = 0 TO COLLEN(I)
200     ROW = COLY(I) - J
210     IF ROW < 1 OR ROW > 25 THEN 280
220     REM Pick random character
230     CH$ = MID$(CHARS$, INT(RND * LEN(CHARS$)) + 1, 1)
240     REM Color: bright head, fading tail
250     C = 2: IF J < 3 THEN C = 10: IF J = 0 THEN C = 15
260     LOCATE ROW, COL: COLOR C, 0
270     PRINT CH$;
280   NEXT J
290   REM Erase tail end
300   TAILROW = COLY(I) - COLLEN(I) - 1
310   IF TAILROW < 1 OR TAILROW > 25 THEN 340
320   LOCATE TAILROW, COL
330   PRINT " ";
340   REM Move column down
350   COLY(I) = COLY(I) + COLSPD(I)
360   REM Reset when off screen
370   IF COLY(I) - COLLEN(I) <= 26 THEN 390
375   COLY(I) = -INT(RND * 10)
380   COLSPD(I) = 1 + INT(RND * 2): COLLEN(I) = 5 + INT(RND * 15)
390 NEXT I
395 FOR D = 1 TO 3000: NEXT D
397 GOTO 150
400 COLOR 7, 0: SCREEN 0: END
""";

    private const string Moire = """
10 REM *** Moire Pattern ***
20 SCREEN 9
30 CLS
40 FOR I = 10 TO 300 STEP 5
50   CIRCLE (320, 175), I, 15
60   CIRCLE (350, 175), I, 14
70 NEXT I
80 A$ = INKEY$: IF A$ = "" THEN 80
90 END
""";

    private const string Plasma = """
10 REM *** Animated Plasma Effect ***
20 REM *** With Double Buffering ***
30 SCREEN 9,,0,1: CLS
40 T = 0
50 REM Precompute sine table for speed
60 DIM SINTAB(360)
70 FOR I = 0 TO 359
80   SINTAB(I) = SIN(I * 3.14159 / 180) * 7
90 NEXT I
100 PAGE = 0
110 REM Main loop
120 K$ = INKEY$: IF K$ <> "" THEN 330
130 REM Switch to draw on back buffer
140 IF PAGE = 0 THEN SCREEN 9,,0,1 ELSE SCREEN 9,,1,0
150 FOR Y = 0 TO 349 STEP 4
160   FOR X = 0 TO 639 STEP 4
170     REM Plasma formula with multiple sine waves
180     A1 = INT(X / 2 + T * 3) MOD 360
190     A2 = INT(Y / 2 + T * 2) MOD 360
200     A3 = INT((X + Y) / 4 + T * 4) MOD 360
210     IF A1 < 0 THEN A1 = A1 + 360
220     IF A2 < 0 THEN A2 = A2 + 360
230     IF A3 < 0 THEN A3 = A3 + 360
240     V = SINTAB(A1) + SINTAB(A2) + SINTAB(A3)
250     C = INT(V + 8)
260     IF C < 0 THEN C = 0
270     IF C > 15 THEN C = 15
280     LINE (X, Y)-(X + 3, Y + 3), C, BF
290   NEXT X
300 NEXT Y
310 REM Flip pages - show what we just drew
320 IF PAGE = 0 THEN SCREEN 9,,1,0 ELSE SCREEN 9,,0,1
330 PAGE = 1 - PAGE
340 T = T + 2
350 IF T > 360 THEN T = T - 360
360 GOTO 120
370 SCREEN 0: END
""";

    private const string Rose = """
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
""";

    private const string Sierpinski = """
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
""";

    private const string Spiral = """
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
""";

    private const string Starburst = """
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
""";

    private const string Starfield = """
10 REM *** 3D Starfield ***
20 REM *** With Double Buffering ***
30 SCREEN 9,,0,1
40 DIM SX(100), SY(100), SZ(100)
50 RANDOMIZE TIMER
60 FOR I = 1 TO 100
70   SX(I) = RND * 640 - 320
80   SY(I) = RND * 350 - 175
90   SZ(I) = RND * 100 + 1
100 NEXT I
110 PAGE = 0
120 REM Main loop
130 K$ = INKEY$: IF K$ <> "" THEN 300
140 REM Switch to draw on back buffer
150 IF PAGE = 0 THEN SCREEN 9,,0,1 ELSE SCREEN 9,,1,0
160 CLS
170 FOR I = 1 TO 100
180   SZ(I) = SZ(I) - 2
190   IF SZ(I) < 1 THEN SZ(I) = 100: SX(I) = RND * 640 - 320: SY(I) = RND * 350 - 175
200   X = 320 + SX(I) * 100 / SZ(I)
210   Y = 175 + SY(I) * 100 / SZ(I)
220   IF X > 0 AND X < 639 AND Y > 0 AND Y < 349 THEN
230     C = 15 - INT(SZ(I) / 10)
240     IF C < 1 THEN C = 1
250     PSET (X, Y), C
260   END IF
270 NEXT I
280 REM Flip pages - show what we just drew
290 IF PAGE = 0 THEN SCREEN 9,,1,0 ELSE SCREEN 9,,0,1
300 PAGE = 1 - PAGE
310 GOTO 130
320 SCREEN 0: END
""";

    private const string Tunnel = """
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
""";

    private const string Waves = """
10 REM *** Animated Sine Waves ***
20 SCREEN 9
30 FOR F = 0 TO 200
40   CLS
50   FOR W = 1 TO 5
60     C = W + 9
70     FOR X = 0 TO 639
80       Y = 175 + 50 * SIN(X / 30 + F / 10 + W) * SIN(W)
90       PSET (X, Y), C
100    NEXT X
110  NEXT W
120 NEXT F
130 END
""";
}
