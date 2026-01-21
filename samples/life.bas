10 REM *** Conway's Game of Life - Text Mode ***
20 REM
30 REM Controls:
40 REM   Arrow keys - Move cursor
50 REM   Space      - Toggle cell
60 REM   Enter      - Start/Stop simulation
70 REM   R          - Randomize grid
80 REM   C          - Clear grid
90 REM   G          - Add Glider at cursor
100 REM  Q/Esc      - Quit
110 REM
120 CLS
130 REM Grid parameters (leave room for status)
140 GW = 78: REM Grid width
150 GH = 22: REM Grid height (25 - 3 for status)
160 DIM C(GW, GH), N(GW, GH)
170 REM Cursor position (1-based for LOCATE)
180 CX = GW / 2: CY = GH / 2
190 SIM = 0: REM 0=Edit mode, 1=Running
200 GEN = 0
210 REM Draw border
220 LOCATE 1, 1: PRINT STRING$(80, "-");
230 FOR Y = 2 TO GH + 1
240   LOCATE Y, 1: PRINT "|";
250   LOCATE Y, 80: PRINT "|";
260 NEXT Y
270 LOCATE GH + 2, 1: PRINT STRING$(80, "-");
280 GOSUB 2000: REM Draw status
290 REM *** Main Loop ***
300 IF SIM = 1 THEN GOSUB 1000: REM Run generation
310 GOSUB 1500: REM Draw cursor
320 REM Check keyboard
330 K$ = INKEY$
340 IF K$ = "" THEN 300
350 REM Process key
360 GOSUB 1600: REM Restore cell before moving
370 IF K$ = CHR$(27) OR K$ = "q" OR K$ = "Q" THEN 950
380 IF K$ = " " THEN GOSUB 500: REM Toggle cell
390 IF K$ = CHR$(13) THEN SIM = 1 - SIM: GEN = 0: GOSUB 2000
400 IF K$ = "r" OR K$ = "R" THEN GOSUB 600: REM Randomize
410 IF K$ = "c" OR K$ = "C" THEN GOSUB 700: REM Clear
420 IF K$ = "g" OR K$ = "G" THEN GOSUB 800: REM Add glider
430 REM Arrow keys (extended codes)
440 IF LEN(K$) = 2 THEN K2 = ASC(RIGHT$(K$, 1)) ELSE 480
450 IF K2 = 72 AND CY > 1 THEN CY = CY - 1: REM Up
460 IF K2 = 80 AND CY < GH THEN CY = CY + 1: REM Down
470 IF K2 = 75 AND CX > 1 THEN CX = CX - 1: REM Left
475 IF K2 = 77 AND CX < GW THEN CX = CX + 1: REM Right
480 GOTO 300
490 REM
500 REM *** Toggle Cell ***
510 C(CX, CY) = 1 - C(CX, CY)
520 RETURN
530 REM
600 REM *** Randomize Grid ***
610 RANDOMIZE TIMER
620 FOR Y = 1 TO GH
630   FOR X = 1 TO GW
640     IF RND > 0.75 THEN C(X, Y) = 1 ELSE C(X, Y) = 0
650     LOCATE Y + 1, X + 1
660     IF C(X, Y) = 1 THEN PRINT CHR$(254); ELSE PRINT " ";
670   NEXT X
680 NEXT Y
690 GEN = 0: GOSUB 2000
695 RETURN
700 REM *** Clear Grid ***
710 FOR Y = 1 TO GH
720   FOR X = 1 TO GW
730     C(X, Y) = 0
740   NEXT X
750   LOCATE Y + 1, 2: PRINT STRING$(GW, " ");
760 NEXT Y
770 GEN = 0: GOSUB 2000
780 RETURN
790 REM
800 REM *** Add Glider at cursor ***
810 IF CX < GW - 2 AND CY < GH - 2 THEN
820   C(CX + 1, CY) = 1
830   C(CX + 2, CY + 1) = 1
840   C(CX, CY + 2) = 1: C(CX + 1, CY + 2) = 1: C(CX + 2, CY + 2) = 1
850   FOR DY = 0 TO 2
860     FOR DX = 0 TO 2
870       LOCATE CY + DY + 1, CX + DX + 1
880       IF C(CX + DX, CY + DY) = 1 THEN PRINT CHR$(254); ELSE PRINT " ";
890     NEXT DX
900   NEXT DY
910 END IF
920 RETURN
930 REM
940 REM *** Exit ***
950 CLS: END
960 REM
1000 REM *** Calculate Next Generation ***
1010 GEN = GEN + 1
1020 FOR Y = 2 TO GH - 1
1030   FOR X = 2 TO GW - 1
1040     REM Count neighbors
1050     NB = 0
1060     FOR DY = -1 TO 1
1070       FOR DX = -1 TO 1
1080         IF DX = 0 AND DY = 0 THEN 1100
1090         NB = NB + C(X + DX, Y + DY)
1100       NEXT DX
1110     NEXT DY
1120     REM Apply rules
1130     IF C(X, Y) = 1 THEN
1140       IF NB < 2 OR NB > 3 THEN N(X, Y) = 0 ELSE N(X, Y) = 1
1150     ELSE
1160       IF NB = 3 THEN N(X, Y) = 1 ELSE N(X, Y) = 0
1170     END IF
1180   NEXT X
1190 NEXT Y
1200 REM Update display (only changed cells)
1210 FOR Y = 2 TO GH - 1
1220   FOR X = 2 TO GW - 1
1230     IF C(X, Y) <> N(X, Y) THEN
1240       C(X, Y) = N(X, Y)
1250       LOCATE Y + 1, X + 1
1260       IF N(X, Y) = 1 THEN PRINT CHR$(254); ELSE PRINT " ";
1270     END IF
1280   NEXT X
1290 NEXT Y
1300 GOSUB 2000
1310 RETURN
1400 REM
1500 REM *** Draw Cursor (highlight) ***
1510 LOCATE CY + 1, CX + 1
1520 COLOR 0, 7: REM Inverse
1530 IF C(CX, CY) = 1 THEN PRINT CHR$(254); ELSE PRINT " ";
1540 COLOR 7, 0: REM Normal
1550 RETURN
1560 REM
1600 REM *** Restore Cell (remove highlight) ***
1610 LOCATE CY + 1, CX + 1
1620 COLOR 7, 0
1630 IF C(CX, CY) = 1 THEN PRINT CHR$(254); ELSE PRINT " ";
1640 RETURN
1700 REM
2000 REM *** Draw Status Bar ***
2010 COLOR 15, 0
2020 LOCATE 24, 1
2030 IF SIM = 0 THEN PRINT "EDIT"; ELSE PRINT "RUN ";
2040 LOCATE 24, 7: PRINT "Gen:"; GEN; "  ";
2050 LOCATE 24, 20: PRINT "Pos:"; CX; ","; CY; "   ";
2060 LOCATE 25, 1
2070 IF SIM = 0 THEN PRINT "[Spc]Toggle [Enter]Run [R]andom [C]lear [G]lider [Q]uit";
2080 IF SIM = 1 THEN PRINT "[Enter]Stop [Q]uit                                     ";
2090 COLOR 7, 0
2100 RETURN
