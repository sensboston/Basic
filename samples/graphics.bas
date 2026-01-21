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
