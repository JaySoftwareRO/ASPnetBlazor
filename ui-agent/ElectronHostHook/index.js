"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.HookService = void 0;
const connector_1 = require("./connector");
class HookService extends connector_1.Connector {
    constructor(socket, app) {
        super(socket, app);
        this.app = app;
        // SSL/TSL: this is the self signed certificate support
        app.on('certificate-error', (event, webContents, url, error, certificate, callback) => {
            // On certificate error we disable default behaviour (stop loading the page)
            // and we then say "it is all fine - true" to the callback
            // TODO: only discard certificate for the local address, not for other URLs
            event.preventDefault();
            callback(true);
        });
    }
    onHostReady() {
    }
}
exports.HookService = HookService;
//# sourceMappingURL=index.js.map