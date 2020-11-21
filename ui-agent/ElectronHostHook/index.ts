// @ts-ignore
import * as Electron from "electron";
import { Connector } from "./connector";

Electron.session.defaultSession.webRequest.onBeforeSendHeaders((details, callback) => {
    // EBay requires this agent, otherwise the login process becomes unpredictable
    details.requestHeaders['User-Agent'] = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/86.0.4240.198 Safari/537.36';

    // We can't login with google unless we set the browser's agent to Chrome
    var location = new URL(details.url);
    if (location.hostname.includes("google")) {
        details.requestHeaders['User-Agent'] = 'Chrome';
    }

    callback({ cancel: false, requestHeaders: details.requestHeaders });
});

export class HookService extends Connector {
    constructor(socket: SocketIO.Socket, public app: Electron.App) {
        super(socket, app);

        // SSL/TSL: this is the self signed certificate support
        app.on('certificate-error', (event, webContents, url, error, certificate, callback) => {
            // On certificate error we disable default behaviour (stop loading the page)
            // and we then say "it is all fine - true" to the callback

            // TODO: only discard certificate for the local address, not for other URLs
            event.preventDefault();
            callback(true);
        });
    }

    onHostReady(): void {
       
    }
}
