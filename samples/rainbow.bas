10 REM *** Rainbow Bars Demo ***
20 REM *** SCREEN 15: 640x480 24-bit RGB ***
30 SCREEN 15: CLS
40 REM Draw horizontal rainbow gradient using lines (fast)
50 FOR Y = 0 TO 479
60   REM Calculate hue based on Y position
70   HUE = (Y * 360) / 480
80   GOSUB 500
90   LINE (0, Y)-(639, Y), RGB(R, G, B)
100 NEXT Y
110 REM Draw white circles
120 FOR I = 1 TO 5
130   CX = 64 + (I - 1) * 128
140   CIRCLE (CX, 240), 50, RGB(255, 255, 255)
150 NEXT I
160 LOCATE 1, 1: COLOR 15
170 PRINT "SCREEN 15: 24-bit RGB Rainbow";
180 A$ = INKEY$: IF A$ = "" THEN 180
190 SCREEN 0: END
500 REM HSV to RGB (S=1, V=1)
510 REM Input: HUE (0-359), Output: R, G, B
520 H = HUE / 60
530 I = INT(H)
540 F = H - I
550 Q = INT(255 * (1 - F))
560 T = INT(255 * F)
570 IF I = 0 THEN R = 255: G = T: B = 0: RETURN
580 IF I = 1 THEN R = Q: G = 255: B = 0: RETURN
590 IF I = 2 THEN R = 0: G = 255: B = T: RETURN
600 IF I = 3 THEN R = 0: G = Q: B = 255: RETURN
610 IF I = 4 THEN R = T: G = 0: B = 255: RETURN
620 R = 255: G = 0: B = Q: RETURN
