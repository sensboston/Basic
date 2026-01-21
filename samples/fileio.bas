10 REM File I/O Demo for SharpBasic Interpreter
20 PRINT "File I/O Demo"
30 PRINT "============="
40 PRINT
50 REM Write data to a file
60 PRINT "Writing to test.dat..."
70 OPEN "test.dat" FOR OUTPUT AS #1
80 FOR I = 1 TO 5
90 PRINT #1, "Line"; I; " - Data value:"; I * 10
100 NEXT I
110 CLOSE #1
120 PRINT "Done writing!"
130 PRINT
140 REM Read data back from file
150 PRINT "Reading from test.dat..."
160 OPEN "test.dat" FOR INPUT AS #1
170 IF EOF(1) THEN GOTO 210
180 INPUT #1, LINE$
190 PRINT LINE$
200 GOTO 170
210 CLOSE #1
220 PRINT
230 PRINT "File I/O Demo Complete!"
240 END
