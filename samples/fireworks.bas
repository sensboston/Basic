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
