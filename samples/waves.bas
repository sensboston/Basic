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
