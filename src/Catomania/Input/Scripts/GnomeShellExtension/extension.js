// Catomania: shares the cursor position with the application. Wayland does not let applications
// read the global cursor position, GNOME Shell is the only one that knows it.
import Gio from 'gi://Gio';
import {Extension} from 'resource:///org/gnome/shell/extensions/extension.js';

const BUS_NAME = 'org.catomania.CursorTracker';
const OBJECT_PATH = '/org/catomania/CursorTracker';
const INTERFACE_XML = `
<node>
  <interface name="org.catomania.CursorTracker">
    <method name="GetPosition">
      <arg type="i" direction="out" name="x"/>
      <arg type="i" direction="out" name="y"/>
      <arg type="i" direction="out" name="width"/>
      <arg type="i" direction="out" name="height"/>
    </method>
  </interface>
</node>`;

export default class CatomaniaCursorExtension extends Extension {
    enable() {
        this._exportedObject = Gio.DBusExportedObject.wrapJSObject(INTERFACE_XML, this);
        this._exportedObject.export(Gio.DBus.session, OBJECT_PATH);
        this._nameId = Gio.bus_own_name_on_connection(
            Gio.DBus.session, BUS_NAME, Gio.BusNameOwnerFlags.NONE, null, null);
    }

    disable() {
        Gio.bus_unown_name(this._nameId);
        this._nameId = 0;
        this._exportedObject.unexport();
        this._exportedObject = null;
    }

    // Logical stage coordinates together with the logical stage size.
    GetPosition() {
        const [x, y] = global.get_pointer();
        const [width, height] = global.display.get_size();
        return [x, y, width, height];
    }
}
