// Catomania: sends the cursor position to the application. Wayland does not let applications
// read the global cursor position, KWin is the only one that knows it.
// The position is in logical coordinates and sent together with the logical virtual screen geometry.
// %SERVICE% is replaced with the D-Bus name of the running Catomania instance.
var service = "%SERVICE%";

function sendPosition() {
    var position = workspace.cursorPos;
    var geometry = workspace.virtualScreenGeometry;
    callDBus(
        service,
        "/org/catomania/CursorTracker",
        "org.catomania.CursorTracker",
        "SetPosition",
        position.x,
        position.y,
        geometry.x,
        geometry.y,
        geometry.width,
        geometry.height);
}

workspace.cursorPosChanged.connect(sendPosition);
sendPosition();
