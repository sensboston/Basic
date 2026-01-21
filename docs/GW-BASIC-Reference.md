# GW-BASIC Complete Reference

Полный список команд, операторов, функций и переменных GW-BASIC.

---

## 1. Commands (Команды редактора/среды)

| Command | Description |
|---------|-------------|
| AUTO | Automatic line numbering |
| CONT | Continue execution after break |
| DELETE | Delete program lines |
| EDIT | Edit a program line |
| FILES | Display directory listing |
| LIST | List program lines |
| LLIST | List program to printer |
| LOAD | Load program from file |
| MERGE | Merge program file with current |
| NEW | Clear program from memory |
| RENUM | Renumber program lines |
| RUN | Execute program |
| SAVE | Save program to file |
| SYSTEM | Exit to operating system |

---

## 2. Statements (Операторы)

### 2.1 Program Flow (Управление потоком)

| Statement | Description |
|-----------|-------------|
| CALL | Call assembly language subroutine |
| CHAIN | Load and run another program |
| END | End program execution |
| FOR...NEXT | Loop construct |
| GOSUB | Call subroutine |
| GOTO | Unconditional jump |
| IF...THEN...ELSE | Conditional execution |
| ON...GOSUB | Computed GOSUB |
| ON...GOTO | Computed GOTO |
| RETURN | Return from subroutine |
| STOP | Stop execution (can CONT) |
| WHILE...WEND | While loop |

### 2.2 Variables & Data (Переменные и данные)

| Statement | Description |
|-----------|-------------|
| CLEAR | Clear variables and set memory |
| COMMON | Pass variables to CHAINed program |
| DATA | Define inline data |
| DEF FN | Define user function |
| DEFDBL | Define default double precision |
| DEFINT | Define default integer |
| DEFSNG | Define default single precision |
| DEFSTR | Define default string |
| DIM | Dimension arrays |
| ERASE | Erase arrays |
| LET | Assign value to variable |
| OPTION BASE | Set array base (0 or 1) |
| READ | Read DATA values |
| RESTORE | Reset DATA pointer |
| SWAP | Swap two variables |

### 2.3 Input/Output (Ввод/Вывод)

| Statement | Description |
|-----------|-------------|
| BEEP | Sound the speaker |
| CLS | Clear screen |
| COLOR | Set screen colors |
| INPUT | Get input from keyboard |
| INPUT# | Read from file |
| KEY | Define function keys |
| LINE INPUT | Input entire line |
| LINE INPUT# | Read line from file |
| LOCATE | Position cursor |
| LPRINT | Print to printer |
| LPRINT USING | Formatted print to printer |
| PRINT | Output to screen |
| PRINT USING | Formatted output |
| PRINT# | Write to file |
| PRINT# USING | Formatted write to file |
| SPC | Print spaces |
| TAB | Tab to column |
| VIEW PRINT | Set text viewport |
| WIDTH | Set screen/printer width |
| WRITE | Output with delimiters |
| WRITE# | Write to file with delimiters |

### 2.4 Graphics (Графика)

| Statement | Description |
|-----------|-------------|
| CIRCLE | Draw circle/ellipse/arc |
| DRAW | Draw using graphics macro language |
| GET (graphics) | Capture screen area to array |
| LINE | Draw line or box |
| PAINT | Fill area with color |
| PALETTE | Set color palette |
| PALETTE USING | Set palette from array |
| PCOPY | Copy video pages |
| PSET | Set pixel |
| PRESET | Reset pixel (background color) |
| PUT (graphics) | Display graphics array |
| SCREEN | Set screen mode |
| VIEW | Set graphics viewport |
| WINDOW | Define logical coordinates |

### 2.5 Sound (Звук)

| Statement | Description |
|-----------|-------------|
| BEEP | Sound 800Hz for 0.25 sec |
| PLAY | Play music (macro language) |
| SOUND | Generate tone (freq, duration) |

### 2.6 File I/O (Файловый ввод/вывод)

| Statement | Description |
|-----------|-------------|
| BLOAD | Load binary file to memory |
| BSAVE | Save memory to binary file |
| CHDIR | Change directory |
| CLOSE | Close file(s) |
| EOF | End of file function |
| FIELD | Define random file fields |
| GET (file) | Read random file record |
| KILL | Delete file |
| LOCK | Lock file/records |
| LSET | Left-justify in field |
| MKDIR | Create directory |
| NAME | Rename file |
| OPEN | Open file |
| PUT (file) | Write random file record |
| RESET | Close all files |
| RMDIR | Remove directory |
| RSET | Right-justify in field |
| UNLOCK | Unlock file/records |

### 2.7 Error Handling (Обработка ошибок)

| Statement | Description |
|-----------|-------------|
| ERROR | Simulate error |
| ON ERROR GOTO | Set error handler |
| RESUME | Resume after error |

### 2.8 Event Trapping (Перехват событий)

| Statement | Description |
|-----------|-------------|
| COM | Enable/disable COM port trapping |
| KEY(n) | Enable/disable key trapping |
| ON COM | Set COM port event handler |
| ON KEY | Set key event handler |
| ON PEN | Set light pen event handler |
| ON PLAY | Set music buffer event handler |
| ON STRIG | Set joystick event handler |
| ON TIMER | Set timer event handler |
| PEN | Enable/disable light pen |
| STRIG | Enable/disable joystick |
| TIMER | Enable/disable timer |

### 2.9 Memory & Machine (Память и машинный код)

| Statement | Description |
|-----------|-------------|
| BLOAD | Load binary to memory |
| BSAVE | Save memory to binary |
| CALL | Call machine language |
| DEF SEG | Set segment address |
| DEF USR | Define USR function address |
| OUT | Output byte to port |
| POKE | Write byte to memory |
| USR | Call user routine |
| WAIT | Wait for port status |

### 2.10 Miscellaneous (Прочее)

| Statement | Description |
|-----------|-------------|
| REM | Remark/comment |
| ' | Comment (apostrophe) |
| TRON | Trace on |
| TROFF | Trace off |
| ENVIRON | Set environment variable |
| IOCTL | Device control |
| MOTOR | Control cassette motor |
| RANDOMIZE | Seed random generator |
| SHELL | Execute DOS command |

---

## 3. Functions (Функции)

### 3.1 Mathematical (Математические)

| Function | Description |
|----------|-------------|
| ABS(n) | Absolute value |
| ATN(n) | Arctangent (radians) |
| COS(n) | Cosine (radians) |
| EXP(n) | e raised to power n |
| FIX(n) | Truncate to integer |
| INT(n) | Integer (floor) |
| LOG(n) | Natural logarithm |
| RND | Random number (0-1) |
| SGN(n) | Sign (-1, 0, 1) |
| SIN(n) | Sine (radians) |
| SQR(n) | Square root |
| TAN(n) | Tangent (radians) |

### 3.2 String (Строковые)

| Function | Description |
|----------|-------------|
| ASC(s$) | ASCII code of first char |
| CHR$(n) | Character from ASCII code |
| HEX$(n) | Hexadecimal string |
| INKEY$ | Read keyboard buffer |
| INPUT$(n,#f) | Read n chars from file/keyboard |
| INSTR(s$,t$) | Find substring position |
| LEFT$(s$,n) | Left n characters |
| LEN(s$) | String length |
| MID$(s$,p,n) | Substring |
| OCT$(n) | Octal string |
| RIGHT$(s$,n) | Right n characters |
| SPACE$(n) | String of n spaces |
| STR$(n) | Number to string |
| STRING$(n,c) | String of repeated char |
| VAL(s$) | String to number |

### 3.3 Type Conversion (Преобразование типов)

| Function | Description |
|----------|-------------|
| CDBL(n) | Convert to double precision |
| CINT(n) | Convert to integer (rounded) |
| CSNG(n) | Convert to single precision |
| CVD(s$) | String to double (from file) |
| CVI(s$) | String to integer (from file) |
| CVS(s$) | String to single (from file) |
| MKD$(n) | Double to string (for file) |
| MKI$(n) | Integer to string (for file) |
| MKS$(n) | Single to string (for file) |

### 3.4 Screen & Graphics (Экран и графика)

| Function | Description |
|----------|-------------|
| CSRLIN | Current cursor row |
| POINT(x,y) | Pixel color at x,y |
| POS(0) | Current cursor column |
| SCREEN(r,c) | Character at row,col |

### 3.5 File & Device (Файлы и устройства)

| Function | Description |
|----------|-------------|
| EOF(n) | End of file test |
| ERDEV | Device error code |
| ERDEV$ | Device error name |
| EXTERR(n) | Extended DOS error |
| IOCTL$(n) | Device control string |
| LOC(n) | Current file position |
| LOF(n) | Length of file |
| LPOS(n) | Printer position |

### 3.6 Error (Ошибки)

| Function | Description |
|----------|-------------|
| ERL | Error line number |
| ERR | Error code |

### 3.7 Input Device (Устройства ввода)

| Function | Description |
|----------|-------------|
| INP(n) | Read byte from port |
| PEN(n) | Light pen information |
| STICK(n) | Joystick position |
| STRIG(n) | Joystick button status |

### 3.8 Memory (Память)

| Function | Description |
|----------|-------------|
| FRE(x) | Free memory |
| PEEK(n) | Read byte from memory |
| VARPTR(v) | Variable address |
| VARPTR$(v) | Variable address string |

### 3.9 Miscellaneous (Прочие)

| Function | Description |
|----------|-------------|
| ENVIRON$(s$) | Environment variable |
| TIMER | Seconds since midnight |

---

## 4. System Variables (Системные переменные)

| Variable | Description |
|----------|-------------|
| DATE$ | Current date |
| TIME$ | Current time |
| INKEY$ | Keyboard buffer (function-like) |

---

## 5. Operators (Операторы)

### 5.1 Arithmetic (Арифметические)

| Operator | Description |
|----------|-------------|
| + | Addition |
| - | Subtraction |
| * | Multiplication |
| / | Division |
| \ | Integer division |
| MOD | Modulo (remainder) |
| ^ | Exponentiation |

### 5.2 Relational (Сравнения)

| Operator | Description |
|----------|-------------|
| = | Equal |
| <> | Not equal |
| < | Less than |
| > | Greater than |
| <= | Less or equal |
| >= | Greater or equal |

### 5.3 Logical (Логические)

| Operator | Description |
|----------|-------------|
| AND | Logical AND |
| OR | Logical OR |
| NOT | Logical NOT |
| XOR | Exclusive OR |
| EQV | Equivalence |
| IMP | Implication |

### 5.4 String (Строковый)

| Operator | Description |
|----------|-------------|
| + | Concatenation |

---

## 6. SCREEN Modes (Режимы экрана)

| Mode | Type | Resolution | Colors | Pages |
|------|------|------------|--------|-------|
| 0 | Text | 40x25 or 80x25 | 16 | 1-8 |
| 1 | Graphics | 320x200 | 4 | 1 |
| 2 | Graphics | 640x200 | 2 | 1 |
| 7 | Graphics | 320x200 | 16 | 2-8 |
| 8 | Graphics | 640x200 | 16 | 1-4 |
| 9 | Graphics | 640x350 | 16 | 1-2 |
| 10 | Graphics | 640x350 | 4 | 1-2 |
| 11 | Graphics | 640x480 | 2 | 1 |
| 12 | Graphics | 640x480 | 16 | 1 |
| 13 | Graphics | 320x200 | 256 | 1 |

---

## 7. DRAW Command Codes (Коды команды DRAW)

| Code | Description |
|------|-------------|
| U n | Up n pixels |
| D n | Down n pixels |
| L n | Left n pixels |
| R n | Right n pixels |
| E n | Diagonal up-right |
| F n | Diagonal down-right |
| G n | Diagonal down-left |
| H n | Diagonal up-left |
| M x,y | Move to x,y |
| B | Move without drawing |
| N | Return after drawing |
| A n | Angle (0-3, 90° increments) |
| TA n | Turn angle (degrees) |
| C n | Color |
| S n | Scale |
| X | Execute substring |
| P c,b | Paint (color, border) |

---

## 8. PLAY Command Codes (Коды команды PLAY)

| Code | Description |
|------|-------------|
| A-G | Notes A through G |
| # + | Sharp |
| - | Flat |
| . | Dotted note |
| O n | Octave (0-6) |
| > | Octave up |
| < | Octave down |
| L n | Length (1-64) |
| T n | Tempo (32-255) |
| N n | Note (0-84) |
| P n | Pause |
| MF | Music foreground |
| MB | Music background |
| MN | Normal (7/8 length) |
| ML | Legato |
| MS | Staccato |

---

## 9. Error Codes (Коды ошибок)

| Code | Message |
|------|---------|
| 1 | NEXT without FOR |
| 2 | Syntax error |
| 3 | RETURN without GOSUB |
| 4 | Out of DATA |
| 5 | Illegal function call |
| 6 | Overflow |
| 7 | Out of memory |
| 8 | Undefined line number |
| 9 | Subscript out of range |
| 10 | Duplicate definition |
| 11 | Division by zero |
| 12 | Illegal direct |
| 13 | Type mismatch |
| 14 | Out of string space |
| 15 | String too long |
| 16 | String formula too complex |
| 17 | Can't continue |
| 18 | Undefined user function |
| 19 | No RESUME |
| 20 | RESUME without error |
| 22 | Missing operand |
| 23 | Line buffer overflow |
| 24 | Device timeout |
| 25 | Device fault |
| 26 | FOR without NEXT |
| 27 | Out of paper |
| 29 | WHILE without WEND |
| 30 | WEND without WHILE |
| 50 | FIELD overflow |
| 51 | Internal error |
| 52 | Bad file number |
| 53 | File not found |
| 54 | Bad file mode |
| 55 | File already open |
| 57 | Device I/O error |
| 58 | File already exists |
| 61 | Disk full |
| 62 | Input past end |
| 63 | Bad record number |
| 64 | Bad file name |
| 66 | Direct statement in file |
| 67 | Too many files |
| 68 | Device unavailable |
| 69 | Communication buffer overflow |
| 70 | Permission denied |
| 71 | Disk not ready |
| 72 | Disk media error |
| 73 | Advanced feature unavailable |
| 74 | Rename across disks |
| 75 | Path/File access error |
| 76 | Path not found |

---

## Summary Statistics (Статистика)

- **Commands:** 14
- **Statements:** ~90
- **Functions:** ~60
- **Operators:** 17
- **SCREEN modes:** 10
- **Error codes:** 50+

---

*Reference based on Microsoft GW-BASIC 3.23 (1988)*
