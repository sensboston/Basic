# SharpBasic

A GW-BASIC / QBasic interpreter written in C#. Runs classic BASIC programs in the console or browser.

**[Try it online](https://senssoft.com/basic)** — no installation required!

![SharpBasic Screenshot](screenshots/ss0.jpg)

## Features

- **GW-BASIC Compatibility**: Supports most GW-BASIC commands and functions
- **QBasic Extensions**: SUB/FUNCTION procedures, SELECT CASE, DO/LOOP, CONST
- **Graphics**: Full support for SCREEN modes, LINE, CIRCLE, PSET, PAINT, etc.
- **Sound**: BEEP, SOUND, and PLAY commands
- **File I/O**: OPEN, CLOSE, INPUT#, PRINT#, LINE INPUT#
- **Cross-Platform Core**: Interpreter core runs on any .NET platform
- **Two Interfaces**:
  - Windows console with graphics overlay
  - WebAssembly browser version (Blazor)

## Quick Start

### Download

Pre-built binaries are available in the `bin/` folder:
- `basic.exe` — Windows console application (self-contained, no .NET required)
- `basic-web.zip` — Browser version (extract and open `index.html`)

### Run from Source

```bash
# Clone the repository
git clone https://github.com/user/Basic.git
cd Basic

# Run the interpreter
dotnet run --project Basic.Cli

# Run a specific program
dotnet run --project Basic.Cli -- samples/mandelbrot.bas

# Run with separate graphics window
dotnet run --project Basic.Cli -- --window samples/plasma.bas
```

## Usage

### Interactive Mode (REPL)

```
SharpBasic 1.0
60300 Bytes free
Ok
PRINT "Hello, World!"
Hello, World!
Ok
```

### Running Programs

```bash
# Run a program file
basic samples/cube3d.bas

# Run with graphics window mode
basic --window samples/fire.bas
basic -w samples/plasma.bas

# Console overlay mode (default)
basic --console samples/starfield.bas
basic -c samples/mandelbrot.bas
```

### Commands

| Command | Description |
|---------|-------------|
| `RUN` | Execute the current program |
| `LIST` | Display program listing |
| `LOAD "file.bas"` | Load a program from file |
| `SAVE "file.bas"` | Save program to file |
| `NEW` | Clear program from memory |
| `CLS` | Clear screen |
| `FILES` | List files in current directory |

## Sample Programs

The `samples/` directory contains various demonstration programs:

| Program | Description |
|---------|-------------|
| `mandelbrot.bas` | Mandelbrot fractal visualization |
| `plasma.bas` | Real-time plasma effect |
| `fire.bas` | Fire simulation |
| `cube3d.bas` | Rotating 3D wireframe cube |
| `starfield.bas` | 3D starfield animation |
| `fern.bas` | Barnsley fern fractal |
| `sierpinski.bas` | Sierpinski triangle |
| `gorilla.bas` | Classic QBasic game |
| `life.bas` | Conway's Game of Life |
| `tunnel.bas` | 3D tunnel effect |

## Supported Commands

### Program Flow
`GOTO`, `GOSUB`, `RETURN`, `IF...THEN...ELSE`, `FOR...NEXT`, `WHILE...WEND`, `DO...LOOP`, `SELECT CASE`, `END`, `STOP`

### Variables & Arrays
`LET`, `DIM`, `REDIM`, `CONST`, `SWAP`, `DEF FN`

### Input/Output
`PRINT`, `INPUT`, `LINE INPUT`, `INKEY$`, `LOCATE`, `CLS`, `COLOR`, `WIDTH`

### Graphics
`SCREEN`, `PSET`, `PRESET`, `LINE`, `CIRCLE`, `PAINT`, `DRAW`, `PALETTE`

### Sound
`BEEP`, `SOUND`, `PLAY`

### File Operations
`OPEN`, `CLOSE`, `INPUT#`, `PRINT#`, `LINE INPUT#`, `EOF`, `LOF`

### Functions
`ABS`, `SGN`, `INT`, `FIX`, `SQR`, `SIN`, `COS`, `TAN`, `ATN`, `LOG`, `EXP`, `RND`, `LEN`, `LEFT$`, `RIGHT$`, `MID$`, `CHR$`, `ASC`, `STR$`, `VAL`, `INSTR`, `STRING$`, `SPACE$`, `TIMER`, `DATE$`, `TIME$`

### QBasic Extensions
`SUB...END SUB`, `FUNCTION...END FUNCTION`, `EXIT FOR`, `EXIT DO`, `EXIT SUB`, `EXIT FUNCTION`, `ELSEIF`

## Building

### Requirements

- .NET 10 SDK
- Windows (for graphics features in console version)

### Build Commands

```bash
# Build all projects
dotnet build

# Run tests
dotnet test

# Publish Windows executable
dotnet publish Basic.Cli/Basic.Cli.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=none -o bin
mv bin/Basic.Cli.exe bin/basic.exe

# Publish Web version
dotnet publish Basic.Web/Basic.Web.csproj -c Release -o bin/web-temp
cd bin/web-temp/wwwroot && zip -r ../../basic-web.zip *
rm -rf bin/web-temp
```

## Project Structure

```
Basic/
├── src/
│   ├── Basic.Core/          # Interpreter core (platform-independent)
│   │   ├── Ast/             # Abstract Syntax Tree
│   │   ├── Lexer.cs         # Tokenizer
│   │   ├── Parser.cs        # Parser
│   │   └── Interpreter.cs   # Main interpreter
│   ├── Basic.Windows/       # Windows graphics (GDI, Console API)
│   ├── Basic.Cli/           # Console application
│   └── Basic.Web/           # Blazor WebAssembly version
├── tests/
│   └── Basic.Core.Tests/    # Unit tests
├── samples/                 # Sample BASIC programs
└── bin/                     # Release binaries
```

## Web Version

The web version runs entirely in the browser using Blazor WebAssembly. Features:

- Full interpreter running client-side
- Canvas-based graphics rendering
- Keyboard input support
- Sample programs included
- No server required after loading

To use: extract `basic-web.zip` and open `index.html` in a modern browser.

## License

MIT License

## Acknowledgments

- Microsoft for GW-BASIC and QBasic
- The retrocomputing community for preserving classic BASIC programs
