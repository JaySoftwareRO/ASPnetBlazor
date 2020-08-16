// @ts-ignore
import * as Electron from "electron";
import { Connector } from "./connector";

export class HookService extends Connector {
    constructor(socket: SocketIO.Socket, public app: Electron.App) {
        super(socket, app);

        // SSL/TSL: this is the self signed certificate support
        app.on('certificate-error', (event, webContents, url, error, certificate, callback) => {
            // On certificate error we disable default behaviour (stop loading the page)
            // and we then say "it is all fine - true" to the callback
            event.preventDefault();
            callback(true);
        });
    }

    onHostReady(): void {
       
    }
}
