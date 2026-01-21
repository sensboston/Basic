// SharpBasic Canvas - Unified text and graphics rendering
// All output (text and graphics) is drawn directly on canvas
// Text is rendered as graphics - no separate text buffer

window.sharpCanvas = {
    // Canvas
    canvas: null,
    ctx: null,

    // Dimensions in pixels
    width: 640,
    height: 400,

    // Dimensions in characters
    cols: 80,
    rows: 25,
    charWidth: 8,
    charHeight: 16,

    // Cursor position (in characters)
    cursorRow: 0,
    cursorCol: 0,
    cursorVisible: true,
    cursorBlinkState: true,
    cursorBlinkInterval: null,
    savedCursorArea: null,

    // Colors
    fgColor: 7,
    bgColor: 0,

    // Input state
    inputStartRow: 0,
    inputStartCol: 0,
    inputText: '',
    savedInputArea: null,

    // Scrollback buffer (array of ImageData for each scrolled line)
    scrollbackBuffer: [],
    scrollbackMaxLines: 500,
    scrollOffset: 0,
    savedScreenForScroll: null,  // Saved screen when viewing scrollback

    // Track if we're in graphics mode (to restore properly)
    inGraphicsMode: false,
    graphicsWidth: 0,
    graphicsHeight: 0,

    // Keyboard queue
    keyQueue: [],

    // Program execution state
    dotNetRef: null,
    running: false,
    frameId: null,

    // EGA 16-color palette
    palette: [
        '#000000', '#0000AA', '#00AA00', '#00AAAA',
        '#AA0000', '#AA00AA', '#AA5500', '#AAAAAA',
        '#555555', '#5555FF', '#55FF55', '#55FFFF',
        '#FF5555', '#FF55FF', '#FFFF55', '#FFFFFF'
    ],

    // ==================== Initialization ====================

    init: function(canvasId) {
        this.canvas = document.getElementById(canvasId);
        if (!this.canvas) {
            console.error('Canvas not found:', canvasId);
            return false;
        }

        this.ctx = this.canvas.getContext('2d', { willReadFrequently: true });
        this.ctx.imageSmoothingEnabled = false;

        // Measure character size
        this.charHeight = 16;
        this.ctx.font = this.charHeight + 'px monospace';
        this.charWidth = Math.ceil(this.ctx.measureText('M').width);

        // Size to container
        this.sizeToContainer();

        // Clear screen
        this.clear();

        // Setup handlers
        this.setupKeyboard();
        this.setupMouseWheel();
        this.setupResizeHandler();

        // Start cursor blink and show cursor immediately
        this.cursorVisible = true;
        this.cursorBlinkState = true;
        this.startCursorBlink();
        this.showCursor();

        return true;
    },

    sizeToContainer: function() {
        const container = this.canvas.parentElement;
        if (!container) return;

        const padding = 8;
        const availWidth = container.clientWidth - padding;
        const availHeight = container.clientHeight - padding;

        this.cols = Math.max(40, Math.floor(availWidth / this.charWidth));
        this.rows = Math.max(10, Math.floor(availHeight / this.charHeight));
        this.width = this.cols * this.charWidth;
        this.height = this.rows * this.charHeight;

        this.canvas.width = this.width;
        this.canvas.height = this.height;

        this.ctx.font = this.charHeight + 'px monospace';
        this.ctx.textBaseline = 'top';
        this.ctx.imageSmoothingEnabled = false;
    },

    // ==================== Basic Operations ====================

    clear: function(bgColor) {
        if (bgColor !== undefined) this.bgColor = bgColor & 15;
        this.ctx.fillStyle = this.palette[this.bgColor];
        this.ctx.fillRect(0, 0, this.width, this.height);
        this.cursorRow = 0;
        this.cursorCol = 0;
        this.scrollbackBuffer = [];
        this.scrollOffset = 0;
        this.savedInputArea = null;
        this.savedCursorArea = null;
    },

    locate: function(row, col) {
        this.cursorRow = Math.max(0, Math.min((row || 1) - 1, this.rows - 1));
        this.cursorCol = Math.max(0, Math.min((col || 1) - 1, this.cols - 1));
    },

    color: function(fg, bg) {
        if (fg !== undefined && fg !== null) this.fgColor = fg & 15;
        if (bg !== undefined && bg !== null) this.bgColor = bg & 15;
    },

    // ==================== Text Output ====================

    // Draw a single character at current cursor position and advance cursor
    printChar: function(ch) {
        if (this.cursorRow < 0 || this.cursorRow >= this.rows) return;
        if (this.cursorCol < 0 || this.cursorCol >= this.cols) return;

        const x = this.cursorCol * this.charWidth;
        const y = this.cursorRow * this.charHeight;

        // Draw background
        this.ctx.fillStyle = this.palette[this.bgColor];
        this.ctx.fillRect(x, y, this.charWidth, this.charHeight);

        // Draw character
        if (ch !== ' ') {
            this.ctx.font = this.charHeight + 'px monospace';
            this.ctx.textBaseline = 'top';
            this.ctx.fillStyle = this.palette[this.fgColor];
            this.ctx.fillText(ch, x, y);
        }

        // Advance cursor
        this.cursorCol++;
        if (this.cursorCol >= this.cols) {
            this.cursorCol = 0;
            this.cursorRow++;
            if (this.cursorRow >= this.rows) {
                this.scrollUp();
                this.cursorRow = this.rows - 1;
            }
        }
    },

    print: function(text, newline) {
        this.hideCursor();
        this.scrollOffset = 0;

        text = String(text || '');
        for (let i = 0; i < text.length; i++) {
            const ch = text[i];
            if (ch === '\n' || ch === '\r') {
                this.cursorCol = 0;
                this.cursorRow++;
                if (this.cursorRow >= this.rows) {
                    this.scrollUp();
                    this.cursorRow = this.rows - 1;
                }
            } else if (ch === '\t') {
                const spaces = 8 - (this.cursorCol % 8);
                for (let s = 0; s < spaces && this.cursorCol < this.cols; s++) {
                    this.printChar(' ');
                }
            } else {
                this.printChar(ch);
            }
        }

        if (newline) {
            this.cursorCol = 0;
            this.cursorRow++;
            if (this.cursorRow >= this.rows) {
                this.scrollUp();
                this.cursorRow = this.rows - 1;
            }
        }

        this.showCursor();
    },

    println: function(text) {
        this.print(text, true);
    },

    // ==================== Scrolling ====================

    scrollUp: function() {
        // Save top line to scrollback
        if (this.scrollbackBuffer.length < this.scrollbackMaxLines) {
            const topLine = this.ctx.getImageData(0, 0, this.width, this.charHeight);
            this.scrollbackBuffer.push(topLine);
        } else {
            // Rotate buffer
            this.scrollbackBuffer.shift();
            const topLine = this.ctx.getImageData(0, 0, this.width, this.charHeight);
            this.scrollbackBuffer.push(topLine);
        }

        // Shift pixels up
        const remaining = this.ctx.getImageData(0, this.charHeight, this.width, this.height - this.charHeight);
        this.ctx.putImageData(remaining, 0, 0);

        // Clear bottom line
        this.ctx.fillStyle = this.palette[this.bgColor];
        this.ctx.fillRect(0, this.height - this.charHeight, this.width, this.charHeight);

        // Adjust input start row
        if (this.inputStartRow > 0) {
            this.inputStartRow--;
        }

        // Invalidate saved areas
        this.savedInputArea = null;
        this.savedCursorArea = null;
    },

    // Show scrollback history (called by mouse wheel)
    showScrollback: function() {
        if (this.scrollbackBuffer.length === 0) {
            this.scrollOffset = 0;
            return;
        }

        // Save current screen if not already saved
        if (!this.savedScreenForScroll) {
            this.savedScreenForScroll = this.ctx.getImageData(0, 0, this.width, this.height);
        }

        // Clamp scroll offset
        this.scrollOffset = Math.min(this.scrollOffset, this.scrollbackBuffer.length);

        if (this.scrollOffset <= 0) {
            // Return to current view
            this.returnFromScrollback();
            return;
        }

        // Clear screen
        this.ctx.fillStyle = this.palette[this.bgColor];
        this.ctx.fillRect(0, 0, this.width, this.height);

        // Calculate which scrollback lines to show
        const linesToShow = Math.min(this.rows, this.scrollbackBuffer.length);
        const startIndex = this.scrollbackBuffer.length - this.scrollOffset;

        // Draw scrollback lines from buffer
        for (let i = 0; i < linesToShow && (startIndex + i) < this.scrollbackBuffer.length; i++) {
            const lineData = this.scrollbackBuffer[startIndex + i];
            if (lineData) {
                // Scale line if needed (scrollback might be from different resolution)
                const destY = i * this.charHeight;
                this.ctx.putImageData(lineData, 0, destY, 0, 0, Math.min(lineData.width, this.width), Math.min(lineData.height, this.charHeight));
            }
        }

        // Draw scroll indicator
        this.ctx.fillStyle = this.palette[14]; // Yellow
        this.ctx.font = '14px monospace';
        const indicator = '\u2191 ' + this.scrollOffset + '/' + this.scrollbackBuffer.length + ' lines \u2191 (scroll down to return)';
        // Draw with background for readability
        this.ctx.fillStyle = this.palette[1]; // Blue background
        this.ctx.fillRect(0, 0, this.width, 18);
        this.ctx.fillStyle = this.palette[15]; // White text
        this.ctx.fillText(indicator, 5, 2);
    },

    // Return from scrollback view to current screen
    returnFromScrollback: function() {
        this.scrollOffset = 0;
        if (this.savedScreenForScroll) {
            this.ctx.putImageData(this.savedScreenForScroll, 0, 0);
            this.savedScreenForScroll = null;
        }
        this.showCursor();
    },

    // ==================== Cursor ====================

    showCursor: function() {
        if (!this.cursorVisible || !this.cursorBlinkState) return;
        if (this.scrollOffset > 0) return;

        const x = this.cursorCol * this.charWidth;
        const y = this.cursorRow * this.charHeight + this.charHeight - 3;

        // Save area under cursor
        this.savedCursorArea = {
            x: x,
            y: y,
            data: this.ctx.getImageData(x, y, this.charWidth, 3)
        };

        // Draw cursor
        this.ctx.fillStyle = this.palette[this.fgColor];
        this.ctx.fillRect(x, y, this.charWidth, 3);
    },

    hideCursor: function() {
        if (this.savedCursorArea && this.savedCursorArea.data) {
            this.ctx.putImageData(this.savedCursorArea.data, this.savedCursorArea.x, this.savedCursorArea.y);
            this.savedCursorArea = null;
        }
    },

    startCursorBlink: function() {
        if (this.cursorBlinkInterval) {
            clearInterval(this.cursorBlinkInterval);
        }
        this.cursorVisible = true;
        this.cursorBlinkState = true;
        this.showCursor(); // Show cursor immediately

        const self = this;
        this.cursorBlinkInterval = setInterval(function() {
            if (self.running) return; // Don't blink while program running
            if (self.scrollOffset > 0) return; // Don't blink when viewing scrollback

            self.cursorBlinkState = !self.cursorBlinkState;
            if (self.cursorBlinkState) {
                self.showCursor();
            } else {
                self.hideCursor();
            }
        }, 500);
    },

    stopCursorBlink: function() {
        if (this.cursorBlinkInterval) {
            clearInterval(this.cursorBlinkInterval);
            this.cursorBlinkInterval = null;
        }
    },

    // ==================== User Input ====================

    startInput: function() {
        this.inputStartRow = this.cursorRow;
        this.inputStartCol = this.cursorCol;
        this.inputText = '';
        this.scrollOffset = 0;

        // Save area from cursor to end of line
        const x = this.inputStartCol * this.charWidth;
        const y = this.inputStartRow * this.charHeight;
        const w = this.width - x;
        const h = this.charHeight;

        if (w > 0 && h > 0) {
            this.savedInputArea = {
                x: x,
                y: y,
                width: w,
                height: h,
                data: this.ctx.getImageData(x, y, w, h)
            };
        }

        this.showCursor();
    },

    showInput: function(text) {
        this.hideCursor();
        this.inputText = text || '';

        // Restore saved area
        if (this.savedInputArea && this.savedInputArea.data) {
            this.ctx.putImageData(this.savedInputArea.data, this.savedInputArea.x, this.savedInputArea.y);
        }

        // Draw input text
        this.cursorRow = this.inputStartRow;
        this.cursorCol = this.inputStartCol;

        for (let i = 0; i < this.inputText.length; i++) {
            if (this.cursorCol >= this.cols) break;
            this.printChar(this.inputText[i]);
        }

        this.showCursor();
    },

    submitInput: function() {
        this.hideCursor();
        this.savedInputArea = null;
        this.cursorCol = 0;
        this.cursorRow++;
        if (this.cursorRow >= this.rows) {
            this.scrollUp();
            this.cursorRow = this.rows - 1;
        }
        this.showCursor();
    },

    // ==================== Graphics Primitives ====================

    setGraphicsMode: function(width, height) {
        this.hideCursor();

        // Track graphics mode for restoration
        this.inGraphicsMode = true;
        this.graphicsWidth = width;
        this.graphicsHeight = height;

        this.width = width;
        this.height = height;
        this.canvas.width = width;
        this.canvas.height = height;

        // Adjust char height for resolution
        if (height <= 200) this.charHeight = 8;
        else if (height <= 350) this.charHeight = 14;
        else this.charHeight = 16;

        this.ctx.font = this.charHeight + 'px monospace';
        this.ctx.textBaseline = 'top';
        this.ctx.imageSmoothingEnabled = false;
        this.charWidth = Math.ceil(this.ctx.measureText('M').width);

        this.cols = Math.floor(width / this.charWidth);
        this.rows = Math.floor(height / this.charHeight);

        // Clear with background color
        this.ctx.fillStyle = this.palette[this.bgColor];
        this.ctx.fillRect(0, 0, this.width, this.height);

        this.cursorRow = 0;
        this.cursorCol = 0;
        this.scrollbackBuffer = [];
        this.scrollOffset = 0;
        this.savedInputArea = null;
        this.savedCursorArea = null;
    },

    setTextMode: function(cols, rows) {
        // Return to text mode - resize to container
        this.sizeToContainer();
        this.clear();
    },

    pixel: function(x, y, color) {
        if (x < 0 || x >= this.width || y < 0 || y >= this.height) return;
        this.ctx.fillStyle = this.getColorHex(color);
        this.ctx.fillRect(x, y, 1, 1);
    },

    line: function(x1, y1, x2, y2, color) {
        this.ctx.strokeStyle = this.getColorHex(color);
        this.ctx.lineWidth = 1;
        this.ctx.beginPath();
        this.ctx.moveTo(x1 + 0.5, y1 + 0.5);
        this.ctx.lineTo(x2 + 0.5, y2 + 0.5);
        this.ctx.stroke();
    },

    box: function(x1, y1, x2, y2, color, filled) {
        const x = Math.min(x1, x2);
        const y = Math.min(y1, y2);
        const w = Math.abs(x2 - x1);
        const h = Math.abs(y2 - y1);

        if (filled) {
            this.ctx.fillStyle = this.getColorHex(color);
            this.ctx.fillRect(x, y, w, h);
        } else {
            this.ctx.strokeStyle = this.getColorHex(color);
            this.ctx.lineWidth = 1;
            this.ctx.strokeRect(x + 0.5, y + 0.5, w, h);
        }
    },

    circle: function(cx, cy, radius, color, startAngle, endAngle, aspect) {
        this.ctx.strokeStyle = this.getColorHex(color);
        this.ctx.lineWidth = 1;
        this.ctx.beginPath();
        if (aspect === 1.0 || aspect === undefined) {
            this.ctx.arc(cx, cy, radius, startAngle || 0, endAngle || Math.PI * 2);
        } else {
            this.ctx.ellipse(cx, cy, radius, radius * aspect, 0, startAngle || 0, endAngle || Math.PI * 2);
        }
        this.ctx.stroke();
    },

    paint: function(x, y, fillColor, borderColor) {
        const imageData = this.ctx.getImageData(0, 0, this.width, this.height);
        const data = imageData.data;
        const fillRgb = this.hexToRgb(this.getColorHex(fillColor));
        const borderRgb = this.hexToRgb(this.getColorHex(borderColor));

        const startIdx = (y * this.width + x) * 4;
        const targetR = data[startIdx];
        const targetG = data[startIdx + 1];
        const targetB = data[startIdx + 2];

        if (targetR === fillRgb.r && targetG === fillRgb.g && targetB === fillRgb.b) return;

        const stack = [[x, y]];
        const visited = new Set();

        while (stack.length > 0) {
            const [px, py] = stack.pop();
            if (px < 0 || px >= this.width || py < 0 || py >= this.height) continue;

            const key = py * this.width + px;
            if (visited.has(key)) continue;
            visited.add(key);

            const idx = key * 4;
            const r = data[idx], g = data[idx + 1], b = data[idx + 2];

            if (r === borderRgb.r && g === borderRgb.g && b === borderRgb.b) continue;
            if (r !== targetR || g !== targetG || b !== targetB) continue;

            data[idx] = fillRgb.r;
            data[idx + 1] = fillRgb.g;
            data[idx + 2] = fillRgb.b;
            data[idx + 3] = 255;

            stack.push([px + 1, py], [px - 1, py], [px, py + 1], [px, py - 1]);
        }

        this.ctx.putImageData(imageData, 0, 0);
    },

    getColorHex: function(color) {
        if (color > 255) {
            // RGB color
            const r = (color >> 16) & 0xFF;
            const g = (color >> 8) & 0xFF;
            const b = color & 0xFF;
            return '#' + r.toString(16).padStart(2, '0') +
                         g.toString(16).padStart(2, '0') +
                         b.toString(16).padStart(2, '0');
        }
        return this.palette[color & 15];
    },

    hexToRgb: function(hex) {
        const result = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(hex);
        return result ? {
            r: parseInt(result[1], 16),
            g: parseInt(result[2], 16),
            b: parseInt(result[3], 16)
        } : { r: 0, g: 0, b: 0 };
    },

    // ==================== Keyboard ====================

    setupKeyboard: function() {
        const self = this;
        document.addEventListener('keydown', function(e) {
            let key = '';
            switch(e.key) {
                case 'Enter': key = '\r'; break;
                case 'Backspace': key = '\b'; e.preventDefault(); break;
                case 'Tab': key = '\t'; e.preventDefault(); break;
                case 'Escape': key = '\x1B'; break;
                case 'ArrowUp': key = '\0H'; e.preventDefault(); break;
                case 'ArrowDown': key = '\0P'; e.preventDefault(); break;
                case 'ArrowLeft': key = '\0K'; e.preventDefault(); break;
                case 'ArrowRight': key = '\0M'; e.preventDefault(); break;
                case 'Home': key = '\0G'; e.preventDefault(); break;
                case 'End': key = '\0O'; e.preventDefault(); break;
                case 'PageUp': key = '\0I'; e.preventDefault(); break;
                case 'PageDown': key = '\0Q'; e.preventDefault(); break;
                case 'Insert': key = '\0R'; e.preventDefault(); break;
                case 'Delete': key = '\0S'; e.preventDefault(); break;
                case 'F1': case 'F2': case 'F3': case 'F4': case 'F5':
                case 'F6': case 'F7': case 'F8': case 'F9': case 'F10':
                    key = '\0' + String.fromCharCode(';'.charCodeAt(0) + parseInt(e.key.slice(1)) - 1);
                    e.preventDefault();
                    break;
                default:
                    if (e.key.length === 1 && !e.ctrlKey && !e.altKey) {
                        key = e.key;
                    }
            }

            if (key) self.keyQueue.push(key);

            // Ctrl+C / Ctrl+Break
            if (e.ctrlKey && (e.key === 'c' || e.key === 'Pause' || e.key === 'Break')) {
                e.preventDefault();
                if (self.dotNetRef) {
                    self.dotNetRef.invokeMethodAsync('OnCtrlBreak');
                }
            }
        });
    },

    setupMouseWheel: function() {
        const self = this;
        this.canvas.addEventListener('wheel', function(e) {
            if (self.running) return;
            e.preventDefault();

            const maxScroll = self.scrollbackBuffer.length;
            const oldOffset = self.scrollOffset;

            if (e.deltaY < 0) {
                // Scroll up (view history)
                self.scrollOffset = Math.min(self.scrollOffset + 3, maxScroll);
            } else {
                // Scroll down (back to current)
                self.scrollOffset = Math.max(self.scrollOffset - 3, 0);
            }

            if (self.scrollOffset !== oldOffset) {
                if (self.scrollOffset > 0) {
                    self.hideCursor();
                    self.showScrollback();
                } else {
                    self.returnFromScrollback();
                }
            }
        }, { passive: false });
    },

    setupResizeHandler: function() {
        const self = this;
        let resizeTimeout;
        window.addEventListener('resize', function() {
            clearTimeout(resizeTimeout);
            resizeTimeout = setTimeout(function() {
                if (!self.running) {
                    // Save current screen
                    const oldImage = self.ctx.getImageData(0, 0, self.width, self.height);
                    const oldWidth = self.width;
                    const oldHeight = self.height;

                    // Resize
                    self.sizeToContainer();

                    // Restore what fits
                    const restoreWidth = Math.min(oldWidth, self.width);
                    const restoreHeight = Math.min(oldHeight, self.height);

                    // Clear first
                    self.ctx.fillStyle = self.palette[self.bgColor];
                    self.ctx.fillRect(0, 0, self.width, self.height);

                    // Put back old content
                    self.ctx.putImageData(oldImage, 0, 0, 0, 0, restoreWidth, restoreHeight);

                    // Adjust cursor
                    self.cursorRow = Math.min(self.cursorRow, self.rows - 1);
                    self.cursorCol = Math.min(self.cursorCol, self.cols - 1);
                }
            }, 100);
        });
    },

    readKey: function() {
        return this.keyQueue.length > 0 ? this.keyQueue.shift() : '';
    },

    keyAvailable: function() {
        return this.keyQueue.length > 0;
    },

    clearKeys: function() {
        this.keyQueue = [];
    },

    // ==================== Sound ====================

    beep: function(frequency, durationMs) {
        try {
            const audioCtx = new (window.AudioContext || window.webkitAudioContext)();
            const osc = audioCtx.createOscillator();
            const gain = audioCtx.createGain();
            osc.connect(gain);
            gain.connect(audioCtx.destination);
            osc.frequency.value = frequency || 800;
            osc.type = 'square';
            gain.gain.value = 0.1;
            osc.start();
            setTimeout(function() { osc.stop(); audioCtx.close(); }, durationMs || 200);
        } catch (e) {}
    },

    // ==================== Program Execution ====================

    startGameLoop: function(dotNetRef, statementsPerFrame) {
        this.dotNetRef = dotNetRef;
        this.running = true;
        this.hideCursor();
        this.cursorVisible = false;

        const self = this;
        const statements = statementsPerFrame || 1000;

        function gameLoop() {
            if (!self.running) return;

            self.dotNetRef.invokeMethodAsync('ExecuteChunk', statements)
                .then(function(continueRunning) {
                    if (continueRunning && self.running) {
                        self.frameId = requestAnimationFrame(gameLoop);
                    } else {
                        self.endProgram();
                        self.dotNetRef.invokeMethodAsync('OnProgramEnded');
                    }
                })
                .catch(function(error) {
                    console.error('Execution error:', error);
                    self.endProgram();
                    self.dotNetRef.invokeMethodAsync('OnProgramError', error.message || 'Unknown error');
                });
        }

        this.frameId = requestAnimationFrame(gameLoop);
    },

    stopGameLoop: function() {
        this.running = false;
        if (this.frameId) {
            cancelAnimationFrame(this.frameId);
            this.frameId = null;
        }
        this.endProgram();
    },

    endProgram: function() {
        this.running = false;
        this.cursorVisible = true;

        // Reset colors to default (light gray on black)
        this.fgColor = 7;
        this.bgColor = 0;

        // If we were in graphics mode, restore canvas to container size
        // while preserving the graphics in the top-left corner
        if (this.inGraphicsMode) {
            this.restoreToContainerSize();
            this.inGraphicsMode = false;
        }

        // Move cursor to next line if not at start
        if (this.cursorCol > 0) {
            this.cursorCol = 0;
            this.cursorRow++;
            if (this.cursorRow >= this.rows) {
                this.scrollUp();
                this.cursorRow = this.rows - 1;
            }
        }

        this.showCursor();
        this.startCursorBlink();
    },

    // Restore canvas to container size, preserving current graphics
    restoreToContainerSize: function() {
        const container = this.canvas.parentElement;
        if (!container) return;

        const padding = 8;
        const availWidth = container.clientWidth - padding;
        const availHeight = container.clientHeight - padding;

        // Save current graphics
        const oldImage = this.ctx.getImageData(0, 0, this.width, this.height);
        const oldWidth = this.width;
        const oldHeight = this.height;

        // Calculate new size
        this.charHeight = 16;
        const tempCanvas = document.createElement('canvas');
        const tempCtx = tempCanvas.getContext('2d');
        tempCtx.font = this.charHeight + 'px monospace';
        this.charWidth = Math.ceil(tempCtx.measureText('M').width);

        this.cols = Math.max(40, Math.floor(availWidth / this.charWidth));
        this.rows = Math.max(10, Math.floor(availHeight / this.charHeight));
        this.width = this.cols * this.charWidth;
        this.height = this.rows * this.charHeight;

        // Resize canvas
        this.canvas.width = this.width;
        this.canvas.height = this.height;

        this.ctx.font = this.charHeight + 'px monospace';
        this.ctx.textBaseline = 'top';
        this.ctx.imageSmoothingEnabled = false;

        // Clear with background
        this.ctx.fillStyle = this.palette[this.bgColor];
        this.ctx.fillRect(0, 0, this.width, this.height);

        // Restore old graphics in top-left corner
        this.ctx.putImageData(oldImage, 0, 0);

        // Position cursor below the graphics
        this.cursorRow = Math.min(Math.floor(oldHeight / this.charHeight), this.rows - 1);
        this.cursorCol = 0;

        this.scrollbackBuffer = [];
        this.scrollOffset = 0;
        this.savedInputArea = null;
        this.savedCursorArea = null;
    }
};
